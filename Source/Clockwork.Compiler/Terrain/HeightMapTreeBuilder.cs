using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clockwork.Terrain.Compiler
{
    public class HeightMapTreeBuilder : ImageTreeBuilder
    {
        private Stack<QuadTreeNode<TerrainTileData>> nodes;
        private QuadTree<TerrainTileData> tree;
        private float scale;

        public Action<HeightMap, int, int> SaveHeightMap;

        public HeightMapTreeBuilder(ImageTreeBuilderContext resamplerContext, PixelFormat intermediateFormat, PixelFormat targetFormat, TerrainMetrics metrics, TerrainDescription description)
            : base(resamplerContext, intermediateFormat, targetFormat, metrics)
        {
            tree = description.Tree;
            nodes = new Stack<QuadTreeNode<TerrainTileData>>(metrics.LevelCount);
            scale = metrics.HeightScale;
        }

        protected override async Task<bool> BuildRecursive(int depth, Int2 position)
        {
            QuadTreeNode<TerrainTileData> parent = null;
            QuadTreeNode<TerrainTileData> node = null;

            if (depth == 0)
            {
                node = tree.Root;
            }
            else
            {
                var quadrant = (position.X & 1) | (position.Y & 1) << 1;
                parent = nodes.Peek();
                tree.Expand(parent);
                node = parent.Children[quadrant];
            }

            nodes.Push(node);
            node.Value = new TerrainTileData();

            bool isOccupied = await base.BuildRecursive(depth, position);

            if (isOccupied)
            {
                if (parent != null)
                {
                    parent.Value.MinimumHeight = Math.Max(parent.Value.MinimumHeight, node.Value.MinimumHeight);
                    parent.Value.MaximumHeight = Math.Max(parent.Value.MaximumHeight, node.Value.MaximumHeight);
                }
            }
            else
            {
                node.Prune();
                node.Value = null;
            }

            nodes.Pop();
            return isOccupied;
        }

        protected override unsafe void ProcessImage(Image image, int depth, Point position)
        {
            int count = image.Description.Width * image.Description.Height;
            var data = (ushort*)image.DataPointer;

            ushort min, max;

            if (data[1] > data[0])
            {
                min = data[0];
                max = data[1];
            }
            else
            {
                min = data[1];
                max = data[0];
            }

            for (int i = 2; i < count; i++)
            {
                var value = data[i];

                if (value > max)
                {
                    max = value;
                }
                else if (value < min)
                {
                    min = value;
                }
            }

            var node = nodes.Peek();
            node.Value.MaximumHeight = max * scale / ushort.MaxValue;
            node.Value.MinimumHeight = min * scale / ushort.MaxValue;

            if (depth == tree.MaximumDepth)
            {
                int size = Metrics.EffectiveVerticesPerPatch;
                int overlap = Metrics.TotalOverlap;
                var heightMap = new HeightMap(size, size) { Resolution = scale / ushort.MaxValue };

                int k = Metrics.VertexOverlap.X * (1 + Metrics.VerticesPerPatch);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        heightMap[y, x] = data[k++] * heightMap.Resolution;
                    }
                    k += overlap;
                }

                if (SaveHeightMap != null)
                    SaveHeightMap(heightMap, position.X, position.Y);
            }
        }
    }
}
