using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SEE.Controls;
using SEE.Charts.Scripts;
using SEE.DataModel;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.Utils;
using SEE.Utils;
using UnityEngine;
using static SEE.Game.AbstractSEECity;

namespace SEE.Game
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
        /// <param name="settings">the settings for the visualization</param>
        public GraphRenderer(AbstractSEECity settings)
        {
            this.settings = settings;
            switch (this.settings.LeafObjects)
            {
                case SEECity.LeafNodeKinds.Blocks:
                    leafNodeFactory = new CubeFactory();
                    break;
                case SEECity.LeafNodeKinds.Buildings:
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
        private InnerNodeFactory GetInnerNodeFactory(AbstractSEECity.InnerNodeKinds innerNodeKinds)
        {
            switch (innerNodeKinds)
            {
                case AbstractSEECity.InnerNodeKinds.Empty:
                case AbstractSEECity.InnerNodeKinds.Donuts:
                    return new VanillaFactory();
                case AbstractSEECity.InnerNodeKinds.Circles:
                    return new CircleFactory(leafNodeFactory.Unit);
                case AbstractSEECity.InnerNodeKinds.Cylinders:
                    return new CylinderFactory();
                case AbstractSEECity.InnerNodeKinds.Rectangles:
                    return new RectangleFactory(leafNodeFactory.Unit);
                case AbstractSEECity.InnerNodeKinds.Blocks:
                    return new CubeFactory();
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds");
            }
        }

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        protected readonly AbstractSEECity settings;

        /// <summary>
        /// The factory used to create blocks for leaves.
        /// </summary>
        protected readonly NodeFactory leafNodeFactory;

        /// <summary>
        /// The factory used to create game nodes for inner graph nodes.
        /// </summary>
        private readonly InnerNodeFactory innerNodeFactory;

        /// <summary>
        /// The scale used to normalize the metrics determining the lengths of the blocks.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// mapping from node to ilayoutNode
        /// </summary>
        protected Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();

        /// <summary>
        /// the groundlevel of the nodes
        /// </summary>
        protected float groundLevel = 0.0f;

        /// <summary>
        /// Draws the graph (nodes and edges and all decorations).
        /// </summary>
        /// <param name="graph">the graph to be drawn</param>
        /// <param name="parent">every game object drawn for this graph will be added to this parent</param>
        public virtual void Draw(Graph graph, GameObject parent)
        {
            SetScaler(graph);
            graph.SortHierarchyByName();
            DrawCity(graph, parent);
        }

        /// <summary>
        /// Sets the scaler to be used to map metric values onto graphical attributes
        /// (e.g., width, height, depth, style) across all given <paramref name="graphs"/>
        /// based on the user's choice (settings).
        /// </summary>
        /// <param name="graphs">set of graphs whose node metrics are to be scaled</param>
        public void SetScaler(ICollection<Graph> graphs)
        {
            List<string> nodeMetrics = settings.AllMetricAttributes();

            if (settings.ZScoreScale)
            {
                scaler = new ZScoreScale(graphs, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
            }
            else
            {
                scaler = new LinearScale(graphs, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
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
            SetScaler(new List<Graph>() { graph });
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        /// <param name="gameNodes">the subset of nodes for which to draw the edges</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        public ICollection<GameObject> EdgeLayout(Graph graph, ICollection<GameObject> gameNodes)
        {
            return EdgeLayout(graph, ToLayoutNodes(gameNodes));
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        /// <param name="layoutNodes">the subset of layout nodes for which to draw the edges</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        public ICollection<GameObject> EdgeLayout(Graph graph, ICollection<ILayoutNode> layoutNodes)
        {
            float minimalEdgeLevelDistance = 2.5f * settings.EdgeWidth;
            IEdgeLayout layout;
            switch (settings.EdgeLayout)
            {
                case SEECity.EdgeLayouts.Straight:
                    layout = new StraightEdgeLayout(settings.EdgesAboveBlocks, minimalEdgeLevelDistance);
                    break;
                case SEECity.EdgeLayouts.Spline:
                    layout = new SplineEdgeLayout(settings.EdgesAboveBlocks, minimalEdgeLevelDistance, settings.RDP);
                    break;
                case SEECity.EdgeLayouts.Bundling:
                    layout = new BundledEdgeLayout(settings.EdgesAboveBlocks, minimalEdgeLevelDistance, settings.Tension, settings.RDP);
                    break;
                case SEECity.EdgeLayouts.None:
                    // nothing to be done
                    return new List<GameObject>();
                default:
                    throw new Exception("Unhandled edge layout " + settings.EdgeLayout.ToString());
            }
            Performance p = Performance.Begin("edge layout " + layout.Name);
            EdgeFactory edgeFactory = new EdgeFactory(layout, settings.EdgeWidth);
            ICollection<GameObject> result = edgeFactory.DrawEdges(layoutNodes);
            p.End();
            return result;
        }

        /// <summary>
        /// Draws the nodes and edges of the graph by applying the layouts according to the user's
        /// choice in the settings.
        /// </summary>
        /// <param name="graph">graph whose nodes and edges are to be laid out</param>
        /// <param name="parent">every game object drawn for this graph will be added to this parent</param>
        protected virtual void DrawCity(Graph graph, GameObject parent)
        {
            // all nodes of the graph
            List<Node> nodes = graph.Nodes();
            if (nodes.Count == 0)
            {
                Debug.LogWarning("The graph has no nodes.\n");
                return;
            }
            // game objects for the leaves
            Dictionary<Node, GameObject> nodeMap = CreateBlocks(nodes);
            // the layout to be applied
            NodeLayout nodeLayout = GetLayout();

            // for a hierarchical layout, we need to add the game objects for inner nodes

            Dictionary<Node, GameObject>.ValueCollection gameNodes;
            ICollection<ILayoutNode> layoutNodes = new List<ILayoutNode>();
            Node artificalRoot  = null;

            Performance p;
            if (settings.NodeLayout.GetModel().CanApplySublayouts && nodeLayout.IsHierarchical())
            {
                try
                {
                    ICollection<SublayoutNode> sublayoutNodes = AddInnerNodesForSublayouts(nodeMap, nodes);
                    artificalRoot = AddRootIfNecessary(graph, nodeMap);
                    layoutNodes = ToLayoutNodes(nodeMap, sublayoutNodes);
                    RemoveRootIfNecessary(ref artificalRoot, graph, nodeMap, layoutNodes);

                    List<SublayoutLayoutNode> sublayoutLayoutNodes = ConvertSublayoutToLayoutNodes(sublayoutNodes.ToList());
                    foreach (SublayoutLayoutNode layoutNode in sublayoutLayoutNodes)
                    {
                        Sublayout sublayout = new Sublayout(layoutNode, groundLevel, leafNodeFactory, graph, settings);
                        sublayout.Layout();
                    }

                    p = Performance.Begin("layout name" + settings.NodeLayout + ", layout of nodes");
                    if (nodeLayout.UsesEdgesAndSublayoutNodes())
                    {
                        nodeLayout.Apply(layoutNodes, graph.ConnectingEdges(layoutNodes), sublayoutLayoutNodes);
                    }
                    p.End();

                    NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
                    NodeLayout.MoveTo(layoutNodes, parent.transform.position);

                    gameNodes = nodeMap.Values;

                    // add the plane surrounding all game objects for nodes
                    BoundingBox(layoutNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
                    GameObject plane = NewPlane(leftFrontCorner, rightBackCorner, parent.transform.position.y);
                    AddToParent(plane, parent);

                    CreateObjectHierarchy(nodeMap, parent);
                    InteractionDecorator.PrepareForInteraction(gameNodes);

                    // add the decorations, too
                    if (sublayoutLayoutNodes.Count <= 0)
                    {
                        AddDecorations(gameNodes);
                    }
                    else
                    {
                        AddDecorationsForSublayouts(layoutNodes, sublayoutLayoutNodes, parent);
                    }
                                     
                    if (settings.calculateMeasurements)
                    {
                        Measurements measurements = new Measurements(layoutNodes, graph, leftFrontCorner, rightBackCorner, p);
                        settings.Measurements = measurements.ToStringDictionary(true);
                    }
                    else
                    {
                        settings.Measurements = new SortedDictionary<string, string>();
                    }
                } finally
                {
                    // If we added an artifical root node to the graph, we must remove it again
                    // from the graph when we are done.
                    RemoveRootIfNecessary(ref artificalRoot, graph, nodeMap, layoutNodes);
                }
               
            }
            else
            {
                try
                {
                    if (nodeLayout.IsHierarchical())
                    {
                        // for a hierarchical layout, we need to add the game objects for inner nodes
                        AddInnerNodes(nodeMap, nodes);
                        artificalRoot = AddRootIfNecessary(graph, nodeMap);
                    }

                    // calculate and apply the node layout
                    layoutNodes = ToLayoutNodes(nodeMap.Values);
                    RemoveRootIfNecessary(ref artificalRoot, graph, nodeMap, layoutNodes);

                    p = Performance.Begin("layout name" + settings.NodeLayout + ", layout of nodes");
                    nodeLayout.Apply(layoutNodes);
                    p.End();

                    NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
                    NodeLayout.MoveTo(layoutNodes, parent.transform.position);

                    gameNodes = nodeMap.Values;
                    // add the plane surrounding all game objects for nodes
                    GameObject plane = NewPlane(gameNodes, parent.transform.position.y);
                    AddToParent(plane, parent);

                    CreateObjectHierarchy(nodeMap, parent);
                    InteractionDecorator.PrepareForInteraction(gameNodes);

                    // Decorations must be applied after the blocks have been placed, so that
                    // we also know their positions.
                    AddDecorations(gameNodes);
                }
                finally
                {
                    // If we added an artifical root node to the graph, we must remove it again
                    // from the graph when we are done.
                    RemoveRootIfNecessary(ref artificalRoot, graph, nodeMap, layoutNodes);
                }
            }

            // create the laid out edges
            AddToParent(EdgeLayout(graph, layoutNodes), parent);
        }

        /// <summary>
        /// Creates the same nesting of all game objects in <paramref name="nodeMap"/> as in
        /// the graph node hierarchy. Every root node in the graph node hierarchy will become
        /// a child of the given <paramref name="root"/>.
        /// </summary>
        /// <param name="nodeMap">mapping of graph nodes onto their representing game objects</param>
        /// <param name="root">the parent of every game object not nested in any other game object</param>
        private void CreateObjectHierarchy(Dictionary<Node, GameObject> nodeMap, GameObject root)
        {
            foreach (var entry in nodeMap)
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
                    try
                    {
                        AddToParent(entry.Value, nodeMap[parent]);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("Exception raised for {0}: {1}\n", parent.ID, e);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the decoration to the sublayout
        /// </summary>
        /// <param name="layoutNodes">the layoutnodes</param>
        /// <param name="sublayoutLayoutNodes">the sublayout nodes</param>
        /// <param name="parent">the parent gameobject</param>
        private void AddDecorationsForSublayouts(ICollection<ILayoutNode> layoutNodes, List<SublayoutLayoutNode> sublayoutLayoutNodes, GameObject parent)
        {
            List<ILayoutNode> remainingLayoutNodes = layoutNodes.ToList();
            foreach (SublayoutLayoutNode layoutNode in sublayoutLayoutNodes)
            {
                ICollection<GameObject> gameObjects = new List<GameObject>();
                foreach (GameNode gameNode in layoutNode.Nodes)
                {
                    gameObjects.Add(gameNode.GetGameObject());
                }
                AddDecorations(gameObjects, layoutNode.InnerNodeKind, layoutNode.NodeLayout);
                remainingLayoutNodes.RemoveAll(node => layoutNode.Nodes.Contains(node));
            }

            ICollection<GameObject> remainingGameObjects = new List<GameObject>();
            foreach (GameNode gameNode in remainingLayoutNodes)
            {
                remainingGameObjects.Add(gameNode.GetGameObject());
            }

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
                    AddInnerNodes(nodeMap, sublayoutNode.Nodes, GetInnerNodeFactory(sublayoutNode.InnerNodeKind));
                    remainingNodes.RemoveAll(node => sublayoutNode.Nodes.Contains(node));
                }
                AddInnerNodes(nodeMap, remainingNodes);
            }
            else
            {
                AddInnerNodes(nodeMap, nodes);
            }

            return coseSublayoutNodes;
        }

        /// <summary>
        /// Creates the sublayoutnodes for a given set of nodes 
        /// </summary>
        /// <param name="nodes">the nodes, which should be layouted as sublayouts</param>
        /// <returns>a list with sublayout nodes</returns>
        private List<SublayoutNode> CreateSublayoutNodes(List<Node> nodes)
        {
            List<SublayoutNode> coseSublayoutNodes = new List<SublayoutNode>();
            foreach (KeyValuePair<string, bool> dir in settings.CoseGraphSettings.ListDirToggle)
            {
                if (dir.Value)
                {
                    var name = dir.Key;
                    if (settings.CoseGraphSettings.DirNodeLayout.ContainsKey(name) && settings.CoseGraphSettings.DirShape.ContainsKey(name))
                    {
                        IEnumerable<Node> matches = nodes.Where(i => i.ID.Equals(name));
                        if (matches.Count() > 0)
                        {
                            coseSublayoutNodes.Add(new SublayoutNode(matches.First(), settings.CoseGraphSettings.DirShape[name], settings.CoseGraphSettings.DirNodeLayout[name]));
                        }
                    }
                }
            }
            return coseSublayoutNodes;
        }

        /// <summary>
        /// Calculate the child/ removed nodes for each sublayout
        /// </summary>
        /// <param name="sublayoutNodes">the sublayout nodes</param>
        private void CalculateNodesSublayout(List<SublayoutNode> sublayoutNodes)
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

        /// <summary>
        /// If <paramref name="graph"/> has a single root, nothing is done. Otherwise
        /// an artifical root is created and added to both the <paramref name="graph"/>
        /// and <paramref name="nodeMap"/> (and there mapped onto a newly created game 
        /// object for inner nodes). All true roots of <paramref name="graph"/> will
        /// become children of this artificial root.
        /// Note: This method is the counterpart to RemoveRootIfNecessary.
        /// </summary>
        /// <param name="graph">graph where a unique root node should be added</param>
        /// <param name="nodeMap">mapping of nodes onto game objects, which will be updated
        /// when a new artifical root is added</param>
        /// <returns>the new artifical root or null if <paramref name="graph"/> has
        /// already a single root</returns>
        private Node AddRootIfNecessary(Graph graph, Dictionary<Node, GameObject> nodeMap)
        {
            // Note: Because this method is called only when a hierarchical layout is to
            // be applied (and then both leaves and inner nodes were added to nodeMap), we 
            // could traverse through graph.GetRoots() or nodeMaps.Keys. It would not make
            // a difference. If -- for any reason --, we decide not to create a game object
            // for some inner nodes, we should rather iterate on nodeMaps.Keys.
            ICollection<Node> roots = graph.GetRoots();

            if (roots.Count > 1)
            {
                Node artificalRoot = new Node()
                {
                    ID = graph.Name + "#ROOT",
                    SourceName = graph.Name + "#ROOT",
                    Type = "ROOTTYPE"
                };
                graph.AddNode(artificalRoot);
                foreach (Node root in roots)
                {
                    artificalRoot.AddChild(root);
                }
                nodeMap[artificalRoot] = NewInnerNode(artificalRoot);
                return artificalRoot;
            }
            else
            {
                return null;
            }
        }

        private List<SublayoutLayoutNode> ConvertSublayoutToLayoutNodes(List<SublayoutNode> sublayouts)
        {
            List<SublayoutLayoutNode> sublayoutLayoutNodes = new List<SublayoutLayoutNode>();
            sublayouts.ForEach(sublayoutNode => {

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
        private List<Node> WithAllChildren(Node root)
        {
            List<Node> allNodes = new List<Node> { root };
            foreach (Node node in root.Children())
            {
                allNodes.AddRange(WithAllChildren(node));
            }

            return allNodes;
        }


        /// <summary>
        /// If <paramref name="root"/> is null, nothing happens. Otherwise <paramref name="root"/> will
        /// be removed from <paramref name="graph"/> and <paramref name="nodeMap"/>. The value of
        /// <paramref name="root"/> will be null afterward.
        /// Note: This method is the counterpart to AddRootIfNecessary.
        /// </summary>
        /// <param name="root">artifical root node to be removed (created by AddRootIfNecessary) or null;
        /// will be null afterward</param>
        /// <param name="graph">graph where <paramref name="root"/> should be removed/param>
        /// <param name="nodeMap">mapping of nodes onto game objects from which to remove 
        /// <paramref name="root"/></param>
        private void RemoveRootIfNecessary(ref Node root, Graph graph, Dictionary<Node, GameObject> nodeMap, ICollection<ILayoutNode> layoutNodes)
        {
            if (!ReferenceEquals(root, null))
            {                
                if (layoutNodes != null)
                {
                    // Remove from layout
                    ILayoutNode toBeRemoved = null;
                    foreach (ILayoutNode layoutNode in layoutNodes)
                    {
                        if (layoutNode.ID.Equals(root.ID))
                        {
                            toBeRemoved = layoutNode;
                            break;
                        }
                    }
                    if (toBeRemoved != null)
                    {
                        layoutNodes.Remove(toBeRemoved);
                    }
                }
                GameObject go = nodeMap[root];
                nodeMap.Remove(root);
                graph.RemoveNode(root);                                
                Destroyer.DestroyGameObject(go);
                root = null;
            }
        }

        /// <summary>
        /// Returns the node layouter according to the settings. The node layouter will
        /// place the nodes at ground level 0.
        /// </summary>
        /// <returns>node layout selected</returns>
        public NodeLayout GetLayout()
        {
            switch (settings.NodeLayout)
            {
                case SEECity.NodeLayouts.Manhattan:                    
                    return new ManhattanLayout(groundLevel, leafNodeFactory.Unit);
                case SEECity.NodeLayouts.RectanglePacking:
                    return new RectanglePackingNodeLayout(groundLevel, leafNodeFactory.Unit);
                case SEECity.NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory.Unit);
                case SEECity.NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, 1000.0f * Unit(), 1000.0f * Unit());
                case SEECity.NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel);
                case SEECity.NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel);
                case SEECity.NodeLayouts.CompoundSpringEmbedder:
                    return new CoseLayout(groundLevel, settings);
                case SEECity.NodeLayouts.FromFile:
                    return new LoadedNodeLayout(groundLevel, settings.GVLPath());
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
        }

        /// <summary>
        /// Creates and returns a new plane enclosing all given <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">the game objects to be enclosed by the new plane</param>
        /// <returns>new plane enclosing all given <paramref name="gameNodes"/></returns>
        public GameObject NewPlane(ICollection<GameObject> gameNodes, float yLevel)
        {
            BoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            // Place the plane somewhat under ground level.
            return PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, yLevel - 0.01f, Color.gray);
        }

        /// <summary>
        /// Returns a new plane for a vector describing the left front corner position and a vector describing the right bar position
        /// </summary>
        /// <param name="leftFrontCorner">the left front corner</param>
        /// <param name="rightBackCorner">the right back corner</param>
        /// <returns>a new plane</returns>
        public GameObject NewPlane(Vector2 leftFrontCorner, Vector2 rightBackCorner, float yLevel)
        {
            return PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, yLevel - 0.01f, Color.gray);
        }

        /// <summary>
        /// Adjusts the x and z co-ordinates of the given <paramref name="plane"/> so that all
        /// <paramref name="gameNodes"/> fit onto it.
        /// </summary>
        /// <param name="plane">the plane to be adjusted</param>
        /// <param name="gameNodes">the game nodes that should be fitted onto <paramref name="plane"/></param>
        public void AdjustPlane(GameObject plane, ICollection<GameObject> gameNodes)
        {
            BoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
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
            BoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
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
        /// draws Decoration for the given List of gameNodes with the global settings for inner node kinds and nodelayout
        /// </summary>
        /// <param name="gameNodes"> a list with gamenode objects</param>
        /// <returns>a list with gamennode objects</returns>
        protected void AddDecorations(ICollection<GameObject> gameNodes)
        {
            AddDecorations(gameNodes, settings.InnerNodeObjects, settings.NodeLayout);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        /// <param name="innerNodeKinds">the inner node kinds for the gameobject</param>
        /// <param name="nodeLayout">the nodeLayout used for this gameobject</param>
        /// <returns>the game objects added for the decorations; may be an empty collection</returns>
        private void AddDecorations(ICollection<GameObject> gameNodes, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayout)
        {
            var innerNodeFactory = GetInnerNodeFactory(innerNodeKinds);
      
            // Add software erosion decorators for all leaf nodes if requested.
            if (settings.ShowErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(settings.LeafIssueMap(), leafNodeFactory, scaler, settings.MaxErosionWidth);
                issueDecorator.Add(LeafNodes(gameNodes));
            }

            // Add text labels for all inner nodes
            if (nodeLayout == SEECity.NodeLayouts.Balloon 
                || nodeLayout == SEECity.NodeLayouts.EvoStreets)
            {
                AddLabels(InnerNodes(gameNodes), innerNodeFactory);
            }

            // Add decorators specific to the shape of inner nodes (circle decorators for circles
            // and donut decorators for donuts.

            switch (innerNodeKinds)
            {
                case SEECity.InnerNodeKinds.Empty:
                    // do nothing
                    break;
                case SEECity.InnerNodeKinds.Circles:
                    {
                        // We want to adjust the size and the line width of the circle line created by the CircleFactory.
                        CircleDecorator decorator = new CircleDecorator(innerNodeFactory, Color.white);                        
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case SEECity.InnerNodeKinds.Rectangles:
                    {
                        // We want to adjust the line width of the rectangle line created by the RectangleFactory.
                        RectangleDecorator decorator = new RectangleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case SEECity.InnerNodeKinds.Donuts:
                    {
                        DonutDecorator decorator = new DonutDecorator(innerNodeFactory, scaler, settings.InnerDonutMetric, 
                                                                      settings.AllInnerNodeIssues().ToArray<string>());
                        // the circle segments and the inner circle for the donut are added as children by Add();
                        // that is why we do not add the result to decorations.
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case SEECity.InnerNodeKinds.Cylinders:
                    break;
                case SEECity.InnerNodeKinds.Blocks:
                    // TODO
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds " + settings.InnerNodeObjects);
            }
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        public ICollection<ILayoutNode> ToLayoutNodes(ICollection<GameObject> gameObjects)
        {
            return ToLayoutNodes(gameObjects, leafNodeFactory, innerNodeFactory);
        }

        /// <summary>
        /// Converts the given nodes and sublayoutsnodes to a List with ILayoutNodes
        /// </summary>
        /// <param name="nodeMap">mapping between nodes and gameobjects</param>
        /// <param name="sublayoutNodes">a collection with sublayoutNodes</param>
        /// <returns></returns>
        private ICollection<ILayoutNode> ToLayoutNodes(Dictionary<Node, GameObject> nodeMap, ICollection<SublayoutNode> sublayoutNodes)
        {
            List<ILayoutNode> layoutNodes = new List<ILayoutNode>();
            List<GameObject> remainingGameobjects = nodeMap.Values.ToList();

            foreach (SublayoutNode sublayoutNode in sublayoutNodes)
            {
                ICollection<GameObject> gameObjects = new List<GameObject>();
                sublayoutNode.Nodes.ForEach(node => gameObjects.Add(nodeMap[node]));
                layoutNodes.AddRange(ToLayoutNodes(gameObjects, leafNodeFactory, GetInnerNodeFactory(sublayoutNode.InnerNodeKind)));
                remainingGameobjects.RemoveAll(gameObject => gameObjects.Contains(gameObject));
            }

            layoutNodes.AddRange(ToLayoutNodes(remainingGameobjects, leafNodeFactory, innerNodeFactory));

            return layoutNodes;
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <param name="leafNodeFactory">the leaf node factory that created the leaf nodes in <paramref name="gameNodes"/></param>
        /// <param name="innerNodeFactory">the inner node factory that created the inner nodes in <paramref name="gameNodes"/></param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        private ICollection<ILayoutNode> ToLayoutNodes
            (ICollection<GameObject> gameNodes, 
            NodeFactory leafNodeFactory,
            NodeFactory innerNodeFactory)
        {
            IList<ILayoutNode> result = new List<ILayoutNode>();

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().node;
                if (node.IsLeaf())
                {
                    result.Add(new GameNode(to_layout_node, gameObject, leafNodeFactory));
                }
                else
                {
                    result.Add(new GameNode(to_layout_node, gameObject, innerNodeFactory));
                }
            }
            LayoutNodes.SetLevels(result);
            return result;
        }

        /// <summary>
        /// Adds the source name as a label to the center of the given game nodes as a child.
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <param name="innerNodeFactory">inner node factory</param>
        /// <returns>the game objects created for the text labels</returns>
        private void AddLabels(ICollection<GameObject> gameNodes, NodeFactory innerNodeFactory)
        {
            foreach (GameObject node in gameNodes)
            {
                Vector3 size = innerNodeFactory.GetSize(node);
                float length = Mathf.Min(size.x, size.z);
                // The text may occupy up to 30% of the length.
                GameObject text = TextFactory.GetText(node.GetComponent<NodeRef>().node.SourceName, 
                                                      node.transform.position, length * 0.3f);
                text.transform.SetParent(node.transform);
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
        public void Apply(GameObject gameNode, ILayoutNode layout)
        {
            Node node = gameNode.GetComponent<NodeRef>().node;

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
            Rotate(gameNode, layout.Rotation);
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
        /// Adds a <see cref="NodeHighlights"/> component to the given game node.
        /// </summary>
        /// <param name="gameNode">The given game node.</param>
        protected void AttachHighlighter(GameObject gameNode)
        {
            gameNode.AddComponent<NodeHighlights>();
        }

        /// <summary>
        /// Creates and scales blocks for all leaf nodes in given list of nodes.
        /// </summary>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        /// <returns>blocks for all leaf nodes in given list of nodes</returns>
        protected Dictionary<Node, GameObject> CreateBlocks(IList<Node> nodes)
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>();

            foreach (Node node in nodes)
            {
                // We add only leaves.
                if (node.IsLeaf())
                {
                    GameObject block = NewLeafNode(node);
                    result[node] = block;
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
        public GameObject NewLeafNode(Node node)
        {
            GameObject block = leafNodeFactory.NewBlock(SelectStyle(node));
            block.name = node.ID;
            AttachNode(block, node);
            AttachHighlighter(block);
            AdjustScaleOfLeaf(block);
            return block;
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
        private int SelectStyle(Node node, InnerNodeFactory innerNodeFactory = null)
        {
            if (innerNodeFactory == null)
            {
                innerNodeFactory = this.innerNodeFactory;
            }

            bool isLeaf = node.IsLeaf();
            string styleMetric = isLeaf ? settings.LeafStyleMetric : settings.InnerNodeStyleMetric;
            int numberOfStyles = isLeaf ? leafNodeFactory.NumberOfStyles() : innerNodeFactory.NumberOfStyles();
            float metricMaximum;

            if (TryGetFloat(styleMetric, out float value))
            {
                // The styleMetric name is actually a number.
                metricMaximum = numberOfStyles;
                value = Mathf.Clamp(value, 0, metricMaximum);

            }
            else
            {
                metricMaximum = scaler.GetNormalizedMaximum(styleMetric);
                value = scaler.GetNormalizedValue(styleMetric, node);
            }
            return Mathf.RoundToInt(Mathf.Lerp(0.0f,
                                               (float)(numberOfStyles - 1),
                                               value / metricMaximum));
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
            else
            {
                Node node = noderef.node;
                if (node.IsLeaf())
                {
                    return leafNodeFactory.Roof(gameNode);
                }
                else
                {
                    return innerNodeFactory.Roof(gameNode);
                }
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
            else
            {
                Node node = noderef.node;
                if (node.IsLeaf())
                {
                    return leafNodeFactory.GetSize(gameNode);
                }
                else
                {
                    return innerNodeFactory.GetSize(gameNode);
                }
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
                float value = GetMetricValue(noderef.node, settings.InnerNodeHeightMetric);
                innerNodeFactory.SetHeight(gameNode, value);
            }
        }

        /// <summary>
        /// Adjusts the style of the given <paramref name="gameNode"/> according
        /// to the metric value of the graph node attached to <paramref name="gameNode"/>
        /// chosen to determine style.
        /// </summary>
        /// <param name="gameNode">a game node representing a leaf or inner graph node</param>
        public void AdjustStyle(GameObject gameNode, InnerNodeFactory innerNodeFactory = null)
        {
            if (innerNodeFactory == null)
            {
                innerNodeFactory = this.innerNodeFactory;
            }

            NodeRef noderef = gameNode.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("Game object " + gameNode.name + " does not have a graph node attached to it.");
            }
            else
            {
                Node node = noderef.node;
                int style = SelectStyle(node, innerNodeFactory);
                if (node.IsLeaf())
                {
                    leafNodeFactory.SetStyle(gameNode, style);
                }
                else
                {
                    innerNodeFactory.SetStyle(gameNode, style);
                }
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
            NodeRef noderef = gameNode.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("Game object " + gameNode.name + " does not have a graph node attached to it.");
            }
            else
            {
                Node node = noderef.node;
                if (node.IsLeaf())
                {
                    // Scaled metric values for the three dimensions.
                    Vector3 scale = GetScale(node);

                    // Scale according to the metrics.
                    if (settings.NodeLayout == SEECity.NodeLayouts.Treemap)
                    {
                        // FIXME: This is ugly. The graph renderer should not need to care what
                        // kind of layout was applied.
                        // In case of treemaps, the width metric is mapped on the ground area.
                        float widthOfSquare = Mathf.Sqrt(scale.x);
                        leafNodeFactory.SetWidth(gameNode, leafNodeFactory.Unit * widthOfSquare);
                        leafNodeFactory.SetDepth(gameNode, leafNodeFactory.Unit * widthOfSquare);
                        leafNodeFactory.SetHeight(gameNode, leafNodeFactory.Unit * scale.y);
                    }
                    else
                    {
                        leafNodeFactory.SetSize(gameNode, leafNodeFactory.Unit * scale);
                    }
                }
                else
                {
                    throw new Exception("Game object " + gameNode.name + " is not a leaf.");
                }
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
            return new Vector3(GetMetricValue(node, settings.WidthMetric),
                               GetMetricValue(node, settings.HeightMetric),
                               GetMetricValue(node, settings.DepthMetric));
        }

        /// <summary>
        /// If <paramref name="metricName"/> is the name of a metric, the corresponding
        /// normalized value for <paramref name="node"/> is returned. If <paramref name="metricName"/>
        /// can be parsed as a number instead, the parsed number is returned.
        /// </summary>
        /// <param name="node">node whose metric is to be returned</param>
        /// <param name="metricName">the name of a node metric or a number</param>
        /// <returns></returns>
        private float GetMetricValue(Node node, string metricName)
        {
            if (TryGetFloat(metricName, out float value))
            {
                return value;
            }
            else
            {
                return scaler.GetNormalizedValue(metricName, node);
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
        /// <param name="innerNodeFactory">the inner node factory, if the innerNodeFactory is null the global innernnodeFactory is used</param>
        /// <returns>new game object for the inner node</returns>
        public GameObject NewInnerNode(Node node, InnerNodeFactory innerNodeFactory = null)
        {
            if (innerNodeFactory == null)
            {
                innerNodeFactory = this.innerNodeFactory;
            }

            GameObject innerGameObject = innerNodeFactory.NewBlock();
            innerGameObject.name = node.ID;
            innerGameObject.tag = Tags.Node;
            AttachNode(innerGameObject, node);
            AttachHighlighter(innerGameObject);
            AdjustStyle(innerGameObject);
            AdjustHeightOfInnerNode(innerGameObject);
            return innerGameObject;
        }

        /// <summary>
        /// Adds game objects for all inner nodes in given list of nodes to nodeMap.
        /// Note: added game objects for inner nodes are not scaled.
        /// </summary>
        /// <param name="nodeMap">nodeMap to which the game objects are to be added</param>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        /// <param name="innerNodeFactory">the node factory for the inner nodes</param>
        protected void AddInnerNodes(Dictionary<Node, GameObject> nodeMap, IList<Node> nodes, InnerNodeFactory innerNodeFactory = null)
        {
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
        /// Returns the bounding box (2D rectangle) enclosing all given game nodes.
        /// </summary>
        /// <param name="gameNodes">the list of game nodes that are enclosed in the resulting bounding box</param>
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
        public void BoundingBox(ICollection<ILayoutNode> layoutNodes, out Vector2 leftLowerCorner, out Vector2 rightUpperCorner)
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
    }
}
