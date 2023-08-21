using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
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
    /// Incremental Tree Map Layout is a Tree Map Layout,
    /// that can guarantee stability in the layouts over the series of graphs in the evolution
    /// while not neglecting the aspect of visual quality either.
    /// </summary>
    public class IncrementalTreeMapLayout : HierarchicalNodeLayout, IIncrementalNodeLayout
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level</param>
        /// <param name="width">width of the rectangle in which to place all nodes in Unity units</param>
        /// <param name="depth">width of the rectangle in which to place all nodes in Unity units</param>
        /// <param name="settings">the settings for the layout</param>
        public IncrementalTreeMapLayout(float groundLevel,
            float width,
            float depth,
            IncrementalTreeMapSetting settings)
            : base(groundLevel)
        {
            name = "IncrementalTreeMap";
            _width = width;
            _depth = depth;
            _settings = settings;
        }

        /// <summary>
        /// The adjustable parameters for the layout.
        /// </summary>
        private readonly IncrementalTreeMapSetting _settings;

        /// <summary>
        /// The width of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float _width;

        /// <summary>
        /// The depth of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float _depth;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private readonly Dictionary<ILayoutNode, NodeTransform> _layoutResult = new();

        /// <summary>
        /// A map to find a node (fast) by its ID
        /// </summary>
        private readonly Dictionary<string, Node> _nodeMap = new();

        /// <summary>
        /// A map to find a ILayoutNode (fast) by its ID
        /// </summary>
        private readonly Dictionary<string, ILayoutNode> _iLayoutNodeMap = new();

        /// <summary>
        /// The layout of the last revision in the evolution. Can be null.
        /// </summary>
        private IncrementalTreeMapLayout _oldLayout;

        public IIncrementalNodeLayout OldLayout
        {
            set
            {
                if (value is IncrementalTreeMapLayout layout)
                {
                    this._oldLayout = layout;
                }
                else
                {
                    this._oldLayout = null;
                    Debug.LogWarning("Incremental layout of last revision is not from same type " +
                                     "as the layout for this revision");
                }
            }
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes)
        {
            var layoutNodesList = layoutNodes.ToList();
            if (!layoutNodesList.Any()) throw new ArgumentException("No nodes to be laid out.");
            this.roots = LayoutNodes.GetRoots(layoutNodesList);
            InitTNodes();
            Rectangle rectangle = new Rectangle(x: -_width / 2.0f, z: -_depth / 2.0f, _width, _depth);
            CalculateLayout(roots, rectangle);
            return _layoutResult;
        }

        /// <summary>
        /// Creates and a <see cref="Node"/> for each <see cref="ILayoutNode"/>
        /// and sets the wanted <see cref="Node.Size"/>.
        /// Fills the <see cref="_nodeMap"/> and the <see cref="_iLayoutNodeMap"/>.
        /// </summary>
        private void InitTNodes()
        {
            float totalSize = 0;
            foreach (ILayoutNode node in roots)
            {
                totalSize += InitTNode(node);
            }

            // adjust the absolute size to the rectangle of the layout
            float adjustFactor = (_width * _depth) / totalSize;
            foreach (var node in _nodeMap.Values)
            {
                node.Size *= adjustFactor;
            }
        }

        /// <summary>
        /// Creates a <see cref="Node"/> for the given <see cref="ILayoutNode"/> <paramref name="node"/>
        /// and continue recursively with the children of the ILayoutNode <paramref name="node"/>.
        /// Extend both <see cref="_nodeMap"/> and <see cref="_iLayoutNodeMap"/> by the node.
        /// </summary>
        /// <param name="node">node of the layout</param>
        /// <returns>the absolute size of the node</returns>
        private float InitTNode(ILayoutNode node)
        {
            Node newNode = new Node(node.ID);
            _nodeMap.Add(node.ID, newNode);
            _iLayoutNodeMap.Add(node.ID, node);

            if (node.IsLeaf)
            {
                // x and z lengths may differ; we need to consider the larger value
                float size = Mathf.Max(node.LocalScale.x, node.LocalScale.z);
                newNode.Size = size;
                return size;
            }
            else
            {
                var totalSize = node.Children().Sum(InitTNode);
                newNode.Size = totalSize;
                return totalSize;
            }
        }

        /// <summary>
        /// Calculates the layout for <paramref name="siblings"/> so that they fit in <paramref name="rectangle"/>.
        /// Works recursively on the children of each sibling.
        /// Adds the actual layout to <see cref="_layoutResult"/>
        /// </summary>
        /// <param name="siblings">nodes with same parent (or roots)</param>
        /// <param name="rectangle">area to place siblings</param>
        private void CalculateLayout(ICollection<ILayoutNode> siblings, Rectangle rectangle)
        {
            var nodes = siblings.Select(n => _nodeMap[n.ID]).ToList();
            // check if the old layout can be used for to lay out siblings.
            if (_oldLayout == null
                || NumberOfOccurrencesInOldGraph(nodes) <= 4
                || ParentsInOldGraph(nodes).Count != 1)
            {
                Dissect.Apply(nodes, rectangle);
            }
            else
            {
                ApplyIncrementalLayout(nodes, rectangle);
            }

            AddToLayout(nodes);

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count <= 0) continue;
                Rectangle childRectangle = _nodeMap[node.ID].Rectangle;
                CalculateLayout(children, childRectangle);
            }
        }

        /// <summary>
        /// Calculates a stable layout for <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">nodes to be laid out</param>
        /// <param name="rectangle">rectangle in that the nodes should be laid out</param>
        private void ApplyIncrementalLayout(List<Node> nodes, Rectangle rectangle)
        {
            // oldNodes are not only the siblings that are in the old graph and in the new one,
            // but all siblings in old graph. Note that there is exact one single parent (because of if-clause),
            // but this parent can be null if children == roots
            var oldILayoutParent = ParentsInOldGraph(nodes).First();
            var oldILayoutSiblings = oldILayoutParent == null ? _oldLayout.roots : oldILayoutParent.Children();
            var oldNodes = oldILayoutSiblings.Select(n => _oldLayout._nodeMap[n.ID]).ToList();

            SetupNodeLists(nodes, oldNodes,
                out var workWith,
                out var nodesToBeDeleted,
                out var nodesToBeAdded);

            Rectangle oldRectangle = IncrementalTreeMap.Utils.CreateParentRectangle(oldNodes);
            IncrementalTreeMap.Utils.TransformRectangles(workWith,
                oldRectangle: oldRectangle,
                newRectangle: rectangle);

            foreach (var obsoleteNode in nodesToBeDeleted)
            {
                LocalMoves.DeleteNode(obsoleteNode);
                workWith.Remove(obsoleteNode);
            }

            CorrectAreas.Correct(workWith, _settings);
            foreach (var nodeToBeAdded in nodesToBeAdded)
            {
                LocalMoves.AddNode(workWith, nodeToBeAdded);
                workWith.Add(nodeToBeAdded);
            }

            CorrectAreas.Correct(workWith, _settings);

            LocalMoves.LocalMovesSearch(workWith, _settings);
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
        private void SetupNodeLists(
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

            // get nodes form old layout .. copy their rectangles
            // setup workWith and nodesToBeDeleted
            workWith = new List<Node>();
            nodesToBeDeleted = new List<Node>();
            foreach (var oldNode in oldNodes)
            {
                Node newNode = nodes.Find(x => x.ID.Equals(oldNode.ID));
                if (newNode == null)
                {
                    // create a artificial node, that has no corresponding ILayoutNode in this layout
                    // they are designed to be deleted but necessary to copy the layout of oldNodes
                    newNode = new Node(oldNode.ID);
                    nodesToBeDeleted.Add(newNode);
                }

                workWith.Add(newNode);
                newNode.Rectangle = oldNode.Rectangle.Clone();
            }

            IncrementalTreeMap.Utils.CloneSegments(
                from: oldNodes,
                to: workWith.ToDictionary(n => n.ID, n => n));

            var workWithAlias = workWith;
            nodesToBeAdded = nodes.Where(n => !workWithAlias.Contains(n)).ToList();
        }

        /// <summary>
        /// Collection all nodes of <see cref="_oldLayout"/> that are parent to a node,
        /// with same id as a node in <paramref name="nodes"/>. The result will contain null
        /// if there is a root in the old layout with a equivalent node in <paramref name="nodes"/>
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private ICollection<ILayoutNode> ParentsInOldGraph(IEnumerable<Node> nodes)
        {
            Assert.IsNotNull(_oldLayout);
            HashSet<ILayoutNode> parents = new();
            foreach (var node in nodes)
            {
                if (_oldLayout._iLayoutNodeMap.TryGetValue(node.ID, out var oldNode))
                {
                    parents.Add(oldNode.Parent);
                }
            }

            return parents;
        }

        /// <summary>
        /// The number of the nodes, that are also in the last layout present.
        /// </summary>
        /// <param name="nodes">the nodes to look up</param>
        /// <returns></returns>
        private int NumberOfOccurrencesInOldGraph(IEnumerable<Node> nodes)
        {
            Assert.IsNotNull(_oldLayout);
            return nodes.Sum(n => _oldLayout._iLayoutNodeMap.ContainsKey(n.ID) ? 1 : 0);
        }

        /// <summary>
        /// Adds the result of layout calculation to <see cref="_layoutResult"/>.
        /// Applies padding to the result.
        /// </summary>
        /// <param name="nodes">nodes with calculated layout</param>
        private void AddToLayout(IEnumerable<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                float absolutePadding = _settings.paddingMm / 1000;
                var rectangle = node.Rectangle;
                var layoutNode = _iLayoutNodeMap[node.ID];

                if (rectangle.Width - absolutePadding <= 0 ||
                    rectangle.Depth - absolutePadding <= 0)
                {
                    absolutePadding = 0;
                }

                var position = new Vector3(
                    (float)(rectangle.X + rectangle.Width / 2.0d),
                    groundLevel,
                    (float)(rectangle.Z + rectangle.Depth / 2.0d));
                var scale = new Vector3(
                    (float)(rectangle.Width - absolutePadding),
                    layoutNode.LocalScale.y,
                    (float)(rectangle.Depth - absolutePadding));

                _layoutResult[layoutNode] = new NodeTransform(position, scale);
            }
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout
        (ICollection<ILayoutNode> layoutNodes, ICollection<Edge> edges,
            ICollection<SublayoutLayoutNode> sublayouts)
        {
            throw new NotImplementedException();
        }

        public override bool UsesEdgesAndSublayoutNodes()
        {
            return false;
        }
    }
}
