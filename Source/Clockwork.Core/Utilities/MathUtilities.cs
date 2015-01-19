using System;

namespace Clockwork
{
    public static class MathUtilities
    {
        public static int UpperPowerOfTwo(this int x)
        {
            if (x < 0)
                return 0;

            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        public static bool IsPowerOfTwo(this int value)
        {
            if (value < 1) return false;
            return (value & (value - 1)) == 0;
        }

        public static int ZigZagEncode(int value)
        {
            return (value << 1) ^ (value >> 31);
        }

        public static int ZigZagDecode(int value)
        {
            return (value >> 1) ^ -(value & 1);
        }

        public static long ZigZagEncode(long value)
        {
            return (value << 1) ^ (value >> 63);
        }

        public static long ZigZagDecode(long value)
        {
            return (value >> 1) ^ -(value & 1);
        }

        public static float Gaussian(float value, float mean, float standardDeviation)
        {
            const float normalizationFactor = 0.39894228040143267793994605993438f; // 1.0f / (float)Math.Sqrt(2 * Math.PI);
            float offset = value - mean;
            return normalizationFactor / standardDeviation * (float)Math.Exp(-0.5f * offset * offset / (standardDeviation * standardDeviation));
        }
    }
}
