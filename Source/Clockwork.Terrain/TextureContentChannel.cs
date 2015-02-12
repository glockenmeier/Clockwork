using Clockwork.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Threading.Tasks;

namespace Clockwork.Terrain
{
    public class TextureContentChannel : TiledContentChannel<QuadTreeNode<TerrainTileData>>
    {
        private const int ArraySizeIncrement = 32;

        private ITileChannelStore store;
        private byte[] nullData;

        public Texture Texture { get; private set; }

        public event Action<Texture> TextureChanged;

        public TextureContentChannel(ITileChannelStore store, GraphicsDevice device, TextureContentChannelData data)
        {
            this.store = store;

            TextureDescription description = data.Description;
            description.ArraySize = ArraySizeIncrement;
            description.Usage = GraphicsResourceUsage.Default;
            description.Flags = TextureFlags.ShaderResource;

            Texture = Texture.New(device, description);
            nullData = new byte[Texture.CalculateWidth<byte>() * Texture.Height];
        }

        public override Task<IDisposable> Prepare(IAssetManager assetManager)
        {
            return store.OpenAsync(assetManager);
        }

        public override async Task LoadTileAsync(QuadTreeNode<TerrainTileData> node, int physicalOffset)
        {
            int tileIndex = QuadTreeHelper.GetNodeIndex(node.Depth, node.Position);

            bool isOccupied = await store.Visit(tileIndex, async (data) =>
            {
                EnsureArraySize(physicalOffset);
                Texture.SetData(data, physicalOffset);
            });

            // TODO: Skip this and just don't use this channel in the tile material
            if (!isOccupied)
            {
                EnsureArraySize(physicalOffset);
                Texture.SetData(nullData, physicalOffset);
            }
        }

        private void EnsureArraySize(int physicalOffset)
        {
            if (Texture.Description.ArraySize <= physicalOffset)
            {
                var device = Texture.GraphicsDevice;
                var description = Texture.Description;
                description.ArraySize += ArraySizeIncrement;

                var oldTexture = Texture;
                Texture = Texture.New(device, description);

                for (int i = 0; i < oldTexture.Description.ArraySize; i++)
                    device.CopyRegion(oldTexture, i, null, Texture, i);

                var textureChanged = TextureChanged;
                if (textureChanged != null)
                    textureChanged(Texture);

                oldTexture.Dispose();
            }
        }

        protected override void Destroy()
        {
            Texture.Dispose();
        }
    }
}
