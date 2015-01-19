using SiliconStudio.Core.Mathematics;
using System;

namespace Clockwork.Serialization
{
    public class QuadTreeContentObserver
    {
        public Vector3 Position { get; set; }

        public float LoadingRange { get; set; }

        public float UnloadingRange { get; set; }

        public float SafeRange { get; set; }

        private IVisibleRanges ranges;

        public QuadTreeContentObserver(IVisibleRanges ranges)
        {
            this.ranges = ranges;

            LoadingRange = 20;
            UnloadingRange = 30;
            SafeRange = 0;
        }

        public float GetPriority(ref BoundingBox bounds)
        {
            return -Vector3.DistanceSquared(bounds.Center, Position);
        }

        public bool ShouldLoad(ref BoundingBox bounds, int level)
        {
            var sphere = new BoundingSphere(Position, ranges[level] + LoadingRange);
            return sphere.Intersects(ref bounds);
        }

        public bool ShouldUnload(ref BoundingBox bounds, int level)
        {
            var sphere = new BoundingSphere(Position, ranges[level] + UnloadingRange);
            return !sphere.Intersects(ref bounds);
        }

        public bool IsSafe(ref BoundingBox bounds, int level)
        {
            if (level == 0)
            {
                return false;
            }
            else
            {
                // TODO: Use actual morph start distance
                var sphere = new BoundingSphere(Position, Math.Max(0, ranges[level - 1] + SafeRange));
                return sphere.Contains(ref bounds) == ContainmentType.Contains;
            }
        }
    }
}
