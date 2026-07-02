using SEE.Layout.NodeLayouts.EvoStreets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// Lays out nodes in a tree hierarchy in a street-like manner (EvoStreets
    /// according to Frank Steinbrückner).
    /// </summary>
    public class EvoStreetsNodeLayout : NodeLayout, IIncrementalNodeLayout
    {
        static EvoStreetsNodeLayout()
        {
            Name = "EvoStreets";
        }

        /// <summary>
        /// The orientation of the root.
        /// </summary>
        internal const Orientation RootOrientation = Orientation.East;

        /// <summary>
        /// The formerly run layout. Can be null if this is the first time, this layout is calculated.
        /// This is the backing field for <see cref="OldLayout"/>.
        /// </summary>
        private EvoStreetsNodeLayout oldLayout;

        /// <summary>
        /// <inheritdoc cref="IIncrementalNodeLayout.OldLayout"/>.
        /// </summary>
        public IIncrementalNodeLayout OldLayout
        {
            set
            {
                if (value is EvoStreetsNodeLayout layout)
                {
                    oldLayout = layout;
                }
                else
                {
                    throw new ArgumentException(
                        $"Predecessor of {nameof(EvoStreetsNodeLayout)} was not an {nameof(EvoStreetsNodeLayout)}.");
                }
            }
        }

        /// <summary>
        /// Saves the last layout, that is, the one computed previously. Can be null, if no
        /// previous layout was computed. It is a mapping from node ids onto the layout
        /// data, i.e., <see cref="NodeTransform"/>. It was computed by the <see cref="EvoStreetsNodeLayout"/>
        /// set via <see cref="OldLayout"/>.
        /// </summary>
        protected Dictionary<ILayoutNode, NodeTransform> LastLayout { get; set; }

        /// <summary>
        /// Comparator for <see cref="ILayoutNode"/>. Two nodes are equivalent if they have
        /// the same ID.
        /// </summary>
        public class ILayoutNodeComparer : IEqualityComparer<ILayoutNode>
        {
            /// <summary>
            /// True if <paramref name="left"/> and <paramref name="left"/> have the same id.
            /// </summary>
            /// <param name="left">Left argument</param>
            /// <param name="right">Right argument.</param>
            /// <returns>True if <paramref name="left"/> and <paramref name="left"/> have the same id.</returns>
            bool IEqualityComparer<ILayoutNode>.Equals(ILayoutNode? left, ILayoutNode? right)
            {
                return left.ID == right.ID;
            }

            /// <summary>
            /// Returns the hash value for the id of <paramref name="node"/>.
            /// </summary>
            /// <param name="node">Nodes whose hash code is required.</param>
            /// <returns>Hash code for the id of <paramref name="node"/>.</returns>
            int IEqualityComparer<ILayoutNode>.GetHashCode(ILayoutNode node)
            {
                return node.ID.GetHashCode();
            }
        }

        /// <summary>
        /// <see cref="CalculateStreetWidth(IList{ILayoutNode})"/> determines a statistical
        /// parameter of the widths and depths of all leaf nodes (the average) and adjusts
        /// this statistical parameter by multiplying it with this factor <see cref="streetWidthPercentage"/>.
        /// </summary>
        private const float streetWidthPercentage = 0.3f;

        /// <summary>
        /// Is used to calculate the offset between buildings as this factor multiplied by
        /// the absolute street width for the root node.
        /// </summary>
        private const float offsetBetweenBuildingsPercentage = 0.3f;

        /// <summary>
        /// The height (y co-ordinate) of game objects (inner tree nodes) represented by streets.
        /// The actual value used will be multiplied by leafNodeFactory.Unit.
        /// </summary>
        private readonly float streetHeight = 0.0001f;

        /// <summary>
        /// See <see cref="NodeLayout.Layout"/>.
        /// </summary>
        /// <exception cref="Exception">Thrown if there is no or more than one root in
        /// <paramref name="gameNodes"/>.</exception>
        protected override Dictionary<ILayoutNode, NodeTransform> Layout
            (IEnumerable<ILayoutNode> gameNodes,
            Vector3 centerPosition,
            Vector2 rectangle)
        {
            IList<ILayoutNode> layoutNodes = gameNodes.ToList();
            if (layoutNodes.Count == 0)
            {
                /// Empty graph => empty layout.
                LastLayout = new();
                return LastLayout;
            }

            if (layoutNodes.Count == 1)
            {
                /// Graph with only one node => node is placed at the center with given scale.
                ILayoutNode singleNode = layoutNodes.First();
                LastLayout = new Dictionary<ILayoutNode, NodeTransform>()
                {
                    [singleNode] = new NodeTransform(0, 0, singleNode.AbsoluteScale)
                };
                return LastLayout;
            }

            Roots = LayoutNodes.GetRoots(layoutNodes);
            if (Roots.Count == 0)
            {
                /// We can never arrive here because we made sure above that we have at least one node.
                LastLayout = new();
                throw new Exception("Graph has no root node.");
            }

            if (Roots.Count > 1)
            {
                /// Graph must have a single root.
                LastLayout = new();
                throw new Exception("Graph has multiple roots.");
            }

            // The nodes that are only in gameNodes but not in the last layout
            // and the nodes in last layout whose parent has changed.
            // Note:
            HashSet<ILayoutNode> newNodes;
            // The nodes that are both in gameNodes and the last layout having
            // the same parent as before.
            HashSet<ILayoutNode> existingNodes;
            // The nodes that only in the last layout but not in gameNodes,
            // and the nodes in gameNodes whose parent has changed.
            HashSet<ILayoutNode> deletedNodes;

            if (oldLayout != null)
            {
                Assert.IsNotNull(oldLayout.LastLayout);
                ILayoutNodeComparer comparer = new();
                GetDifferences(new(layoutNodes, comparer),
                               new(oldLayout.LastLayout.Keys, comparer),
                               out newNodes, out existingNodes, out deletedNodes);
            }
            else
            {
                newNodes = new();
                existingNodes = new();
                deletedNodes = new();
            }

            LayoutDescriptor treeDescriptor;
            treeDescriptor.StreetWidth = CalculateStreetWidth(layoutNodes);
            treeDescriptor.OffsetBetweenBuildings = treeDescriptor.StreetWidth * offsetBetweenBuildingsPercentage;
            /// We have exactly one root. See above.
            ILayoutNode root = Roots.FirstOrDefault();
            ENode rootNode = GenerateHierarchy(root);
            treeDescriptor.MaximalDepth = MaxDepth(root);

            /// The layouting works in two steps:
            ///
            /// (1) The sizes required for all nodes are calculated and the children of
            ///     inner nodes are distributed left and right along their street
            ///     relative to the origin of the street. The resulting rectangles of
            ///     both inner nodes and leaves may still overlap at this stage.
            /// (2) Once the required size (rectangle) and relative position of each node
            ///     along a street is known, we can calculate the final positions such
            ///     the rectangles do not overlap.
            rootNode.SetSizeAndDistribute(RootOrientation, treeDescriptor,
                                          oldLayout == null ? null : oldLayout.LastLayout,
                                          newNodes, existingNodes);
            /// The sizes are known now so that the positions (the actual layout) can
            /// be computed in the following.
            rootNode.SetLocation(RootOrientation, new Location(0, 0));

            Dictionary<ILayoutNode, NodeTransform> layoutResult = new();
            rootNode.ToLayout(ref layoutResult, streetHeight);
            /// Save this layout for the next incremental <see cref="EvoStreetsNodeLayout"/>.
            LastLayout = layoutResult;
            return layoutResult;
        }

        /// <summary>
        /// Determines the differences between <paramref name="newNodes"/> and
        /// <paramref name="oldNodes"/> based on their node ids, that is,
        /// <see cref="ILayoutNode.ID"/>.
        /// </summary>
        /// <param name="newNodes">The nodes for which the new layout is to be calculated.</param>
        /// <param name="oldNodes">The nodes of the previous <see cref="EvoStreetsNodeLayout"/>.</param>
        /// <param name="addedNodes">All nodes that are in <paramref name="newNodes"/> but not in
        /// <paramref name="oldNodes"/> and all nodes in <paramref name="oldNodes"/> whose
        /// parent has changed.
        /// Note: These nodes stem from <paramref name="newNodes"/>.</param>
        /// <param name="deletedNodes">All nodes that are only in <paramref name="oldNodes"/>
        /// but not in <paramref name="newNodes"/> and all nodes in both whose parent
        /// has changed.
        /// Note: These nodes stem from <paramref name="oldNodes"/>.</param>
        /// <param name="existingNodes">All nodes that are in <paramref name="oldNodes"/>
        /// and in <paramref name="newNodes"/> and whose parent has not changed.
        /// Note: These nodes stem from in <paramref name="oldNodes"/>.</param>
        private static void GetDifferences
            (HashSet<ILayoutNode> newNodes,
             HashSet<ILayoutNode> oldNodes,
             out HashSet<ILayoutNode> addedNodes,
             out HashSet<ILayoutNode> existingNodes,
             out HashSet<ILayoutNode> deletedNodes)
        {
            ILayoutNodeComparer comparer = new();

            existingNodes = new(oldNodes, comparer);
            existingNodes.IntersectWith(newNodes);

            deletedNodes = new(oldNodes, comparer);
            deletedNodes.ExceptWith(newNodes);

            addedNodes = new(newNodes, comparer);
            addedNodes.ExceptWith(existingNodes);

            /// We need to move existingNodes whose parentship has changed to addedNodes.
            /// They will show up at a different tree level (street) in the new layout,
            /// hence, must be considered anew.
            HashSet<ILayoutNode> movedNodes = new(comparer);
            foreach (ILayoutNode node in existingNodes)
            {
                /// node is contained in oldNodes.
                /// Retrieve the node in newNodes corresponding to 'node'.
                if (newNodes.TryGetValue(node, out ILayoutNode correspondent))
                {
                    /// correspondent is in newNodes.
                    if (!HaveSameParents(node, correspondent))
                    {
                        Debug.Log($"Hierarchy change detected for {node.ID}.\n");
                        movedNodes.Add(node); /// moved node (from oldNodes) needs to be removed from existingNodes
                        addedNodes.Add(correspondent); // correspondent stems from newNodes
                        deletedNodes.Add(node);        // node stems from oldNodes
                    }
                }
                else
                {
                    // We should never arrive here.
                    Assert.IsTrue(false);
                }
            }
            // Remove all movedNodes after the iteration.
            existingNodes.ExceptWith(movedNodes);

            /// True if left and right have the same parent, more precisely,
            /// if the id of their parents is equal. True also if they are both roots.
            static bool HaveSameParents(ILayoutNode left, ILayoutNode right)
            {
                if (left.Parent == null)
                {
                    return right.Parent == null;
                }
                // left.Parent != null
                if (right.Parent == null)
                {
                    return false;
                }
                // left.Parent != null and right.Parent != null
                return left.Parent.ID == right.Parent.ID;
            }
        }

        /// <summary>
        /// Returns the width of the street for the root as a percentage <see cref="streetWidthPercentage"/>
        /// of the average of all widths and depths of leaf nodes in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">The nodes to be laid out.</param>
        /// <returns>Width of street for the root.</returns>
        private static float CalculateStreetWidth(IList<ILayoutNode> layoutNodes)
        {
            float result = 0;
            int numberOfLeaves = 0;
            foreach (ILayoutNode node in layoutNodes)
            {
                if (node.IsLeaf)
                {
                    numberOfLeaves++;
                    result += node.AbsoluteScale.x > node.AbsoluteScale.z ? node.AbsoluteScale.x : node.AbsoluteScale.z;
                }
            }
            UnityEngine.Assertions.Assert.IsTrue(numberOfLeaves > 0);
            result /= numberOfLeaves;
            // result is now the average length over all widths and depths of all leaf nodes.
            return result * streetWidthPercentage;
        }

        /// <summary>
        /// Creates the <see cref="ENode"/> tree hierarchy starting at given <paramref name="node"/>.
        /// The root has depth 0.
        /// </summary>
        /// <param name="node">Currently processed node.</param>
        /// <param name="depth">The current depth of <paramref name="node"/> in the hierarchy.
        /// The root has depth 0.</param>
        /// <returns>Root ENode.</returns>
        private static ENode GenerateHierarchy(ILayoutNode node, int depth = 0)
        {
            ENode result = ENodeFactory.Create(node);
            result.TreeDepth = depth;
            if (result is EInner inner)
            {
                foreach (ILayoutNode child in node.Children())
                {
                    inner.AddChild(GenerateHierarchy(child, depth + 1));
                }
            }
            return result;
        }
    }
}
