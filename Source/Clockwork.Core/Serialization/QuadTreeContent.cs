using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using System.Collections.Generic;

namespace Clockwork.Serialization
{
    public class QuadTreeContent<T> : TiledContent<QuadTreeNode<T>>
        where T : QuadTreeContentTile, new()
    {
        private readonly QuadTree<T> tree;

        private readonly List<QuadTreeContentObserver> observers = new List<QuadTreeContentObserver>();

        public ICollection<QuadTreeContentObserver> Observers
        {
            get { return observers; }
        }

        public QuadTreeContent(IServiceRegistry serviceRegistry, QuadTree<T> tree, int maximumTileCount)
            : base(serviceRegistry, maximumTileCount)
        {
            this.tree = tree;
        }

        protected override ContentTile GetTile(QuadTreeNode<T> key)
        {
            return key.Value;
        }

        protected virtual BoundingBox GetBoundingBox(QuadTreeNode<T> node)
        {
            return new BoundingBox
            {
                Minimum = new Vector3(node.BoundingRectangle.Left, float.NegativeInfinity, node.BoundingRectangle.Top),
                Maximum = new Vector3(node.BoundingRectangle.Right, float.PositiveInfinity, node.BoundingRectangle.Bottom)
            };
        }

        public int[] count;

        protected override void ObserveOverride()
        {
            Observe(tree.Root);

            if (count == null)
            {
                count = new int[tree.MaximumDepth + 1];
            }
            else
            {
                for (int i = 0; i < tree.MaximumDepth + 1; i++)
                    count[i] = 0;
            }

            tree.Traverse(node =>
            {
                if (node.Value != null)
                {
                    if (node.Value.State != TileState.None)
                        count[node.Depth]++;

                    if (node.Value.HasObservedDescendants == true)
                        return true;
                }

                return false;
            });
        }

        public void Select(IQuadTreeSelection<T> selection)
        {
            selection.ClearSelectedNodes();
            Select(tree.Root, selection, false, false);
        }

        private void Select(QuadTreeNode<T> node, IQuadTreeSelection<T> selection, bool parentCompletelyInFrustum, bool ignoreVisibilityCheck)
        {
            if (node.Value == null)
                return;

            var boundingBox = GetBoundingBox(node);

            BoundingSphere sphere;

            if (!ignoreVisibilityCheck)
            {
                sphere = selection.GetVisibilitySphere(tree.MaximumDepth - node.Depth);
                if (!boundingBox.Intersects(ref sphere))
                {
                    return;
                }
            }

            ContainmentType containmentType = ContainmentType.Contains;
            if (!parentCompletelyInFrustum)
            {
                containmentType = selection.Frustum.Contains(boundingBox);
            }

            // If we reached a leaf node, add it, if it is still visible
            if (!node.HasChildren)
            {
                if (containmentType != ContainmentType.Disjoint)
                    selection.Add(node);

                return;
            }

            // If this node is out of the next visibilitiy sphere, children needn't be checked.
            sphere = selection.GetVisibilitySphere(tree.MaximumDepth - (node.Depth + 1));
            if (!boundingBox.Intersects(ref sphere))
            {
                if (containmentType != ContainmentType.Disjoint)
                    selection.Add(node);

                return;
            }

            bool isCompletelyContained = containmentType == ContainmentType.Contains;
            bool isAnyChildSelected = false;

            foreach (var child in node.Children)
            {
                isAnyChildSelected |= IsNodeVisible(child, selection, isCompletelyContained);
                if (isAnyChildSelected)
                    break;
            }

            if (isAnyChildSelected)
            {
                // TODO: Propagate relative eye position and sort children by distance
                foreach (var child in node.Children)
                {
                    Select(child, selection, isCompletelyContained, true);
                }
            }
            else
            {
                if (containmentType != ContainmentType.Disjoint)
                    selection.Add(node);
            }
        }

        private bool IsNodeVisible(QuadTreeNode<T> node, IQuadTreeSelection<T> selection, bool isParentCompletelyContained)
        {
            if (node.Value == null)
                return false;

            var boundingBox = GetBoundingBox(node);

            var sphere = selection.GetVisibilitySphere(tree.MaximumDepth - node.Depth);
            return boundingBox.Intersects(ref sphere);
        }

        private void LoadAndNotifyAncestors(QuadTreeNode<T> node)
        {
            Load(node);

            var ancestor = node.Parent;
            while (ancestor != null && !ancestor.Value.HasObservedDescendants)
            {
                ancestor.Value.HasObservedDescendants = true;
                ancestor = ancestor.Parent;
            }
        }

        private void UnloadSubtree(QuadTreeNode<T> node)
        {
            if (node.Value == null)
                return;

            Unload(node);

            if (node.Value.HasObservedDescendants)
            {
                foreach (var child in node.Children)
                    UnloadSubtree(child);

                node.Value.HasObservedDescendants = false;
            }
        }

        private bool ShouldUnload(ref BoundingBox bounds, int level)
        {
            foreach (var observer in observers)
            {
                if (!observer.ShouldUnload(ref bounds, level))
                    return false;
            }

            return true;
        }

        private bool ShouldLoad(ref BoundingBox bounds, int level)
        {
            foreach (var observer in observers)
            {
                if (observer.ShouldLoad(ref bounds, level))
                    return true;
            }

            return false;
        }

        private bool IsSafe(ref BoundingBox bounds, int level)
        {
            foreach (var observer in observers)
            {
                if (!observer.IsSafe(ref bounds, level))
                    return false;
            }

            return true;
        }

        private bool Observe(QuadTreeNode<T> node)
        {
            if (node.Value == null)
                return true;

            int level = tree.MaximumDepth - node.Depth;
            int childLevel = level - 1;

            // Load leaf nodes that were not ruled out by their parent
            if (!node.HasChildren) // level == 0
            {
                LoadAndNotifyAncestors(node);
                return false;
            }

            bool isAnyChildInRange = false;

            foreach (var child in node.Children)
            {
                if (child.Value == null)
                    continue;

                var childBoundingBox = GetBoundingBox(child);

                isAnyChildInRange |= ShouldLoad(ref childBoundingBox, childLevel);
                if (isAnyChildInRange)
                    break;
            }

            if (isAnyChildInRange)
            {
                bool allChildrenSafe = true;

                foreach (var child in node.Children)
                {
                    if (child.Value == null)
                        continue;

                    allChildrenSafe &= Observe(child);
                }

                if (allChildrenSafe)
                {
                    Unload(node);
                }
                else
                {
                    LoadAndNotifyAncestors(node);
                }
            }
            else
            {
                foreach (var child in node.Children)
                {
                    if (child.Value == null)
                        continue;

                    var childBoundingBox = GetBoundingBox(child);

                    if (ShouldUnload(ref childBoundingBox, childLevel))
                        UnloadSubtree(child);
                }

                LoadAndNotifyAncestors(node);
            }

            var boundingBox = GetBoundingBox(node);
            return IsSafe(ref boundingBox, level);
        }
    }
}
