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
        private Dictionary<Node, GameObject> nodeMap;

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

        public Vector3 LayoutScale { get => layoutScale; set => layoutScale = value; }
        public Vector3 LayoutPosition { get => layoutPosition; set => layoutPosition = value; }

        /// <summary>
        /// constructor 
        /// </summary>
        /// <param name="root">the root node</param>
        /// <param name="nodeMap">map between all nodes and gameobjects</param>
        /// <param name="groundLevel">the y co-ordinate setting the ground level; all nodes will be
        /// placed on this level</param>
        /// <param name="leafNodeFactory">the factory used to created leaf nodes</param>
        /// <param name="innerNodeHeight">The height of objects (y co-ordinate) drawn for inner nodes.</param>
        public CoseSublayout(CoseNode root, Dictionary<Node, GameObject> nodeMap, float groundLevel, NodeFactory leafNodeFactory, float innerNodeHeight)
        {
            this.nodeLayout = root.CNodeSublayoutValues.NodeLayout;
            this.root = root;
            this.nodeMap = nodeMap;
            this.groundLevel = groundLevel;
            this.leafNodeFactory = leafNodeFactory;
            this.innerNodeHeight = innerNodeHeight;
            this.onlyLeaves = OnlyLeaveNodes();

            if (root.Child != null)
            {
                this.nodeMapSublayout = CalculateGameobjectsForSublayout();
            }

            root.CNodeSublayoutValues.Sublayout = this;
        }

        /// <summary>
        /// Calculates and sets the nodes positions
        /// </summary>
        public void Layout()
        {
            Dictionary<GameObject, NodeTransform> layout = CalculateSublayout();

            foreach (GameObject gameObject in layout.Keys)
            {
                CoseNode coseNode = nodeMapSublayout.FirstOrDefault(i => i.Value == gameObject).Key;
                coseNode.SetPositionScale(layout[gameObject].position, layout[gameObject].scale);

                if (coseNode == root)
                {
                    LayoutScale = layout[gameObject].scale;
                    LayoutPosition = layout[gameObject].position;
                    //coseNode.CNodeSublayoutValues.relativeRect = new Rect(new Vector2(layout[gameObject].position.x, layout[gameObject].position.z), new Vector2(layout[gameObject].scale.x, layout[gameObject].scale.z));
                }
            }

            if (onlyLeaves ||  nodeLayout == NodeLayouts.EvoStreets)
            {
                float left = Int32.MaxValue;
                float right = -Int32.MaxValue;
                float top = Int32.MaxValue;
                float bottom = -Int32.MaxValue;
                float nodeLeft;
                float nodeRight;
                float nodeTop;
                float nodeBottom;

                foreach (CoseNode cNode in nodeMapSublayout.Keys)
                {
                    nodeLeft = (float)cNode.GetLeft();
                    nodeRight = (float)cNode.GetRight();
                    nodeTop = (float)cNode.GetTop();
                    nodeBottom = (float)cNode.GetBottom();

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

                Rect boundingRect = new Rect(left, top, right - left, bottom - top);
                Vector3 position = new Vector3(boundingRect.center.x, groundLevel, boundingRect.center.y);
                Vector3 scale = new Vector3(boundingRect.width, innerNodeHeight, boundingRect.height);
                root.SetPositionScale(position, scale);
            }

            SetCoseNodeToLayoutPosition(root, root);
            root.SetIntergraphEdgesToSublayoutRoot();
        }

        /// <summary>
        /// calculates the gameobjects and nodes needed for the sublayout
        /// </summary>
        /// <returns></returns>
        private Dictionary<CoseNode, GameObject> CalculateGameobjectsForSublayout()
        {
            Dictionary<CoseNode, GameObject> nodeMapping = new Dictionary<CoseNode, GameObject>();
            List<CoseNode> nodesForLayout = new List<CoseNode>();
            CoseGraph graph = root.Child;
            graph.Parent.NodeObject.Parent = null;

            if (!onlyLeaves)
            {
                nodesForLayout.Add(graph.Parent);
                graph.Parent.CNodeSublayoutValues.IsSubLayoutNode = true;
            }

            nodesForLayout.AddRange(graph.CalculateNodesForSublayout(onlyLeaves));

            foreach (CoseNode coseNode in nodesForLayout)
            {
                if (nodeMap.ContainsKey(coseNode.NodeObject))
                {
                    nodeMapping.Add(coseNode, nodeMap[coseNode.NodeObject]);
                }
            }
            return nodeMapping;
        }

        /// <summary>
        /// sets the sublayout node position relativ to its root node
        /// </summary>
        /// <param name="origin">the node with relativ position</param>
        /// <param name="cRoot">the root node</param>
        private void SetCoseNodeToLayoutPosition(CoseNode origin, CoseNode cRoot)
        {
            if (cRoot.Child != null)
            {
                cRoot.Child.IsSubLayout = true;
                foreach (CoseNode node in cRoot.Child.Nodes)
                {
                    node.SetPositionRelativ(origin);
                    SetCoseNodeToLayoutPosition(origin, node);
                }
            }
        }

        /// <summary>
        /// Calculates the sublayout positions 
        /// </summary>
        /// <returns></returns>
        private Dictionary<GameObject, NodeTransform> CalculateSublayout()
        {
            List<GameObject> gameObjects = nodeMapSublayout.Values.ToList();

            switch (nodeLayout)
            {
                case GraphSettings.NodeLayouts.Manhattan:
                    return new ManhattanLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.FlatRectanglePacking:
                    return new RectanglePacker(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.Treemap:
                //return new TreemapLayout(groundLevel, leafNodeFactory, 1000.0f * Unit(), 1000.0f * Unit()).Layout(nodeMap.Values);
                case GraphSettings.NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel, leafNodeFactory).Layout(gameObjects);
                case GraphSettings.NodeLayouts.CompoundSpringEmbedder:
                // do nothing
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
    }
}

