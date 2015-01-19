using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using System;
using System.Collections.Generic;

namespace Clockwork.Serialization
{
    public class RegularGridContent<T> : TiledContent<Int2>
        where T : ContentTile, new()
    {
        private readonly Dictionary<Int2, T> tileMap = new Dictionary<Int2, T>();
        private readonly List<RegularGridContentObserver> observers = new List<RegularGridContentObserver>();

        public RegularGridContentData Data { get; private set; }

        public IReadOnlyDictionary<Int2, T> Tiles
        {
            get { return tileMap; }
        }

        public ICollection<RegularGridContentObserver> Observers
        {
            get { return observers; }
        }

        public RegularGridContent(IServiceRegistry serviceRegistry, RegularGridContentData data)
            : base(serviceRegistry, 1000)
        {
            Data = data;
        }

        protected override ContentTile GetTile(Int2 key)
        {
            T page;
            if (!tileMap.TryGetValue(key, out page))
            {
                page = new T();
                tileMap.Add(key, page);
            }

            return page;
        }

        protected override void OnUnload(Int2 key)
        {
            tileMap.Remove(key);
        }

        protected override void ObserveOverride()
        {
            foreach (var key in tileMap.Keys)
            {
                foreach (var observer in observers)
                {
                    var bounds = new RectangleF(
                        (key.X * Data.CellSize.X - Data.Origin.X),
                        (key.Y * Data.CellSize.Y - Data.Origin.Y),
                        Data.CellSize.X, Data.CellSize.Y);

                    if (observer.ShouldUnload(ref bounds))
                    {
                        Unload(key);
                    }
                }
            }

            foreach (var observer in observers)
            {
                var loadingBounds = new RectangleF(
                    ((observer.Position.X - observer.LoadingRange) - Data.Origin.X) / Data.CellSize.X,
                    ((observer.Position.Y - observer.LoadingRange) - Data.Origin.Y) / Data.CellSize.Y,
                    observer.LoadingRange * 2 / Data.CellSize.X,
                    observer.LoadingRange * 2 / Data.CellSize.Y);

                int left = (int)Math.Max(Math.Floor(loadingBounds.Left), Data.Bounds.Left);
                int right = (int)Math.Min(Math.Ceiling(loadingBounds.Right), Data.Bounds.Right);
                int top = (int)Math.Max(Math.Floor(loadingBounds.Top), Data.Bounds.Top);
                int bottom = (int)Math.Min(Math.Ceiling(loadingBounds.Bottom), Data.Bounds.Bottom);

                for (int y = top; y < bottom; y++)
                {
                    for (int x = left; x < right; x++)
                    {
                        Load(new Int2(x, y));
                    }
                }
            }
        }
    }
}
