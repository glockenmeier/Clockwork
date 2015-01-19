using Clockwork.Threading;
using SiliconStudio.Core;
using SiliconStudio.Core.LZ4;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Graphics;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Clockwork.Serialization
{
    public interface ITileChannelStore
    {
        Task<IDisposable> OpenAsync(IAssetManager assetManager);

        Task<bool> Visit(int tileIndex, Func<byte[], Task> action);
    }

    public interface ITileStore
    {
        ITileChannelStore GetChannel(int channelIndex);
    }

    public class InterleavedTileStore : ITileStore
    {
        private string url;

        private int referenceCount;
        private Stream stream;
        private BinarySerializationReader reader;
        private byte[] buffer;
        private AsyncLock asyncLock = new AsyncLock();

        private DataRange[,] tileMap;
        private int channelCount;

        public InterleavedTileStore(string url)
        {
            this.url = url;
        }

        public ITileChannelStore GetChannel(int channelIndex)
        {
            return new ChannelStore(this, channelIndex);
        }

        private async Task<IDisposable> OpenAsync(IAssetManager assetManager)
        {
            if (referenceCount == 0)
            {
                stream = assetManager.OpenAsStream(url);

                // We need a seekable stream but don't want to buffer the whole file. Asset-level compression is not allowed.
                if (!stream.CanSeek)
                    throw new InvalidOperationException("Tile stream must be seekable by default");

                reader = new BinarySerializationReader(stream);

                if (tileMap == null)
                {
                    var tileCount = reader.ReadInt32();
                    var channelCount = reader.ReadInt32();
                    tileMap = new DataRange[tileCount, channelCount];

                    for (int i = 0; i < tileCount; i++)
                        for (int j = 0; j < channelCount; j++)
                            tileMap[i, j] = new DataRange(reader.ReadInt32(), reader.ReadInt32());
                }
            }

            referenceCount++;

            return new AnonymousDisposable(() =>
            {
                referenceCount--;

                if (referenceCount == 0)
                {
                    stream.Dispose();
                    stream = null;
                    reader = null;
                }
            });
        }

        private async Task<bool> Visit(int tileIndex, int channelIndex, Func<byte[], Task> action)
        {
            if (reader == null)
                throw new InvalidOperationException();

            var dataRange = tileMap[tileIndex, channelIndex];
            if (dataRange.Length > 0)
            {
                using (await asyncLock.LockAsync())
                {
                    stream.Seek(dataRange.Start, SeekOrigin.Begin);

                    if (buffer == null || buffer.Length != dataRange.Length)
                        buffer = new byte[dataRange.Length];

                    using (var compressedStream = new LZ4Stream(stream, CompressionMode.Decompress, false, dataRange.Length))
                    {
                        await compressedStream.ReadAsync(buffer, 0, dataRange.Length);
                        //await reader.NativeStream.ReadAsync(buffer, 0, dataRange.Length);
                    }

                    // TODO: separate lock?
                    await action(buffer);

                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private class ChannelStore : ITileChannelStore
        {
            private InterleavedTileStore parent;
            private int channelIndex;

            public ChannelStore(InterleavedTileStore parent, int channelIndex)
            {
                this.parent = parent;
                this.channelIndex = channelIndex;
            }

            public Task<IDisposable> OpenAsync(IAssetManager assetManager)
            {
                return parent.OpenAsync(assetManager);
            }

            public Task<bool> Visit(int tileIndex, Func<byte[], Task> action)
            {
                return parent.Visit(tileIndex, channelIndex, action);
            }
        }
    }

    public abstract class TiledContentChannel<T> : ComponentBase
    {
        public abstract Task<IDisposable> Prepare(IAssetManager assetManager);

        public abstract Task LoadTileAsync(T key, int physicalOffset);
    }

    public struct TextureContentChannelData
    {
        public string Url;

        public ImageDescription Description;
    }
}
