using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.HolisticMetrics;
using SEE.GO;
using SEE.GO.Decorators;
using SEE.GO.NodeFactories;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using UnityEngine;
using Plane = SEE.GO.Plane;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// A renderer for graphs. Encapsulates handling of block types, node and edge layouts,
    /// decorations and other visual attributes.
    /// </summary>
    public partial class GraphRenderer : IGraphRenderer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">the settings for the visualization</param>
        /// <param name="graph">the graph to be rendered</param>
        /// <exception cref="ArgumentNullException">thrown in case <paramref name="graph"/> is null</exception>
        public GraphRenderer(AbstractSEECity settings, Graph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }
            SetGraph(settings, new List<Graph> { graph });
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">the settings for the visualization</param>
        /// <param name="graphs">the graphs to be rendered</param>
        /// <exception cref="ArgumentNullException">thrown in case <paramref name="graphs"/> is null or empty</exception>
        public GraphRenderer(AbstractSEECity settings, IList<Graph> graphs)
        {
            if (graphs == null || graphs.Count == 0)
            {
                throw new ArgumentNullException(nameof(graphs));
            }
            SetGraph(settings, graphs);
        }

        /// <summary>
        /// Initializes the rendering of the <paramref name="graph"/>. Must be used
        /// if the constructor of <see cref="GraphRenderer"/> was used without a graph.
        /// </summary>
        /// <param name="graph">the graph to be rendered; must not be null</param>
        private void SetGraph(AbstractSEECity settings, IList<Graph> graphs)
        {
            this.Settings = settings;
            this.graphs = graphs;
            SetScaler(graphs);
            foreach (Graph graph in graphs)
            {
                graph.SortHierarchyByName();
            }
            SetNodeFactories();
        }

        /// <summary>
        /// Returns the name of the node types for all <see cref="graphs"/>.
        /// </summary>
        /// <returns>node types for all <see cref="graphs"/></returns>
        private ISet<string> AllNodeTypes()
        {
            ISet<string> nodeTypes = new HashSet<string>();
            foreach (Graph graph in graphs)
            {
                nodeTypes.UnionWith(graph.AllNodeTypes());
            }
            return nodeTypes;
        }

        /// <summary>
        /// Sets the node factories for <see cref="nodeTypeToFactory"/> for all node types
        /// in <see cref="Settings"/> and those in the <see cref="graphs"/> (i.e., <see cref="AllNodeTypes"/>.
        /// There may be nodes in the graphs for which no specification exists in <see cref="Settings"/>,
        /// in which case defaults are used. This could happen, for instance, if we add an artifical
        /// root with an <see cref="Graph.UnknownType"/>.
        /// </summary>
        private void SetNodeFactories()
        {
            // We add all node types in the settings even if there may not be any node
            // in the graphs with a node type therein. That will not cause any harm.
            ISet<string> specifiedNodeTypes = Settings.NodeTypes.Types;
            foreach (string nodeType in specifiedNodeTypes)
            {
                VisualNodeAttributes visualNodeAttributes = Settings.NodeTypes[nodeType];
                nodeTypeToFactory[nodeType] = GetNodeFactory(visualNodeAttributes);
                nodeTypeToAntennaDectorator[nodeType] = GetAntennaDecorator(visualNodeAttributes);
            }

            // Now we need defaults for node types in the graphs for which no setting could
            // be found above.
            ISet<string> nodeTypesInGraph = AllNodeTypes();
            nodeTypesInGraph.ExceptWith(specifiedNodeTypes);

            foreach (string nodeType in nodeTypesInGraph)
            {
                Debug.LogWarning($"No specification of visual attributes for node type {nodeType}. Using a default.\n");
                nodeTypeToFactory[nodeType] = GetDefaultNodeFactory();
                nodeTypeToAntennaDectorator[nodeType] = null;
            }

            // The default node factory that we use if the we cannot find a setting for a given node type.
            NodeFactory GetDefaultNodeFactory()
            {
                return new CubeFactory(shaderType, ColorRange.Default());
            }

            // The appropriate node factory for value.Shape.
            NodeFactory GetNodeFactory(VisualNodeAttributes value)
            {
                ColorRange colorRange = GetColorRange(value.ColorProperty);

                return value.Shape switch
                {
                    NodeShapes.Blocks => new CubeFactory(shaderType, colorRange),
                    NodeShapes.Cylinders => new CylinderFactory(shaderType, colorRange),
                    NodeShapes.Spiders => new SpiderFactory(shaderType, colorRange),
                    NodeShapes.Polygons => new PolygonFactory(shaderType, colorRange),
                    NodeShapes.Bars => new BarsFactory(shaderType, colorRange),
                    _ => throw new NotImplementedException($"Missing handling of {value.Shape}.")
                };
            }

            // The color range for the given colorProperty depending upon whether the property
            // used to determine the color range is PropertyKind.Type or PropertyKind.Metric.
            ColorRange GetColorRange(ColorProperty colorProperty)
            {
                switch (colorProperty.Property)
                {
                    case PropertyKind.Type:
                        return GetColorRangeForNodeType(colorProperty.TypeColor);
                    case PropertyKind.Metric:
                        return Settings.GetColorForMetric(colorProperty.ColorMetric);
                    default:
                        throw new NotImplementedException($"Missing handling of {colorProperty.Property}.");
                }
            }

            // Returns a color range where the given color is the upper color and
            // the lower color is the given color lightened by 50 %. The number of colors
            // in this color range is the maximal node hierarchy level of all graphs.
            ColorRange GetColorRangeForNodeType(Color color)
            {
                uint maxLevel = (uint)graphs.Max(x => x.MaxDepth);

                return new ColorRange(color.Lighter(), color, maxLevel + 1);
            }

            AntennaDecorator GetAntennaDecorator(VisualNodeAttributes value)
            {
                return new AntennaDecorator
                             (scaler,
                              value.AntennaSettings, Settings.AntennaWidth, Settings.MaximalAntennaSegmentHeight,
                              Settings.MetricToColor);
            }
        }

        /// <summary>
        /// The shader to be used for drawing the nodes.
        /// </summary>
        private const Materials.ShaderType shaderType = Materials.ShaderType.OpaqueMetallic;

        /// <summary>
        /// The distance between two stacked game objects (parent/child).
        /// </summary>
        private const float levelDistance = 0.001f;

        /// <summary>
        /// the ground level of the nodes
        /// </summary>
        private const float groundLevel = 0.0f;

        /// <summary>
        /// The graphs to be rendered.
        /// </summary>
        private IList<Graph> graphs;

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        public AbstractSEECity Settings;

        /// <summary>
        /// A mapping of the name of node types of <see cref="graphs"/> onto the factories creating those nodes.
        /// </summary>
        private readonly Dictionary<string, NodeFactory> nodeTypeToFactory = new Dictionary<string, NodeFactory>();

        /// <summary>
        /// A mapping of the name of node types of <see cref="graphs"/> onto the
        /// <see cref="AntennaDecorator"/>s creating the antennas of those nodes.
        /// </summary>
        private readonly Dictionary<string, AntennaDecorator> nodeTypeToAntennaDectorator = new Dictionary<string, AntennaDecorator>();

        /// <summary>
        /// The scale used to normalize the metrics determining the lengths of the blocks.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// A mapping from Node to ILayoutNode.
        /// </summary>
        private readonly Dictionary<Node, ILayoutNode> toLayoutNode = new Dictionary<Node, ILayoutNode>();

        /// <summary>
        /// True if edges are to be actually drawn, that is, if the user has selected an
        /// edge layout different from <see cref="EdgeLayoutKind.None"/>.
        /// </summary>
        /// <returns>True if edges are to be actually drawn.</returns>
        public bool AreEdgesDrawn()
        {
            return Settings.EdgeLayoutSettings.Kind != EdgeLayoutKind.None;
        }

        /// <summary>
        /// Sets the scaler to be used to map metric values onto graphical attributes
        /// (e.g., width, height, depth, color) across all given <paramref name="graphs"/>
        /// based on the user's choice (settings).
        /// </summary>
        /// <param name="graphs">set of graphs whose node metrics are to be scaled</param>
        public void SetScaler(ICollection<Graph> graphs)
        {
            ISet<string> nodeMetrics = Graph.AllNodeMetrics(graphs);

            if (Settings.ZScoreScale)
            {
                scaler = new ZScoreScale(graphs, nodeMetrics, Settings.ScaleOnlyLeafMetrics);
            }
            else
            {
                scaler = new LinearScale(graphs, nodeMetrics, Settings.ScaleOnlyLeafMetrics);
            }
        }

        /// <summary>
        /// Returns a mapping of each graph Node onto its containing GameNode for every
        /// element in <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>mapping of graph node onto its corresponding game node</returns>
        private static Dictionary<Node, T> NodeToGameNodeMap<T>(IEnumerable<T> gameNodes) where T : AbstractLayoutNode
        {
            Dictionary<Node, T> map = new();
            foreach (T node in gameNodes)
            {
                map[node.ItsNode] = node;
            }
            return map;
        }

        /// <summary>
        /// Draws the nodes and edges of the graph and their decorations by applying the layouts according
        /// to the user's choice in the settings.
        /// </summary>
        /// <param name="graph">the graph to be drawn; it should be one initially passed to the constructor</param>
        /// <param name="parent">every game object drawn for this graph will be added to this parent</param>
        /// <param name="updateProgress">action to be called with the progress of the operation</param>
        /// <param name="token">cancellation token with which to cancel the operation</param>
        public async UniTask DrawGraphAsync(Graph graph, GameObject parent, Action<float> updateProgress = null,
                                            CancellationToken token = default)
        {
            // all nodes of the graph
            IList<Node> nodes = graph.Nodes();
            if (nodes.Count == 0)
            {
                Debug.LogWarning("The graph has no nodes.\n");
                return;
            }
            IDictionary<Node, GameObject> nodeMap = await DrawNodesAsync(nodes, x => updateProgress?.Invoke(x * 0.5f), token);

            // the layout to be applied
            NodeLayout nodeLayout = GetLayout(parent);

            // If we have multiple roots, we need to add a unique one.
            AddGameRootNodeIfNecessary(graph, nodeMap);

            // The representation of the nodes for the layout.
            ICollection<LayoutGameNode> gameNodes = ToLayoutNodes(nodeMap.Values);

            // 1) Calculate the layout.
            Performance p = Performance.Begin($"Node layout {Settings.NodeLayoutSettings.Kind} for {gameNodes.Count} nodes");
            // Equivalent to gameNodes but as an ICollection<ILayoutNode> instead of ICollection<GameNode>
            // (GameNode implements ILayoutNode).
            ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();
            // 2) Apply the calculated layout to the game objects.
            nodeLayout.Apply(layoutNodes);
            p.End();
            Debug.Log($"Built \"{Settings.NodeLayoutSettings.Kind}\" node layout for {gameNodes.Count} nodes in {p.GetElapsedTime()} [h:m:s:ms].\n");

            // Fit layoutNodes into parent.
            Fit(parent, layoutNodes);
            Stack(parent, layoutNodes);

            CreateGameNodeHierarchy(nodeMap, parent);

            // Create the laid out edges; they will be children of the unique root game node
            // representing the node hierarchy. This way the edges can be moved along with
            // the nodes.
            GameObject rootGameNode = RootGameNode(parent);
            try
            {
                await EdgeLayoutAsync(gameNodes, rootGameNode, true, x => updateProgress?.Invoke(0.5f + x * 0.5f), token);
            }
            catch (OperationCanceledException)
            {
                // If the operation gets canceled, we need to clean up the dangling edge game objects.
                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge).Where(x => x.transform.parent is null))
                {
                    Destroyer.Destroy(edge);
                }
                // Then re-throw.
                throw;
            }

            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            AddDecorations(nodeMap.Values);

            Portal.SetPortal(parent);

            // Add light to simulate emissive effect
            // AddLight(nodeToGameObject, parent);

            if (parent.TryGetComponent(out Plane portalPlane))
            {
                portalPlane.HeightOffset = rootGameNode.transform.position.y - parent.transform.position.y;
            }

            // This is necessary for the holistic metrics boards. They need to be informed when a code city is being
            // drawn because then there will be a new graph loaded. In that case, the metrics boards might
            // need to start listening for change events from that graph.
            BoardsManager.OnGraphDraw();

            updateProgress?.Invoke(1.0f);
            return;

            void AddGameRootNodeIfNecessary(Graph graph, IDictionary<Node, GameObject> nodeMap)
            {
                if (graph.GetRoots().Count > 1 && graph.AddSingleRoot(out Node artificialRoot))
                {
                    nodeMap[artificialRoot] = DrawNode(artificialRoot);
                    Debug.Log($"Artificial unique root {artificialRoot.ID} was added.\n");
                }
            }
        }

        /// <summary>
        /// The <paramref name="layoutNodes"/> are put just above the <paramref name="plane"/> w.r.t. the y axis.
        /// </summary>
        /// <param name="plane">plane upon which <paramref name="layoutNodes"/> should be stacked</param>
        /// <param name="layoutNodes">the layout nodes to be stacked</param>
        public static void Stack(GameObject plane, IEnumerable<ILayoutNode> layoutNodes)
        {
            NodeLayout.Stack(layoutNodes, plane.transform.position.y + plane.transform.lossyScale.y / 2.0f + levelDistance);
        }

        /// <summary>
        /// Adds light to simulate an emissive effect. The new light object will be added to
        /// <paramref name="parent"/> above its center such that it covers all <paramref name="gameObjects"/>.
        /// </summary>
        /// <param name="gameObjects">the game object to be covered by the light</param>
        /// <param name="parent">parent of the new light</param>
        private void AddLight(ICollection<GameObject> gameObjects, GameObject parent)
        {
            GameObject lightGameObject = new("Light")
            {
                tag = Tags.Decoration
            };
            lightGameObject.transform.parent = parent.transform;

            Light light = lightGameObject.AddComponent<Light>();

            ComputeBoundingBox(gameObjects, out Vector2 minCorner, out Vector2 maxCorner);
            float boundingBoxWidth = maxCorner.x - minCorner.x;
            float boundingBoxDepth = maxCorner.y - minCorner.y;

            lightGameObject.transform.position = parent.transform.position + new Vector3(0.0f, 0.25f * (boundingBoxWidth + boundingBoxDepth), 0.0f);

            light.range = 3.0f * Mathf.Sqrt(boundingBoxWidth * boundingBoxWidth + boundingBoxDepth * boundingBoxDepth);
            light.type = LightType.Point;
            light.intensity = 1.0f;
        }

        /// <summary>
        /// Scales and moves the <paramref name="layoutNodes"/> so that they fit into the <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">the parent in which to fit the <paramref name="layoutNodes"/></param>
        /// <param name="layoutNodes">the nodes to be fitted into the <paramref name="parent"/></param>
        public static void Fit(GameObject parent, IEnumerable<ILayoutNode> layoutNodes)
        {
            NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x, parent.transform.lossyScale.z);
            NodeLayout.MoveTo(layoutNodes, parent.transform.position);
        }

        /// <summary>
        /// Creates the same nesting of all game nodes in <paramref name="nodeMap"/> as in
        /// the graph node hierarchy. Every root node in the graph node hierarchy will become
        /// a child of the given <paramref name="root"/>.
        /// </summary>
        /// <param name="nodeMap">mapping of graph nodes onto their representing game object</param>
        /// <param name="root">the parent of every game object not nested in any other game object
        /// (must not be null)</param>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="root"/> is null</exception>
        public static void CreateGameNodeHierarchy(IDictionary<Node, GameObject> nodeMap, GameObject root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }
            foreach (KeyValuePair<Node, GameObject> entry in nodeMap)
            {
                Node node = entry.Key;
                Node parent = node.Parent;

                // If node is a root, it will be added to parent as a child.
                // Otherwise, node is a child of another game node.
                AddToParent(entry.Value, parent == null ? root : nodeMap[parent]);
                Portal.SetPortal(root, entry.Value);
            }
        }

        /// <summary>
        /// Returns the node layouter according to the settings. The node layouter will
        /// place the nodes at ground level 0. This method just returns the layouter,
        /// it does not actually calculate the layout.
        /// </summary>
        /// <param name="parent">the parent in which to fit the nodes</param>
        /// <returns>node layout selected</returns>
        public NodeLayout GetLayout(GameObject parent) =>
            Settings.NodeLayoutSettings.Kind switch
            {
                NodeLayoutKind.Manhattan => new ManhattanLayout(groundLevel),
                NodeLayoutKind.RectanglePacking => new RectanglePackingNodeLayout(groundLevel),
                NodeLayoutKind.EvoStreets => new EvoStreetsNodeLayout(groundLevel),
                NodeLayoutKind.Treemap => new TreemapLayout(groundLevel, parent.transform.lossyScale.x, parent.transform.lossyScale.z),
                NodeLayoutKind.IncrementalTreeMap => new IncrementalTreeMapLayout(
                    groundLevel,
                    parent.transform.lossyScale.x,
                    parent.transform.lossyScale.z,
                    Settings.NodeLayoutSettings.IncrementalTreeMap),
                NodeLayoutKind.Balloon => new BalloonNodeLayout(groundLevel),
                NodeLayoutKind.CirclePacking => new CirclePackingNodeLayout(groundLevel),
                NodeLayoutKind.CompoundSpringEmbedder => new CoseLayout(groundLevel, Settings),
                NodeLayoutKind.FromFile => new LoadedNodeLayout(groundLevel, Settings.NodeLayoutSettings.LayoutPath.Path),
                _ => throw new Exception("Unhandled node layout " + Settings.NodeLayoutSettings.Kind)
            };

        /// <summary>
        /// Adds <paramref name="child"/> as a child to <paramref name="parent"/>,
        /// maintaining the world position of <paramref name="child"/>.
        /// </summary>
        /// <param name="child">child to be added</param>
        /// <param name="parent">new parent of child</param>
        private static void AddToParent(GameObject child, GameObject parent)
        {
            child.transform.SetParent(parent.transform, true);
        }

        /// <summary>
        /// Adds all <paramref name="children"/> as a child to <paramref name="parent"/>.
        /// </summary>
        /// <param name="children">children to be added</param>
        /// <param name="parent">new parent of children</param>
        private static void AddToParent(ICollection<GameObject> children, GameObject parent)
        {
            foreach (GameObject child in children)
            {
                AddToParent(child, parent);
            }
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        private ICollection<LayoutGameNode> ToLayoutNodes(ICollection<GameObject> gameObjects)
        {
            return ToLayoutNodes(gameObjects, go => new LayoutGameNode(toLayoutNode, go));
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of <see cref="LayoutGameNode"/>s.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <param name="newLayoutNode">delegate that returns a new layout node <see cref="T"/> for each <see cref="GameObject"/></param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        private static ICollection<T> ToLayoutNodes<T>
            (ICollection<GameObject> gameNodes,
             Func<GameObject, T> newLayoutNode) where T : class, ILayoutNode
        {
            ICollection<T> result = gameNodes.Select(newLayoutNode).ToList();
            LayoutNodes.SetLevels(result);
            return result;
        }

        /// <summary>
        /// Returns only the inner nodes in gameNodes as a list.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>the inner nodes in gameNodes as a list</returns>
        private static IEnumerable<GameObject> FindInnerNodes(IEnumerable<GameObject> gameNodes)
        {
            return gameNodes.Where(o => !o.IsLeaf());
        }

        /// <summary>
        /// Returns only the leaf nodes in gameNodes as a list.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>the leaf nodes in gameNodes as a list</returns>
        private static IEnumerable<GameObject> FindLeafNodes(IEnumerable<GameObject> gameNodes)
        {
            return gameNodes.Where(o => o.IsLeaf());
        }

        /// <summary>
        /// Draws the nodes of the graph and returns a mapping of each graph node onto its corresponding game object.
        /// </summary>
        /// <param name="nodes">The nodes to be drawn</param>
        /// <param name="updateProgress">action to be called with the progress of the operation</param>
        /// <param name="token">token with which to cancel the operation</param>
        /// <returns>mapping of graph node onto its corresponding game object</returns>
        private async UniTask<IDictionary<Node, GameObject>> DrawNodesAsync(IList<Node> nodes,
                                                                            Action<float> updateProgress,
                                                                            CancellationToken token = default)
        {
            IDictionary<Node, GameObject> nodeMap = new Dictionary<Node, GameObject>();

            int totalNodes = nodes.Count;
            int i = 0;
            await foreach (Node node in nodes.BatchPerFrame(50, token))
            {
                nodeMap[node] = DrawNode(node);
                updateProgress?.Invoke((float)++i / totalNodes);
            }
            return nodeMap;
        }

        /// <summary>
        /// Returns the bounding box (2D rectangle) enclosing all given game nodes.
        /// </summary>
        /// <param name="gameNodes">the list of game nodes that are enclosed in the resulting bounding box</param>
        /// <param name="leftLowerCorner">the left lower front corner (x axis in 3D space) of the bounding box</param>
        /// <param name="rightUpperCorner">the right lower back corner (z axis in 3D space) of the bounding box</param>
        private static void ComputeBoundingBox(ICollection<GameObject> gameNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
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
                    Vector3 extent = go.transform.lossyScale / 2.0f;
                    // Note: position denotes the center of the object in world space
                    Vector3 position = go.transform.position;
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

        /// <summary>
        /// calculates the left lower corner position and the right uppr corner position for a given list of ILayoutNodes
        /// </summary>
        /// <param name="layoutNodes">the layout nodes</param>
        /// <param name="leftLowerCorner">the left lower corner</param>
        /// <param name="rightUpperCorner">the right upper corner</param>
        public static void ComputeBoundingBox(ICollection<ILayoutNode> layoutNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
        {
            if (layoutNodes.Count == 0)
            {
                leftLowerCorner = Vector2.zero;
                rightUpperCorner = Vector2.zero;
            }
            else
            {
                leftLowerCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
                rightUpperCorner = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

                foreach (ILayoutNode layoutNode in layoutNodes)
                {
                    Vector3 extent = layoutNode.LocalScale / 2.0f;
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
            }
        }

        /// <summary>
        /// Returns the child object of <paramref name="codeCity"/> tagged by Tags.Node.
        /// If there is no such child or if there are more than one, an exception will
        /// be thrown.
        /// </summary>
        /// <param name="codeCity">game object representing a code city</param>
        /// <returns>child object of <paramref name="codeCity"/> tagged by Tags.Node</returns>
        public static GameObject RootGameNode(GameObject codeCity)
        {
            GameObject result = null;
            foreach (Transform child in codeCity.transform)
            {
                if (child.CompareTag(Tags.Node))
                {
                    if (result == null)
                    {
                        result = child.gameObject;
                    }
                    else
                    {
                        throw new Exception($"Code city {codeCity.name} has multiple children tagged by {Tags.Node}"
                            + $": {result.name} and {child.name}");
                    }
                }
            }
            if (result == null)
            {
                throw new Exception($"Code city {codeCity.name} has no child tagged by {Tags.Node}");
            }
            return result;
        }
    }
}
