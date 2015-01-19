using SiliconStudio.Core.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Clockwork
{
    /// <summary>
    /// Base class for kd-trees.
    /// </summary>
    /// <typeparam name="T">The element type of the tree.</typeparam>
    /// <typeparam name="TNode">The node type.</typeparam>
    public abstract class SpacePartitionTree<T, TNode> : IEnumerable<TNode>
        where TNode : SpacePartitionTreeNode<T, TNode>
    {
        internal int maximumDepth;

        /// <summary>
        /// Gets the tree's root node.
        /// </summary>
        public TNode Root { get; private set; }

        /// <summary>
        /// Gets the tree's maximum depth.
        /// </summary>
        public int MaximumDepth
        {
            get { return maximumDepth; }
        }

        /// <summary>
        /// Creates a new <see cref="SpacePartitionTree" />.
        /// </summary>
        /// <param name="root">The tree's root node.</param>
        /// <param name="maximumDepth">The tree's maximum depth.</param>
        public SpacePartitionTree(TNode root, int maximumDepth = 0)
        {
            if (maximumDepth < 0)
                throw new ArgumentOutOfRangeException("maxDepth");

            Root = root;
            Root.Depth = 0;
            Root.Parent = null;
            Root.Tree = this;
            this.maximumDepth = maximumDepth;
        }

        /// <summary>
        /// Subdivides a node, converting it from a leaf into an inner node.
        /// </summary>
        /// <param name="node">The node to expand.</param>
        /// <returns><c>true</c> if the node was subdivided; otherwise, <c>false</c></returns>
        public bool Expand(TNode node)
        {
            if (node.Tree != this)
                throw new InvalidOperationException("The node must be a child of this tree.");

            if (node.Depth >= MaximumDepth)
                return false;

            if (node.HasChildren)
                return true;

            if (node.children == null || node.children.Length <= 0)
            {
                node.children = ExpandNode(node);

                foreach (var child in node.children)
                {
                    child.Tree = this;
                    child.Depth = node.Depth + 1;
                    child.Parent = node;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates the array of children for a given node.
        /// </summary>
        /// <param name="node">The node to expand.</param>
        /// <returns>An array of child nodes.</returns>
        protected abstract TNode[] ExpandNode(TNode node);

        public IEnumerator<TNode> GetEnumerator()
        {
            return GetEnumerator(Root).GetEnumerator();
        }

        private IEnumerable<TNode> GetEnumerator(TNode node)
        {
            yield return node;
            if (node != null && node.HasChildren)
            {
                foreach (var child in node.Children)
                {
                    foreach (var result in GetEnumerator(child))
                        yield return result;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Traverses the tree in depth-first order.
        /// </summary>
        /// <param name="callback">A predicate which determines, if a nodes children should be traversed.</param>
        public void Traverse(Func<TNode, bool> callback)
        {
            Traverse(Root, callback);
        }

        private void Traverse(TNode node, Func<TNode, bool> callback)
        {
            if (node == null)
                return;

            callback(node);

            if (node.HasChildren)
            {
                foreach (var child in node.Children)
                    Traverse(child, callback);
            }
        }
    }

    public abstract class SpacePartitionTreeNode<T, TNode>
        where TNode : SpacePartitionTreeNode<T, TNode>
    {
        private static ReadOnlyCollection<TNode> Empty = new ReadOnlyCollection<TNode>(new TNode[0]);

        internal TNode[] children;

        public IReadOnlyList<TNode> Children
        {
            get
            {
                if (HasChildren)
                {
                    return children;
                }
                else
                {
                    return Empty;
                }
            }
        }

        public bool HasChildren
        {
            get { return children != null; }
        }

        public SpacePartitionTree<T, TNode> Tree { get; internal set; }

        public int Depth { get; internal set; }

        public SpacePartitionTreeNode<T, TNode> Parent { get; internal set; }

        public T Value;

        public void Prune()
        {
            children = null;
        }
    }

    public class SpacePartitionTreeSerializer<T, TTree, TNode> : DataSerializer<TTree>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
        where TNode : SpacePartitionTreeNode<T, TNode>
        where TTree : SpacePartitionTree<T, TNode>, new()
    {
        private DataSerializer<T> itemSerializer;

        public virtual void Initialize(SerializerSelector serializerSelector)
        {
            itemSerializer = MemberSerializer<T>.Create(serializerSelector, true);
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }

        public override void PreSerialize(ref TTree obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                {
                    obj = new TTree();
                }
            }
        }

        public override void Serialize(ref TTree obj, ArchiveMode mode, SerializationStream stream)
        {
            stream.Serialize(ref obj.maximumDepth);

            if (mode == ArchiveMode.Serialize)
            {
                WriteNode(obj, obj.Root, stream);
            }
            else
            {
                ReadNode(obj, obj.Root, stream);
            }
        }

        private void WriteNode(SpacePartitionTree<T, TNode> tree, TNode node, SerializationStream stream)
        {
            itemSerializer.Serialize(ref node.Value, ArchiveMode.Serialize, stream);

            stream.Write(node.HasChildren);
            if (node.HasChildren)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    WriteNode(tree, node.Children[i], stream);
                }
            }
        }

        private void ReadNode(SpacePartitionTree<T, TNode> tree, TNode node, SerializationStream stream)
        {
            itemSerializer.Serialize(ref node.Value, ArchiveMode.Deserialize, stream);

            var hasChildren = stream.ReadBoolean();
            if (hasChildren)
            {
                tree.Expand(node);
                for (int i = 0; i < node.Children.Count; i++)
                {
                    ReadNode(tree, node.Children[i], stream);
                }
            }
        }
    }
}