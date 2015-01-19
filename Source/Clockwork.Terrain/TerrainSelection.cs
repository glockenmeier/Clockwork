using Clockwork.Serialization;
using SiliconStudio.Core.Mathematics;

namespace Clockwork.Terrain
{
    public class TerrainSelection : IQuadTreeSelection<TerrainTileData>
    {
        private QuadTreeNode<TerrainTileData>[] selectedNodes;

        public int MaxSelectedNodeCount { get; private set; }

        public Camera Camera { get; set; }

        public IVisibleRanges VisibleRanges { get; private set; }

        public BoundingFrustum Frustum
        {
            get { return Camera.Frustum; }
        }

        public Vector3 EyePosition
        {
            get { return Camera.Position; }
        }

        public int SelectedNodeCount { get; private set; }

        public TerrainSelection(IVisibleRanges visibleRanges, int maxSelectionCount)
        {
            VisibleRanges = visibleRanges;
            MaxSelectedNodeCount = maxSelectionCount;
            selectedNodes = new QuadTreeNode<TerrainTileData>[maxSelectionCount];
        }

        public void ClearSelectedNodes()
        {
            SelectedNodeCount = 0;
        }

        public QuadTreeNode<TerrainTileData> GetSelectedNode(int index)
        {
            return selectedNodes[index];
        }

        public BoundingSphere GetVisibilitySphere(int level)
        {
            return new BoundingSphere(Camera.Position, VisibleRanges[level]);
        }

        public void Add(QuadTreeNode<TerrainTileData> node)
        {
            if (SelectedNodeCount >= MaxSelectedNodeCount)
                return;

            selectedNodes[SelectedNodeCount++] = node;
        }
    }
}
