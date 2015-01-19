using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;

namespace Clockwork
{
    public class Octree<T> : SpacePartitionTree<T, OctreeNode<T>>
    {
        private const int ChildCount = 8;

        public BoundingBox Bounds;

        public Octree() : base(new OctreeNode<T>())
        {

        }

        public Octree(BoundingBox boundingBox, int maximumDepth)
            : base(new OctreeNode<T>(), maximumDepth)
        {
        }

        public BoundingBox GetNodeBounds(OctreeNode<T> node)
        {
            Vector3 stride = (Bounds.Maximum - Bounds.Minimum) / (1 << node.Depth);
            return new BoundingBox(stride * new Vector3(node.Position.X, node.Position.Y, node.Position.Z), stride);
        }

        protected override OctreeNode<T>[] ExpandNode(OctreeNode<T> node)
        {
            var children = new OctreeNode<T>[ChildCount];

            var min = node.BoundingBox.Minimum;
            var max = node.BoundingBox.Maximum;
            var center = min + (max - min) / 2;

            for (int i = 0; i < ChildCount; i++)
            {
                var child = new OctreeNode<T>();

                child.Position = node.Position + new Int3(i & 1, i & 2, i & 3); 

                children[i] = child;
            }

            return children;
        }
    }

    public class OctreeNode<T> : SpacePartitionTreeNode<T, OctreeNode<T>>
    {
        public Int3 Position { get; internal set; }

        public BoundingBox BoundingBox
        {
            get { return ((Octree<T>)Tree).GetNodeBounds(this); }
        }
    }

    public class OctreeSerializer<T> : SpacePartitionTreeSerializer<T, Octree<T>, OctreeNode<T>>
    {
        private DataSerializer<BoundingBox> boundsSerializer;

        public override void Initialize(SerializerSelector serializerSelector)
        {
            base.Initialize(serializerSelector);
            boundsSerializer = MemberSerializer<BoundingBox>.Create(serializerSelector, false);
        }

        public override void Serialize(ref Octree<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            boundsSerializer.Serialize(ref obj.Bounds, mode, stream);
            base.Serialize(ref obj, mode, stream);
        }
    }
}
