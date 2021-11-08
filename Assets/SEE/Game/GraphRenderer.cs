using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using Plane = SEE.GO.Plane;

namespace SEE.Game
{
    /// <summary>
    /// A renderer for graphs. Encapsulates handling of block types, node and edge layouts,
    /// decorations and other visual attributes.
    /// </summary>
    public class GraphRenderer
    {
        /// <summary>
        /// Constructor. If the <paramref name="graph"/> is null, you need to call
        /// SetScaler() before you can call Draw().
        /// </summary>
        /// <param name="graph">the graph to be rendered</param>
        /// <param name="settings">the settings for the visualization</param>
        public GraphRenderer(City.AbstractSEECity settings, Graph graph)
        {
            this.settings = settings;

            ColorRange leafColorRange = this.settings.LeafNodeSettings.ColorRange;
            leafNodeFactory = this.settings.LeafNodeSettings.Kind switch
            {
                LeafNodeKinds.Blocks => new CubeFactory(ShaderType, leafColorRange),
                _ => throw new Exception($"Unhandled {nameof(LeafNodeKinds)}")
            };

            ColorRange innerColorRange = this.settings.InnerNodeSettings.ColorRange;
            switch (this.settings.InnerNodeSettings.Kind)
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
        public readonly City.AbstractSEECity settings;

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
        /// Sets the scaler to be used to map metric values onto graphical attributes
        /// (e.g., width, height, depth, color) across all given <paramref name="graphs"/>
        /// based on the user's choice (settings).
        /// </summary>
        /// <param name="graphs">set of graphs whose node metrics are to be scaled</param>
        public void SetScaler(ICollection<Graph> graphs)
        {
            List<string> nodeMetrics = settings.AllMetricAttributes();

            if (settings.ZScoreScale)
            {
                scaler = new ZScoreScale(graphs, nodeMetrics, settings.ScaleOnlyLeafMetrics);
            }
            else
            {
                scaler = new LinearScale(graphs, nodeMetrics, settings.ScaleOnlyLeafMetrics);
            }
        }

        /// <summary>
        /// Sets the scaler to be used to map metric values onto graphical attributes
        /// (e.g., width, height, depth, style) for given <paramref name="graph"/>
        /// based on the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose node metrics are to be scaled</param>
        public void SetScaler(Graph graph)
        {
            SetScaler(new List<Graph> { graph });
        }

        /// <summary>
        /// Creates and returns a new edge between <paramref name="from"/> and <paramref name="to"/>
        /// based on the current settings. A new edge will be added to the underlying graph, too.
        ///
        /// Note: A default edge layout will be used if no edge layout was chosen.
        ///
        /// Precondition: <paramref name="from"/> and <paramref name="to"/> must have a valid
        /// node reference. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="from">source of the new edge</param>
        /// <param name="to">target of the new edge</param>
        /// <param name="id">id of the new edge. If it is null or empty, a new id will be generated</param>
        /// <returns>the new edge</returns>
        /// <exception cref="Exception">thrown if <paramref name="from"/> or <paramref name="to"/>
        /// are not contained in any graph or contained in different graphs</exception>
        public GameObject DrawEdge(GameObject from, GameObject to, string id)
        {
            Node fromNode = from.GetNode();
            if (fromNode == null)
            {
                throw new Exception($"The source {from.name} of the edge is not contained in any graph.");
            }
            Node toNode = to.GetNode();
            if (toNode == null)
            {
                throw new Exception($"The target {to.name} of the edge is not contained in any graph.");
            }
            if (fromNode.ItsGraph != toNode.ItsGraph)
            {
                throw new Exception($"The source {from.name} and target {to.name} of the edge are in different graphs.");
            }

            // Creating the edge in the underlying graph
            Edge edge = string.IsNullOrEmpty(id) ? new Edge() : new Edge(id);
            edge.Source = fromNode;
            edge.Target = toNode;
            edge.Type = Graph.UnknownType; // FIXME: We need to set the type of the edge.

            Graph graph = fromNode.ItsGraph;
            graph.AddEdge(edge);
            // Save edge layout so that we can restore it if we need to select a default layout.
            EdgeLayoutKind savedEdgeLayout = settings.EdgeLayoutSettings.Kind;
            if (savedEdgeLayout == EdgeLayoutKind.None)
            {
                Debug.LogWarning($"An edge {edge.ID} from {fromNode.ID} to {toNode.ID} was added to the graph, but no edge layout was chosen.\n");
                // Select default layout
                settings.EdgeLayoutSettings.Kind = EdgeLayoutKind.Spline;
            }

            // Creating the game object representing the edge.
            // The edge layout will be calculated for the following gameNodes. This list will
            // contain the source and target of the edge but also all their ascendants. The
            // ascendants are needed for hierarchical layouts.
            HashSet<GameObject> gameNodes = new HashSet<GameObject>();
            // We add the descendants of the source and target nodes in case the edge layout is hierarchical.
            AddAscendants(from, gameNodes);
            AddAscendants(to, gameNodes);
            Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();
            // The layout nodes corresponding to those game nodes.
            ICollection<GameNode> layoutNodes = ToLayoutNodes(gameNodes, leafNodeFactory, innerNodeFactory, to_layout_node);

            GameNode fromLayoutNode = null; // layout node in layoutNodes corresponding to source node
            GameNode toLayoutNode = null;   // layout node in layoutNodes corresponding to target node
            // We need fromLayoutNode and toLayoutNode to create a single layout edge to be passed
            // to the edge layouter.
            foreach (GameNode layoutNode in layoutNodes)
            {
                //TODO: Should this be a ReferenceEquals() or Equals() comparison?
                if (layoutNode.ItsNode == fromNode)
                {
                    fromLayoutNode = layoutNode;
                }
                // note: fromNode = toNode is possible, hence, there is no 'else' here.
                if (layoutNode.ItsNode == toNode)
                {
                    toLayoutNode = layoutNode;
                }
            }
            Assert.IsNotNull(fromLayoutNode, $"source node {fromNode.ID} does not have a layout node.\n");
            Assert.IsNotNull(toLayoutNode, $"target node {toNode.ID} does not have a layout node.\n");
            // The single layout edge between source and target. We want the layout only for this edge.
            ICollection<LayoutEdge> layoutEdges = new List<LayoutEdge> { new LayoutEdge(fromLayoutNode, toLayoutNode, edge) };
            // Calculate the edge layout (for the single edge only).
            ICollection<GameObject> edges = EdgeLayout(layoutNodes, layoutEdges);
            GameObject resultingEdge = edges.FirstOrDefault();
            InteractionDecorator.PrepareForInteraction(resultingEdge);
            // The edge becomes a child of the root node of the game-node hierarchy
            GameObject codeCity = SceneQueries.GetCodeCity(from.transform).gameObject;
            GameObject rootNode = SceneQueries.GetCityRootNode(codeCity).gameObject;
            resultingEdge.transform.SetParent(rootNode.transform);
            // The portal of the new edge is inherited from the codeCity.
            Portal.SetPortal(root: codeCity, gameObject: resultingEdge);
            // Reset original edge layout.
            settings.EdgeLayoutSettings.Kind = savedEdgeLayout;
            return resultingEdge;
        }

        /// <summary>
        /// Adds <paramref name="node"/> and all its transitive parent game objects tagged by
        /// Tags.Node to <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="node">the game objects whose ascendant game nodes are to be added to <paramref name="gameNodes"/></param>
        /// <param name="gameNodes">where to add the ascendants</param>
        private void AddAscendants(GameObject node, HashSet<GameObject> gameNodes)
        {
            GameObject cursor = node;
            while (cursor != null && cursor.CompareTag(Tags.Node))
            {
                gameNodes.Add(cursor);
                cursor = cursor.transform.parent.gameObject;
            }
        }

        /// <summary>
        /// Applies the edge layout according to the user's choice (settings) for
        /// all edges in between nodes in <paramref name="gameNodes"/>. The resulting
        /// edges are added to <paramref name="parent"/> as children.
        /// </summary>
        /// <param name="gameNodes">the subset of nodes for which to draw the edges</param>
        /// <param name="parent">the object the new edges are to become children of</param>
        /// <param name="draw">Decides whether the edges should only be calculated, or whether they should also be drawn.</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        public ICollection<GameObject> EdgeLayout(ICollection<GameObject> gameNodes, GameObject parent, bool draw = true)
        {
            return EdgeLayout(ToLayoutNodes(gameNodes), parent, draw);
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings) for
        /// all edges in between nodes in <paramref name="gameNodes"/>. The resulting
        /// edges are added to <paramref name="parent"/> as children.
        /// </summary>
        /// <param name="gameNodes">the subset of nodes for which to draw the edges</param>
        /// <param name="parent">the object the new edges are to become children of</param>
        /// <param name="draw">Decides whether the edges should only be calculated, or whether they should also be drawn.</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private ICollection<GameObject> EdgeLayout(ICollection<GameNode> gameNodes, GameObject parent, bool draw = true)
        {
            ICollection<GameObject> result = EdgeLayout(gameNodes, ConnectingEdges(gameNodes), draw);
            AddToParent(result, parent);
            return result;
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="gameNodes">the set of layout nodes for which to create game edges</param>
        /// <param name="layoutEdges">the edges to be laid out</param>
        /// <param name="draw">Decides whether the edges should only be calculated, or whether they should also be drawn.</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private ICollection<GameObject> EdgeLayout(ICollection<GameNode> gameNodes, ICollection<LayoutEdge> layoutEdges, bool draw = true)
        {
            float minimalEdgeLevelDistance = 2.5f * settings.EdgeLayoutSettings.EdgeWidth;
            bool edgesAboveBlocks = settings.EdgeLayoutSettings.EdgesAboveBlocks;
            float rdp = settings.EdgeLayoutSettings.RDP;
            IEdgeLayout layout;
            switch (settings.EdgeLayoutSettings.Kind)
            {
                case EdgeLayoutKind.Straight:
                    layout = new StraightEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance);
                    break;
                case EdgeLayoutKind.Spline:
                    layout = new SplineEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance, rdp);
                    break;
                case EdgeLayoutKind.Bundling:
                    layout = new BundledEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance, settings.EdgeLayoutSettings.Tension, rdp);
                    break;
                case EdgeLayoutKind.None:
                    // nothing to be done
                    return new List<GameObject>();
                default:
                    throw new Exception("Unhandled edge layout " + settings.EdgeLayoutSettings.Kind);
            }
#if UNITY_EDITOR
            Performance p = Performance.Begin("edge layout " + layout.Name);
#endif
            EdgeFactory edgeFactory = new EdgeFactory(
                layout,
                settings.EdgeLayoutSettings.EdgeWidth,
                settings.EdgeSelectionSettings.TubularSegments,
                settings.EdgeSelectionSettings.Radius,
                settings.EdgeSelectionSettings.RadialSegments,
                settings.EdgeSelectionSettings.AreSelectable);
            // The resulting game objects representing the edges.
            ICollection<GameObject> result;
            // Calculate only
            if (!draw)
            {
                result = edgeFactory.CalculateNewEdges(gameNodes.Cast<ILayoutNode>().ToList(), layoutEdges);
            }
            // Calculate and draw edges
            else
            {
                result = edgeFactory.DrawEdges(gameNodes.Cast<ILayoutNode>().ToList(), layoutEdges);
                InteractionDecorator.PrepareForInteraction(result);
                AddLOD(result);
            }


#if UNITY_EDITOR
            p.End();
            Debug.Log($"Calculated \"  {settings.EdgeLayoutSettings.Kind} \" edge layout for {gameNodes.Count}"
                      + $" nodes and {result.Count} edges in {p.GetElapsedTime()} [h:m:s:ms].\n");
#endif
            return result;
        }

        /// <summary>
        /// Returns the list of layout edges for all edges in between <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">set of game nodes whose connecting edges are requested</param>
        /// <returns>list of layout edges/returns>
        private static ICollection<LayoutEdge> ConnectingEdges(ICollection<GameNode> gameNodes)
        {
            ICollection<LayoutEdge> edges = new List<LayoutEdge>();
            Dictionary<Node, GameNode> map = NodeToGameNodeMap(gameNodes);

            foreach (GameNode source in gameNodes)
            {
                Node sourceNode = source.ItsNode;

                foreach (Edge edge in sourceNode.Outgoings)
                {
                    Node target = edge.Target;
                    edges.Add(new LayoutEdge(source, map[target], edge));
                }
            }
            return edges;
        }

        /// <summary>
        /// Returns a mapping of each graph Node onto its containing GameNode for every
        /// element in <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes"></param>
        /// <returns>mapping of graph node onto its corresponding game node</returns>
        private static Dictionary<Node, GameNode> NodeToGameNodeMap(ICollection<GameNode> gameNodes)
        {
            Dictionary<Node, GameNode> map = new Dictionary<Node, GameNode>();
            foreach (GameNode node in gameNodes)
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
        public void Draw(GameObject parent)
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
            AdjustScaleBetweenNodeKinds(nodeMap);
            // the layout to be applied
            NodeLayout nodeLayout = GetLayout(parent);

            // a mapping of graph nodes onto the game objects by which they are represented
            Dictionary<Node, GameObject>.ValueCollection nodeToGameObject;
            ICollection<GameNode> gameNodes = new List<GameNode>();
            // the artificial unique graph root we add if the graph has more than one root
            Node artificialRoot = null;
            // the plane upon which the game objects will be placed
            GameObject plane;

            Performance p;
            if (settings.NodeLayoutSettings.Kind.GetModel().CanApplySublayouts && nodeLayout.IsHierarchical())
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
                        Sublayout sublayout = new Sublayout(layoutNode, GroundLevel, graph, settings);
                        sublayout.Layout();
                    }

                    p = Performance.Begin("node layout " + settings.NodeLayoutSettings.Kind + " (with sublayouts)");
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
                        AddDecorationsForSublayouts(layoutNodes, sublayoutLayoutNodes, parent);
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
                    p = Performance.Begin("node layout " + settings.NodeLayoutSettings.Kind + " for " + gameNodes.Count + " nodes");
                    // Equivalent to gameNodes but as an ICollection<ILayoutNode> instead of ICollection<GameNode>
                    // (GameNode implements ILayoutNode).
                    ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();
                    nodeLayout.Apply(layoutNodes);
                    p.End();
                    Debug.Log($"Built \"{settings.NodeLayoutSettings.Kind}\" node layout for {gameNodes.Count} nodes in {p.GetElapsedTime()} [h:m:s:ms].\n");

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
        public static void Stack(GameObject plane, ICollection<ILayoutNode> layoutNodes)
        {
            NodeLayout.Stack(layoutNodes, plane.transform.position.y + plane.transform.lossyScale.y / 2.0f + LevelDistance);
        }

        /// <summary>
        /// Applies the given <paramref name="layout"/> to the given <paramref name="gameNode"/>,
        /// i.e., sets its size and position according to the <paramref name="layout"/> and
        /// possibly rotates it. The game node can represent a leaf or inner node of the node
        /// hierarchy.
        ///
        /// Precondition: <paramref name="gameNode"/> must have NodeRef component referencing a
        /// graph node.
        /// </summary>
        /// <param name="gameNode">the game node the layout should be applied to</param>
        /// <param name="layout">layout to be applied to the game node</param>
        public void Apply(GameObject gameNode, GameObject itsParent, ILayoutNode layout)
        {
            Node node = gameNode.GetComponent<NodeRef>().Value;

            if (node.IsLeaf())
            {
                // Leaf nodes were created as blocks by leaveNodeFactory.
                // We need to first scale the game node and only afterwards set its
                // position because transform.scale refers to the center position.
                leafNodeFactory.SetSize(gameNode, layout.LocalScale);
                // FIXME: Must adjust layout.CenterPosition.y
                leafNodeFactory.SetGroundPosition(gameNode, layout.CenterPosition);
            }
            else
            {
                // Inner nodes were created by innerNodeFactory.
                innerNodeFactory.SetSize(gameNode, layout.LocalScale);
                // FIXME: Must adjust layout.CenterPosition.y
                innerNodeFactory.SetGroundPosition(gameNode, layout.CenterPosition);
            }
            // Rotate the game object.
            if (node.IsLeaf())
            {
                leafNodeFactory.Rotate(gameNode, layout.Rotation);
            }
            else
            {
                innerNodeFactory.Rotate(gameNode, layout.Rotation);
            }

            // fit layoutNodes into parent
            //Fit(itsParent, layoutNodes); // FIXME

            // Stack the node onto its parent (maintaining its x and z co-ordinates)
            Vector3 levelIncrease = gameNode.transform.position;
            levelIncrease.y = itsParent.transform.position.y + itsParent.transform.lossyScale.y / 2.0f + LevelDistance;
            gameNode.transform.position = levelIncrease;

            // Add the node to the node hierarchy
            gameNode.transform.SetParent(itsParent.transform);

            // Prepare the node for interactions
            InteractionDecorator.PrepareForInteraction(gameNode);

            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            AddDecorations(gameNode);
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
        public static void Fit(GameObject parent, ICollection<ILayoutNode> layoutNodes)
        {
            NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
            NodeLayout.MoveTo(layoutNodes, parent.transform.position);
        }

        /// <summary>
        /// Creates the same nesting of all game nodes in <paramref name="nodeMap"/> as in
        /// the graph node hierarchy. Every root node in the graph node hierarchy will become
        /// a child of the given <paramref name="root"/>.
        /// </summary>
        /// <param name="nodeMap">mapping of graph node IDs onto their representing game objects</param>
        /// <param name="root">the parent of every game object not nested in any other game object</param>
        public static void CreateGameNodeHierarchy(Dictionary<Node, GameObject> nodeMap, GameObject root)
        {
            foreach (KeyValuePair<Node, GameObject> entry in nodeMap)
            {
                Node node = entry.Key;
                Node parent = node.Parent;

                if (parent == null)
                {
                    // node is a root => it will be added to parent as a child
                    AddToParent(entry.Value, root);
                }
                else
                {
                    // node is a child of another game node
                    AddToParent(entry.Value, nodeMap[parent]);
                }
            }
        }

        /// <summary>
        /// Adds the decoration to the sublayout
        /// </summary>
        /// <param name="layoutNodes">the layoutnodes</param>
        /// <param name="sublayoutLayoutNodes">the sublayout nodes</param>
        /// <param name="parent">the parent gameobject</param>
        private void AddDecorationsForSublayouts(IEnumerable<ILayoutNode> layoutNodes, IEnumerable<SublayoutLayoutNode> sublayoutLayoutNodes, GameObject parent)
        {
            List<ILayoutNode> remainingLayoutNodes = layoutNodes.ToList();
            foreach (SublayoutLayoutNode layoutNode in sublayoutLayoutNodes)
            {
                ICollection<GameObject> gameObjects = (from GameNode gameNode in layoutNode.Nodes select gameNode.GetGameObject()).ToList();
                AddDecorations(gameObjects, layoutNode.InnerNodeKind, layoutNode.NodeLayout);
                remainingLayoutNodes.RemoveAll(node => layoutNode.Nodes.Contains(node));
            }

            ICollection<GameObject> remainingGameObjects = (from GameNode gameNode in remainingLayoutNodes select gameNode.GetGameObject()).ToList();

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
            (from dir in settings.CoseGraphSettings.ListInnerNodeToggle
             where dir.Value
             select dir.Key into name
             where settings.CoseGraphSettings.InnerNodeLayout.ContainsKey(name)
                   && settings.CoseGraphSettings.InnerNodeShape.ContainsKey(name)
             let matches = nodes.Where(i => i.ID.Equals(name))
             where matches.Any()
             select new SublayoutNode(matches.First(), settings.CoseGraphSettings.InnerNodeShape[name],
                                      settings.CoseGraphSettings.InnerNodeLayout[name])).ToList();

        /// <summary>
        /// Calculate the child/ removed nodes for each sublayout
        /// </summary>
        /// <param name="sublayoutNodes">the sublayout nodes</param>
        private void CalculateNodesSublayout(ICollection<SublayoutNode> sublayoutNodes)
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
                    SourceName = graph.Name + "#ROOT",
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
        private void RemoveRootIfNecessary(ref Node root, Graph graph, Dictionary<Node, GameObject> nodeMap, ICollection<GameNode> layoutNodes)
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
            settings.NodeLayoutSettings.Kind switch
            {
                NodeLayoutKind.Manhattan => new ManhattanLayout(GroundLevel, NodeFactory.Unit),
                NodeLayoutKind.RectanglePacking => new RectanglePackingNodeLayout(GroundLevel, NodeFactory.Unit),
                NodeLayoutKind.EvoStreets => new EvoStreetsNodeLayout(GroundLevel, NodeFactory.Unit),
                NodeLayoutKind.Treemap => new TreemapLayout(GroundLevel, parent.transform.lossyScale.x, parent.transform.lossyScale.z),
                NodeLayoutKind.Balloon => new BalloonNodeLayout(GroundLevel),
                NodeLayoutKind.CirclePacking => new CirclePackingNodeLayout(GroundLevel),
                NodeLayoutKind.CompoundSpringEmbedder => new CoseLayout(GroundLevel, settings),
                NodeLayoutKind.FromFile => new LoadedNodeLayout(GroundLevel, settings.NodeLayoutSettings.LayoutPath.Path),
                _ => throw new Exception("Unhandled node layout " + settings.NodeLayoutSettings.Kind)
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
        protected static void AddToParent(GameObject child, GameObject parent)
        {
            child.transform.SetParent(parent.transform, true);
        }

        /// <summary>
        /// Adds all <paramref name="children"/> as a child to <paramref name="parent"/>.
        /// </summary>
        /// <param name="children">children to be added</param>
        /// <param name="parent">new parent of children</param>
        protected static void AddToParent(ICollection<GameObject> children, GameObject parent)
        {
            foreach (GameObject child in children)
            {
                AddToParent(child, parent);
            }
        }

        /// <summary>
        /// Adds decoration for the given <paramref name="gameNode"/> with the global settings
        /// for inner node kinds and nodelayout.
        /// </summary>
        /// <param name="gameNode"></param>
        protected void AddDecorations(GameObject gameNode)
        {
            AddDecorations(new List<GameObject> { gameNode });
        }

        /// <summary>
        /// Adds decoration for the given list of <paramref name="gameNodes"/> with the global settings
        /// for inner node kinds and nodelayout.
        /// </summary>
        /// <param name="gameNodes">a list with gamenode objects</param>
        protected void AddDecorations(ICollection<GameObject> gameNodes)
        {
            // FIXME the decorations must be added for every kind separately. currently, the kind
            // at index '0' is used, which is not correct
            AddDecorations(gameNodes, settings.InnerNodeSettings.Kind, settings.NodeLayoutSettings.Kind);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        /// <param name="innerNodeKinds">the inner node kinds for the gameobject</param>
        /// <param name="nodeLayout">the nodeLayout used for this gameobject</param>
        /// <returns>the game objects added for the decorations; may be an empty collection</returns>
        private void AddDecorations(ICollection<GameObject> gameNodes, InnerNodeKinds innerNodeKinds,
                                    NodeLayoutKind nodeLayout)
        {
            ICollection<GameObject> leafNodes = FindLeafNodes(gameNodes);
            ICollection<GameObject> innerNodes = FindInnerNodes(gameNodes);

            // Add software erosion decorators for all nodes if requested.
            if (settings.ErosionSettings.ShowInnerErosions)
            {
                //FIXME: This should instead check whether each node has non-aggregated metrics available,
                // and use those instead of the aggregated ones, because they are usually more accurate (see MetricImporter).
                ErosionIssues issueDecorator = new ErosionIssues(settings.InnerIssueMap(), innerNodeFactory,
                                                                 scaler, settings.ErosionSettings.ErosionScalingFactor);
                issueDecorator.Add(innerNodes);
            }
            if (settings.ErosionSettings.ShowLeafErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(settings.LeafIssueMap(), leafNodeFactory,
                                                                 scaler, settings.ErosionSettings.ErosionScalingFactor*5);
                issueDecorator.Add(leafNodes);
            }

            // Add text labels for all inner nodes
            if (nodeLayout == NodeLayoutKind.Balloon
                || nodeLayout == NodeLayoutKind.EvoStreets
                || nodeLayout == NodeLayoutKind.CirclePacking)
            {
                AddLabels(innerNodes, innerNodeFactory);
            }

            // Add decorators specific to the shape of inner nodes (circle decorators for circles
            // and donut decorators for donuts).

            switch (innerNodeKinds)
            {
                case InnerNodeKinds.Empty:
                    // do nothing
                    break;
                case InnerNodeKinds.Circles:
                    {
                        // We want to adjust the size and the line width of the circle line created by the CircleFactory.
                        CircleDecorator decorator = new CircleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(innerNodes);
                    }
                    break;
                case InnerNodeKinds.Rectangles:
                    {
                        // We want to adjust the line width of the rectangle line created by the RectangleFactory.
                        RectangleDecorator decorator = new RectangleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(innerNodes);
                    }
                    break;
                case InnerNodeKinds.Donuts:
                    {
                        DonutDecorator decorator = new DonutDecorator(innerNodeFactory, scaler, settings.InnerNodeSettings.InnerDonutMetric,
                                                                      settings.AllInnerNodeIssues().ToArray());
                        // the circle segments and the inner circle for the donut are added as children by Add();
                        // that is why we do not add the result to decorations.
                        decorator.Add(innerNodes);
                    }
                    break;
                case InnerNodeKinds.Cylinders:
                    break;
                case InnerNodeKinds.Blocks:
                    // TODO
                    break;
                default:
                    throw new InvalidOperationException("Unhandled GraphSettings.InnerNodeKinds "
                                                        + $"{settings.InnerNodeSettings.Kind}");
            }
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        public ICollection<GameNode> ToLayoutNodes(ICollection<GameObject> gameObjects)
        {
            return ToLayoutNodes(gameObjects, leafNodeFactory, innerNodeFactory, to_layout_node);
        }

        /// <summary>
        /// Converts the given nodes and sublayoutsnodes to a List with ILayoutNodes
        /// </summary>
        /// <param name="nodeMap">mapping between nodes and gameobjects</param>
        /// <param name="sublayoutNodes">a collection with sublayoutNodes</param>
        /// <returns></returns>
        private ICollection<GameNode> ToLayoutNodes(Dictionary<Node, GameObject> nodeMap, IEnumerable<SublayoutNode> sublayoutNodes)
        {
            List<GameNode> layoutNodes = new List<GameNode>();
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
        private static ICollection<GameNode> ToLayoutNodes
            (ICollection<GameObject> gameNodes,
             NodeFactory leafNodeFactory,
             NodeFactory innerNodeFactory,
             Dictionary<Node, ILayoutNode> toLayoutNode)
        {
            IList<GameNode> result = new List<GameNode>(gameNodes.Count);

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().Value;
                NodeFactory factory = node.IsLeaf() ? leafNodeFactory : innerNodeFactory;
                result.Add(new GameNode(toLayoutNode, gameObject, factory));
            }
            LayoutNodes.SetLevels(result.Cast<ILayoutNode>().ToList());
            return result;
        }

        /// <summary>
        /// Adds the source name as a label to the center of the given game nodes as a child.
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <param name="innerNodeFactory">inner node factory</param>
        /// <returns>the game objects created for the text labels</returns>
        /// <returns>the game objects created for the text labels</returns>
        private static void AddLabels(IEnumerable<GameObject> gameNodes, NodeFactory innerNodeFactory)
        {
            GameObject codeCity = null;
            foreach (GameObject node in gameNodes)
            {
                Node theNode = node.GetComponent<NodeRef>().Value;
                Vector3 size = innerNodeFactory.GetSize(node);
                float length = Mathf.Min(size.x, size.z);
                // The text may occupy up to 30% of the length.
                GameObject text = TextFactory.GetTextWithWidth(theNode.SourceName,
                                                               node.transform.position, length * 0.3f);
                text.transform.SetParent(node.transform);
                codeCity ??= SceneQueries.GetCodeCity(node.transform).gameObject;
                Portal.SetPortal(codeCity, text);
            }
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

            for (int i = 0; i < nodes.Count; i++)
            {
                // We add only leaves.
                if (nodes[i].IsLeaf())
                {
                    result[nodes[i]] = DrawLeafNode(nodes[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Create and returns a new game object for representing the given <paramref name="node"/>.
        /// The exact kind of representation depends upon the leaf-node factory. The node is
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings.
        /// Its style is determined by LeafNodeStyleMetric (linerar interpolation of a color gradient).
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        ///
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        public GameObject DrawLeafNode(Node node)
        {
            Assert.IsTrue(node.ItsGraph.MaxDepth >= 0, "Graph of node " + node.ID + " has negative depth");

            // The deeper the node in the node hierarchy (quantified by a node's level), the
            // later it should be drawn, or in other words, the higher its offset in the
            // render queue should be. We are assuming that the nodes are stacked on each
            // other according to the node hierarchy. Leaves are on top of all other nodes.
            // That is why we put them at the highest necessary rendering queue offset.

            int style = SelectStyle(node);
            GameObject result = leafNodeFactory.NewBlock(style, node.ItsGraph.MaxDepth);
            result.name = node.ID;
            result.tag = Tags.Node;
            result.AddComponent<NodeRef>().Value = node;
            AdjustScaleOfLeaf(result);
            AddLOD(result);
            InteractionDecorator.PrepareForInteraction(result);
            return result;
        }

        /// <summary>
        /// Adds a LOD group to <paramref name="gameObject"/> with only a single LOD.
        /// This is used to cull the object if it gets too small. The percentage
        /// by which to cull is retrieved from <see cref="settings.LODCulling"/>
        /// </summary>
        /// <param name="gameObject">object where to add the LOD group</param>
        private void AddLOD(GameObject gameObject)
        {
            LODGroup lodGroup = gameObject.AddComponent<LODGroup>();
            // Only a single LOD: we either or cull.
            LOD[] lods = new LOD[1];
            Renderer[] renderers = new Renderer[1];
            renderers[0] = gameObject.GetComponent<Renderer>();
            lods[0] = new LOD(settings.LODCulling, renderers);
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

        /// <summary>
        /// Applies AddLOD to every game object in <paramref name="gameObjects"/>.
        /// </summary>
        /// <param name="gameObjects">the list of game objects where AddLOD is to be applied</param>
        private void AddLOD(ICollection<GameObject> gameObjects)
        {
            foreach (GameObject go in gameObjects)
            {
                AddLOD(go);
            }
        }

        /// <summary>
        /// Returns a style index as a linear interpolation of X for range [0..M-1]
        /// where M is the number of available styles of the leafNodeFactory (if
        /// the node is a leaf) or innerNodeFactory (if it is an inner node)
        /// and X = C / metricMaximum and C is as follows:
        /// Let M be the metric selected for the style metric (the style metric
        /// for leaves if <paramref name="node"/> is a leaf or for an inner node
        /// otherwise). M can be either the name of a node metric or a number.
        /// If M is the name of a node metric, C is the normalized metric value of
        /// <paramref name="node"/> for the attribute chosen for the style
        /// and metricMaximum is the maximal value of the style metric. If M is
        /// instead a number, C is that value -- clamped into [0, S] where S is
        /// the maximal number of styles available) and metricMaximum is S.
        /// </summary>
        /// <param name="node">node for which to determine the style index</param>
        /// <returns>style index</returns>
        private int SelectStyle(Node node)
        {
            bool isLeaf = node.IsLeaf();
            NodeFactory nodeFactory = isLeaf ? leafNodeFactory : innerNodeFactory;
            uint numberOfStyles = nodeFactory.NumberOfStyles();
            string colorMetric = isLeaf ? settings.LeafNodeSettings.ColorMetric
                                        : settings.InnerNodeSettings.ColorMetric;

            float metricMaximum;
            if (TryGetFloat(colorMetric, out float metricValue))
            {
                // The colorMetric name is actually a constant number.
                metricMaximum = numberOfStyles;
                metricValue = Mathf.Clamp(metricValue, 0.0f, metricMaximum);
            }
            else
            {
                if (!node.TryGetNumeric(colorMetric, out float _))
                {
                    Debug.LogWarning($"value of color metric {colorMetric} for node {node.ID} is undefined.\n");
                    return 0;
                }

                metricMaximum = scaler.GetNormalizedMaximum(colorMetric);
                metricValue = scaler.GetNormalizedValue(colorMetric, node);
                Assert.IsTrue(metricValue <= metricMaximum);
            }
            return Mathf.RoundToInt(Mathf.Lerp(0.0f, numberOfStyles - 1, metricValue / metricMaximum));
        }

        /// <summary>
        /// Tries to parse <paramref name="floatString"/> as a floating point number.
        /// Upon success, its value is return in <paramref name="value"/> and true
        /// is returned. Otherwise false is returned and <paramref name="value"/>
        /// is undefined.
        /// </summary>
        /// <param name="floatString">string to be parsed for a floating point number</param>
        /// <param name="value">parsed floating point value; defined only if this method returns true</param>
        /// <returns>true if a floating point number could be parsed successfully</returns>
        private bool TryGetFloat(string floatString, out float value)
        {
            try
            {
                value = float.Parse(floatString, CultureInfo.InvariantCulture.NumberFormat);
                return true;
            }
            catch (FormatException)
            {
                value = 0.0f;
                return false;
            }
        }

        /// <summary>
        /// Returns the center of the roof of the given <paramref name="gameNode"/>.
        ///
        /// Precondition: <paramref name="gameNode"/> must have been created by this graph renderer.
        /// </summary>
        /// <param name="gameNode">game node for which to determine the roof position</param>
        /// <returns>roof position</returns>
        internal Vector3 GetRoof(GameObject gameNode)
        {
            NodeRef noderef = gameNode.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("Game object " + gameNode.name + " does not have a graph node attached to it.");
            }

            Node node = noderef.Value;
            if (node.IsLeaf())
            {
                return leafNodeFactory.Roof(gameNode);
            }
            else
            {
                return innerNodeFactory.Roof(gameNode);
            }
        }

        /// <summary>
        /// Returns the scale of the given <paramref name="gameNode"/>.
        ///
        /// Precondition: <paramref name="gameNode"/> must have been created by this graph renderer.
        /// </summary>
        /// <param name="gameNode"></param>
        /// <returns>scale of <paramref name="gameNode"/></returns>
        internal Vector3 GetSize(GameObject gameNode)
        {
            NodeRef noderef = gameNode.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("Game object " + gameNode.name + " does not have a graph node attached to it.");
            }
            Node node = noderef.Value;
            if (node.IsLeaf())
            {
                return leafNodeFactory.GetSize(gameNode);
            }
            else
            {
                return innerNodeFactory.GetSize(gameNode);
            }
        }

        /// <summary>
        /// Adjusts the height (y axis) of the given <paramref name="gameNode"/> according
        /// to the InnerNodeHeightMetric.
        ///
        /// Precondition: <paramref name="gameNode"/> must denote an inner node created
        /// by innerNodeFactory.
        /// </summary>
        /// <param name="gameNode">inner node whose height is to be set</param>
        private void AdjustHeightOfInnerNode(GameObject gameNode)
        {
            NodeRef noderef = gameNode.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("Game object " + gameNode.name + " does not have a graph node attached to it.");
            }
            else
            {
                Node node = noderef.Value;
                float value = GetMetricValue(noderef.Value, settings.InnerNodeSettings.HeightMetric);
                innerNodeFactory.SetHeight(gameNode, value);
            }
        }

        /// <summary>
        /// Adjusts the style of the given <paramref name="gameNode"/> according
        /// to the metric value of the graph node attached to <paramref name="gameNode"/>
        /// chosen to determine style.
        /// </summary>
        /// <param name="gameNode">a game node representing a leaf or inner graph node</param>
        public void AdjustStyle(GameObject gameNode)
        {
            NodeRef noderef = gameNode.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("Game object " + gameNode.name + " does not have a graph node attached to it.");
            }

            Node node = noderef.Value;
            int style = SelectStyle(node);
            if (node.IsLeaf())
            {
                leafNodeFactory.SetStyle(gameNode, style);
            }
            else
            {
                // TODO: for some reason, the material is selected twice. Once here and once somewhere earlier (I believe in NewBlock somewhere).
                innerNodeFactory.SetStyle(gameNode, style);
            }
        }

        /// <summary>
        /// Adjusts the scale of the given leaf <paramref name="gameNode"/> according
        /// to the metric values of the <paramref name="node"/> attached to
        /// <paramref name="gameNode"/>.
        /// The scale of a leaf is determined by the node's width, height, and depth
        /// metrics (which are determined by the settings).
        /// Precondition: A graph node is attached to <paramref name="gameNode"/>
        /// and has the width, height, and depth metrics set and is a leaf.
        /// </summary>
        /// <param name="gameNode">the game object whose visual attributes are to be adjusted</param>
        public void AdjustScaleOfLeaf(GameObject gameNode)
        {
            Assert.IsNull(gameNode.transform.parent);
            NodeRef nodeRef = gameNode.GetComponent<NodeRef>();
            if (nodeRef == null)
            {
                throw new Exception($"Game object {gameNode.name} does not have a graph node attached to it.");
            }

            Node node = nodeRef.Value;
            if (node.IsLeaf())
            {
                // Scaled metric values for the three dimensions.
                Vector3 scale = GetScale(node);

                // Scale according to the metrics.
                if (settings.NodeLayoutSettings.Kind == NodeLayoutKind.Treemap)
                {
                    // FIXME: This is ugly. The graph renderer should not need to care what
                    // kind of layout was applied.

                    // Treemaps can represent a metric by the area of a rectangle. Hence,
                    // they can represent only a single metric in the x/z plane.
                    // Let M be the metric selected to be represented by the treemap.
                    // Here, we choose the width metric (as selected by the user) to be M.
                    // That is, M mapped onto the rectangle area.
                    // The area of a rectangle is the product of the lengths of its two sides.
                    // That is why we need to take the square root of M for the lengths, because
                    // sqrt(M) * sqrt(M) = M. If were instead using M as width and depth of the
                    // rectangle, the area would be M^2, which would skew the visual impression
                    // in the eye of the beholder. Nodes with larger values of M would have a
                    // disproportionally larger area.

                    // The input to the treemap layout are rectangles with equally sized lengths,
                    // in other words, squares. This determines only the ground area of the input
                    // blocks. The height of the blocks remains the original value of the metric
                    // chosen to determine the height, without any kind of transformation.
                    float widthOfSquare = Mathf.Sqrt(scale.x);
                    Vector3 targetScale = new Vector3(widthOfSquare, scale.y, widthOfSquare) * NodeFactory.Unit;
                    leafNodeFactory.SetSize(gameNode, targetScale);
                }
                else
                {
                    gameNode.transform.localScale = NodeFactory.Unit * scale;
                }
            }
            else
            {
                throw new Exception($"Game object {gameNode.name} is not a leaf.");
            }
        }

        /// <summary>
        /// Returns the scale of the given <paramref name="node"/> as requested by the user's
        /// settings, i.e., what the use specified for the width, height, and depth of nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>requested absolute scale in world space</returns>
        private Vector3 GetScale(Node node)
        {
            LeafNodeAttributes attribs = settings.LeafNodeSettings;
            return new Vector3(GetMetricValue(node, attribs.WidthMetric),
                               GetMetricValue(node, attribs.HeightMetric),
                               GetMetricValue(node, attribs.DepthMetric));
        }

        /// <summary>
        /// If <paramref name="metricName"/> is the name of a metric, the corresponding
        /// normalized value for <paramref name="node"/> is returned. If <paramref name="metricName"/>
        /// can be parsed as a number instead, the parsed number is returned.
        /// The result is clamped into [MinimalBlockLength, MaximalBlockLength].
        /// </summary>
        /// <param name="node">node whose metric is to be returned</param>
        /// <param name="metricName">the name of a node metric or a number</param>
        /// <returns>the value of <paramref name="node"/>'s metric <paramref name="metricName"/></returns>
        private float GetMetricValue(Node node, string metricName)
        {
            float result;
            if (TryGetFloat(metricName, out float value))
            {
                result = value;
            }
            else
            {
                result = scaler.GetNormalizedValue(metricName, node);
            }
            return Mathf.Clamp(result,
                        settings.LeafNodeSettings.MinimalBlockLength,
                        settings.LeafNodeSettings.MaximalBlockLength);
        }

        /// <summary>
        /// Adjusts the scale of every node such that the maximal extent of each node is one.
        /// </summary>
        /// <param name="nodeMap">The nodes to scale.</param>
        private static void AdjustScaleBetweenNodeKinds(Dictionary<Node, GameObject> nodeMap)
        {
            Vector3 denominator = Vector3.negativeInfinity;
            IList<KeyValuePair<Node, GameObject>> nodeMapMatchingDomain = nodeMap.ToList();
            denominator = nodeMapMatchingDomain.Aggregate(denominator, (current, pair) => Vector3.Max(current, pair.Value.transform.localScale));
            foreach (KeyValuePair<Node, GameObject> pair in nodeMapMatchingDomain)
            {
                pair.Value.transform.localScale = pair.Value.transform.localScale.DividePairwise(denominator);
            }
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
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// The inner <paramref name="node"/> is attached to that new game object
        /// via a NodeRef component. The style and height of the resulting game
        /// object are adjusted according to the selected InnerNodeStyleMetric
        /// and InnerNodeHeightMetric, respectively. The other scale dimensions
        /// are not changed.
        ///
        /// Precondition: <paramref name="node"/> must be an inner node of the node
        /// hierarchy.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        public GameObject DrawInnerNode(Node node)
        {
            // The deeper the node in the node hierarchy (quantified by a node's level), the
            // later it should be drawn, or in other words, the higher its offset in the
            // render queue should be. We are assuming that the nodes are stacked on each
            // other according to the node hierarchy. Leaves are on top of all other nodes.

            GameObject result = innerNodeFactory.NewBlock(style: SelectStyle(node), renderQueueOffset: node.Level);
            result.name = node.ID;
            result.tag = Tags.Node;
            result.AddComponent<NodeRef>().Value = node;
            AdjustHeightOfInnerNode(result);
            AddLOD(result);
            InteractionDecorator.PrepareForInteraction(result);
            return result;
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
                        throw new Exception("Code city " + codeCity.name + " has multiple children tagged by " + Tags.Node
                            + ": " + result.name + " and " + child.name);
                    }
                }
            }
            if (result == null)
            {
                throw new Exception("Code city " + codeCity.name + " has no child tagged by " + Tags.Node);
            }
            return result;
        }
    }
}
