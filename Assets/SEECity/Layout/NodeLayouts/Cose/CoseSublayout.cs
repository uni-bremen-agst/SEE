using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System.Linq;
using System;
using SEE.Game;
using SEE.GO;
using static SEE.Game.AbstractSEECity;

namespace SEE.Layout
{
    public class CoseSublayout
    {
        /// <summary>
        /// the layout of the sublayout
        /// </summary>
        private readonly AbstractSEECity.NodeLayouts nodeLayout;

        /// <summary>
        /// the root of the sublayout
        /// </summary>
        private readonly CoseNode root;

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
        private readonly float innerNodeHeight;

        /// <summary>
        /// TODO
        /// </summary>
        private Vector3 layoutScale;

        /// <summary>
        /// TODO
        /// </summary>
        private Vector3 layoutPosition;

        /// <summary>
        /// TODO
        /// </summary>
        private Node parentNodeObject;

        private readonly NodeFactory leafNodeFactory;

        /// <summary>
        /// Indicates whether the sublayout contains only leaf nodes
        /// </summary>
        private readonly bool onlyLeaves;

        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<ILayoutNode, CoseNode> allNodes = new Dictionary<ILayoutNode, CoseNode>();

        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<ILayoutNode, CoseNode> removedChildren = new Dictionary<ILayoutNode, CoseNode>();

        public Vector3 LayoutScale { get => layoutScale; set => layoutScale = value; }
        public Vector3 LayoutPosition { get => layoutPosition; set => layoutPosition = value; }

        Dictionary<ILayoutNode, CoseSublayoutNode> ILayout_to_CoseSublayoutNode = new Dictionary<ILayoutNode, CoseSublayoutNode>();

        /// <summary>
        /// constructor 
        /// </summary>
        /// <param name="root">the root node</param>
        /// <param name="nodeMap">map between all nodes and gameobjects</param>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="innerNodeHeight">The height of objects (y co-ordinate) drawn for inner nodes.</param>
        /// <param name="allNodes">TODO</param>
        /// <param name="nodeLayout">TODO</param>
        /// <param name="removedChildren">TODO</param>
        public CoseSublayout(CoseNode root, float groundLevel, float innerNodeHeight, NodeLayouts nodeLayout, Dictionary<ILayoutNode, CoseNode> allNodes, Dictionary<ILayoutNode, CoseNode> removedChildren, NodeFactory leafNodeFactory)
        {
            this.nodeLayout = nodeLayout;
            this.root = root;
            this.groundLevel = groundLevel;
            this.innerNodeHeight = innerNodeHeight;
            this.allNodes = allNodes;
            this.removedChildren = removedChildren;
            this.leafNodeFactory = leafNodeFactory;
            onlyLeaves = OnlyLeaveNodes();

            if (root.Child != null)
            {
                sublayoutNodes = CalculateNodesForSublayout(onlyLeaves);

                foreach(ILayoutNode layoutNode in sublayoutNodes)
                {
                    CoseSublayoutNode sublayoutNode = layoutNode as CoseSublayoutNode;
                   
                    CoseNode coseNode = allNodes.ContainsKey(sublayoutNode.Node) ? allNodes[sublayoutNode.Node] : removedChildren[sublayoutNode.Node];
                    coseNode.SublayoutValues.IsSubLayoutNode = true; 
                }
            }

            //root.SublayoutValues.Sublayout = this;
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
                CoseSublayoutNode sublayoutNode = layoutNode as CoseSublayoutNode;
                NodeTransform transform = layout[layoutNode];

                Vector3 position = transform.position;
                Vector3 scale = transform.scale;

                CoseNode coseNode = allNodes.ContainsKey(sublayoutNode.Node) ? allNodes[sublayoutNode.Node] : removedChildren[sublayoutNode.Node];

                if (coseNode.IsLeaf() || removedChildren.ContainsKey(sublayoutNode.Node))
                {
                    if (nodeLayout != NodeLayouts.Treemap)
                    {
                        scale = new Vector3(coseNode.Scale.x, innerNodeHeight, coseNode.Scale.z);
                    } 
                }

                Vector3 pos = transform.position;
                coseNode.SetPositionScale(position, scale);
                pos.y += transform.scale.y / 2.0f;
                sublayoutNode.Node.CenterPosition = pos;
                sublayoutNode.Node.Scale = scale;
                sublayoutNode.Node.Rotation = transform.rotation;
                

                if (coseNode.SublayoutValues.IsSubLayoutRoot)
                {
                    if (coseNode == root)
                    {
                        LayoutScale = scale; 
                    }
                    else
                    {
                        // TODO reparieren
                        //coseNode.NodeObject.children = coseNode.SublayoutValues.removedChildren;

                        /*foreach (ILayoutNode subNode in coseNode.SublayoutValues.Sublayout.sublayoutNodes)
                        {
                            CoseSublayoutNode cSubNode = subNode as CoseSublayoutNode;
                            CoseNode cNode = allNodes.ContainsKey(cSubNode.Node) ? allNodes[cSubNode.Node] : removedChildren[cSubNode.Node];
                            // child notes from subsublayout 
                            if (cNode != coseNode)
                            {
                                cNode.SetOrigin(); // sub1 nodes total zu der neuen position von A_1_new setzen
                                cNode.SublayoutValues.relativeRect = cNode.rect;
                                cNode.SublayoutValues.SubLayoutRoot = root;

                                sublayoutNodes.Add(new CoseSublayoutNode(cSubNode.Node, ILayout_to_CoseSublayoutNode));

                                if (cNode.Child != null )
                                {
                                    CoseGraph child = cNode.Child;
                                    Rect bounds = cNode.rect; 
                                    child.Left = bounds.xMin;
                                    child.Top = bounds.yMin;
                                    child.Right = bounds.xMax;
                                    child.Bottom = bounds.yMax;
                                    child.UpdateBoundingRect();
                                }
                            } 
                        }*/
                        coseNode.SublayoutValues.IsSubLayoutRoot = false;
                    }
                }
            }

            // TODO nodelayout.isHierarchical()
            if (onlyLeaves || nodeLayout == NodeLayouts.EvoStreets)
            {
                double left = Mathf.Infinity;
                double right = Mathf.NegativeInfinity;
                double top = Mathf.Infinity;
                double bottom = Mathf.NegativeInfinity;
                double nodeLeft;
                double nodeRight;
                double nodeTop;
                double nodeBottom;

                foreach (ILayoutNode n in sublayoutNodes)
                {
                    CoseSublayoutNode cSubNode = n as CoseSublayoutNode;
                    CoseNode cNode = allNodes[cSubNode.Node];

                    nodeLeft = cNode.GetLeft();
                    nodeRight = cNode.GetRight();
                    nodeTop = cNode.GetTop();
                    nodeBottom = cNode.GetBottom();

                    if (left > nodeLeft)
                    {
                        left = nodeLeft;
                    }

                    if (right < nodeRight)
                    {
                        right = nodeRight;
                    }

                    if (top > nodeTop)
                    {
                        top = nodeTop;
                    }

                    if (bottom < nodeBottom)
                    {
                        bottom = nodeBottom;
                    }
                }

                double defaultMargin = CoseLayoutSettings.Graph_Margin;
                left -= defaultMargin;
                right += defaultMargin;
                top -= defaultMargin;
                bottom += defaultMargin;

                Rect boundingRect = new Rect((float)left, (float)top, (float)(right - left), (float)(bottom - top));
                Vector3 position = new Vector3(boundingRect.center.x, groundLevel, boundingRect.center.y);
                Vector3 scale = new Vector3(boundingRect.width, innerNodeHeight, boundingRect.height);
                // TODO das ist doch flasch oder?
                LayoutScale = new Vector3(root.Scale.x, innerNodeHeight, root.Scale.z);
                root.SublayoutValues.IsSubLayoutNode = true;
                root.SetPositionScale(position, scale);
            }

            SetCoseNodeToLayoutPosition();
        }

        /// <summary>
        /// sets the sublayout node position relativ to its root node
        /// </summary>
        private void SetCoseNodeToLayoutPosition()
        {
            List<CoseNode> nodes = new List<CoseNode>(allNodes.Values);
            nodes.AddRange(removedChildren.Values);
            nodes.Remove(root);

            nodes.ForEach(node => {
                node.SetPositionRelativ(root);
            });
        }

        /// <summary>
        /// Calculates the sublayout positions 
        /// </summary>
        /// <returns></returns>
        private Dictionary<ILayoutNode, NodeTransform> CalculateSublayout()
        {
            switch (nodeLayout)
            {
                case NodeLayouts.Manhattan:
                    return new ManhattanLayout(groundLevel, leafNodeFactory.Unit).Layout(sublayoutNodes);
                case NodeLayouts.FlatRectanglePacking:
                    return new RectanglePacker(groundLevel, leafNodeFactory.Unit).Layout(sublayoutNodes);
                case NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory.Unit).Layout(sublayoutNodes);
                case NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, 10.0f * leafNodeFactory.Unit, 10.0f * leafNodeFactory.Unit).Layout(sublayoutNodes);
                case NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel).Layout(sublayoutNodes);
                case NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel).Layout(sublayoutNodes);
                default:
                    throw new System.Exception("Unhandled node layout ");
            }
        }

        /// <summary>
        /// Calculates all nodes needed for a sublayout with this graphs parent node as the sublayouts root node
        /// </summary>
        /// <param name="onlyLeaves"></param>
        /// <returns></returns>
        public ICollection<ILayoutNode> CalculateNodesForSublayout(bool onlyLeaves)
        {
            List<CoseNode> nodesForLayout = new List<CoseNode>(allNodes.Values);
            nodesForLayout.Remove(root);
            ICollection<ILayoutNode> sublayoutNodes = ConvertToCoseSublayoutNodes(nodesForLayout);
            // TODO
            //nodesForLayout.ForEach(node => node.SublayoutValues.IsSubLayoutNode = true);

            if (onlyLeaves)
            {
                return sublayoutNodes;
            } else
            {
                sublayoutNodes.Add(new CoseSublayoutNode(root.NodeObject, ILayout_to_CoseSublayoutNode));

                // bei einem subsubLayout wird der root wieder hinzugefügt
                foreach (KeyValuePair<ILayoutNode, CoseNode> kvp in removedChildren)
                {
                    CoseNode node = kvp.Value;
                    ILayoutNode layoutNode = kvp.Key;
                    if (node.SublayoutValues.IsSubLayoutRoot)
                    {
                        //sublayoutNodes.Add(new CoseSublayoutNode(layoutNode, new List<ILayoutNode>(), true, layoutNode.Parent, node.SublayoutValues.Sublayout.layoutScale, ILayout_to_CoseSublayoutNode));
                    }
                }
            }
            return sublayoutNodes;
        }

        /// <summary>
        /// Calculates if the sublayout only contains leaf nodes
        /// </summary>
        /// <returns>only leave nodes</returns>
        private bool OnlyLeaveNodes()
        {
            return nodeLayout == NodeLayouts.Manhattan || nodeLayout == NodeLayouts.FlatRectanglePacking;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="layoutNodes"></param>
        /// <returns></returns>
        private ICollection<ILayoutNode> ConvertToCoseSublayoutNodes(List<CoseNode> layoutNodes)
        {
            List<ILayoutNode> sublayoutNodes = new List<ILayoutNode>();
            layoutNodes.ForEach( layoutNode =>
            {
                ILayoutNode node = layoutNode.NodeObject;
                sublayoutNodes.Add(new CoseSublayoutNode(node, ILayout_to_CoseSublayoutNode));
            });

            return sublayoutNodes;
        }
    }
}

