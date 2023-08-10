using SEE.DataModel.DG;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Layout.NodeLayouts.IncrementalTreeMap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Node = SEE.Layout.NodeLayouts.IncrementalTreeMap.Node;

namespace SEE.Layout.NodeLayouts
{
    /// <summary>
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
            this.width = width;
            this.depth = depth;
        }

        /// <summary>
        /// The width of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float width;

        /// <summary>
        /// The depth of the rectangle in which to place all nodes in Unity units.
        /// </summary>
        private readonly float depth;

        /// <summary>
        /// The node layout we compute as a result.
        /// </summary>
        private readonly Dictionary<ILayoutNode, NodeTransform> layout_result  = new Dictionary<ILayoutNode, NodeTransform>();
        private readonly Dictionary<string,Node>                NodeMap       = new Dictionary<string, Node>();
        private readonly Dictionary<string,ILayoutNode>         ILayoutNodeMap = new Dictionary<string, ILayoutNode>();
        
        private IncrementalTreeMapLayout oldLayout;

        public IIncrementalNodeLayout OldLayout
        {   set 
            {
                if(value is IncrementalTreeMapLayout layout)
                {
                    this.oldLayout = layout;
                }
                else
                {
                    this.oldLayout = null;
                    Debug.LogWarning("Incremental layout was not of same type");
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
                    layout_result[gameNode] = new NodeTransform(Vector3.zero,
                        new Vector3(width, gameNode.LocalScale.y, depth));
                    break;
                }
                default:
                {
                    this.roots = LayoutNodes.GetRoots(layoutNodeList);
                    InitTNodes();
                    CalculateLayout();
                    break;
                }
            }
            return layout_result;
        }    

        private void InitTNodes()
        {
            float totalLocalScale = 0;
            foreach(ILayoutNode node in roots)
            {
                totalLocalScale += InitTNode(node);
            }
            // adjust size 
            float adjustFactor = (width*depth) / totalLocalScale;
            foreach(var node in NodeMap.Values)
            {
                node.Size *= adjustFactor;
            }
        }
        private float InitTNode(ILayoutNode node)
        {
            if (node.IsLeaf)
            {
                // a leaf
                Vector3 size = node.LocalScale;
                // x and z lengths may differ; we need to consider the larger value
                float result = Mathf.Max(size.x, size.z);
                Node newTNode = new Node(node.ID);
                newTNode.Size = result;
                NodeMap.Add(node.ID, newTNode);
                ILayoutNodeMap.Add(node.ID,node);
                return result;
            }
            else
            {
                Node newTNode = new Node(node.ID);
                NodeMap.Add(node.ID, newTNode);
                ILayoutNodeMap.Add(node.ID,node);
                float totalSize = 0.0f;
                foreach (ILayoutNode child in node.Children())
                {
                    totalSize += InitTNode(child);
                }
                newTNode.Size = totalSize;
                return totalSize;
            }
        }

        /// <summary>
        /// Adds positioning and scales to <see cref="layout_result"/> for all root nodes (nodes with no parent)
        /// within a rectangle whose center position is Vector3.zero and whose width and depth is
        /// as specified by the constructor call. This function is then called recursively for the
        /// children of each root (until leaves are reached).
        /// </summary>
        private void CalculateLayout()
        {
            // Our "logical" rectangle in which to put the whole treemap is assumed to have its
            // center at Vector3.zero here. <see cref="CalculateLayout(ICollection{ILayoutNode}, float, float, float, float)"/>
            // assumes the rectangle's location be specified by its left front corner.
            // Hence, we need to transform the center of the "logical" rectangle to the left front
            // corner of the rectangle by -width/2 and -depth/2, respectively.
            Rectangle rectangle = new Rectangle(x: -width / 2.0f, z: -depth / 2.0f, width, depth);
            if (roots.Count == 1)
            {
                ILayoutNode root = roots[0];
                Assert.AreEqual(root.AbsoluteScale, root.LocalScale);
                layout_result[root] = new NodeTransform(Vector3.zero,
                    new Vector3(width, root.LocalScale.y, depth));
                CalculateLayout(root.Children(), rectangle);
            }
            else
            {
                CalculateLayout(roots,rectangle);
            }
        }

        private void CalculateLayout(ICollection<ILayoutNode> siblings,Rectangle rectangle)
        {
            // GetNodes can be done before if then else
            if(    this.oldLayout == null
                || NumberOfOccurrencesInOldGraph(siblings) <= 4
                || ParentsInOldGraph(siblings).Count != 1)
            {
                IList<Node> nodes = GetNodes(siblings);
                Dissect.Apply(rectangle, nodes);
            }
            else
            {
                // here can be done some improvement

                //  [         oldTNodes           ]--------------   <- nodes of old Layout, do not edit them
                //  [         workWith            ]--------------   <- nodes of new Layout, designed to be changed over time to newTNodes
                //  --------------[           newTNodes         ]   <- nodes of new Layout 
                //  [ toBeDeleted ]---------------[  toBeAdded  ]   <- nodes of new Layout

                // not only the siblings that are in the old graph and in the new one, but all siblings in old graph
                // note that there is exact one single parent (because of if-clause), but can be null if children == roots
                ILayoutNode oldILayoutParent = ParentsInOldGraph(siblings).First();
                ICollection<ILayoutNode> oldILayoutSiblings = oldILayoutParent == null ? oldLayout.roots : oldILayoutParent.Children();
                IList<Node> oldTNodes = oldLayout.GetNodes(oldILayoutSiblings);
                IList<Node> newTNodes = GetNodes(siblings);

                IList<Node> workWith          = new List<Node>();
                IList<Node> nodesToBeDeleted  = new List<Node>();
                IList<Node> nodesToBeAdded    = new List<Node>();

                // get nodes form old layout .. over take their rectangles
                foreach(var oldTNode in oldTNodes)
                {
                    Node newTNode = ((List<Node>) newTNodes).Find(x => x.ID.Equals(oldTNode.ID));
                    if(newTNode == null)
                    {   
                        newTNode = new Node(oldTNode.ID);
                        nodesToBeDeleted.Add(newTNode);
                        workWith.Add(newTNode);
                    }
                    else
                    {
                        workWith.Add(newTNode);
                    }
                    newTNode.Rectangle = (Rectangle) oldTNode.Rectangle.Clone();
                }

                foreach(var newTNode in newTNodes)
                {
                    if( !workWith.Contains(newTNode))
                    {
                        nodesToBeAdded.Add(newTNode);
                    }
                }

                // get segments from old layout
                var oldSegments = oldTNodes.SelectMany(n => n.SegmentsDictionary().Values).ToHashSet();
                foreach(var segment in oldSegments)
                {
                    Segment newSegment = new Segment(segment.IsConst, segment.IsVertical);
                    foreach(var oldTNode in segment.Side1Nodes)
                    {
                        Node newNode = workWith.First(x => x.ID == oldTNode.ID);
                        newNode.RegisterSegment(newSegment, newSegment.IsVertical ? Direction.Right : Direction.Upper);
                    }
                    foreach(var oldTNode in segment.Side2Nodes)
                    {
                        Node newNode = workWith.First(x => x.ID == oldTNode.ID);
                        newNode.RegisterSegment(newSegment, newSegment.IsVertical ? Direction.Left : Direction.Lower);
                    }
                }


                Rectangle oldRectangle = oldLayout.GetParentTRectangle(oldTNodes[0]);
                
                IncrementalTreeMap.Utils.TransformRectangles(workWith, 
                    oldRectangle: oldRectangle, 
                    newRectangle: rectangle);

                foreach(var obsoleteNode in nodesToBeDeleted)
                {
                    LocalMoves.DeleteNode(obsoleteNode);
                    workWith.Remove(obsoleteNode);
                    CheckConsistent(workWith);
                }
                CorrectAreas.Correct(workWith);
                CheckConsistent(workWith);
                foreach(var nodeToBeAdded in nodesToBeAdded)
                {
                    LocalMoves.AddNode(workWith,nodeToBeAdded);
                    workWith.Add(nodeToBeAdded);
                    CheckConsistent(workWith);
                }
                CheckEqualNodeSets(workWith, newTNodes);

                CorrectAreas.Correct(workWith);
                LocalMoves.IncreaseAspectRatioWithLocalMoves(workWith.ToList(), Parameters.NumberOfLocalMoves);
                CheckConsistent(workWith);
            }

            AddToLayout(GetNodes(siblings));

            foreach (ILayoutNode node in siblings)
            {
                ICollection<ILayoutNode> children = node.Children();
                if (children.Count > 0)
                {
                    // Note: nodeTransform.position is the center position, while
                    // CalculateLayout assumes co-ordinates x and z as the left front corner

                    Assert.AreEqual(node.AbsoluteScale, node.LocalScale);
                    Rectangle childRectangle = NodeMap[node.ID].Rectangle;
                    CalculateLayout(children, childRectangle);
                }
            }
        }

        private ICollection<ILayoutNode> ParentsInOldGraph(ICollection<ILayoutNode> nodes)
        {
            HashSet<ILayoutNode> parents = new HashSet<ILayoutNode>();
            foreach(ILayoutNode node in nodes)
            {
                if(oldLayout.ILayoutNodeMap.TryGetValue(node.ID, out var oldNode))
                {
                    parents.Add(oldNode.Parent);
                }
            }
            return parents;
        }

        private int NumberOfOccurrencesInOldGraph(ICollection<ILayoutNode> nodes)
        {
            int counter = 0;
            foreach (ILayoutNode node in nodes)
            {
                counter += oldLayout.ILayoutNodeMap.ContainsKey(node.ID) ? 1 : 0;
            }
            return counter;
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

        private IList<Node> GetNodes(ICollection<ILayoutNode> layoutNodes)
        {
            List<Node> result = new List<Node>();
            foreach( ILayoutNode layoutNode in layoutNodes)
            {
                result.Add(this.NodeMap[layoutNode.ID]);
            }
            return result;
        }
        
        private void AddToLayout (IList<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                var layoutNode = ILayoutNodeMap[node.ID];
                Vector3 position = new Vector3(
                    (float)(node.Rectangle.x + node.Rectangle.width / 2.0d),
                    groundLevel,
                    (float)(node.Rectangle.z + node.Rectangle.depth / 2.0d));
                Vector3 scale = new Vector3(
                    node.Rectangle.width - 2 * Parameters.Padding > 0 ? 
                        (float) (node.Rectangle.width - 2 * Parameters.Padding) : (float) node.Rectangle.width,
                    layoutNode.LocalScale.y,
                    node.Rectangle.depth - 2 * Parameters.Padding > 0 ? 
                        (float) (node.Rectangle.depth - 2 * Parameters.Padding) : (float) node.Rectangle.depth);
                layout_result[layoutNode] = new NodeTransform(position, scale);
            }
        }
        
        private Rectangle GetParentTRectangle(Node node)
        {
            var layoutNode = ILayoutNodeMap[node.ID];
            Rectangle result;
            try
            {
                var parentTNode = NodeMap[layoutNode.Parent.ID];
                result = (Rectangle) parentTNode.Rectangle.Clone();
            }
            catch
            {   
                result = new Rectangle(-.5f * width,-.5f * depth, width, depth);
            }
            return result;
        }

        private void CheckConsistent(IList<Node> nodes)
        {
            foreach(var node in nodes)
            {
                var segs = node.SegmentsDictionary(); 
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    var seg = segs[dir];
                    Assert.IsNotNull(seg);
                    if(seg.IsConst)
                    {
                        Assert.IsTrue(seg.Side1Nodes.Count == 0 || seg.Side2Nodes.Count == 0);
                    }
                    else
                    {
                        Assert.IsTrue(seg.Side1Nodes.Count != 0 && seg.Side2Nodes.Count != 0);
                    }
                    if(dir == Direction.Left || dir == Direction.Right)
                    {
                        Assert.IsTrue(seg.IsVertical);
                    }
                    else
                    {
                        Assert.IsTrue(!seg.IsVertical);
                    }

                    if(dir == Direction.Left)
                    {
                        foreach(Node neighborNode in seg.Side1Nodes)
                        {
                            Assert.IsTrue(neighborNode.SegmentsDictionary()[Direction.Right] == seg);
                        }
                    }
                    if(dir == Direction.Right)
                    {
                        foreach(Node neighborNode in seg.Side2Nodes)
                        {
                            Assert.IsTrue(neighborNode.SegmentsDictionary()[Direction.Left] == seg);
                        }
                    }
                    if(dir == Direction.Lower)
                    {
                        foreach(Node neighborNode in seg.Side1Nodes)
                        {
                            Assert.IsTrue(neighborNode.SegmentsDictionary()[Direction.Upper] == seg);
                        }
                    }
                    if(dir == Direction.Upper)
                    {
                        foreach(Node neighborNode in seg.Side2Nodes)
                        {
                            Assert.IsTrue(neighborNode.SegmentsDictionary()[Direction.Lower] == seg);
                        }
                    }

                }
                Assert.IsTrue(segs[Direction.Left].Side2Nodes.Contains(node));
                Assert.IsTrue(segs[Direction.Right].Side1Nodes.Contains(node));
                Assert.IsTrue(segs[Direction.Lower].Side2Nodes.Contains(node));
                Assert.IsTrue(segs[Direction.Upper].Side1Nodes.Contains(node));

                Assert.IsTrue(node.Rectangle.width > 0);
                Assert.IsTrue(node.Rectangle.depth > 0);
            }
        }

        private void CheckEqualNodeSets(IList<Node> nodes1, IList<Node> nodes2)
        {
            foreach (var node in nodes1)
            {
                Assert.IsTrue(nodes2.Contains(node));
            }

            foreach (var node in nodes2)
            {
                Assert.IsTrue(nodes1.Contains(node));
            }
        }
    }
}
