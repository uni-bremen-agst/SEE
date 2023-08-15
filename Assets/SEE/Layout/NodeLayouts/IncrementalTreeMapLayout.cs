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
using Object = UnityEngine.Object;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
    /// A node layout designed for the evolution setting.
    /// Incremental Tree Map Layout is a Tree Map Layout,
    /// that can guarantee stability in the layouts over the series of graphs in the evolution
    /// while not neglecting this aspect of visual quality either.
    /// </summary>
    public class IncrementalTreeMapLayout : HierarchicalNodeLayout, IIncrementalNodeLayout
    {
        /// <summary>
        /// Constructor. The width and depth are assumed to be in Unity units.
        /// </summary>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="width">width of the rectangle in which to place all nodes in Unity units</param>
        /// <param name="depth">width of the rectangle in which to place all nodes in Unity units</param>
        public IncrementalTreeMapLayout(float groundLevel,
            float width,
            float depth)
            : base(groundLevel)
        {
            name = "IncrementalTreeMap";
            this._width = width;
            this._depth = depth;

            // This is actually not a good solution to get the settings for the layout.
            // Because it only works reliably when the SEECityEvolution is the only AbstractSEECity in the scene.
            // However the architecture provides no good way to set the parameters of an layout. 
            _settings = Object.FindObjectOfType<AbstractSEECity>().NodeLayoutSettings.incrementalTreeMapSetting;
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
                                     "then the layout for this revision");
                }
            }
        }

        public override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes)
        {
            List<ILayoutNode> layoutNodeList = layoutNodes.ToList();
            switch (layoutNodeList.Count)
            {
                case 0:
                    throw new ArgumentException("No nodes to be laid out.");
                case 1:
                {
                    ILayoutNode gameNode = layoutNodeList.First();
                    _layoutResult[gameNode] = new NodeTransform(Vector3.zero,
                        new Vector3(_width, gameNode.LocalScale.y, _depth));
                    break;
                }
                default:
                {
                    this.roots = LayoutNodes.GetRoots(layoutNodeList);
                    InitTNodes();
                    Rectangle rectangle = new Rectangle(x: -_width / 2.0f, z: -_depth / 2.0f, _width, _depth);
                    CalculateLayout(roots, rectangle);
                    break;
                }
            }

            return _layoutResult;
        }

        /// <summary>
        /// Creates and a <see cref="Node"/> for each <see cref="ILayoutNode"/> in the layout
        /// and initiate it with the right <see cref="Node.Size"/>.
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
        /// Initiate a <see cref="Node"/> for the given <see cref="ILayoutNode"/> <paramref name="node"/>
        /// and continue recursively with the children of the ILayoutNode <paramref name="node"/>.
        /// Extend both <see cref="_nodeMap"/> and <see cref="_iLayoutNodeMap"/>.
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
            // the nodes of the 
            var nodes =  siblings.Select(n => _nodeMap[n.ID]).ToList();
            // check if the old layout can be used for to lay out siblings.
            if (_oldLayout == null
                || NumberOfOccurrencesInOldGraph(siblings) <= 4
                || ParentsInOldGraph(siblings).Count != 1)
            {
                // calculate a complete new layout
                Dissect.Apply(rectangle, nodes);
            }
            else
            {
                // use old layout and achieve stability

                // setup the different lists
                //  [         oldNodes            ]--------------   <- nodes of old Layout, do not edit them
                //  --------------[              nodes          ]   <- nodes of new Layout 
                //  [ toBeDeleted ]---------------[  toBeAdded  ]   <- nodes of new Layout
                //  [         workWith            ]--------------   <- nodes of new Layout,
                //                                                     designed to be changed over time to nodes

                // not only the siblings that are in the old graph and in the new one, but all siblings in old graph
                // note that there is exact one single parent (because of if-clause),
                // but can be null if children == roots
                var oldILayoutParent = ParentsInOldGraph(siblings).First();
                var oldILayoutSiblings = oldILayoutParent == null ? _oldLayout.roots : oldILayoutParent.Children();
                var oldNodes = oldILayoutSiblings.Select(n => _oldLayout._nodeMap[n.ID]).ToList();

                var workWith = new List<Node>();
                var nodesToBeDeleted = new List<Node>();
                var nodesToBeAdded = new List<Node>();

                // get nodes form old layout .. copy their rectangles
                // setup workWith and nodesToBeDeleted
                foreach (var oldNode in oldNodes)
                {
                    Node newNode = nodes.Find(x => x.ID.Equals(oldNode.ID));
                    if (newNode == null)
                    {
                        // create a artificial node, that has no corresponding ILayoutNode in this layout
                        // will be deleted later
                        newNode = new Node(oldNode.ID);
                        nodesToBeDeleted.Add(newNode);
                        workWith.Add(newNode);
                    }
                    else
                    {
                        workWith.Add(newNode);
                    }
                    newNode.Rectangle = (Rectangle)oldNode.Rectangle.Clone();
                }
                // get segments from old layout
                var oldSegments = oldNodes.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
                foreach (var segment in oldSegments)
                {
                    Segment newSegment = new Segment(segment.IsConst, segment.IsVertical);
                    foreach (var oldTNode in segment.Side1Nodes)
                    {
                        Node newNode = workWith.First(x => x.ID == oldTNode.ID);
                        newNode.RegisterSegment(newSegment, newSegment.IsVertical ? Direction.Right : Direction.Upper);
                    }

                    foreach (var oldTNode in segment.Side2Nodes)
                    {
                        Node newNode = workWith.First(x => x.ID == oldTNode.ID);
                        newNode.RegisterSegment(newSegment, newSegment.IsVertical ? Direction.Left : Direction.Lower);
                    }
                }
                // now workWith is equivalent to oldNodes / the nodes from the last layout

                // setup nodesToBeAdded
                foreach (var newTNode in nodes)
                {
                    if (!workWith.Contains(newTNode))
                    {
                        nodesToBeAdded.Add(newTNode);
                    }
                }
                Rectangle oldRectangle = _oldLayout.ParentRectangle(oldNodes.First());
                IncrementalTreeMap.Utils.TransformRectangles(workWith,
                    oldRectangle: oldRectangle,
                    newRectangle: rectangle);

                foreach (var obsoleteNode in nodesToBeDeleted)
                {
                    LocalMoves.DeleteNode(obsoleteNode);
                    workWith.Remove(obsoleteNode);
                    IncrementalTreeMap.Utils.CheckConsistent(workWith);
                }

                CorrectAreas.Correct(workWith, _settings);
                IncrementalTreeMap.Utils.CheckConsistent(workWith);
                foreach (var nodeToBeAdded in nodesToBeAdded)
                {
                    LocalMoves.AddNode(workWith, nodeToBeAdded);
                    workWith.Add(nodeToBeAdded);
                    IncrementalTreeMap.Utils.CheckConsistent(workWith);
                }

                IncrementalTreeMap.Utils.CheckEqualNodeSets(workWith, nodes);

                CorrectAreas.Correct(workWith, _settings);

                LocalMoves.LocalMovesSearch(workWith.ToList(), _settings);
                IncrementalTreeMap.Utils.CheckConsistent(workWith);
            }

            AddToLayout(nodes);

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count > 0)
                {
                    Assert.AreEqual(node.AbsoluteScale, node.LocalScale);
                    Rectangle childRectangle = _nodeMap[node.ID].Rectangle;
                    CalculateLayout(children, childRectangle);
                }
            }
        }

        /// <summary>
        /// List of all nodes of <see cref="_oldLayout"/> that are parent to a node,
        /// with same id than a node in <paramref name="nodes"/>
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private ICollection<ILayoutNode> ParentsInOldGraph(ICollection<ILayoutNode> nodes)
        {
            HashSet<ILayoutNode> parents = new HashSet<ILayoutNode>();
            foreach (ILayoutNode node in nodes)
            {
                if (_oldLayout._iLayoutNodeMap.TryGetValue(node.ID, out var oldNode))
                {
                    parents.Add(oldNode.Parent);
                }
            }
            return parents;
        }

        /// <summary>
        /// The number of nodes, that are also in the last layout present.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private int NumberOfOccurrencesInOldGraph(ICollection<ILayoutNode> nodes)
        {
            return nodes.Sum(n => _oldLayout._iLayoutNodeMap.ContainsKey(n.ID) ? 1 : 0);
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

        /// <summary>
        /// Adds the result of layout calculation to <see cref="_layoutResult"/>.
        /// </summary>
        /// <param name="nodes">nodes with calculated layout</param>
        private void AddToLayout(IEnumerable<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                float absolutePadding = _settings.paddingMm / 1000;
                Rectangle rectangle = node.Rectangle;
                var layoutNode = _iLayoutNodeMap[node.ID];
                
                if (rectangle.width - absolutePadding <= 0 ||
                    rectangle.depth - absolutePadding <= 0)
                {
                    absolutePadding = 0;
                }
                
                Vector3 position = new Vector3(
                    (float)(rectangle.x + rectangle.width / 2.0d),
                    groundLevel,
                    (float)(rectangle.z + rectangle.depth / 2.0d));
                Vector3 scale = new Vector3(
                        (float)(rectangle.width - absolutePadding),
                        layoutNode.LocalScale.y,
                        (float)(rectangle.depth - absolutePadding));
                
                _layoutResult[layoutNode] = new NodeTransform(position, scale);
            }
        }

        /// <summary>
        /// Returns a rectangle, in which the <paramref name="node"/> is placed.
        /// </summary>
        /// <param name="node">node with layouted parent</param>
        /// <returns>a new rectangle - no reference</returns>
        private Rectangle ParentRectangle(Node node)
        {
            var iLayoutNode = _iLayoutNodeMap[node.ID];
            Rectangle result;
            if (iLayoutNode.Parent == null)
            {
                result = new Rectangle(-.5f * _width, -.5f * _depth, _width, _depth);
            }
            else
            {
                var parentNode = _nodeMap[iLayoutNode.Parent.ID];
                result = (Rectangle) parentNode.Rectangle.Clone();
            }
            return result;
        }
    }
}