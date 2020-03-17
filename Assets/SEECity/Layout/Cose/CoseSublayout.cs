using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using System.Linq;
using System;
using static SEE.GraphSettings;

namespace SEE.Layout
{
    public class CoseSublayout
    {
        /// <summary>
        /// the layout of the sublayout
        /// </summary>
        private readonly GraphSettings.NodeLayouts nodeLayout;

        /// <summary>
        /// the root of the sublayout
        /// </summary>
        private readonly CoseNode root;

        /// <summary>
        /// Indicates whether the sublayout contains only leaf nodes
        /// </summary>
        private readonly bool onlyLeaves;

        /// <summary>
        /// a map from every node to the corresponding gameobject
        /// </summary>
        private readonly Dictionary<Node, GameObject> nodeMap;

        /// <summary>
        /// a map from every sublayout node to the corresponding gameobject
        /// </summary>
        private Dictionary<CoseNode, GameObject> nodeMapSublayout;

        /// <summary>
        /// the y co-ordinate setting the ground level; all nodes will be placed on this level
        /// </summary>
        private readonly float groundLevel;

        /// <summary>
        /// the factory used to created leaf nodes
        /// </summary>
        private readonly NodeFactory leafNodeFactory;

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

        /// <summary>
        /// 
        /// </summary>
        private readonly List<CoseNode> allNodes = new List<CoseNode>();

        /// <summary>
        /// 
        /// </summary>
        private readonly List<CoseNode> removedChildren = new List<CoseNode>();

        public Vector3 LayoutScale { get => layoutScale; set => layoutScale = value; }
        public Vector3 LayoutPosition { get => layoutPosition; set => layoutPosition = value; }
        public Dictionary<CoseNode, GameObject> NodeMapSublayout { get => nodeMapSublayout; set => nodeMapSublayout = value; }

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
        public CoseSublayout(CoseNode root, Dictionary<Node, GameObject> nodeMap, float groundLevel, NodeFactory leafNodeFactory, float innerNodeHeight, NodeLayouts nodeLayout, List<CoseNode> allNodes, List<CoseNode> removedChildren)
        {
            this.nodeLayout = nodeLayout;
            this.root = root;
            this.nodeMap = nodeMap;
            this.groundLevel = groundLevel;
            this.leafNodeFactory = leafNodeFactory;
            this.innerNodeHeight = innerNodeHeight;
            this.onlyLeaves = OnlyLeaveNodes();
            this.allNodes = allNodes;
            this.removedChildren = removedChildren;

            if (root.Child != null)
            {
                this.NodeMapSublayout = CalculateGameobjectsForSublayout();
            }

            root.SublayoutValues.Sublayout = this;
        }

        /// <summary>
        /// Calculates and sets the nodes positions
        /// </summary>
        public void Layout()
        {
            Dictionary<GameObject, NodeTransform> layout = CalculateSublayout();

            root.NodeObject.Parent = parentNodeObject;
           
            foreach (GameObject gameObject in layout.Keys)
            {
                Vector3 position = layout[gameObject].position;
                Vector3 scale = layout[gameObject].scale; 

                CoseNode coseNode = NodeMapSublayout.FirstOrDefault(i => i.Value == gameObject).Key;

                if (coseNode.IsLeaf() || removedChildren.Contains(coseNode))
                {
                    if (nodeLayout != NodeLayouts.Treemap)
                    {
                        scale = new Vector3(coseNode.rect.width, innerNodeHeight, coseNode.rect.height);
                    } 
                } 

                coseNode.SetPositionScale(position, scale);

                if (coseNode.SublayoutValues.IsSubLayoutRoot)
                {
                    if (coseNode == root)
                    {
                        LayoutScale = layout[gameObject].scale;
                        LayoutPosition = layout[gameObject].position;
                    }
                    else
                    {
                        coseNode.NodeObject.children = coseNode.SublayoutValues.removedChildren;

                        foreach (KeyValuePair<CoseNode, GameObject> kvp in coseNode.SublayoutValues.Sublayout.NodeMapSublayout)
                        {
                            // child notes from subsublayout 
                            if (kvp.Key != coseNode)
                            {
                                kvp.Key.SetOrigin(); // sub1 nodes total zu der neuen position von A_1_new setzen
                                kvp.Key.SublayoutValues.relativeRect = kvp.Key.rect;
                                kvp.Key.SublayoutValues.SubLayoutRoot = root;

                                NodeMapSublayout.Add(kvp.Key, kvp.Value);


                                if (kvp.Key.Child != null )
                                {
                                    CoseGraph child = kvp.Key.Child;
                                    Rect bounds = kvp.Key.rect; 
                                    child.Left = bounds.xMin;
                                    child.Top = bounds.yMin;
                                    child.Right = bounds.xMax;
                                    child.Bottom = bounds.yMax;
                                    child.UpdateBoundingRect();
                                }
                            } 
                        }
                        coseNode.SublayoutValues.IsSubLayoutRoot = false;
                    }
                }
            }

            if (onlyLeaves ||  nodeLayout == NodeLayouts.EvoStreets)
            {
                double left = Mathf.Infinity;
                double right = Mathf.NegativeInfinity;
                double top = Mathf.Infinity;
                double bottom = Mathf.NegativeInfinity;
                double nodeLeft;
                double nodeRight;
                double nodeTop;
                double nodeBottom;

                foreach (CoseNode cNode in NodeMapSublayout.Keys)
                {
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

                int defaultMargin = CoseLayoutSettings.Graph_Margin;
                left -= defaultMargin;
                right += defaultMargin;
                top -= defaultMargin;
                bottom += defaultMargin;

                Rect boundingRect = new Rect((float)left, (float)top, (float)(right - left), (float)(bottom - top));
                Vector3 position = new Vector3(boundingRect.center.x, groundLevel, boundingRect.center.y);
                Vector3 scale = new Vector3(boundingRect.width, innerNodeHeight, boundingRect.height);
                LayoutScale = new Vector3(root.rect.width, innerNodeHeight, root.rect.height);
                LayoutPosition = new Vector3(root.rect.x, groundLevel, root.rect.y);
                root.SublayoutValues.IsSubLayoutNode = true;
                root.SetPositionScale(position, scale);
            }

            SetCoseNodeToLayoutPosition();
            //root.SetIntergraphEdgesToSublayoutRoot();
        }

        /// <summary>
        /// calculates the gameobjects and nodes needed for the sublayout
        /// </summary>
        /// <returns></returns>
        private Dictionary<CoseNode, GameObject> CalculateGameobjectsForSublayout()
        {
            Dictionary<CoseNode, GameObject> nodeMapping = new Dictionary<CoseNode, GameObject>();
            List<CoseNode> nodesForLayout = CalculateNodesForSublayout(onlyLeaves);

            // prepair root node for layout
            CoseGraph graph = root.Child;
            parentNodeObject = graph.Parent.NodeObject.Parent;
            graph.Parent.NodeObject.Parent = null;

            nodesForLayout.ForEach(node => {
                if (nodeMap.ContainsKey(node.NodeObject))
                {
                    nodeMapping.Add(node, nodeMap[node.NodeObject]);
                }
            });

            return nodeMapping;
        }

        /// <summary>
        /// sets the sublayout node position relativ to its root node
        /// </summary>
        private void SetCoseNodeToLayoutPosition()
        {
            List<CoseNode> nodes = new List<CoseNode>(allNodes);
            nodes.AddRange(removedChildren);
            nodes.Remove(root);

            nodes.ForEach(node => {
                node.SetPositionRelativ(root);
            });
        }

        /// <summary>
        /// Calculates the sublayout positions 
        /// </summary>
        /// <returns></returns>
        private Dictionary<GameObject, NodeTransform> CalculateSublayout()
        {
            List<GameObject> gameObjects = NodeMapSublayout.Values.ToList();

            switch (nodeLayout)
            {
                case GraphSettings.NodeLayouts.Manhattan:
                    return new ManhattanLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.FlatRectanglePacking:
                    return new RectanglePacker(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, leafNodeFactory, 10.0f * leafNodeFactory.Unit, 10.0f * leafNodeFactory.Unit).Layout(gameObjects);
                case GraphSettings.NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                default:
                    throw new System.Exception("Unhandled node layout ");
            }
        }

        /// <summary>
        /// Calculates if the sublayout only contains leaf nodes
        /// </summary>
        /// <returns>only leave nodes</returns>
        private bool OnlyLeaveNodes()
        {
            return nodeLayout == GraphSettings.NodeLayouts.Manhattan || nodeLayout == GraphSettings.NodeLayouts.FlatRectanglePacking;
        }


        /// <summary>
        /// Calculates all nodes needed for a sublayout with this graphs parent node as the sublayouts root node
        /// </summary>
        /// <param name="onlyLeaves"></param>
        /// <returns></returns>
        public List<CoseNode> CalculateNodesForSublayout(bool onlyLeaves)
        {
            List<CoseNode> nodesForLayout = new List<CoseNode>(allNodes);
            nodesForLayout.ForEach(node => node.SublayoutValues.IsSubLayoutNode = true);

            if (onlyLeaves)
            {
                nodesForLayout.Remove(root);
                return nodesForLayout;
            } else
            {
                // bei einem subsubLayout wird der root wieder hinzugefügt
                removedChildren.ForEach( node =>
                {
                    if (node.SublayoutValues.IsSubLayoutRoot)
                    {
                        nodesForLayout.Add(node);
                        node.SublayoutValues.removedChildren = node.NodeObject.children;
                        node.NodeObject.children = new List<Node>();

                        // set dimensions of this node to the dimensions of the subsublayout
                        if (nodeMap.ContainsKey(node.NodeObject))
                        {
                            GameObject obj = nodeMap[node.NodeObject];
                            Vector3 scale = node.SublayoutValues.Sublayout.layoutScale;
                            obj.transform.localScale = scale;
                        }
                    }
                });
            }
            return nodesForLayout;
        }
    }
}

