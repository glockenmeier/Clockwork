using Clockwork.Serialization;

namespace Clockwork.Terrain
{
    public class DefaultVisibleRanges : IVisibleRanges
    {
        public const float FinestNodeSize = 1.5f; //1.4142135623730950488016887242097f;

        public const float DetailBalance = 2.5f;

        private float[] ranges;

        public int Count
        {
            get { return ranges.Length; }
        }

        public float this[int level]
        {
            get { return ranges[level]; }
        }

        public DefaultVisibleRanges(int levelCount, int leafNodeSize, float patchScale)
        {
            ranges = new float[levelCount];

            float near = 0;
            float lastRange = near;
            float currentDetailBalance = 1;
            float section = FinestNodeSize * leafNodeSize * patchScale;

            for (int i = 0; i < ranges.Length; i++)
            {
                ranges[i] = lastRange + section * currentDetailBalance;
                lastRange = ranges[i];
                currentDetailBalance *= DetailBalance;
            }
        }
    }
}
