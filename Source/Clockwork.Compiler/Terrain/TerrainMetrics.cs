using SiliconStudio.Core.Mathematics;

using System;

namespace Clockwork.Terrain.Compiler
{
    public class TerrainMetrics
    {
        public float VertexSpacing { get; set; }

        public float PatchScale { get { return PatchVertexStride * VertexSpacing; } }

        public float HeightScale;

        public int LevelCount { get { return (int)Math.Log(PatchCount, 2) + 1; } }

        public int MaximumLevel { get { return LevelCount - 1; } }

        public int PatchCount;

        public int VerticesPerPatch;

        public Int2 VertexOverlap;

        public int TotalOverlap { get { return VertexOverlap.X + VertexOverlap.Y; } }

        public int EffectiveVerticesPerPatch { get { return VerticesPerPatch - TotalOverlap; } }

        public int PatchVertexStride { get { return EffectiveVerticesPerPatch - 1; } }

        public int TotalVertexCount { get { return PatchCount * PatchVertexStride + 1 + TotalOverlap; } }

        public Vector2 PatchMargin { get { return new Vector2(0.5f + VertexOverlap.X, 0.5f + VertexOverlap.Y); } }

        public float TotalPatchMargin { get { return PatchMargin.X + PatchMargin.Y; } }

        public Int2 Padding { get { return VertexOverlap * ((1 << (LevelCount - 1)) - 1); } }

        public int TotalPadding { get { return Padding.X + Padding.Y; } }

        public int SourceSize { get { return VerticesPerPatch + TotalPadding; } }

        public RectangleF Bounds
        {
            get
            {
                var size = VertexSpacing * PatchCount * PatchVertexStride;
                return new RectangleF(-size / 2, -size / 2, size, size);
            }
        }
    }
}
