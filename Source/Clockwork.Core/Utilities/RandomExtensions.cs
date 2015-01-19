using SiliconStudio.Core.Mathematics;
using System;

namespace Clockwork
{
    public static class RandomExtensions
    {
        public static Vector2 NextVector2(this Random random)
        {
            return new Vector2(
                (float)random.NextDouble(),
                (float)random.NextDouble());
        }

        public static Vector2 NextVector2(this Random random, Vector2 minimum, Vector2 maximum)
        {
            return minimum + Vector2.Modulate(random.NextVector2(), maximum - minimum);
        }

        public static Vector3 NextVector3(this Random random)
        {
            return new Vector3(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble());
        }

        public static Vector3 NextVector3(this Random random, Vector3 minimum, Vector3 maximum)
        {
            return minimum + Vector3.Modulate(random.NextVector3(), maximum - minimum);
        }

        public static Vector4 NextVector4(this Random random)
        {
            return new Vector4(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble());
        }

        public static Vector4 NextVector4(this Random random, Vector4 minimum, Vector4 maximum)
        {
            return minimum + Vector4.Modulate(random.NextVector4(), maximum - minimum);
        }
    }
}
