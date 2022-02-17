using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using UnityEngine;
using Plane = SEE.GO.Plane;

namespace SEE.Game
{
    /// <summary>
    /// A renderer for graphs. Encapsulates handling of block types, node and edge layouts,
    /// decorations and other visual attributes.
    /// </summary>
    public partial class GraphRenderer
    {
        /// <summary>
        /// Constructor. If the <paramref name="graph"/> is null, you need to call
        /// SetScaler() before you can call Draw().
        /// </summary>
        /// <param name="settings">the settings for the visualization</param>
        /// <param name="graph">the graph to be rendered</param>
        public GraphRenderer(AbstractSEECity settings, Graph graph)
        {
            this.Settings = settings;

            ColorRange leafColorRange = this.Settings.LeafNodeSettings.ColorRange;
            leafNodeFactory = this.Settings.LeafNodeSettings.Kind switch
            {
                LeafNodeKinds.Blocks => new CubeFactory(ShaderType, leafColorRange),
                _ => throw new Exception($"Unhandled {nameof(LeafNodeKinds)}")
            };

            ColorRange innerColorRange = this.Settings.InnerNodeSettings.ColorRange;
            switch (this.Settings.InnerNodeSettings.Kind)
            {
                case InnerNodeKinds.Empty:
                case InnerNodeKinds.Donuts:
                    innerNodeFactory = new VanillaFactory(ShaderType, innerColorRange);
                    break;
                case InnerNodeKinds.Circles:
                    innerNodeFactory = new CircleFactory(innerColorRange);
                    break;
                case InnerNodeKinds.Cylinders:
                    innerNodeFactory = new CylinderFactory(ShaderType, innerColorRange);
                    break;
                case InnerNodeKinds.Rectangles:
                    innerNodeFactory = new RectangleFactory(innerColorRange);
                    break;
                case InnerNodeKinds.Blocks:
                    innerNodeFactory = new CubeFactory(ShaderType, innerColorRange);
                    break;
                default:
                    throw new Exception($"Unhandled {nameof(InnerNodeKinds)}");
            }
            this.graph = graph;
            if (this.graph != null)
            {
                SetScaler(graph);
                graph.SortHierarchyByName();
            }
        }

        /// <summary>
        /// The shader to be used for drawing the nodes.
        /// </summary>
        private const Materials.ShaderType ShaderType = Materials.ShaderType.Transparent;

        /// <summary>
        /// The distance between two stacked game objects (parent/child).
        /// </summary>
        private const float LevelDistance = 0.001f;

        /// <summary>
        /// the ground level of the nodes
        /// </summary>
        private const float GroundLevel = 0.0f;

        /// <summary>
        /// Type of the artificial root node, if one has to be added.
        /// </summary>
        public const string RootType = "ROOTTYPE";

        /// <summary>
        /// The graph to be rendered.
        /// </summary>
        private readonly Graph graph;

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        public readonly AbstractSEECity Settings;

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
        /// A mapping from Node to ILayoutNode.
        /// </summary>
        private readonly Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();

        /// <summary>
        /// True if edges are actually drawn, that is, if the user has selected an
        /// edge layout different from <see cref="EdgeLayoutKind.None"/>.
        /// </summary>
        /// <returns>True if edges are actually drawn.</returns>
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
            List<string> nodeMetrics = Settings.AllDefaultMetrics();

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
        /// Sets the scaler to be used to map metric values onto graphical attributes
        /// (e.g., width, height, depth, color) for given <paramref name="graph"/>
        /// based on the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose node metrics are to be scaled</param>
        public void SetScaler(Graph graph)
        {
            SetScaler(new List<Graph> { graph });
        }

        /// <summary>
        /// Returns a mapping of each graph Node onto its containing GameNode for every
        /// element in <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>mapping of graph node onto its corresponding game node</returns>
        private static Dictionary<Node, T> NodeToGameNodeMap<T>(IEnumerable<T> gameNodes) where T : AbstractLayoutNode
        {
            Dictionary<Node, T> map = new Dictionary<Node, T>();
            foreach (T node in gameNodes)
            {
                map[node.ItsNode] = node;
            }
            return map;
        }

        /// <summary>
        /// Draws the nodes and edges of the graph and their decorations by applying the layouts according
        ///  to the user's choice in the settings.
        /// </summary>
        /// <param name="parent">every game object drawn for this graph will be added to this parent</param>
        public void DrawGraph(GameObject parent)
        {
            // all nodes of the graph
            List<Node> nodes = graph.Nodes();
            if (nodes.Count == 0)
            {
                Debug.LogWarning("The graph has no nodes.\n");
                return;
            }
            // game objects for the leaves
            Dictionary<Node, GameObject> nodeMap = DrawLeafNodes(nodes);
            // the layout to be applied
            NodeLayout nodeLayout = GetLayout(parent);

            // a mapping of graph nodes onto the game objects by which they are represented
            Dictionary<Node, GameObject>.ValueCollection nodeToGameObject;
            ICollection<LayoutGameNode> gameNodes = new List<LayoutGameNode>();
            // the artificial unique graph root we add if the graph has more than one root
            Node artificialRoot = null;
            // the plane upon which the game objects will be placed
            GameObject plane;

            Performance p;
            if (Settings.NodeLayoutSettings.Kind.GetModel().CanApplySublayouts && nodeLayout.IsHierarchical())
            {
                try
                {
                    ICollection<SublayoutNode> sublayoutNodes = AddInnerNodesForSublayouts(nodeMap, nodes);
                    artificialRoot = AddRootIfNecessary(graph, nodeMap);
                    gameNodes = ToLayoutNodes(nodeMap, sublayoutNodes);
                    RemoveRootIfNecessary(ref artificialRoot, graph, nodeMap, gameNodes);

                    List<SublayoutLayoutNode> sublayoutLayoutNodes = ConvertSublayoutToLayoutNodes(sublayoutNodes.ToList());
                    foreach (SublayoutLayoutNode layoutNode in sublayoutLayoutNodes)
                    {
                        Sublayout sublayout = new Sublayout(layoutNode, GroundLevel, graph, Settings);
                        sublayout.Layout();
                    }

                    p = Performance.Begin($"Node layout {Settings.NodeLayoutSettings.Kind} (with sublayouts)");
                    // Equivalent to gameNodes but as an ICollection<ILayoutNode> instead of ICollection<GameNode>
                    // (GameNode implements ILayoutNode).
                    ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();
                    if (nodeLayout.UsesEdgesAndSublayoutNodes())
                    {
                        // FIXME: Could graph.ConnectingEdges(nodes) be replaced by graph.Edges()?
                        // The input graph is already a subset graph if not all data of the GXL file
                        // are to be drawn.
                        nodeLayout.Apply(layoutNodes, graph.ConnectingEdges(nodes), sublayoutLayoutNodes);
                    }
                    p.End();

                    Fit(parent, layoutNodes);

                    nodeToGameObject = nodeMap.Values;

                    // add the plane surrounding all game objects for nodes
                    ComputeBoundingBox(layoutNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
                    plane = DrawPlane(leftFrontCorner, rightBackCorner, parent.transform.position.y + parent.transform.lossyScale.y / 2.0f + LevelDistance);
                    AddToParent(plane, parent);
                    Stack(plane, layoutNodes);

                    CreateGameNodeHierarchy(nodeMap, parent);
                    InteractionDecorator.PrepareForInteraction(nodeToGameObject);

                    // add the decorations, too
                    if (sublayoutLayoutNodes.Count <= 0)
                    {
                        AddDecorations(nodeToGameObject);
                    }
                    else
                    {
                        AddDecorationsForSublayouts(layoutNodes, sublayoutLayoutNodes);
                    }
                }
                finally
                {
                    // If we added an artificial root node to the graph, we must remove it again
                    // from the graph when we are done.
                    RemoveRootIfNecessary(ref artificialRoot, graph, nodeMap, gameNodes);
                }
            }
            else
            {
                try
                {
                    if (nodeLayout.IsHierarchical())
                    {
                        // for a hierarchical layout, we need to add the game objects for inner nodes
                        DrawInnerNodes(nodeMap, nodes);
                        artificialRoot = AddRootIfNecessary(graph, nodeMap);
                        if (artificialRoot != null)
                        {
                            Debug.Log("Artificial unique root was added.\n");
                        }
                    }

                    // calculate and apply the node layout
                    gameNodes = ToLayoutNodes(nodeMap.Values);
                    RemoveRootIfNecessary(ref artificialRoot, graph, nodeMap, gameNodes);

                    // 1) Calculate the layout
                    p = Performance.Begin("node layout " + Settings.NodeLayoutSettings.Kind + " for " + gameNodes.Count + " nodes");
                    // Equivalent to gameNodes but as an ICollection<ILayoutNode> instead of ICollection<GameNode>
                    // (GameNode implements ILayoutNode).
                    ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();
                    nodeLayout.Apply(layoutNodes);
                    p.End();
                    Debug.Log($"Built \"{Settings.NodeLayoutSettings.Kind}\" node layout for {gameNodes.Count} nodes in {p.GetElapsedTime()} [h:m:s:ms].\n");

                    // 2) Apply the calculated layout to the game objects

                    // fit layoutNodes into parent
                    Fit(parent, layoutNodes);

                    nodeToGameObject = nodeMap.Values;

                    // add the plane surrounding all game objects for nodes
                    plane = DrawPlane(nodeToGameObject, parent.transform.position.y + parent.transform.lossyScale.y / 2.0f + LevelDistance);
                    AddToParent(plane, parent);
                    Stack(plane, layoutNodes);

                    CreateGameNodeHierarchy(nodeMap, parent);

                    // Decorations must be applied after the blocks have been placed, so that
                    // we also know their positions.
                    AddDecorations(nodeToGameObject);
                }
                finally
                {
                    // If we added an artificial root node to the graph, we must remove it again
                    // from the graph when we are done.
                    RemoveRootIfNecessary(ref artificialRoot, graph, nodeMap, gameNodes);
                }
            }

            // Create the laid out edges; they will be children of the unique root game node
            // representing the node hierarchy. This way the edges can be moved along with
            // the nodes.
            GameObject rootGameNode = RootGameNode(parent);
            EdgeLayout(gameNodes, rootGameNode);

            Portal.SetPortal(parent);

            // Add light to simulate emissive effect
            AddLight(nodeToGameObject, rootGameNode);

            if (parent.TryGetComponent(out Plane portalPlane))
            {
                portalPlane.HeightOffset = rootGameNode.transform.position.y - parent.transform.position.y;
            }
        }

        /// <summary>
        /// The <paramref name="layoutNodes"/> are put just above the <paramref name="plane"/> w.r.t. the y axis.
        /// </summary>
        /// <param name="plane">plane upon which <paramref name="layoutNodes"/> should be stacked</param>
        /// <param name="layoutNodes">the layout nodes to be stacked</param>
        public static void Stack(GameObject plane, IEnumerable<ILayoutNode> layoutNodes)
        {
            NodeLayout.Stack(layoutNodes, plane.transform.position.y + plane.transform.lossyScale.y / 2.0f + LevelDistance);
        }

        /// <summary>
        /// Adds light to simulate an emissive effect.
        /// </summary>
        /// <param name="gameObjects"></param>
        /// <param name="rootGameNode"></param>
        private void AddLight(ICollection<GameObject> gameObjects, GameObject rootGameNode)
        {
            GameObject lightGameObject = new GameObject("Light");
            lightGameObject.transform.parent = rootGameNode.transform;

            Light light = lightGameObject.AddComponent<Light>();

            ComputeBoundingBox(gameObjects, out Vector2 minCorner, out Vector2 maxCorner);
            float bbw = maxCorner.x - minCorner.x;
            float bbh = maxCorner.y - minCorner.y;

            lightGameObject.transform.position = rootGameNode.transform.position + new Vector3(0.0f, 0.25f * (bbw + bbh), 0.0f);

            light.range = 3.0f * Mathf.Sqrt(bbw * bbw + bbh * bbh);
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
            NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
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
        /// <exception cref="Exception">thrown if <paramref name="root"/> is null</exception>
        public static void CreateGameNodeHierarchy(Dictionary<Node, GameObject> nodeMap, GameObject root)
        {
            if (root == null)
            {
                throw new Exception("Root must not be null.");
            }
            foreach (KeyValuePair<Node, GameObject> entry in nodeMap)
            {
                Node node = entry.Key;
                Node parent = node.Parent;

                // If node is a root, it will be added to parent as a child.
                // Otherwise, node is a child of another game node.
                AddToParent(entry.Value, parent == null ? root : nodeMap[parent]);
                Portal.SetPortal(root, entry.Value, Portal.IncludeDescendants.ONLY_SELF);
            }
        }

        /// <summary>
        /// Adds the decoration to the sublayout
        /// </summary>
        /// <param name="layoutNodes">the layoutnodes</param>
        /// <param name="sublayoutLayoutNodes">the sublayout nodes</param>
        private void AddDecorationsForSublayouts(IEnumerable<ILayoutNode> layoutNodes, IEnumerable<SublayoutLayoutNode> sublayoutLayoutNodes)
        {
            List<ILayoutNode> remainingLayoutNodes = layoutNodes.ToList();
            foreach (SublayoutLayoutNode layoutNode in sublayoutLayoutNodes)
            {
                ICollection<GameObject> gameObjects = (from LayoutGameNode gameNode in layoutNode.Nodes select gameNode.GetGameObject()).ToList();
                AddDecorations(gameObjects, layoutNode.InnerNodeKind, layoutNode.NodeLayout);
                remainingLayoutNodes.RemoveAll(node => layoutNode.Nodes.Contains(node));
            }

            ICollection<GameObject> remainingGameObjects = (from LayoutGameNode gameNode in remainingLayoutNodes select gameNode.GetGameObject()).ToList();
            AddDecorations(remainingGameObjects);
        }

        /// <summary>
        /// Creates Sublayout and Adds the innerNodes for the sublayouts
        /// </summary>
        /// <param name="nodeMap">a map between a node and its gameobject</param>
        /// <param name="nodes">a list with nodes</param>
        /// <returns>the sublayouts</returns>
        private List<SublayoutNode> AddInnerNodesForSublayouts(Dictionary<Node, GameObject> nodeMap, List<Node> nodes)
        {
            List<SublayoutNode> coseSublayoutNodes = CreateSublayoutNodes(nodes);

            if (coseSublayoutNodes.Count > 0)
            {
                coseSublayoutNodes.Sort((n1, n2) => n2.Node.Level.CompareTo(n1.Node.Level));

                CalculateNodesSublayout(coseSublayoutNodes);

                List<Node> remainingNodes = new List<Node>(nodes);
                foreach (SublayoutNode sublayoutNode in coseSublayoutNodes)
                {
                    DrawInnerNodes(nodeMap, sublayoutNode.Nodes);
                    remainingNodes.RemoveAll(node => sublayoutNode.Nodes.Contains(node));
                }
                DrawInnerNodes(nodeMap, remainingNodes);
            }
            else
            {
                DrawInnerNodes(nodeMap, nodes);
            }
            return coseSublayoutNodes;
        }

        /// <summary>
        /// Creates the sublayoutnodes for a given set of nodes
        /// </summary>
        /// <param name="nodes">the nodes, which should be layouted as sublayouts</param>
        /// <returns>a list with sublayout nodes</returns>
        private List<SublayoutNode> CreateSublayoutNodes(IReadOnlyCollection<Node> nodes) =>
            (from dir in Settings.CoseGraphSettings.ListInnerNodeToggle
             where dir.Value
             select dir.Key into name
             where Settings.CoseGraphSettings.InnerNodeLayout.ContainsKey(name)
                   && Settings.CoseGraphSettings.InnerNodeShape.ContainsKey(name)
             let matches = nodes.Where(i => i.ID.Equals(name))
             where matches.Any()
             select new SublayoutNode(matches.First(), Settings.CoseGraphSettings.InnerNodeShape[name],
                                      Settings.CoseGraphSettings.InnerNodeLayout[name])).ToList();

        /// <summary>
        /// Calculate the child/ removed nodes for each sublayout
        /// </summary>
        /// <param name="sublayoutNodes">the sublayout nodes</param>
        private static void CalculateNodesSublayout(ICollection<SublayoutNode> sublayoutNodes)
        {
            foreach (SublayoutNode sublayoutNode in sublayoutNodes)
            {
                List<Node> children = WithAllChildren(sublayoutNode.Node);
                List<Node> childrenToRemove = new List<Node>();

                foreach (Node child in children)
                {
                    SublayoutNode sublayout = CoseHelper.CheckIfNodeIsSublayouRoot(sublayoutNodes, child.ID);

                    if (sublayout != null)
                    {
                        childrenToRemove.AddRange(sublayout.Nodes);
                    }
                }

                sublayoutNode.RemovedChildren = childrenToRemove;
                children.RemoveAll(child => childrenToRemove.Contains(child));
                sublayoutNode.Nodes = children;
            }
        }

        private List<SublayoutLayoutNode> ConvertSublayoutToLayoutNodes(List<SublayoutNode> sublayouts)
        {
            List<SublayoutLayoutNode> sublayoutLayoutNodes = new List<SublayoutLayoutNode>();
            sublayouts.ForEach(sublayoutNode =>
            {
                SublayoutLayoutNode sublayout = new SublayoutLayoutNode(to_layout_node[sublayoutNode.Node], sublayoutNode.InnerNodeKind, sublayoutNode.NodeLayout);
                sublayoutNode.Nodes.ForEach(n => sublayout.Nodes.Add(to_layout_node[n]));
                sublayoutNode.RemovedChildren.ForEach(n => sublayout.RemovedChildren.Add(to_layout_node[n]));
                sublayoutLayoutNodes.Add(sublayout);
            });
            return sublayoutLayoutNodes;
        }

        /// <summary>
        /// Calculates a list with all children for a specific node
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static List<Node> WithAllChildren(Node root)
        {
            List<Node> allNodes = new List<Node> { root };
            foreach (Node node in root.Children())
            {
                allNodes.AddRange(WithAllChildren(node));
            }
            return allNodes;
        }

        /// <summary>
        /// If <paramref name="graph"/> has a single root, nothing is done. Otherwise
        /// an artificial root is created and added to both the <paramref name="graph"/>
        /// and <paramref name="nodeMap"/> (and there mapped onto a newly created game
        /// object for inner nodes). All true roots of <paramref name="graph"/> will
        /// become children of this artificial root.
        /// Note: This method is the counterpart to RemoveRootIfNecessary.
        /// </summary>
        /// <param name="graph">graph where a unique root node should be added</param>
        /// <param name="nodeMap">mapping of nodes onto game objects, which will be updated
        /// when a new artificial root is added</param>
        /// <returns>the new artificial root or null if <paramref name="graph"/> has
        /// already a single root</returns>
        private Node AddRootIfNecessary(Graph graph, IDictionary<Node, GameObject> nodeMap)
        {
            // Note: Because this method is called only when a hierarchical layout is to
            // be applied (and then both leaves and inner nodes were added to nodeMap), we
            // could traverse through graph.GetRoots() or nodeMaps.Keys. It would not make
            // a difference. If -- for any reason --, we decide not to create a game object
            // for some inner nodes, we should rather iterate on nodeMaps.Keys.
            ICollection<Node> roots = graph.GetRoots();

            if (roots.Count > 1)
            {
                Node artificialRoot = new Node
                {
                    ID = graph.Name + "#ROOT",
                    SourceName = "ROOT",
                    Type = RootType
                };
                graph.AddNode(artificialRoot);
                foreach (Node root in roots)
                {
                    artificialRoot.AddChild(root);
                }
                nodeMap[artificialRoot] = DrawInnerNode(artificialRoot);
                return artificialRoot;
            }

            return null;
        }

        /// <summary>
        /// If <paramref name="root"/> is null, nothing happens. Otherwise <paramref name="root"/> will
        /// be removed from <paramref name="graph"/> and <paramref name="nodeMap"/>. The value of
        /// <paramref name="root"/> will be null afterward.
        /// Note: This method is the counterpart to AddRootIfNecessary.
        /// </summary>
        /// <param name="root">artificial root node to be removed (created by AddRootIfNecessary) or null;
        /// will be null afterward</param>
        /// <param name="graph">graph where <paramref name="root"/> should be removed/param>
        /// <param name="nodeMap">mapping of nodes onto game objects from which to remove
        /// <paramref name="root"/></param>
        private void RemoveRootIfNecessary(ref Node root, Graph graph, Dictionary<Node, GameObject> nodeMap, ICollection<LayoutGameNode> layoutNodes)
        {
            return;
            // FIXME: temporarily disabled because the current implementation of the
            // custom shader for culling all city objects falling off the plane assumes
            // that there is exactly one root node of the graph.

            //if (root is object)
            //{
            //    if (layoutNodes != null)
            //    {
            //        // Remove from layout
            //        GameNode toBeRemoved = null;
            //        foreach (GameNode layoutNode in layoutNodes)
            //        {
            //            if (layoutNode.ID.Equals(root.ID))
            //            {
            //                toBeRemoved = layoutNode;
            //                break;
            //            }
            //        }
            //        if (toBeRemoved != null)
            //        {
            //            layoutNodes.Remove(toBeRemoved);
            //        }
            //    }
            //    GameObject go = nodeMap[root];
            //    nodeMap.Remove(root);
            //    graph.RemoveNode(root);
            //    Destroyer.DestroyGameObject(go);
            //    root = null;
            //}
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
                NodeLayoutKind.Manhattan => new ManhattanLayout(GroundLevel, NodeFactory.Unit),
                NodeLayoutKind.RectanglePacking => new RectanglePackingNodeLayout(GroundLevel, NodeFactory.Unit),
                NodeLayoutKind.EvoStreets => new EvoStreetsNodeLayout(GroundLevel, NodeFactory.Unit),
                NodeLayoutKind.Treemap => new TreemapLayout(GroundLevel, parent.transform.lossyScale.x, parent.transform.lossyScale.z),
                NodeLayoutKind.Balloon => new BalloonNodeLayout(GroundLevel),
                NodeLayoutKind.CirclePacking => new CirclePackingNodeLayout(GroundLevel),
                NodeLayoutKind.CompoundSpringEmbedder => new CoseLayout(GroundLevel, Settings),
                NodeLayoutKind.FromFile => new LoadedNodeLayout(GroundLevel, Settings.NodeLayoutSettings.LayoutPath.Path),
                _ => throw new Exception("Unhandled node layout " + Settings.NodeLayoutSettings.Kind)
            };

        /// <summary>
        /// Creates and returns a new plane enclosing all given <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">the game objects to be enclosed by the new plane</param>
        /// <returns>new plane enclosing all given <paramref name="gameNodes"/></returns>
        public GameObject DrawPlane(ICollection<GameObject> gameNodes, float yLevel)
        {
            ComputeBoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            return DrawPlane(leftFrontCorner, rightBackCorner, yLevel);
        }

        /// <summary>
        /// Returns a new plane for a vector describing the left front corner position and a vector describing the right bar position
        /// </summary>
        /// <param name="leftFrontCorner">the left front corner</param>
        /// <param name="rightBackCorner">the right back corner</param>
        /// <returns>a new plane</returns>
        public GameObject DrawPlane(Vector2 leftFrontCorner, Vector2 rightBackCorner, float yLevel)
        {
            return PlaneFactory.NewPlane(ShaderType, leftFrontCorner, rightBackCorner, yLevel, Color.gray, LevelDistance);
        }

        /// <summary>
        /// Adjusts the x and z co-ordinates of the given <paramref name="plane"/> so that all
        /// <paramref name="gameNodes"/> fit onto it.
        /// </summary>
        /// <param name="plane">the plane to be adjusted</param>
        /// <param name="gameNodes">the game nodes that should be fitted onto <paramref name="plane"/></param>
        public void AdjustPlane(GameObject plane, ICollection<GameObject> gameNodes)
        {
            ComputeBoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            PlaneFactory.AdjustXZ(plane, leftFrontCorner, rightBackCorner);
        }

        /// <summary>
        /// Determines the new <paramref name="centerPosition"/> and <paramref name="scale"/> for the given
        /// <paramref name="plane"/> so that it would enclose all given <paramref name="gameNodes"/>
        /// and the y co-ordinate and the height of <paramref name="plane"/> would remain the same.
        ///
        /// Precondition: <paramref name="plane"/> is a plane game object.
        /// </summary>
        /// <param name="plane">a plane game object to be adjusted</param>
        /// <param name="gameNodes">the game nodes that should be fitted onto <paramref name="plane"/></param>
        /// <param name="centerPosition">the new center of the plane</param>
        /// <param name="scale">the new scale of the plane</param>
        public void GetPlaneTransform(GameObject plane, ICollection<GameObject> gameNodes, out Vector3 centerPosition, out Vector3 scale)
        {
            ComputeBoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            PlaneFactory.GetTransform(plane, leftFrontCorner, rightBackCorner, out centerPosition, out scale);
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
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        public ICollection<LayoutGameNode> ToLayoutNodes(ICollection<GameObject> gameObjects)
        {
            return ToLayoutNodes(gameObjects, leafNodeFactory, innerNodeFactory, to_layout_node);
        }

        /// <summary>
        /// Converts the given nodes and sublayoutsnodes to a List with ILayoutNodes
        /// </summary>
        /// <param name="nodeMap">mapping between nodes and gameobjects</param>
        /// <param name="sublayoutNodes">a collection with sublayoutNodes</param>
        /// <returns></returns>
        private ICollection<LayoutGameNode> ToLayoutNodes(Dictionary<Node, GameObject> nodeMap, IEnumerable<SublayoutNode> sublayoutNodes)
        {
            List<LayoutGameNode> layoutNodes = new List<LayoutGameNode>();
            List<GameObject> remainingGameobjects = nodeMap.Values.ToList();

            foreach (SublayoutNode sublayoutNode in sublayoutNodes)
            {
                ICollection<GameObject> gameObjects = new List<GameObject>();
                sublayoutNode.Nodes.ForEach(node => gameObjects.Add(nodeMap[node]));
                layoutNodes.AddRange(ToLayoutNodes(gameObjects, leafNodeFactory, innerNodeFactory, to_layout_node));
                remainingGameobjects.RemoveAll(gameObject => gameObjects.Contains(gameObject));
            }

            layoutNodes.AddRange(ToLayoutNodes(remainingGameobjects, leafNodeFactory, innerNodeFactory, to_layout_node));

            return layoutNodes;
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <param name="leafNodeFactory">the leaf node factory that created the leaf nodes in <paramref name="gameNodes"/></param>
        /// <param name="innerNodeFactory">the inner node factory that created the inner nodes in <paramref name="gameNodes"/></param>
        /// <param name="toLayoutNode">a mapping from graph nodes onto their corresponding layout node</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        private static ICollection<LayoutGameNode> ToLayoutNodes
            (ICollection<GameObject> gameNodes,
             NodeFactory leafNodeFactory,
             NodeFactory innerNodeFactory,
             Dictionary<Node, ILayoutNode> toLayoutNode)
        {
            IList<LayoutGameNode> result = new List<LayoutGameNode>(gameNodes.Count);

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().Value;
                NodeFactory factory = node.IsLeaf() ? leafNodeFactory : innerNodeFactory;
                result.Add(new LayoutGameNode(toLayoutNode, gameObject, factory));
            }
            LayoutNodes.SetLevels(result.Cast<ILayoutNode>().ToList());
            return result;
        }

        /// <summary>
        /// Returns only the inner nodes in gameNodes as a list.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>the inner nodes in gameNodes as a list</returns>
        private static ICollection<GameObject> FindInnerNodes(IEnumerable<GameObject> gameNodes)
        {
            return gameNodes.Where(o => !o.IsLeaf()).ToList();
        }

        /// <summary>
        /// Returns only the leaf nodes in gameNodes as a list.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>the leaf nodes in gameNodes as a list</returns>
        private static ICollection<GameObject> FindLeafNodes(IEnumerable<GameObject> gameNodes)
        {
            return gameNodes.Where(o => o.IsLeaf()).ToList();
        }

        /// <summary>
        /// Creates and scales blocks for all leaf nodes in given list of nodes.
        /// </summary>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        /// <returns>blocks for all leaf nodes in given list of nodes</returns>
        protected Dictionary<Node, GameObject> DrawLeafNodes(IList<Node> nodes)
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>(nodes.Count);

            foreach (Node node in nodes)
            {
                // We add only leaves.
                if (node.IsLeaf())
                {
                    result[node] = DrawLeafNode(node);
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
        protected void DrawInnerNodes(Dictionary<Node, GameObject> nodeMap, IList<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                // We add only inner nodes.
                if (!node.IsLeaf())
                {
                    GameObject innerGameObject = DrawInnerNode(node);
                    nodeMap[node] = innerGameObject;
                }
            }
        }

        /// <summary>
        /// Returns the bounding box (2D rectangle) enclosing all given game nodes.
        /// </summary>
        /// <param name="gameNodes">the list of game nodes that are enclosed in the resulting bounding box</param>
        /// <param name="leftLowerCorner">the left lower front corner (x axis in 3D space) of the bounding box</param>
        /// <param name="rightUpperCorner">the right lower back corner (z axis in 3D space) of the bounding box</param>
        private void ComputeBoundingBox(ICollection<GameObject> gameNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
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
                    Node node = go.GetComponent<NodeRef>().Value;

                    Vector3 extent = node.IsLeaf() ? leafNodeFactory.GetSize(go) / 2.0f : innerNodeFactory.GetSize(go) / 2.0f;
                    // Note: position denotes the center of the object
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
