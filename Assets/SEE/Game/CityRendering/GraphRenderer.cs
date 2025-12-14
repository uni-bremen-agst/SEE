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
using SEE.GO.Factories;
using SEE.GO.Factories.NodeFactories;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
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
        /// Creates a default node factory.
        /// </summary>
        /// <returns>The created factory.</returns>
        private NodeFactory GetDefaultNodeFactory()
        {
            return new CubeFactory(shaderType, ColorRange.Default());
        }

        /// <summary>
        /// Adds a <paramref name="nodeType"/> to the factory.
        /// </summary>
        /// <param name="nodeType">The node type to be added.</param>
        public void AddNewNodeType(string nodeType)
        {
            nodeTypeToFactory[nodeType] = GetDefaultNodeFactory();
            nodeTypeToAntennaDectorator[nodeType] = null;
        }

        /// <summary>
        /// The shader to be used for drawing the nodes.
        /// </summary>
        private const MaterialsFactory.ShaderType shaderType = MaterialsFactory.ShaderType.OpaqueMetallic;

        /// <summary>
        /// The distance between two stacked game objects (parent/child).
        /// </summary>
        private const float levelDistance = 0.001f;

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
        private readonly Dictionary<string, NodeFactory> nodeTypeToFactory = new();

        /// <summary>
        /// A mapping of the name of node types of <see cref="graphs"/> onto the
        /// <see cref="AntennaDecorator"/>s creating the antennas of those nodes.
        /// </summary>
        private readonly Dictionary<string, AntennaDecorator> nodeTypeToAntennaDectorator = new();

        /// <summary>
        /// The scale used to normalize the metrics determining the lengths of the blocks.
        /// </summary>
        private IScale scaler;

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
        /// Returns a mapping of each graph node onto its containing GameNode for every
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
        ///
        /// If the <paramref name="graph"/> has multiple roots, a unique root will be added to <paramref name="graph"/>
        /// whose immediate children are all previous root nodes and a game object will be created for this new root.
        /// This, however, can be avoided by passing true to <paramref name="doNotAddUniqueRoot"/>.
        ///
        /// The game objects representing the nodes will be children of <paramref name="parent"/>, while
        /// the game objects drawn for edges will always be children of the unique root game object representing
        /// the code city as a whole. The <paramref name="parent"/> will generally be the object holding a
        /// <see cref="AbstractSEECity"/> component, while the unique root game object is representing the
        /// root of the graph node hierarchy, i.e., is part of the <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">the graph to be drawn; it should be one initially passed to the constructor since
        /// the node-type factories and the like are set up by the constructor</param>
        /// <param name="parent">every game object drawn will become an immediate child to this parent</param>
        /// <param name="updateProgress">action to be called with the progress of the operation</param>
        /// <param name="token">cancellation token with which to cancel the operation</param>
        /// <param name="doNotAddUniqueRoot">if true, no artificial unique root node will be added if there are multiple root
        /// nodes in <paramref name="graph"/></param>
        /// <returns>The resulting layout informations of the rendering.</returns>
        public async UniTask DrawGraphAsync
            (Graph graph,
             GameObject parent,
             Action<float> updateProgress = null,
             CancellationToken token = default,
             bool doNotAddUniqueRoot = false)
        {
            if (graph.NodeCount == 0)
            {
                Debug.LogWarning("The graph has no nodes.\n");
                return;
            }

            // all nodes of the graph
            IDictionary<Node, GameObject> nodeMap = await DrawNodesAsync(graph.Nodes(), x => updateProgress?.Invoke(x * 0.5f), token);

            // If we have multiple roots, we need to add a unique one.
            if (!doNotAddUniqueRoot)
            {
                AddGameRootNodeIfNecessary(graph, nodeMap);
            }

            // The representation of the nodes for the layout.
            IDictionary<Node, LayoutGameNode> gameNodes = ToLayoutNodes(nodeMap.Values, NewLayoutNode);

            // 1) Calculate the layout.
            Performance p = Performance.Begin($"Node layout {Settings.NodeLayoutSettings.Kind} for {gameNodes.Count} nodes");
            /// The layout to be applied. If <see cref="doNotAddUniqueRoot"/> is true, use <see cref="NodeLayoutKind.Treemap"/> as the layout.
            NodeLayout nodeLayout = !doNotAddUniqueRoot ? GetLayout() : GetLayout(NodeLayoutKind.Treemap);
            // Equivalent to gameNodes but as an ICollection<ILayoutNode> instead of ICollection<GameNode>
            // (GameNode implements ILayoutNode).
            ICollection<ILayoutNode> layoutNodes = gameNodes.Values.Cast<ILayoutNode>().ToList();
            // 2) Apply the calculated layout to the game objects.
            // The center position of the rectangular plane where the nodes should be placed.
            Vector3 planeCenterposition = parent.transform.position;
            planeCenterposition.y += parent.transform.lossyScale.y / 2.0f + levelDistance;
            // The rectangle (width, depth) of the plane in which the nodes should be placed.
            Vector2 planeRectangle = new(parent.transform.lossyScale.x, parent.transform.lossyScale.z);
            NodeLayout.Apply(nodeLayout.Create(layoutNodes, planeCenterposition, planeRectangle));

            p.End();
            Debug.Log($"Built \"{Settings.NodeLayoutSettings.Kind}\" node layout for {gameNodes.Count} nodes in {p.GetElapsedTime()} [h:m:s:ms].\n");

            CreateGameNodeHierarchy(nodeMap, parent);

            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            AddDecorations(nodeMap.Values);

            // Create the laid out edges; they will be children of the unique rootGameNode
            // representing the node hierarchy. This way the edges can be moved along with
            // the nodes.
            GameObject rootGameNode = RootGameNode(parent);

            ICollection<GameObject> edgeLayouts = new List<GameObject>();
            if (Settings.EdgeLayoutSettings.Kind != EdgeLayoutKind.None)
            {
                try
                {
                    edgeLayouts = await EdgeLayoutAsync(gameNodes.Values, rootGameNode, true, x => updateProgress?.Invoke(0.5f + x * 0.5f), token);
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
            }

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

            if (Settings is BranchCity)
            {
                // The authors spheres and author references are created under the code city (parent)
                // and not under the graph root game node (rootGameNode) because we do not want to
                // move them if a user shuffles the root game node.
                DrawAuthorSpheres(nodeMap, parent, graph, planeCenterposition, planeRectangle);
            }

            updateProgress?.Invoke(1.0f);

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
        /// Returns a new <see cref="LayoutGameNode"/> for the given <paramref name="go"/>.
        /// </summary>
        /// <param name="node">ignored</param>
        /// <param name="go">the game object for which to create the <see cref="LayoutGameNode"/></param>
        /// <returns>new <see cref="LayoutGameNode"/> for the given <paramref name="go"/></returns>
        private LayoutGameNode NewLayoutNode(Node node, GameObject go)
        {
            return new LayoutGameNode(go);
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
        /// Returns the node layouter according to the settings. This method just returns
        /// the layouter; it does not actually calculate the layout.
        /// </summary>
        /// <returns>node layout selected</returns>
        public NodeLayout GetLayout() => GetLayout(Settings.NodeLayoutSettings.Kind);

        /// <summary>
        /// Returns the node layouter according to <paramref name="kind"/>. This method
        /// just returns the layouter; it does not actually calculate the layout.
        /// </summary>
        /// <param name="kind">the kind of the node layout requested</param>
        /// <returns>node layout selected</returns>
        private NodeLayout GetLayout(NodeLayoutKind kind) =>
            kind switch
            {
                NodeLayoutKind.Reflexion => new ReflexionLayout(Settings.NodeLayoutSettings.ArchitectureLayoutProportion,
                                                                GetImplementationLayout(Settings.NodeLayoutSettings),
                                                                GetArchitectureLayout(Settings.NodeLayoutSettings)),
                NodeLayoutKind.RectanglePacking => new RectanglePackingNodeLayout(),
                NodeLayoutKind.EvoStreets => new EvoStreetsNodeLayout(),
                NodeLayoutKind.Treemap => new TreemapLayout(),
                NodeLayoutKind.IncrementalTreeMap => new IncrementalTreeMapLayout(Settings.NodeLayoutSettings.IncrementalTreeMap),
                NodeLayoutKind.Balloon => new BalloonNodeLayout(),
                NodeLayoutKind.CirclePacking => new CirclePackingNodeLayout(),
                NodeLayoutKind.FromFile => new LoadedNodeLayout(Settings.NodeLayoutSettings.LayoutPath.Path),
                _ => throw new Exception("Unhandled node layout " + kind)
            };

        /// <summary>
        /// Returns the node layouter for drawing the implementation according to <paramref name="nodeLayoutSettings"/>.
        /// </summary>
        /// <param name="nodeLayoutSettings">the settings for the implementation layout</param>
        /// <returns>layouter for implementation</returns>
        /// <exception cref="Exception">thrown if called for <see cref="NodeLayoutKind.Reflexion"/></exception>
        private NodeLayout GetImplementationLayout(NodeLayoutAttributes nodeLayoutSettings)
        {
            if (nodeLayoutSettings.Implementation == NodeLayoutKind.FromFile)
            {
                return new LoadedNodeLayout(nodeLayoutSettings.LayoutPath.Path);
            }
            if (nodeLayoutSettings.Implementation == NodeLayoutKind.Reflexion)
            {
                throw new Exception("Reflexion layout cannot be used as an implementation layout.");
            }
            return GetLayout(nodeLayoutSettings.Implementation);
        }

        /// <summary>
        /// Returns the node layouter for drawing the architecture according to <paramref name="nodeLayoutSettings"/>.
        /// </summary>
        /// <param name="nodeLayoutSettings">the settings for the architecture layout</param>
        /// <returns>layouter for architecture</returns>
        /// <exception cref="Exception">thrown if called for <see cref="NodeLayoutKind.Reflexion"/></exception>
        private NodeLayout GetArchitectureLayout(NodeLayoutAttributes nodeLayoutSettings)
        {
            if (nodeLayoutSettings.Architecture == NodeLayoutKind.FromFile)
            {
                return new LoadedNodeLayout(nodeLayoutSettings.ArchitectureLayoutPath.Path);
            }
            if (nodeLayoutSettings.Architecture == NodeLayoutKind.Reflexion)
            {
                throw new Exception("Reflexion layout cannot be used as an architecture layout.");
            }
            return GetLayout(nodeLayoutSettings.Architecture);
        }

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
        /// Returns a mapping of all graph nodes associated with any of the given <paramref name="gameNodes"/>
        /// onto newly created <see cref="LayoutGameNode"/>s.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>mapping of graph nodes onto newly created <see cref="LayoutGameNode"/>s</returns>
        public static IDictionary<Node, T> ToLayoutNodes<T>
            (ICollection<GameObject> gameNodes,
            Func<Node, GameObject, T> newLayoutNode)
            where T : AbstractLayoutNode
        {
            Dictionary<Node, T> result = new(gameNodes.Count);
            // Map each graph node onto its corresponding game node.
            foreach (GameObject gameNode in gameNodes)
            {
                if (gameNode.TryGetNode(out Node node))
                {
                    result[node] = newLayoutNode(node, gameNode);
                }
            }
            // Now set the children of every layout node.
            foreach (var item in result)
            {
                Node parent = item.Key;
                T parentGameNode = item.Value;
                foreach (Node child in parent.Children())
                {
                    if (result.TryGetValue(child, out T childGameNode))
                    {
                        parentGameNode.AddChild(childGameNode);
                    }
                }
            }
            LayoutNodes.SetLevels(result.Values);
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
                    Vector3 extent = layoutNode.AbsoluteScale / 2.0f;
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
        /// Returns the first immediate child object of the code-city object containing <paramref name="gameNode"/>
        /// and tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <param name="gameNode">game object representing a node in a code city or a code city as a whole</param>
        /// <returns>first immediate child object of the code-city object containing <paramref name="gameNode"/>
        /// and tagged by <see cref="Tags.Node"/></returns>
        /// <exception cref="Exception">thrown if <paramref name="gameNode"/> is not contained in
        /// a code city or if that code city has no child tagged by <see cref="Tags.Node"/></exception>
        /// <remarks>The code-city object is obtained via <see cref="SceneQueries.GetCodeCity(Transform)"/></remarks>
        private static GameObject RootGameNode(GameObject gameNode)
        {
            GameObject codeCity = gameNode.GetCodeCity();
            if (codeCity == null)
            {
                throw new Exception($"Game node {gameNode.name} is not contained in a code city.");
            }
            GameObject result = codeCity.GetCityRootNode();
            if (result == null)
            {
                throw new Exception($"Code city {codeCity.name} has no child tagged by {Tags.Node}.");
            }
            return result;
        }
    }
}
