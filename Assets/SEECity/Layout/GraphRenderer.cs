using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
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
                case GraphSettings.InnerNodeKinds.Empty:
                case GraphSettings.InnerNodeKinds.Donuts:
                    innerNodeFactory = new VanillaFactory();
                    break;
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
        private readonly InnerNodeFactory innerNodeFactory;

        /// <summary>
        /// The scale used to normalize the metrics determining the lengths of the blocks.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// Draws the graph (nodes and edges and all decorations).
        /// </summary>
        public void Draw(Graph graph)
        {
            SetScaler(graph);
            graph.SortHierarchyByName();
            DrawCity(graph);
        }

        /// <summary>
        /// Sets scaler according to the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose node metrics are to be scaled</param>
        private void SetScaler(Graph graph)
        {
            List<string> nodeMetrics = new List<string>() { settings.WidthMetric, settings.HeightMetric, settings.DepthMetric };
            nodeMetrics.AddRange(settings.IssueMap().Keys);
            nodeMetrics.Add(settings.InnerDonutMetric);

            if (settings.ZScoreScale)
            {
                scaler = new ZScoreScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
            else
            {
                scaler = new LinearScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
        }

        /// <summary>
        /// Apply the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        /// <param name="gameNodes">the subset of nodes for which to draw the edges</param>
        private void EdgeLayout(Graph graph, ICollection<GameObject> gameNodes)
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
                case GraphSettings.EdgeLayouts.None:
                    // nothing to be done
                    return;
                default:
                    throw new Exception("Unhandled edge layout " + settings.EdgeLayout.ToString());
            }
            Performance p = Performance.Begin(layout.Name + " layout of edges");
            layout.DrawEdges(graph, gameNodes);
            p.End();
        }

        /// <summary>
        /// The y co-ordinate of the ground where blocks are placed.
        /// </summary>
        protected const float groundLevel = 0.0f;

        /// <summary>
        /// Draws the nodes and edges of the graph by applying the layouts according to the user's
        /// choice in the settings.
        /// </summary>
        /// <param name="graph">graph whose nodes and edges are to be laid out</param>
        protected void DrawCity(Graph graph)
        {
            Dictionary<Node, GameObject> nodeMap;
            Dictionary<GameObject, NodeTransform> layout;
            List<Node> nodes = graph.Nodes();
            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Manhattan:
                    nodeMap = CreateBlocks(nodes); // only leaves
                    layout = new ManhattanLayout(groundLevel, leaveNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.Treemap:
                    nodeMap = CreateBlocks(nodes); // only leaves
                    layout = new TreemapLayout(groundLevel, leaveNodeFactory, 100.0f, 100.0f).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.Balloon:
                    nodeMap = CreateBlocks(nodes); // leaves
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new BalloonNodeLayout(groundLevel, leaveNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.CirclePacking:
                    nodeMap = CreateBlocks(nodes); // leaves
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new CirclePackingNodeLayout(groundLevel, leaveNodeFactory).Layout(nodeMap.Values);
                    break;
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }

            Apply(layout);
            ICollection<GameObject> gameNodes = nodeMap.Values;
            AddDecorations(gameNodes);
            EdgeLayout(graph, gameNodes);           
            BoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            // Place the plane somewhat under ground level.
            PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, groundLevel - 0.01f, Color.gray);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        private void AddDecorations(ICollection<GameObject> gameNodes)
        {
            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            if (settings.ShowErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(settings.IssueMap(), leaveNodeFactory, scaler);
                issueDecorator.Add(LeafNodes(gameNodes));
            }
            switch (settings.InnerNodeObjects)
            {
                case GraphSettings.InnerNodeKinds.Empty:
                    // do nothing
                    break;
                case GraphSettings.InnerNodeKinds.Circles:
                    {
                        CircleDecorator decorator = new CircleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case GraphSettings.InnerNodeKinds.Donuts:
                    {
                        DonutDecorator decorator = new DonutDecorator(innerNodeFactory, scaler, settings.InnerDonutMetric, settings.IssueMap().Keys.ToArray<string>());
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case GraphSettings.InnerNodeKinds.Cylinders:
                    // TODO
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds " + settings.InnerNodeObjects);
            }
        }

        /// <summary>
        /// Returns only the inner nodes in gameNodes as a list.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>the inner nodes in gameNodes as a list</returns>
        private ICollection<GameObject> InnerNodes(ICollection<GameObject> gameNodes)
        {
            return gameNodes.Where(o => ! IsLeaf(o)).ToList();
        }

        /// <summary>
        /// Returns only the leaf nodes in gameNodes as a list.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>the leaf nodes in gameNodes as a list</returns>
        private ICollection<GameObject> LeafNodes(ICollection<GameObject> gameNodes)
        {
            return gameNodes.Where(o => IsLeaf(o)).ToList();
        }

        /// <summary>
        /// True iff gameNode is a leaf in the graph.
        /// </summary>
        /// <param name="gameNode">game node to be checked</param>
        /// <returns>true iff gameNode is a leaf in the graph</returns>
        private static bool IsLeaf(GameObject gameNode)
        {
            return gameNode.GetComponent<NodeRef>().node.IsLeaf();
        }

        /// <summary>
        /// Applies the layout to all nodes.
        /// </summary>
        /// <param name="layout">node layout to be applied</param>
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
                    // not change their size unless a layout requires that.
                    leaveNodeFactory.SetGroundPosition(gameNode, transform.position);
                    if (settings.NodeLayout == GraphSettings.NodeLayouts.Treemap)
                    {
                        // Treemaps adjust the size of the object's ground area according to
                        // the total space we allow it to use. The x length was initially
                        // mapped onto the area of the ground. The treemap layout yields
                        // an x and z co-ordinate that defines this area, which we use
                        // here to set the width and depth of the game node.
                        // The height (y axis) is not modified by the treemap layout and,
                        // hence, does not need any adustment.
                        leaveNodeFactory.SetWidth(gameNode, transform.scale.x);
                        leaveNodeFactory.SetDepth(gameNode, transform.scale.z);
                    }
                }
                else
                {
                    // Inner nodes were not created by blockFactory.
                    innerNodeFactory.SetSize(gameNode, transform.scale);
                    innerNodeFactory.SetGroundPosition(gameNode, transform.position);
                    // Inner nodes will be drawn later when we add decorations because
                    // they can be drawn as a single circle line or a Donut chart.
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
                    Vector3 scale = new Vector3(scaler.GetNormalizedValue(settings.WidthMetric, node),
                                                scaler.GetNormalizedValue(settings.HeightMetric, node),
                                                scaler.GetNormalizedValue(settings.DepthMetric, node));

                    // Scale according to the metrics.
                    if (settings.NodeLayout == GraphSettings.NodeLayouts.Treemap)
                    {
                        // In case of treemaps, the width metric is mapped on the ground area.
                        float widthOfSquare = Mathf.Sqrt(scale.x);
                        leaveNodeFactory.SetWidth(block, widthOfSquare);
                        leaveNodeFactory.SetDepth(block, widthOfSquare);
                        leaveNodeFactory.SetHeight(block, scale.y);
                    }
                    else
                    {
                        leaveNodeFactory.SetSize(block, scale);
                    }

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

        /// <summary>
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        private GameObject NewInnerNode(Node node)
        {
            GameObject innerGameObject = innerNodeFactory.NewBlock();
            innerGameObject.name = node.LinkName;
            innerGameObject.tag = Tags.Node;
            AttachNode(innerGameObject, node);
            return innerGameObject;
        }

        /// <summary>
        /// Returns the bounding box (2D rectangle) enclosing all given game nodes.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <param name="leftLowerCorner">the left lower front corner (x axis in 3D space) of the bounding box</param>
        /// <param name="rightUpperCorner">the right lower back corner (z axis in 3D space) of the bounding box</param>
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
