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
                    leafNodeFactory = new CubeFactory();
                    break;
                case GraphSettings.LeafNodeKinds.Buildings:
                    leafNodeFactory = new BuildingFactory();
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.LeafNodeKinds");
            }
            innerNodeFactory = GetInnerNodeFactory(this.settings.InnerNodeObjects);
        }

        /// <summary>
        /// Returns the Factory for the inner nodes
        /// </summary>
        /// <param name="innerNodeKinds">the kind of the inner nodes</param>
        /// <returns>inner node factory</returns>
        private InnerNodeFactory GetInnerNodeFactory(GraphSettings.InnerNodeKinds innerNodeKinds)
        {
            switch (innerNodeKinds)
            {
                case GraphSettings.InnerNodeKinds.Empty:
                case GraphSettings.InnerNodeKinds.Donuts:
                    return new VanillaFactory();
                case GraphSettings.InnerNodeKinds.Circles:
                    return new CircleFactory(leafNodeFactory.Unit);
                case GraphSettings.InnerNodeKinds.Cylinders:
                    return new CylinderFactory();
                case GraphSettings.InnerNodeKinds.Rectangles:
                    return new RectangleFactory(leafNodeFactory.Unit);
                case GraphSettings.InnerNodeKinds.Blocks:
                    return new CubeFactory();
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
        private readonly NodeFactory leafNodeFactory;

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
            List<string> nodeMetrics = new List<string>() { settings.WidthMetric, settings.HeightMetric, settings.DepthMetric, settings.ColorMetric };
            nodeMetrics.AddRange(settings.AllLeafIssues());
            nodeMetrics.AddRange(settings.AllInnerNodeIssues());
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
                    layout = new StraightEdgeLayout(leafNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case GraphSettings.EdgeLayouts.Spline:
                    layout = new SplineEdgeLayout(leafNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case GraphSettings.EdgeLayouts.Bundling:
                    layout = new BundledEdgeLayout(leafNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
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
            List<Node> nodes = graph.Nodes();

            Dictionary<Node, GameObject> nodeMap = CreateBlocks(nodes);
            Dictionary<GameObject, NodeTransform> layout;

            Dictionary<string, List<Node>> sublayoutRootsWithNodes = new Dictionary<string, List<Node>>();

            Performance p = Performance.Begin("layout name" + settings.NodeLayout + ", layout of nodes");

            switch (settings.NodeLayout)
            {
                case GraphSettings.NodeLayouts.Manhattan:
                    // only leaves
                    layout = new ManhattanLayout(groundLevel, leafNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.FlatRectanglePacking:
                    // only leaves
                    layout = new RectanglePacker(groundLevel, leafNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.EvoStreets:
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new EvoStreetsNodeLayout(groundLevel, leafNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.Treemap:
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new TreemapLayout(groundLevel, leafNodeFactory, 1000.0f * Unit(), 1000.0f * Unit()).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.Balloon:
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new BalloonNodeLayout(groundLevel, leafNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.CirclePacking:
                    AddContainers(nodeMap, nodes); // and inner nodes
                    layout = new CirclePackingNodeLayout(groundLevel, leafNodeFactory).Layout(nodeMap.Values);
                    break;
                case GraphSettings.NodeLayouts.CompoundSpringEmbedder:
                    sublayoutRootsWithNodes = SetContainersCompoundSpringEmbedder(nodeMap, nodes); 

                    bool isCircle = false;
                    if (settings.InnerNodeObjects == GraphSettings.InnerNodeKinds.Circles || settings.InnerNodeObjects == GraphSettings.InnerNodeKinds.Circles || settings.InnerNodeObjects == GraphSettings.InnerNodeKinds.Circles)
                    {
                        isCircle = true;
                    }
                    layout = new CoseLayout(groundLevel, leafNodeFactory, isCircle, graph.Edges(), settings).Layout(nodeMap.Values);
                    break;
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
            p.End();

            ICollection<GameObject> gameNodes = layout.Keys;

            if (settings.NodeLayout != GraphSettings.NodeLayouts.CompoundSpringEmbedder) 
            {
                Apply(layout, settings.origin);
                AddDecorations(gameNodes);
            } else
            {
                if (sublayoutRootsWithNodes.Count > 0)
                {
                    Dictionary<GameObject, NodeTransform> remainingLayoutNodes = layout;

                    foreach (KeyValuePair<string, List<Node>> kvp in sublayoutRootsWithNodes)
                    {
                        // nodeMap nach knoten gefiltert, die in dem sublayout sind 
                        Dictionary<Node, GameObject> filteredNodeMap = nodeMap.Where(i => kvp.Value.Contains(i.Key)).ToDictionary(i => i.Key, i => i.Value);
                        // layout nach gameobjects gefiltert, die im sublayout sind
                        Dictionary<GameObject, NodeTransform> subLayout = layout.Where(pair => filteredNodeMap.ContainsValue(pair.Key)).ToDictionary(i => i.Key, i => i.Value);
                        remainingLayoutNodes.Where(pair => !subLayout.ContainsKey(pair.Key)).ToDictionary(i => i.Key, i => i.Value);
                        InnerNodeFactory innerNodeFactory = GetInnerNodeFactory(settings.CoseGraphSettings.DirShape[kvp.Key]);
                        Apply(subLayout, settings.origin, innerNodeFactory);
                        AddDecorations(subLayout.Keys.ToList(), settings.CoseGraphSettings.DirShape[kvp.Key], settings.CoseGraphSettings.DirNodeLayout[kvp.Key]);
                    }

                    Apply(remainingLayoutNodes, settings.origin);
                    AddDecorations(remainingLayoutNodes.Keys.ToList());
                }
                else
                {
                    // keine sublayouts 
                    Apply(layout, settings.origin);
                    AddDecorations(gameNodes);
                }
            }
            
            EdgeLayout(graph, gameNodes);
            BoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            // Place the plane somewhat under ground level.
            PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, groundLevel - 0.01f, Color.gray);

            Measurements measurements = new Measurements(nodeMap, graph, settings, leftFrontCorner, rightBackCorner);
            measurements.NodesPerformance(p);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeMap"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private Dictionary<string, List<Node>> SetContainersCompoundSpringEmbedder(Dictionary<Node, GameObject> nodeMap, List<Node> nodes)
        {
            Dictionary<string, List<Node>> subLayoutRootsWithNodes = new Dictionary<string, List<Node>>();

            Dictionary<string, GraphSettings.NodeLayouts> sublayouts = FilterSubLayouts();
            if (sublayouts.Count != 0)
            {
                List<Node> remainingNodes = nodes;

                foreach (KeyValuePair<string, GraphSettings.NodeLayouts> sublayoutKvp in sublayouts)
                {
                    List<Node> filteredNodes = FilterNodes(nodes, sublayoutKvp.Key);
                    remainingNodes = remainingNodes.Where(i => !filteredNodes.Contains(i)).ToList();
                    AddContainers(nodeMap, filteredNodes, GetInnerNodeFactory(settings.CoseGraphSettings.DirShape[sublayoutKvp.Key]));
                    subLayoutRootsWithNodes.Add(sublayoutKvp.Key, filteredNodes);
                }

                AddContainers(nodeMap, remainingNodes);
            }
            else
            {
                AddContainers(nodeMap, nodes);
            }

            return subLayoutRootsWithNodes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, GraphSettings.NodeLayouts> FilterSubLayouts()
        {
            Dictionary<string, GraphSettings.NodeLayouts> sublayouts = new Dictionary<string, GraphSettings.NodeLayouts>();
            foreach (KeyValuePair<string, GraphSettings.NodeLayouts> dir in settings.CoseGraphSettings.DirNodeLayout)
            {
                if (settings.CoseGraphSettings.ListDirToggle[dir.Key])
                {
                    sublayouts.Add(dir.Key, dir.Value);
                }
            }
            return sublayouts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="sublayoutRoot"></param>
        /// <returns></returns>
        private List<Node> FilterNodes(List<Node> nodes, string sublayoutRoot)
        {
            IEnumerable<Node> matches = nodes.Where(i => i.SourceName.Equals(sublayoutRoot));
            List<Node> sublayoutNodes = new List<Node>();

            if (matches.Count() > 0)
            {
                // alle Kind Knoten von Root 
                sublayoutNodes = WithAllChildren(matches.First());
            }
            return sublayoutNodes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private List<Node> WithAllChildren(Node root)
        {
            List<Node> allNodes = new List<Node>();
            allNodes.Add(root);
            foreach (Node node in root.Children())
            {
                allNodes.AddRange(WithAllChildren(node));
            }

            return allNodes;
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        private void AddDecorations(ICollection<GameObject> gameNodes)
        {
            AddDecorations(gameNodes, settings.InnerNodeObjects, settings.NodeLayout);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        private void AddDecorations(ICollection<GameObject> gameNodes, GraphSettings.InnerNodeKinds innerNodeKinds, GraphSettings.NodeLayouts nodeLayout)
        {
            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            if (settings.ShowErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(settings.LeafIssueMap(), leafNodeFactory, scaler);
                issueDecorator.Add(LeafNodes(gameNodes));
            }

            if (nodeLayout == GraphSettings.NodeLayouts.Balloon 
                || nodeLayout == GraphSettings.NodeLayouts.EvoStreets)
            {
                AddLabels(InnerNodes(gameNodes));
            }
            switch (innerNodeKinds)
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
                        DonutDecorator decorator = new DonutDecorator(innerNodeFactory, scaler, settings.InnerDonutMetric, settings.AllInnerNodeIssues().ToArray<string>());
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case GraphSettings.InnerNodeKinds.Cylinders:
                case GraphSettings.InnerNodeKinds.Rectangles:
                case GraphSettings.InnerNodeKinds.Blocks:
                    // TODO
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds " + settings.InnerNodeObjects);
            }
        }

        /// <summary>
        /// Adds the source name as a label to the center of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        private void AddLabels(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject node in gameNodes)
            {
                Vector3 size = innerNodeFactory.GetSize(node);
                float length = Mathf.Min(size.x, size.z);
                // The text may occupy up to 30% of the length.
                GameObject text = TextFactory.GetText(node.GetComponent<NodeRef>().node.SourceName, 
                                                      node.transform.position, length * 0.3f);
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
        /// Applies the layout to all nodes at given origin.
        /// </summary>
        /// <param name="layout">node layout to be applied</param>
        /// <param name="origin">the center origin where the graph should be placed in the world scene</param>
        public void Apply(Dictionary<GameObject, NodeTransform> layout, Vector3 origin, InnerNodeFactory innerNodeFactory = null)
        {
            if (innerNodeFactory == null)
            {
                innerNodeFactory = this.innerNodeFactory;
            }

            foreach (var entry in NodeLayout.Move(layout, origin))
            {
                GameObject gameNode = entry.Key;
                NodeTransform transform = entry.Value;
                Node node = gameNode.GetComponent<NodeRef>().node;

                if (node.IsLeaf())
                {
                    // We need to first scale the game node and only afterwards set its
                    // position because transform.scale refers to the center position.
                    if (settings.NodeLayout == GraphSettings.NodeLayouts.Treemap)
                    {
                        // The Treemap layout adjusts the size of the object's ground area according to
                        // the total space we allow it to use. The x length was initially
                        // mapped onto the area of the ground. The treemap layout yields
                        // an x and z co-ordinate that defines this area, which we use
                        // here to set the width and depth of the game node.
                        // The height (y axis) is not modified by the treemap layout and,
                        // hence, does not need any adustment.
                        leafNodeFactory.SetWidth(gameNode, transform.scale.x);
                        leafNodeFactory.SetDepth(gameNode, transform.scale.z);
                    }
                    // Leaf nodes were created as blocks by leaveNodeFactory.
                    // Leaf nodes have their size set before the layout is computed. We will
                    // not change their size unless a layout requires that.
                    leafNodeFactory.SetGroundPosition(gameNode, transform.position);
                }
                else
                {
                    // Inner nodes were created by innerNodeFactory.
                    innerNodeFactory.SetSize(gameNode, transform.scale);
                    innerNodeFactory.SetGroundPosition(gameNode, transform.position);
                    // Inner nodes will be drawn later when we add decorations because
                    // they can be drawn as a single circle line or a Donut chart.
                }
                //Rotate(gameNode, transform.rotation);
                // Rotate the game object.
                Rotate(gameNode, transform.rotation);
            }
        }

        /// <summary>
        /// Rotates the given object by the given degree along the y axis (i.e., relative to the ground).
        /// </summary>
        /// <param name="gameNode">object to be rotated</param>
        /// <param name="degree">degree of rotation</param>
        private void Rotate(GameObject gameNode, float degree)
        {
            Node node = gameNode.GetComponent<NodeRef>().node;
            if (node.IsLeaf())
            {
                leafNodeFactory.Rotate(gameNode, degree);
            }
            else
            {
                innerNodeFactory.Rotate(gameNode, degree);
            }
        }

        /// <summary>
        /// Returns the unit of the world helpful for scaling. This unit depends upon the
        /// kind of blocks we are using to represent nodes.
        /// </summary>
        /// <returns>unit of the world</returns>
        public float Unit()
        {
            return leafNodeFactory.Unit;
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

            float metricMaximum = scaler.GetNormalizedMaximum(settings.ColorMetric);

            foreach (Node node in nodes)
            {
                // We add only leaves.
                if (node.IsLeaf())
                {
                    int material = Mathf.RoundToInt(Mathf.Lerp(0.0f,
                                                               (float)(leafNodeFactory.NumberOfMaterials() - 1),
                                                               scaler.GetNormalizedValue(settings.ColorMetric, node)
                                                                 / metricMaximum));
                    GameObject block = leafNodeFactory.NewBlock(material);
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
                        leafNodeFactory.SetWidth(block, leafNodeFactory.Unit * widthOfSquare);
                        leafNodeFactory.SetDepth(block, leafNodeFactory.Unit * widthOfSquare);
                        leafNodeFactory.SetHeight(block, leafNodeFactory.Unit * scale.y);
                    }
                    else
                    {
                        leafNodeFactory.SetSize(block, leafNodeFactory.Unit * scale);
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
        private void AddContainers(Dictionary<Node, GameObject> nodeMap, IList<Node> nodes, InnerNodeFactory innerNodeFactory = null)
        {
            if (innerNodeFactory == null)
            {
                innerNodeFactory = this.innerNodeFactory;
            }

            foreach (Node node in nodes)
            {
                // We add only inner nodes.
                if (! node.IsLeaf())
                {
                    GameObject innerGameObject = NewInnerNode(node, innerNodeFactory);
                    nodeMap[node] = innerGameObject;
                }
            }
        }

        /// <summary>
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        private GameObject NewInnerNode(Node node, InnerNodeFactory innerNodeFactory)
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

                    Vector3 extent = node.IsLeaf() ? leafNodeFactory.GetSize(go) / 2.0f : innerNodeFactory.GetSize(go) / 2.0f;
                    Vector3 position = node.IsLeaf() ? leafNodeFactory.GetCenterPosition(go) : innerNodeFactory.GetCenterPosition(go);
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
