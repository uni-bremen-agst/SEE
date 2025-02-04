using SEE.Layout.NodeLayouts.IncrementalTreeMap;
using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Game.City;
using UnityEngine;
using UnityEngine.Assertions;
using Node = SEE.Layout.NodeLayouts.IncrementalTreeMap.Node;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// A node layout designed for the evolution setting.
    /// Incremental Tree Map Layout is a Tree Map Layout
    /// that can guarantee stability in the layouts over the series of graphs in the evolution
    /// while not neglecting the aspect of visual quality either.
    /// </summary>
    public class IncrementalTreeMapLayout : NodeLayout, IIncrementalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">the settings for the layout</param>
        public IncrementalTreeMapLayout(IncrementalTreeMapAttributes settings)
        {
            this.settings = settings;
        }

        static IncrementalTreeMapLayout()
        {
            Name = "IncrementalTreeMap";
        }

        /// <summary>
        /// The adjustable parameters for the layout.
        /// </summary>
        private readonly IncrementalTreeMapAttributes settings;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private readonly Dictionary<ILayoutNode, NodeTransform> layoutResult = new();

        /// <summary>
        /// A map to find a node (fast) by its ID
        /// </summary>
        private readonly Dictionary<string, Node> nodeMap = new();

        /// <summary>
        /// A map to find a ILayoutNode (fast) by its ID
        /// </summary>
        private readonly Dictionary<string, ILayoutNode> iLayoutNodeMap = new();

        /// <summary>
        /// The layout of the last revision in the evolution. Can be null.
        /// </summary>
        private IncrementalTreeMapLayout oldLayout;

        /// <summary>
        /// Property for <see cref="oldLayout"/>
        /// </summary>
        /// <exception cref="ArgumentException">throws exception if the set <see cref="IIncrementalNodeLayout"/>
        /// is not a <see cref="IncrementalTreeMapLayout"/>.</exception>
        public IIncrementalNodeLayout OldLayout
        {
            set
            {
                if (value is IncrementalTreeMapLayout layout)
                {
                    this.oldLayout = layout;
                }
                else
                {
                    throw new ArgumentException(
                        "Predecessor of IncrementalTreeMapLayout was not a IncrementalTreeMapLayout.");
                }
            }
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector2 rectangle)
        {
            List<ILayoutNode> layoutNodesList = layoutNodes.ToList();
            if (!layoutNodesList.Any())
            {
                throw new ArgumentException("No nodes to be laid out.");
            }

            Roots = LayoutNodes.GetRoots(layoutNodesList);
            InitNodes(rectangle);
            CalculateLayout(Roots, new Rectangle(x: -rectangle.x / 2.0f, z: -rectangle.y / 2.0f, rectangle.x, rectangle.y), groundLevel);
            return layoutResult;
        }

        /// <summary>
        /// Creates a <see cref="Node"/> for each <see cref="ILayoutNode"/>
        /// and sets the <see cref="Node.DesiredSize"/>.
        /// Fills the <see cref="nodeMap"/> and the <see cref="iLayoutNodeMap"/>.
        /// </summary>
        private void InitNodes(Vector2 rectangle)
        {
            float totalSize = Roots.Sum(InitNode);

            // adjust the absolute size to the rectangle of the layout
            float adjustFactor = (rectangle.x * rectangle.y) / totalSize;
            foreach (Node node in nodeMap.Values)
            {
                node.DesiredSize *= adjustFactor;
            }
        }

        /// <summary>
        /// Creates a <see cref="Node"/> for the given <see cref="ILayoutNode"/> <paramref name="node"/>
        /// and continues recursively with the children of the ILayoutNode <paramref name="node"/>.
        /// Extends both <see cref="nodeMap"/> and <see cref="iLayoutNodeMap"/> by the node.
        /// </summary>
        /// <param name="node">node of the layout</param>
        /// <returns>the absolute size of the node</returns>
        private float InitNode(ILayoutNode node)
        {
            Node newNode = new(node.ID);
            nodeMap.Add(node.ID, newNode);
            iLayoutNodeMap.Add(node.ID, node);

            if (node.IsLeaf)
            {
                // x and z lengths may differ; we need to consider the larger value
                float size = Mathf.Max(node.LocalScale.x, node.LocalScale.z);
                newNode.DesiredSize = size;
                return size;
            }
            else
            {
                float totalSize = node.Children().Sum(InitNode);
                newNode.DesiredSize = totalSize;
                return totalSize;
            }
        }

        /// <summary>
        /// Calculates the layout for <paramref name="siblings"/> so that they fit in <paramref name="rectangle"/>.
        /// Works recursively on the children of each sibling.
        /// Adds the actual layout to <see cref="layoutResult"/>
        /// </summary>
        /// <param name="siblings">nodes with same parent (or roots)</param>
        /// <param name="rectangle">area to place siblings</param>
        /// <param name="groundLevel">The y-coordindate of the ground where all nodes will be placed.</param>
        private void CalculateLayout(ICollection<ILayoutNode> siblings, Rectangle rectangle, float groundLevel)
        {
            List<Node> nodes = siblings.Select(n => nodeMap[n.ID]).ToList();
            // check if the old layout can be used to lay out siblings.
            if (oldLayout == null
                || NumberOfOccurrencesInOldGraph(nodes) <= 4
                || ParentsInOldGraph(nodes).Count != 1)
            {
                Dissect.Apply(nodes, rectangle);
            }
            else
            {
                ApplyIncrementalLayout(nodes, rectangle);
            }

            AddToLayout(nodes, groundLevel);

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count <= 0)
                {
                    continue;
                }

                Rectangle childRectangle = nodeMap[node.ID].Rectangle;
                CalculateLayout(children, childRectangle, groundLevel);
            }
        }

        /// <summary>
        /// Calculates a stable layout for <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">nodes to be laid out</param>
        /// <param name="rectangle">rectangle in which the nodes should be laid out</param>
        private void ApplyIncrementalLayout(List<Node> nodes, Rectangle rectangle)
        {
            // oldNodes are not only the siblings that are in the old graph and in the new one,
            // but all siblings in old graph. Note that there is exactly one single parent (because of the if-clause),
            // but this parent can be null if children == roots
            ILayoutNode oldILayoutParent = ParentsInOldGraph(nodes).First();
            ICollection<ILayoutNode> oldILayoutSiblings =
                oldILayoutParent == null ? oldLayout.Roots : oldILayoutParent.Children();
            List<Node> oldNodes = oldILayoutSiblings.Select(n => oldLayout.nodeMap[n.ID]).ToList();

            SetupNodeLists(nodes, oldNodes,
                out List<Node> workWith,
                out List<Node> nodesToBeDeleted,
                out List<Node> nodesToBeAdded);

            Rectangle oldRectangle = IncrementalTreeMap.Utils.CreateParentRectangle(oldNodes);
            IncrementalTreeMap.Utils.TransformRectangles(workWith,
                oldRectangle: oldRectangle,
                newRectangle: rectangle);

            foreach (Node obsoleteNode in nodesToBeDeleted)
            {
                LocalMoves.DeleteNode(obsoleteNode);
                workWith.Remove(obsoleteNode);
            }

            CorrectAreas.Correct(workWith, settings);
            foreach (Node nodeToBeAdded in nodesToBeAdded)
            {
                LocalMoves.AddNode(workWith, nodeToBeAdded);
                workWith.Add(nodeToBeAdded);
            }

            CorrectAreas.Correct(workWith, settings);

            LocalMoves.LocalMovesSearch(workWith, settings);
        }

        /// <summary>
        /// Calculates 3 lists of <see cref="Node"/>s to transform stepwise the old layout
        /// to a new one.
        /// </summary>
        /// <param name="nodes">siblings from the current layout</param>
        /// <param name="oldNodes">corresponding siblings from the old layout</param>
        /// <param name="workWith">a copy of the old layout with nodes from the new one</param>
        /// <param name="nodesToBeDeleted">artificial nodes with no equivalent ILayoutNode, part of workWith</param>
        /// <param name="nodesToBeAdded">nodes that are not in workWith, but in nodes</param>
        private static void SetupNodeLists(
            List<Node> nodes,
            List<Node> oldNodes,
            out List<Node> workWith,
            out List<Node> nodesToBeDeleted,
            out List<Node> nodesToBeAdded)
        {
            //  [         oldNodes            ]--------------   <- nodes of OLD layout
            //  --------------[              nodes          ]   <- nodes of new layout
            //  [ toBeDeleted ]---------------[  toBeAdded  ]   <- nodes of new layout
            //  [         workWith            ]--------------   <- nodes of new layout
            //                                                     designed to be changed over time to nodes

            // get nodes from old layout and copy their rectangles
            // setup workWith and nodesToBeDeleted
            workWith = new List<Node>();
            nodesToBeDeleted = new List<Node>();
            foreach (Node oldNode in oldNodes)
            {
                Node newNode = nodes.Find(x => x.ID.Equals(oldNode.ID));
                if (newNode == null)
                {
                    // create an artificial node that has no corresponding ILayoutNode in this layout.
                    // they are designed to be deleted but it's necessary to copy the layout of oldNodes.
                    newNode = new Node(oldNode.ID);
                    nodesToBeDeleted.Add(newNode);
                }

                workWith.Add(newNode);
                newNode.Rectangle = oldNode.Rectangle.Clone();
            }

            IncrementalTreeMap.Utils.CloneSegments(
                from: oldNodes,
                to: workWith.ToDictionary(n => n.ID, n => n));

            List<Node> workWithAlias = workWith;
            nodesToBeAdded = nodes.Where(n => !workWithAlias.Contains(n)).ToList();
        }

        /// <summary>
        /// Returns a collection of all nodes of <see cref="oldLayout"/> that are parent to a node
        /// with the same id as a node in <paramref name="nodes"/>. The result will contain null
        /// if there is a root in the old layout with an equivalent node in <paramref name="nodes"/>
        /// </summary>
        /// <param name="nodes">nodes in this graph</param>
        /// <returns>Collection of parent nodes from <see cref="oldLayout"/>.</returns>
        private ICollection<ILayoutNode> ParentsInOldGraph(IEnumerable<Node> nodes)
        {
            Assert.IsNotNull(oldLayout);
            HashSet<ILayoutNode> parents = new();
            foreach (Node node in nodes)
            {
                if (oldLayout.iLayoutNodeMap.TryGetValue(node.ID, out ILayoutNode oldNode))
                {
                    parents.Add(oldNode.Parent);
                }
            }

            return parents;
        }

        /// <summary>
        /// The number of nodes that are also present in <see cref="oldLayout"/>.
        /// </summary>
        /// <param name="nodes">the nodes to look up</param>
        /// <returns>The number of occurrences in the last graph.</returns>
        private int NumberOfOccurrencesInOldGraph(IEnumerable<Node> nodes)
        {
            Assert.IsNotNull(oldLayout);
            return nodes.Count(n => oldLayout.iLayoutNodeMap.ContainsKey(n.ID));
        }

        /// <summary>
        /// Adds the result of the layout calculation to <see cref="layoutResult"/>.
        /// Applies padding to the result.
        /// </summary>
        /// <param name="nodes">nodes with calculated layout</param>
        /// <param name="groundLevel">The y-coordindate of the ground where all nodes will be placed.</param>
        private void AddToLayout(IEnumerable<Node> nodes, float groundLevel)
        {
            foreach (Node node in nodes)
            {
                float absolutePadding = settings.PaddingMm / 1000;
                Rectangle rectangle = node.Rectangle;
                ILayoutNode layoutNode = iLayoutNodeMap[node.ID];

                if (rectangle.Width - absolutePadding <= 0 ||
                    rectangle.Depth - absolutePadding <= 0)
                {
                    absolutePadding = 0;
                }

                Vector3 position = new Vector3(
                    (float)(rectangle.X + rectangle.Width / 2.0d),
                    groundLevel,
                    (float)(rectangle.Z + rectangle.Depth / 2.0d));
                Vector3 scale = new Vector3(
                    (float)(rectangle.Width - absolutePadding),
                    layoutNode.LocalScale.y,
                    (float)(rectangle.Depth - absolutePadding));

                layoutResult[layoutNode] = new NodeTransform(position, scale);
            }
        }
    }
}
