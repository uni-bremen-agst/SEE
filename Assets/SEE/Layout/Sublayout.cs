using SEE.DataModel;
using SEE.Game;
using SEE.GO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public class Sublayout
    {
        /// <summary>
        /// the layout of the sublayout
        /// </summary>
        private readonly NodeLayouts nodeLayout;

        /// <summary>
        /// a map from every sublayout node to the corresponding gameobject
        /// </summary>
        private ICollection<ILayoutNode> sublayoutNodes;

        /// <summary>
        /// the y co-ordinate setting the ground level; all nodes will be placed on this level
        /// </summary>
        private readonly float groundLevel;

        /// <summary>
        /// he height of objects (y co-ordinate) drawn for inner nodes
        /// </summary>
        private float innerNodeHeight;

        /// <summary>
        /// the scale of the calculated layout
        /// </summary>
        private Vector3 layoutScale;

        /// <summary>
        /// the layout offset
        /// </summary>
        private Vector3 layoutOffset;

        /// <summary>
        /// the "real" scale of the root node (is needed, if the nodelayout doenst enclose the inner nodes)
        /// </summary>
        private Vector3 rootNodeRealScale;

        /// <summary>
        /// the leaf node factory
        /// </summary>
        private readonly NodeFactory leafNodeFactory;

        /// <summary>
        ///  the sublayout node
        /// </summary>
        private readonly SublayoutLayoutNode sublayout;

        /// <summary>
        /// the graph
        /// </summary>
        private readonly Graph graph;

        public Vector3 LayoutScale { get => layoutScale; set => layoutScale = value; }

        public Vector3 RootNodeRealScale { get => rootNodeRealScale; set => rootNodeRealScale = value; }

        public Vector3 LayoutOffset { get => layoutOffset; set => layoutOffset = value; }

        public SublayoutLayoutNode SublayoutLayoutNode => sublayout;

        Dictionary<ILayoutNode, CoseSublayoutNode> ILayout_to_CoseSublayoutNode = new Dictionary<ILayoutNode, CoseSublayoutNode>();

        public AbstractSEECity settings;

        private ICollection<Edge> edges = new List<Edge>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sublayout">the sublayout node</param>
        /// <param name="groundLevel">the groundlevel for the nodes</param>
        /// <param name="leafNodeFactory">the leafnodefactory</param>
        /// <param name="graph">the underlying graph</param>
        public Sublayout(SublayoutLayoutNode sublayout, float groundLevel, NodeFactory leafNodeFactory, Graph graph, AbstractSEECity settings)
        {
            this.nodeLayout = sublayout.NodeLayout;
            this.groundLevel = groundLevel;  
            this.leafNodeFactory = leafNodeFactory;
            this.sublayout = sublayout;
            this.graph = graph;
            this.settings = settings;

            if (sublayout.Node.Children().Count > 0)
            {
                sublayoutNodes = CalculateNodesForSublayout();

                foreach(ILayoutNode layoutNode in sublayoutNodes)
                {
                    ILayoutNode sublayoutNode = (layoutNode as CoseSublayoutNode).Node;
                    sublayoutNode.IsSublayoutNode = true; 
                    // TODO setsublayoutRoot
                }
            }

            edges = graph.ConnectingEdges(sublayoutNodes);
            sublayout.Node.Sublayout = this;
        }


        /// <summary>
        /// Calculates all nodes needed for a sublayout with this graphs parent node as the sublayouts root node
        /// </summary>
        /// <returns></returns>
        public ICollection<ILayoutNode> CalculateNodesForSublayout()
        {
            List<ILayoutNode> nodesForLayout = new List<ILayoutNode>(sublayout.Nodes);
            nodesForLayout.Remove(sublayout.Node);
            ICollection<ILayoutNode> sublayoutNodes = ConvertToCoseSublayoutNodes(nodesForLayout);

            if (sublayout.NodeLayout.GetModel().OnlyLeaves)
            {
                return sublayoutNodes;
            }
            else
            {
                sublayoutNodes.Add(new CoseSublayoutNode(sublayout.Node,  ILayout_to_CoseSublayoutNode));

                // bei einem subsubLayout wird der root wieder hinzugefügt
                foreach (ILayoutNode node in sublayout.RemovedChildren)
                {
                    if (node.IsSublayoutRoot)
                    {
                        sublayoutNodes.Add(new CoseSublayoutNode(node, new List<ILayoutNode>(), true, node.Parent, node.Sublayout.layoutScale, ILayout_to_CoseSublayoutNode));
                    }
                }
            }
            return sublayoutNodes;
        }

        /// <summary>
        /// Converts a layoutNode to a layout node used for the sublayout calculation
        /// </summary>
        /// <param name="layoutNodes"></param>
        /// <returns></returns>
        private ICollection<ILayoutNode> ConvertToCoseSublayoutNodes(List<ILayoutNode> layoutNodes)
        {
            List<ILayoutNode> sublayoutNodes = new List<ILayoutNode>();
            layoutNodes.ForEach(layoutNode =>
            {
                sublayoutNodes.Add(new CoseSublayoutNode(layoutNode, ILayout_to_CoseSublayoutNode));
            });

            return sublayoutNodes;
        }

        /// <summary>
        /// Calculates and sets the nodes positions
        /// </summary>
        public void Layout()
        {
            Dictionary<ILayoutNode, NodeTransform> layout = CalculateSublayout();

            //root.NodeObject.Parent = parentNodeObject;

            foreach (ILayoutNode layoutNode in layout.Keys)
            {
                ILayoutNode sublayoutNode = (layoutNode as CoseSublayoutNode).Node;
                NodeTransform transform = layout[layoutNode];

                Vector3 position = transform.position;
                Vector3 scale = transform.scale;

                sublayoutNode.RelativePosition = position; 
                sublayoutNode.CenterPosition = position;
                sublayoutNode.Scale = scale;
                sublayoutNode.Rotation = transform.rotation;

                if (sublayoutNode.IsSublayoutRoot)
                {
                    if (sublayoutNode == sublayout.Node)
                    {
                        LayoutScale = scale;
                    }
                    else
                    {
                        // TODO reparieren
                        //coseNode.NodeObject.children = coseNode.SublayoutValues.removedChildren;

                        foreach (ILayoutNode subNode in sublayoutNode.Sublayout.sublayoutNodes)
                        {

                            ILayoutNode subSubNode = (subNode as CoseSublayoutNode).Node;

                            subSubNode.Rotation = sublayoutNode.Rotation;

                            if (subSubNode != sublayoutNode)
                            {
                                subSubNode.SetOrigin(); // sub1 nodes total zu der neuen position von A_1_new setzen
                                subSubNode.RelativePosition = subSubNode.CenterPosition;

                                sublayoutNodes.Add(new CoseSublayoutNode(subSubNode, ILayout_to_CoseSublayoutNode));

                            }
                        }
                        sublayoutNode.IsSublayoutRoot = false;
                    }
                }
            }

            if (!sublayout.NodeLayout.GetModel().InnerNodesEncloseLeafNodes)
            {
                Vector2 leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.NegativeInfinity);
                Vector2 rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.Infinity);

                Vector2 leftLowerCornerNode;
                Vector2 rightUpperCornerNode;

                foreach (ILayoutNode layoutNode in sublayoutNodes)
                {
                    ILayoutNode sublayoutNode = (layoutNode as CoseSublayoutNode).Node;

                    leftLowerCornerNode = new Vector2()
                    {
                        x = sublayoutNode.CenterPosition.x - sublayoutNode.Scale.x / 2,
                        y = sublayoutNode.CenterPosition.z + sublayoutNode.Scale.z / 2,
                    };

                    rightUpperCornerNode = new Vector2()
                    {
                        x = sublayoutNode.CenterPosition.x + sublayoutNode.Scale.x / 2,
                        y = sublayoutNode.CenterPosition.z - sublayoutNode.Scale.z / 2,
                    };


                    if (leftLowerCorner.x > leftLowerCornerNode.x)
                    {
                        leftLowerCorner.x = leftLowerCornerNode.x;
                    }

                    if (rightUpperCorner.x < rightUpperCornerNode.x)
                    {
                        rightUpperCorner.x = rightUpperCornerNode.x;
                    }

                    if (rightUpperCorner.y > rightUpperCornerNode.y)
                    {
                        rightUpperCorner.y = rightUpperCornerNode.y;
                    }

                    if (leftLowerCorner.y < leftLowerCornerNode.y)
                    {
                        leftLowerCorner.y = leftLowerCornerNode.y;
                    }
                }

                Vector3 scale = new Vector3
                {
                    x = rightUpperCorner.x - leftLowerCorner.x,
                    z = leftLowerCorner.y - rightUpperCorner.y
                };

                Vector3 position = new Vector3
                {
                    x = leftLowerCorner.x + scale.x / 2,
                    z = rightUpperCorner.y + scale.z / 2
                };

                LayoutScale = scale;

                if (!nodeLayout.GetModel().OnlyLeaves)
                {
                   rootNodeRealScale = new Vector3(sublayout.Node.Scale.x, innerNodeHeight, sublayout.Node.Scale.z);
                   LayoutOffset = position - sublayout.Node.CenterPosition;
                }

                sublayout.Node.Scale = scale;
                sublayout.Node.CenterPosition = position;
                sublayout.Node.IsSublayoutNode = true;
            }

            SetCoseNodeToLayoutPosition();
        }

        /// <summary>
        /// sets the sublayout node position relativ to its root node
        /// </summary>
        private void SetCoseNodeToLayoutPosition()
        {
            List<ILayoutNode> nodes = new List<ILayoutNode>(sublayout.Nodes);
            nodes.AddRange(sublayout.RemovedChildren);
            nodes.Remove(sublayout.Node);

            nodes.ForEach(node => {
                node.SetRelative(sublayout.Node);
            });
        }

        /// <summary>
        /// Calculates the sublayout positions 
        /// </summary>
        /// <returns></returns>
        private Dictionary<ILayoutNode, NodeTransform> CalculateSublayout()
        {
            NodeLayout layout = GetLayout();
            innerNodeHeight = layout.InnerNodeHeight;
            if (layout.UsesEdgesAndSublayoutNodes())
            {
                return layout.Layout(sublayoutNodes, edges, new List<SublayoutLayoutNode>());
            } else
            {
                return layout.Layout(sublayoutNodes);
            }
        }

        private NodeLayout GetLayout()
        {
            switch (nodeLayout)
            {
                case NodeLayouts.Manhattan:
                    return new ManhattanLayout(groundLevel, leafNodeFactory.Unit);
                case NodeLayouts.RectanglePacking:
                    return new RectanglePackingNodeLayout(groundLevel, leafNodeFactory.Unit);
                case NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory.Unit);
                case NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, 10.0f * leafNodeFactory.Unit, 10.0f * leafNodeFactory.Unit);
                case NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel);
                case NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel);
                case NodeLayouts.CompoundSpringEmbedder:
                    return new CoseLayout(groundLevel, settings);
                default:
                    throw new System.Exception("Unhandled node layout ");
            }
        }

        /// <summary>
        /// evalutates the sublayout 
        /// </summary>
        /// <returns></returns>
        private bool EvaluateSublayout()
        {
            Vector2 leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
            Vector2 rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

            foreach (ILayoutNode layoutNode in sublayoutNodes)
            {
                Vector3 extent = layoutNode.Scale / 2.0f;
                // Note: position denotes the center of the object
                Vector3 position = layoutNode.CenterPosition;
                {
                    // x co-ordinate of lower left corner
                    float x = position.x - extent.x;
                    if (x < leftLowerCorner.x)
                    {
                        leftLowerCorner.x = x;
                    }
                }
                {
                    // z co-ordinate of lower left corner
                    float z = position.z - extent.z;
                    if (z < leftLowerCorner.y)
                    {
                        leftLowerCorner.y = z;
                    }
                }
                {   // x co-ordinate of upper right corner
                    float x = position.x + extent.x;
                    if (x > rightUpperCorner.x)
                    {
                        rightUpperCorner.x = x;
                    }
                }
                {
                    // z co-ordinate of upper right corner
                    float z = position.z + extent.z;
                    if (z > rightUpperCorner.y)
                    {
                        rightUpperCorner.y = z;
                    }
                }
            }

            Measurements _ = new Measurements(sublayoutNodes, graph: graph, leftFrontCorner: leftLowerCorner, rightBackCorner: rightUpperCorner);

            return true; 
        }
    }
}

