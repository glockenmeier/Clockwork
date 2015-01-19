using SiliconStudio.Core.Mathematics;
using System;

namespace Clockwork.Terrain
{
    public struct ImageTreeDescription
    {
        public ImageTileDescription Tile;

        public int LevelCount;

        public int MaximumLevel { get { return LevelCount - 1; } }

        public int Stride { get { return Tile.EffectiveSize - 1; } }

        public int TotalSize { get { return TileCount * Stride + 1 + Tile.TotalOverlap; } }

        public int TileCount { get { return 1 << MaximumLevel; } }

        public ImageTreeDescription(ImageTileDescription tile, int levelCount)
        {
            Tile = tile;
            LevelCount = levelCount;
        }

        public static ImageTreeDescription FromSize(int width, int height, ImageTileDescription tile)
        {
            int size = Math.Max(width, height);
            int tileStride = tile.EffectiveSize - 1;
            float fractionalTileCount = (float)(size - 1 - tile.TotalOverlap) / tileStride;
            int tileCount = MathUtilities.UpperPowerOfTwo((int)Math.Ceiling(fractionalTileCount));
            int levelCount = (int)Math.Log(tileCount, 2) + 1;

            return new ImageTreeDescription(tile, levelCount);
        }
    }


    public struct Interval
    {
        public int Start;

        public int Length;

        public Interval(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int End
        {
            get { return Start + Length; }
            set { Length = value - Start; }
        }
    }

    public struct IntervalF
    {
        public float Start;

        public float Length;

        public IntervalF(float start, float length)
        {
            Start = start;
            Length = length;
        }

        public float End
        {
            get { return Start + Length; }
            set { Length = value - Start; }
        }

        public static implicit operator IntervalF(Interval interval)
        {
            return new IntervalF(interval.Start, interval.Length);
        }

        public static explicit operator Interval(IntervalF interval)
        {
            return new Interval((int)interval.Start, (int)interval.Length);
        }

        public static IntervalF operator *(IntervalF left, float right)
        {
            return new IntervalF(left.Start * right, left.Length * right);
        }
    }


    public struct ImageTileDescription
    {
        public int Size;

        public Int2 Overlap;

        public int TotalOverlap { get { return Overlap.X + Overlap.Y; } }

        public int EffectiveSize { get { return Size - TotalOverlap; } }

        public float RasterOffset { get { return Overlap.X + 0.5f; } }

        public float RelativeRasterOffset { get { return RasterOffset / Size; } }

        public int EffectiveRasterSize { get { return EffectiveSize - 1; } }

        public float RelativeEffectiveRasterSize { get { return (float)EffectiveRasterSize / Size; } }

        public ImageTileDescription(int size, Int2 overlap)
        {
            Size = size;
            Overlap = overlap;
        }
    }
}
