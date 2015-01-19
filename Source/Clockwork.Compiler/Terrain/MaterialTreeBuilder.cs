using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clockwork.Terrain.Compiler
{
    public class MaterialTreeData
    {
        public HashSet<string> Materials = new HashSet<string>();
    }

    public class MaterialTreeBuilder : ImageTreeBuilder
    {
        private Stack<QuadTreeNode<MaterialTreeData>> nodes;
        private QuadTree<MaterialTreeData> tree;
        private float scale;

        public Action<HeightMap, int, int> SaveHeightMap;

        public string LayerName;

        public float OpacityThreshold = 0.05f;

        public MaterialTreeBuilder(ImageTreeBuilderContext resamplerContext, PixelFormat intermediateFormat, PixelFormat targetFormat, TerrainMetrics metrics, QuadTree<MaterialTreeData> tree)
            : base(resamplerContext, intermediateFormat, targetFormat, metrics)
        {
            this.tree = tree;
            nodes = new Stack<QuadTreeNode<MaterialTreeData>>(metrics.LevelCount);
            scale = metrics.HeightScale;
        }

        protected override async Task<bool> BuildRecursive(int depth, Int2 position)
        {
            QuadTreeNode<MaterialTreeData> parent = null;
            QuadTreeNode<MaterialTreeData> node = null;

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

            if (node.Value == null)
                node.Value = new MaterialTreeData();

            bool isOccupied = await base.BuildRecursive(depth, position);

            /*if (isOccupied)
            {
                if (parent != null)
                {
                    //parent.Value.MinimumHeight = Math.Max(parent.Value.MinimumHeight, node.Value.MinimumHeight);
                    //parent.Value.MaximumHeight = Math.Max(parent.Value.MaximumHeight, node.Value.MaximumHeight);
                }
            }
            else
            {
                node.Prune();
                node.Value = null;
            }*/

            nodes.Pop();
            return isOccupied && node.Value.Materials.Contains(LayerName);
        }

        protected override unsafe void ProcessImage(Image image, int depth, Point position)
        {
            int count = image.Description.Width * image.Description.Height;
            var data = (byte*)image.DataPointer;

            bool isOccupied = false;
            for (int i = 0; i < count; i++)
            {
                if (data[i] > OpacityThreshold)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (isOccupied)
            {
                var node = nodes.Peek();
                node.Value.Materials.Add(LayerName);
            }
        }
    }
}
