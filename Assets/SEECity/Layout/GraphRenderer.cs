using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEEC.Layout;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A renderer for graphs. Encapsulates handling of block types, node and edge layouts,
    /// decorations and other visual attributes.
    /// </summary>
    public class GraphRenderer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph">the graph to be drawn</param>
        /// <param name="settings">the settings for the visualization</param>
        public GraphRenderer(GraphSettings settings)
        {
            this.settings = settings;
            switch (this.settings.LeafObjects)
            {
                case GraphSettings.LeafNodeKinds.Blocks:
                    leaveNodeFactory = new CubeFactory();
                    break;
                case GraphSettings.LeafNodeKinds.Buildings:
                    leaveNodeFactory = new BuildingFactory();
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.LeafNodeKinds");
            }
            switch (this.settings.InnerNodeObjects)
            {
                case GraphSettings.InnerNodeKinds.Circles:
                    innerNodeFactory = new CircleFactory();
                    break;
                case GraphSettings.InnerNodeKinds.Cylinders:
                    innerNodeFactory = new CylinderFactory();
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds");
            }
        }

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        private readonly GraphSettings settings;

        /// <summary>
        /// The factory used to create blocks for leaves.
        /// </summary>
        private readonly NodeFactory leaveNodeFactory;

        /// <summary>
        /// The factory used to create game nodes for inner graph nodes.
        /// </summary>
        private readonly NodeFactory innerNodeFactory;

        /// <summary>
        /// The scale used to normalize the metrics determining the lengths of the blocks.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// Draws the graph.
        /// </summary>
        public void Draw(Graph graph)
        {
            SetScaler(graph);
            graph.SortHierarchyByName();

            if (settings.NodeLayout == GraphSettings.NodeLayouts.Manhattan
                || settings.NodeLayout == GraphSettings.NodeLayouts.Treemap
                || settings.NodeLayout == GraphSettings.NodeLayouts.BallonNode
                || settings.NodeLayout == GraphSettings.NodeLayouts.CirclePackingNode)
            {
                DrawCity(graph);
            }
            else
            {
                Dictionary<Node, GameObject> gameNodes = NodeLayout(graph, scaler);
                if (settings.EdgeLayout != GraphSettings.EdgeLayouts.None)
                {
                    EdgeLayout(graph, gameNodes);
                }
            }
        }

        private void SetScaler(Graph graph)
        {
            List<string> nodeMetrics = new List<string>() { settings.WidthMetric, settings.HeightMetric, settings.DepthMetric };
            nodeMetrics.AddRange(settings.IssueMap().Keys);
            if (settings.ZScoreScale)
            {
                scaler = new ZScoreScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
            else
            {
                scaler = new LinearScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
        }

        private void EdgeLayout(Graph graph, Dictionary<Node, GameObject> gameNodes)
        {
            IEdgeLayout layout;
            switch (settings.EdgeLayout)
            {
                case GraphSettings.EdgeLayouts.Straight:
                    layout = new StraightEdgeLayout(leaveNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case GraphSettings.EdgeLayouts.Spline:
                    layout = new SplineEdgeLayout(leaveNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case GraphSettings.EdgeLayouts.Bundling:
                    layout = new BundledEdgeLayout(leaveNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                default:
                    throw new Exception("Unhandled edge layout " + settings.EdgeLayout.ToString());
            }
            Performance p = Performance.Begin(layout.Name + " layout of edges");
            layout.DrawEdges(graph, gameNodes.Values.ToList());
            p.End();
        }

        private Dictionary<Node, GameObject> NodeLayout(Graph graph, IScale scaler)
        {
            INodeLayout layout;
            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Balloon:
                    {
                        layout = new BalloonLayout(settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                   settings.IssueMap(),
                                                   settings.InnerNodeMetrics,
                                                   leaveNodeFactory,
                                                   scaler,
                                                   settings.ShowErosions,
                                                   settings.ShowDonuts);
                        break;
                    }
                case GraphSettings.NodeLayouts.CirclePacking:
                    {
                        layout = new CirclePackingLayout(settings.WidthMetric, settings.HeightMetric, settings.DepthMetric,
                                                         settings.IssueMap(),
                                                         settings.InnerNodeMetrics,
                                                         leaveNodeFactory,
                                                         scaler,
                                                         settings.ShowErosions,
                                                         settings.ShowDonuts);
                        break;
                    }
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
            Performance p = Performance.Begin(layout.Name + " layout of nodes");
            layout.Draw(graph);
            p.End();
            return layout.Nodes();
        }

        /// <summary>
        /// The y co-ordinate of the ground where blocks are placed.
        /// </summary>
        protected const float groundLevel = 0.0f;

        protected void DrawCity(Graph graph)
        {            
            Dictionary<Node, GameObject> nodeMap;
            Dictionary<GameObject, NodeTransform> layout;
            List<Node> nodes = graph.Nodes();
            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Manhattan:
                    nodeMap = CreateBlocks(nodes); // only leaves
                    layout = new ManhattenLayout(groundLevel, leaveNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.Treemap:
                    nodeMap = CreateBlocks(nodes); // only leaves
                    layout = new TreemapLayout(groundLevel, leaveNodeFactory, 100.0f, 100.0f).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.BallonNode:
                    nodeMap = CreateBlocks(nodes); // leaves
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new BalloonNodeLayout(groundLevel, leaveNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.CirclePackingNode:
                    nodeMap = CreateBlocks(nodes); // leaves
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new CirclePackingNodeLayout(groundLevel, leaveNodeFactory).Layout(nodeMap.Values);
                    break;
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
            
            Apply(layout);
            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            if (settings.ShowErosions)
            {
                AddErosionIssues(nodeMap.Values);
            }
            BoundingBox(nodeMap.Values, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            Debug.LogFormat("New plane: left front corner = {0}, right back corner = {1}\n", leftFrontCorner, rightBackCorner);
            PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, groundLevel - 0.01f, Color.gray);
        }

        protected void AddErosionIssues(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject block in gameNodes)
            {
                AddErosionIssues(block);
            }
        }

        public void Apply(Dictionary<GameObject, NodeTransform> layout)
        {
            foreach (var entry in layout)
            {
                GameObject gameNode = entry.Key;
                NodeTransform transform = entry.Value;
                Node node = gameNode.GetComponent<NodeRef>().node;

                if (node.IsLeaf())
                {
                    // Leaf nodes were created as blocks by leaveNodeFactory.
                    // Leaf nodes have their size set before the layout is computed. We will
                    // not change their size.
                    leaveNodeFactory.SetGroundPosition(gameNode, transform.position);
                }
                else
                {
                    // Inner nodes were not created by blockFactory.
                    innerNodeFactory.SetGroundPosition(gameNode, transform.position);
                    innerNodeFactory.SetSize(gameNode, transform.scale);
                }
            }
        }

        /// <summary>
        /// Returns the unit of the world helpful for scaling. This unit depends upon the
        /// kind of blocks we are using to represent nodes.
        /// </summary>
        /// <returns>unit of the world</returns>
        public float Unit()
        {
            return leaveNodeFactory.Unit();
        }

        /// <summary>
        /// Adds a NodeRef component to given game node referencing to given graph node.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <param name="node"></param>
        protected void AttachNode(GameObject gameNode, Node node)
        {
            NodeRef nodeRef = gameNode.AddComponent<NodeRef>();
            nodeRef.node = node;
        }

        /// <summary>
        /// Creates and scales blocks for all leaf nodes in given list of nodes.
        /// </summary>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        /// <returns>blocks for all leaf nodes in given list of nodes</returns>
        private Dictionary<Node, GameObject> CreateBlocks(IList<Node> nodes)
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>();

            foreach (Node node in nodes)
            {
                // We add only leaves.
                if (node.IsLeaf())
                {
                    GameObject block = leaveNodeFactory.NewBlock();
                    block.name = node.LinkName;

                    AttachNode(block, node);
                    // Scaled metric values for the dimensions.
                    Vector3 scale = new Vector3(scaler.GetNormalizedValue(node, settings.WidthMetric),
                                                scaler.GetNormalizedValue(node, settings.HeightMetric),
                                                scaler.GetNormalizedValue(node, settings.DepthMetric));

                    // Scale according to the metrics.
                    leaveNodeFactory.SetSize(block, scale);

                    result[node] = block;
                }
            }
            return result;
        }

        /// <summary>
        /// Adds game objects for all inner nodes in given list of nodes to nodeMap.
        /// Note: added game objects for inner nodes are not scaled.
        /// </summary>
        /// <param name="nodeMap">nodeMap to which the game objects are to be added</param>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        private void AddContainers(Dictionary<Node, GameObject> nodeMap, IList<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                // We add only inner nodes.
                if (! node.IsLeaf())
                {
                    GameObject innerGameObject = NewInnerNode(node);
                    nodeMap[node] = innerGameObject;
                }
            }
        }

        private GameObject NewInnerNode(Node node)
        {
            GameObject innerGameObject = innerNodeFactory.NewBlock();
            innerGameObject.name = node.LinkName;
            innerGameObject.tag = Tags.Node;
            AttachNode(innerGameObject, node);
            Debug.LogFormat("size of inner node {0} = {1}\n", innerGameObject.name, innerNodeFactory.GetSize(innerGameObject));
            return innerGameObject;
        }

        /// <summary>
        /// Stacks sprites for software-erosion issues atop of the roof of the given node
        /// in ascending order in terms of the sprite width. The sprite width is proportional
        /// to the normalized metric value for the erosion issue.
        /// </summary>
        /// <param name="node"></param>
        protected void AddErosionIssues(GameObject gameNode)
        {
            Node node = gameNode.GetComponent<NodeRef>().node;

            // The list of sprites for the erosion issues.
            List<GameObject> sprites = new List<GameObject>();

            // Create and scale the sprites and add them to the list of sprites.
            foreach (KeyValuePair<string, IconFactory.Erosion> issue in settings.IssueMap())
            {
                if (node.TryGetNumeric(issue.Key, out float value))
                {
                    if (value > 0.0f)
                    {
                        GameObject sprite = IconFactory.Instance.GetIcon(Vector3.zero, issue.Value);
                        sprite.name = sprite.name + " " + node.SourceName;

                        Vector3 spriteSize = GetSizeOfSprite(sprite);
                        // Scale the sprite to one Unity unit.
                        float spriteScale = 1.0f / spriteSize.x;
                        // Scale the erosion issue by normalization.
                        float metricScale = scaler.GetNormalizedValue(node, issue.Key);
                        // First: scale its width to unit size 1.0 maintaining the aspect ratio
                        sprite.transform.localScale *= spriteScale * leaveNodeFactory.Unit();
                        // Now scale it by the normalized metric.
                        sprite.transform.localScale *= metricScale;
                        sprite.transform.position = leaveNodeFactory.Roof(gameNode);
                        sprites.Add(sprite);
                    }
                }
            }

            // Now we stack the sprites on top of the roof of the building in
            // ascending order of their widths.
            {
                // The space that we put in between two subsequent erosion issue sprites.
                Vector3 delta = Vector3.up / 100.0f;
                Vector3 currentRoof = leaveNodeFactory.Roof(gameNode);
                sprites.Sort(Comparer<GameObject>.Create((left, right) => GetSizeOfSprite(left).x.CompareTo(GetSizeOfSprite(right).x)));
                foreach (GameObject sprite in sprites)
                {
                    Vector3 size = GetSizeOfSprite(sprite);
                    // Note: Consider that the position of the sprite is its center.
                    Vector3 halfHeight = (size.y / 2.0f) * Vector3.up;
                    sprite.transform.position = currentRoof + delta + halfHeight;
                    currentRoof = sprite.transform.position + halfHeight;
                }
            }
        }

        protected static Vector3 GetSizeOfSprite(GameObject go)
        {
            // The game object representing an erosion is a composite of 
            // multiple LOD child objects to be drawn depending how close
            // the camera is. The container object 'go' itself does not
            // have a renderer. We need to obtain the renderer of the
            // first child hat represents the object at LOD 0 instead.
            Renderer renderer = go.GetComponentInChildren<Renderer>();
            // Note: renderer.sprite.bounds.size yields the original size
            // of the sprite of the prefab. It does not consider the scaling.
            // It depends only upon the imported graphic. That is why we
            // need to use renderer.bounds.size.
            return renderer.bounds.size;
        }

        private void BoundingBox(ICollection<GameObject> gameNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
        {
            if (gameNodes.Count == 0)
            {
                leftLowerCorner = Vector2.zero;
                rightUpperCorner = Vector2.zero;
            }
            else
            {
                leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
                rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (GameObject go in gameNodes)
                {
                    Node node = go.GetComponent<NodeRef>().node;

                    // Note: go.transform.position denotes the center of the object

                    Vector3 extent = node.IsLeaf() ? leaveNodeFactory.GetSize(go) / 2.0f : innerNodeFactory.GetSize(go) / 2.0f;
                    Debug.LogFormat("extent of {0} = {1}\n", go.name, extent);
                    Vector3 position = node.IsLeaf() ? leaveNodeFactory.GetCenterPosition(go) : innerNodeFactory.GetCenterPosition(go);
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
            }
        }
    }
}
