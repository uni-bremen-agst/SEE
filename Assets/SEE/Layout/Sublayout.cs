// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Layout.Utils;
using UnityEngine;

namespace SEE.Layout
{
    public class Sublayout
    {
        /// <summary>
        /// the layout of the sublayout
        /// </summary>
        private readonly NodeLayoutKind nodeLayout;

        /// <summary>
        /// a map from every sublayout node to the corresponding gameobject
        /// </summary>
        private readonly ICollection<ILayoutNode> sublayoutNodes;

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
        ///  the sublayout node
        /// </summary>
        private readonly SublayoutLayoutNode sublayout;

        /// <summary>
        /// the graph
        /// </summary>
        private readonly Graph graph;

        /// <summary>
        /// A Mapping from ILayoutNodes to ILayoutSublayoutNodes
        /// </summary>
        private readonly Dictionary<ILayoutNode, ILayoutSublayoutNode> ILayout_to_CoseSublayoutNode = new Dictionary<ILayoutNode, ILayoutSublayoutNode>();

        /// <summary>
        /// abstract see city settings 
        /// </summary>
        public AbstractSEECity settings;

        /// <summary>
        /// the edges of the sublayout
        /// </summary>
        private readonly ICollection<Edge> edges = new List<Edge>();

        public Vector3 LayoutScale { get => layoutScale; set => layoutScale = value; }

        public Vector3 RootNodeRealScale { get => rootNodeRealScale; set => rootNodeRealScale = value; }

        public Vector3 LayoutOffset { get => layoutOffset; set => layoutOffset = value; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="sublayout">the sublayout node</param>
        /// <param name="groundLevel">the groundlevel for the nodes</param>
        /// <param name="graph">the underlying graph</param>
        /// <param name="settings">abstract see city settings</param>
        public Sublayout(SublayoutLayoutNode sublayout, float groundLevel, Graph graph, AbstractSEECity settings)
        {
            nodeLayout = sublayout.NodeLayout;
            this.groundLevel = groundLevel;
            this.sublayout = sublayout;
            this.graph = graph;
            this.settings = settings;

            if (sublayout.Node.Children().Count > 0)
            {
                sublayoutNodes = CalculateNodesForSublayout();

                foreach (ILayoutNode layoutNode in sublayoutNodes)
                {
                    ILayoutNode sublayoutNode = (layoutNode as ILayoutSublayoutNode).Node;
                    sublayoutNode.IsSublayoutNode = true;
                }
            }

            edges = graph.ConnectingEdges(sublayoutNodes);
            sublayout.Node.Sublayout = this;
        }


        /// <summary>
        /// Calculates all nodes needed for a sublayout with this graphs parent node as the sublayouts root node
        /// </summary>
        /// <returns>a collection with ILayoutSublayoutNodes</returns>
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
                sublayoutNodes.Add(new ILayoutSublayoutNode(sublayout.Node, ILayout_to_CoseSublayoutNode));

                // bei einem subsubLayout wird der root wieder hinzugefügt
                foreach (ILayoutNode node in sublayout.RemovedChildren)
                {
                    if (node.IsSublayoutRoot)
                    {
                        sublayoutNodes.Add(new ILayoutSublayoutNode(node, new List<ILayoutNode>(), true, node.Parent, node.Sublayout.layoutScale, ILayout_to_CoseSublayoutNode));
                    }
                }
            }
            return sublayoutNodes;
        }

        /// <summary>
        /// Converts a layoutNode to a layout node used for the sublayout calculation
        /// </summary>
        /// <param name="layoutNodes"></param>
        /// <returns>a collection with ILayoutSublayoutNodes</returns>
        private ICollection<ILayoutNode> ConvertToCoseSublayoutNodes(List<ILayoutNode> layoutNodes)
        {
            List<ILayoutNode> sublayoutNodes = new List<ILayoutNode>();
            layoutNodes.ForEach(layoutNode =>
            {
                sublayoutNodes.Add(new ILayoutSublayoutNode(layoutNode, ILayout_to_CoseSublayoutNode));
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
                ILayoutNode sublayoutNode = (layoutNode as ILayoutSublayoutNode).Node;
                NodeTransform transform = layout[layoutNode];

                Vector3 position = transform.position;
                Vector3 scale = transform.scale;

                sublayoutNode.RelativePosition = position;
                sublayoutNode.CenterPosition = position;
                sublayoutNode.LocalScale = scale;
                sublayoutNode.Rotation = transform.rotation;

                if (sublayoutNode.IsSublayoutRoot)
                {
                    if (sublayoutNode == sublayout.Node)
                    {
                        LayoutScale = scale;
                    }
                    else
                    {
                        foreach (ILayoutNode subNode in sublayoutNode.Sublayout.sublayoutNodes)
                        {
                            ILayoutNode subSubNode = (subNode as ILayoutSublayoutNode).Node;

                            subSubNode.Rotation = sublayoutNode.Rotation;

                            if (subSubNode != sublayoutNode)
                            {
                                subSubNode.SetOrigin();
                                subSubNode.RelativePosition = subSubNode.CenterPosition;

                                sublayoutNodes.Add(new ILayoutSublayoutNode(subSubNode, ILayout_to_CoseSublayoutNode));

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
                    ILayoutNode sublayoutNode = (layoutNode as ILayoutSublayoutNode).Node;

                    leftLowerCornerNode = new Vector2()
                    {
                        x = sublayoutNode.CenterPosition.x - sublayoutNode.LocalScale.x / 2,
                        y = sublayoutNode.CenterPosition.z + sublayoutNode.LocalScale.z / 2,
                    };

                    rightUpperCornerNode = new Vector2()
                    {
                        x = sublayoutNode.CenterPosition.x + sublayoutNode.LocalScale.x / 2,
                        y = sublayoutNode.CenterPosition.z - sublayoutNode.LocalScale.z / 2,
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
                    rootNodeRealScale = new Vector3(sublayout.Node.LocalScale.x, innerNodeHeight, sublayout.Node.LocalScale.z);
                    LayoutOffset = position - sublayout.Node.CenterPosition;
                }

                sublayout.Node.LocalScale = scale;
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

            nodes.ForEach(node =>
            {
                node.SetRelative(sublayout.Node);
            });
        }

        /// <summary>
        /// Calculates the sublayout positions 
        /// </summary>
        /// <returns>a mapping from iLayoutNode to the calcualted nodeTransform</returns>
        private Dictionary<ILayoutNode, NodeTransform> CalculateSublayout()
        {
            NodeLayout layout = CoseHelper.GetNodelayout(nodeLayout, groundLevel, NodeFactory.Unit, settings);
            innerNodeHeight = layout.InnerNodeHeight;
            if (layout.UsesEdgesAndSublayoutNodes())
            {
                return layout.Layout(sublayoutNodes, edges, new List<SublayoutLayoutNode>());
            }
            else
            {
                return layout.Layout(sublayoutNodes);
            }
        }
    }
}

