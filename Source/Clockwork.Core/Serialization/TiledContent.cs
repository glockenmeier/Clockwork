using SiliconStudio.Core;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Clockwork.Serialization
{
    /// <summary>
    /// Base class for assets, made up of streamable tiles.
    /// </summary>
    /// <typeparam name="T">The tile index type.</typeparam>
    public abstract class TiledContent<T> : ComponentBase
    {
        private readonly List<TiledContentChannel<T>> channels = new List<TiledContentChannel<T>>();
        private readonly Queue<T> requestedTiles = new Queue<T>();
        private readonly IntegerPool physicalOffsetPool;
        private bool isLoading;

        /// <summary>
        /// The asset manager used for streaming.
        /// </summary>
        public IAssetManager Asset { get; private set; }

        /// <summary>
        /// The script system used to schedule streaming jobs.
        /// </summary>
        public ScriptSystem Script { get; private set; }

        /// <summary>
        /// A collection of data channels that each tile contains.
        /// </summary>
        protected IList<TiledContentChannel<T>> Channels
        {
            get { return channels; }
        }

        /// <summary>
        /// Creates a new <see cref="TiledContent"/> instance.
        /// </summary>
        /// <param name="serviceRegistry">The game services.</param>
        /// <param name="maximumTileCount">The maximum number of tiles that can be mapped at the same time.</param>
        public TiledContent(IServiceRegistry serviceRegistry, int maximumTileCount)
        {
            Asset = serviceRegistry.GetSafeServiceAs<IAssetManager>();
            Script = serviceRegistry.GetSafeServiceAs<ScriptSystem>();

            physicalOffsetPool = new IntegerPool(maximumTileCount);
        }

        /// <summary>
        /// Get the tile with the given index.
        /// </summary>
        /// <param name="item">The tile index.</param>
        /// <returns>A tile.</returns>
        protected abstract ContentTile GetTile(T item);

        /// <summary>
        /// Disposes of resources when a tile is unloaded.
        /// </summary>
        /// <param name="key">The tile index.</param>
        protected virtual void OnUnload(T key) { }

        /// <summary>
        /// Reevaluates which tiles need to be loaded or unloaded.
        /// </summary>
        public void Observe()
        {
            ObserveOverride();

            if (requestedTiles.Count > 0 && !isLoading)
            {
                isLoading = true;
                Script.Add(LoadTiles);
            }
        }

        /// <summary>
        /// Opens the tile stores.
        /// </summary>
        /// <returns>A task that can be awaited for a disposable connection to the tile store.</returns>
        protected virtual async Task<IDisposable> OpenAsync()
        {
            var disposables = await Task.WhenAll(channels
                .Select(channel => channel.Prepare(Asset))
                .ToArray());

            return new AnonymousDisposable(() =>
            {
                foreach (var disposable in disposables)
                    disposable.Dispose();
            });
        }

        /// <summary>
        /// Loads a single tile.
        /// </summary>
        /// <param name="key">The tile to load.</param>
        /// <param name="physicalOffset">The physical resource to map the tile to.</param>
        /// <returns>A task that succedes when the tile is loaded.</returns>
        protected virtual async Task LoadTileAsync(T key, int physicalOffset)
        {
            await Task.WhenAll(channels
                .Select(channel => channel.LoadTileAsync(key, physicalOffset))
                .ToArray());
        }

        /// <summary>
        /// Reevaluates which tiles are needed and calls <see cref="Load"/> and <see cref="Unload"/> to update them.
        /// </summary>
        protected virtual void ObserveOverride()
        {
        }

        /// <summary>
        /// Called by derived classes to request unloading of tiles.
        /// </summary>
        /// <param name="key">The tile index.</param>
        protected void Load(T key)
        {
            var tile = GetTile(key);

            if (tile.State != TileState.None)
                return;

            tile.State = TileState.Observed;

            requestedTiles.Enqueue(key);
        }

        /// <summary>
        /// Called by derived classes to request loading of tiles.
        /// </summary>
        /// <param name="key">The tile index.</param>
        protected void Unload(T key)
        {
            var tile = GetTile(key);

            if (tile.State == TileState.None)
                return;

            if (tile.State == TileState.Mapped)
            {
                OnUnload(key);
                physicalOffsetPool.Release(tile.PhysicalOffset);
                tile.PhysicalOffset = 0;
            }

            tile.State = TileState.None;
        }

        private async Task LoadTiles()
        {
            using (var session = await OpenAsync())
            {
                while (requestedTiles.Count > 0)
                {
                    var key = requestedTiles.Dequeue();
                    var tile = GetTile(key);

                    // Did we change our mind in the meantime?
                    if (tile.State == TileState.None)
                        continue;

                    // Out of space, try next time
                    if (physicalOffsetPool.Count <= 0)
                    {
                        //tile.State = TileState.None;
                        requestedTiles.Enqueue(key);
                        continue;
                    }

                    int physicalOffset = physicalOffsetPool.Acquire();

                    await LoadTileAsync(key, physicalOffset);

                    // Unloading was requested while we were busy
                    if (tile.State == TileState.None)
                    {
                        OnUnload(key);
                        physicalOffsetPool.Release(physicalOffset);
                    }
                    else
                    {
                        tile.PhysicalOffset = physicalOffset;
                        tile.State = TileState.Mapped;
                    }
                }
            }

            isLoading = false;
        }

        protected override void Destroy()
        {
            // TODO: stop streaming

            foreach (var channel in channels)
                channel.Dispose();

            base.Destroy();
        }
    }
}
