using SiliconStudio.Core.Mathematics;

namespace Clockwork.Serialization
{
    public interface IQuadTreeSelection<T>
    {
        IVisibleRanges VisibleRanges { get; }

        BoundingFrustum Frustum { get; }

        Vector3 EyePosition { get; }

        int SelectedNodeCount { get; }

        void ClearSelectedNodes();

        BoundingSphere GetVisibilitySphere(int level);

        void Add(QuadTreeNode<T> node);
    }
}
