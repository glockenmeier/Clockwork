using SiliconStudio.Core.Mathematics;
using System;

namespace Clockwork.Serialization
{
    public class RegularGridContentObserver
    {
        public Vector2 Position { get; set; }

        public float LoadingRange { get; set; }

        public float UnloadingRange { get; set; }

        public RegularGridContentObserver()
        {
            LoadingRange = 100;
            UnloadingRange = 200;
        }

        public bool ShouldUnload(ref RectangleF bounds)
        {
            var unloadRectangle = bounds;
            unloadRectangle.Inflate(UnloadingRange, UnloadingRange);
            return !unloadRectangle.Contains(Position);
        }

        public bool ShouldLoad(ref RectangleF bounds)
        {
            var loadRectangle = bounds;
            loadRectangle.Inflate(LoadingRange, LoadingRange);
            return loadRectangle.Contains(Position);
        }

        public bool IsSafe(ref RectangleF bounds)
        {
            throw new NotImplementedException();
        }
    }
}
