using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Serializers;

namespace Clockwork
{
    public class QuadTree<T> : SpacePartitionTree<T, QuadTreeNode<T>>
    {
        private const int ChildCount = 4;

        public RectangleF Bounds;

        public QuadTree() : base(new QuadTreeNode<T>())
        {
        }

        public QuadTree(RectangleF boundingRectangle, int maximumDepth)
            : base(new QuadTreeNode<T>(), maximumDepth)
        {
            Bounds = boundingRectangle;
        }

        public RectangleF GetNodeBounds(Int2 position, int depth)
        {
            float scale = 1.0f / (1 << depth);
            float width = Bounds.Width * scale;
            float height = Bounds.Height * scale;

            return new RectangleF(
                Bounds.X + position.X * width,
                Bounds.Y + position.Y * height,
                width, height);
        }

        protected override QuadTreeNode<T>[] ExpandNode(QuadTreeNode<T> node)
        {
            var children = new QuadTreeNode<T>[ChildCount];

            for (int i = 0; i < ChildCount; i++)
            {
                children[i] = new QuadTreeNode<T>(
                    2 * node.Position.X + (i & 1),
                    2 * node.Position.Y + (i >> 1 & 1));
            }

            return children;
        }
    }

    public class QuadTreeNode<T> : SpacePartitionTreeNode<T, QuadTreeNode<T>>
    {
        public Int2 Position { get; private set; }

        public RectangleF BoundingRectangle
        {
            get { return ((QuadTree<T>)Tree).GetNodeBounds(Position, Depth); }
        }

        public QuadTreeNode()
        {
        }

        public QuadTreeNode(int x, int y)
        {
            Position = new Int2(x, y);
        }
    }

    [DataSerializerGlobal(typeof(QuadTreeSerializer<>), typeof(QuadTree<>), DataSerializerGenericMode.GenericArguments)]
    public class QuadTreeSerializer<T> : SpacePartitionTreeSerializer<T, QuadTree<T>, QuadTreeNode<T>>
    {
        private DataSerializer<RectangleF> boundsSerializer;

        public override void Initialize(SerializerSelector serializerSelector)
        {
            base.Initialize(serializerSelector);
            boundsSerializer = MemberSerializer<RectangleF>.Create(serializerSelector, false);
        }

        public override void Serialize(ref QuadTree<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            boundsSerializer.Serialize(ref obj.Bounds, mode, stream);
            base.Serialize(ref obj, mode, stream);
        }
    }

    public static class QuadTreeHelper
    {
        public static int GetNodeIndex(int depth, Int2 position)
        {
            return ((1 << (2 * depth)) - 1) / 3 + (1 << depth) * position.Y + position.X;

            /*
            int index = ((1 << (2 * depth)) - 1) / 3;

            int blockSize = 1;
            while (depth > 0)
            {
                index += ((position.X % 2) + (position.Y % 2) * 2) * blockSize;
                position /= 2;
                blockSize *= 4;
                depth--;
            }

            return index;
            */
        }
    }
}
