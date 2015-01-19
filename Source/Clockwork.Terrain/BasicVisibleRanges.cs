using Clockwork.Serialization;
using System;

namespace Clockwork.Terrain
{
    public class BasicVisibleRanges : IVisibleRanges
    {
        private const float DetailBalance = 2;

        private float[] ranges;

        public BasicVisibleRanges(int levelCount, float maxVisibleDistance)
        {
            Count = levelCount;

            // Geometric series
            float total = ((float)Math.Pow(DetailBalance, levelCount + 1) - 1) / (DetailBalance - 1);

            ranges = new float[levelCount];

            float start = 0;
            float range = maxVisibleDistance / total;
            for (int i = 1; i < levelCount; i++)
            {
                start = ranges[i] = start + range;
                range *= DetailBalance;
            }
        }

        public int Count { get; private set; }

        public float this[int level]
        {
            get { return ranges[level]; }
        }
    }
}
