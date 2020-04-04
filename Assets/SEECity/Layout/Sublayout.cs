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
        private readonly NodeFactory innerNodeFactory;

        private SublayoutLayoutNode sublayout;

        public Vector3 LayoutScale { get => layoutScale; set => layoutScale = value; }

        public SublayoutLayoutNode SublayoutLayoutNode => sublayout;

        Dictionary<ILayoutNode, CoseSublayoutNode> ILayout_to_CoseSublayoutNode = new Dictionary<ILayoutNode, CoseSublayoutNode>();

        public Sublayout(SublayoutLayoutNode sublayout, float groundLevel, NodeFactory innerNodeFactory)
        {
            this.nodeLayout = sublayout.NodeLayout;
            this.groundLevel = groundLevel;  
            this.innerNodeFactory = innerNodeFactory;
            this.sublayout = sublayout;

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

            if (sublayout.NodeLayout.OnlyLeaves())
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
        /// TODO
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

                if (sublayoutNode.IsLeaf || sublayout.RemovedChildren.Contains(sublayoutNode))
                {
                    /*if (nodeLayout != NodeLayouts.Treemap)
                    {
                        scale = new Vector3(coseNode.rect.width, innerNodeHeight, coseNode.rect.height);
                    }*/
                }

                position.y += transform.scale.y / 2.0f;
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

            if (!sublayout.NodeLayout.InnerNodesEncloseLeafNodes())
            {
                double left = Mathf.Infinity;
                double right = Mathf.NegativeInfinity;
                double top = Mathf.Infinity;
                double bottom = Mathf.NegativeInfinity;
                double nodeLeft;
                double nodeRight;
                double nodeTop;
                double nodeBottom;

                foreach (ILayoutNode layoutNode in sublayoutNodes)
                {
                    ILayoutNode sublayoutNode = (layoutNode as CoseSublayoutNode).Node;

                    nodeLeft = sublayoutNode.CenterPosition.x - sublayoutNode.Scale.x / 2;
                    nodeRight = sublayoutNode.CenterPosition.x + sublayoutNode.Scale.x / 2;
                    nodeTop = sublayoutNode.CenterPosition.z - sublayoutNode.Scale.z / 2;
                    nodeBottom = sublayoutNode.CenterPosition.z + sublayoutNode.Scale.z / 2;

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

                double defaultMargin = 0.5;
                left -= defaultMargin;
                right += defaultMargin;
                top -= defaultMargin;
                bottom += defaultMargin;

                Rect boundingRect = new Rect((float)left, (float)top, (float)(right - left), (float)(bottom - top));
                Vector3 scale = new Vector3(boundingRect.width, innerNodeHeight, boundingRect.height);
                Vector3 position = new Vector3(boundingRect.center.x, groundLevel, boundingRect.center.y);
                // TODO das ist doch falsch so, bzw. jetzt richtig beim anderen noch falsch
                LayoutScale = new Vector3(sublayout.Node.Scale.x, innerNodeHeight, sublayout.Node.Scale.z);

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
                // TODO COSE LAYOUT
                return layout.Layout(sublayoutNodes);
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
                    return new ManhattanLayout(groundLevel, innerNodeFactory.Unit);
                case NodeLayouts.FlatRectanglePacking:
                    return new RectanglePacker(groundLevel, innerNodeFactory.Unit);
                case NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, innerNodeFactory.Unit);
                case NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, 10.0f * innerNodeFactory.Unit, 10.0f * innerNodeFactory.Unit);
                case NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel);
                case NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel);
                //case NodeLayouts.CompoundSpringEmbedder:
                    //return new CoseLayout(groundLevel, settings, leafNodeFactory);
                default:
                    throw new System.Exception("Unhandled node layout ");
            }
        }

    }
}

