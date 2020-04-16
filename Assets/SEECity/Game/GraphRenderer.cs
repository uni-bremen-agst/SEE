using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.GO;
using SEE.Layout;
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
        /// mapping from node to ilayoutNode
        /// </summary>
        private Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();

        /// <summary>
        /// TODO
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
        /// Apply the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        /// <param name="gameNodes">the subset of nodes for which to draw the edges</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        public ICollection<GameObject> EdgeLayout(Graph graph, ICollection<GameObject> gameNodes)
        {
            return EdgeLayout(graph, ToLayoutNodes(gameNodes));
        }

        /// <summary>
        /// Apply the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="graph">graph whose edges are to be drawn</param>
        /// <param name="layoutNodes">the subset of layout nodes for which to draw the edges</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        public ICollection<GameObject> EdgeLayout(Graph graph, ICollection<ILayoutNode> layoutNodes)
        {
            IEdgeLayout layout;
            switch (settings.EdgeLayout)
            {
                case SEECity.EdgeLayouts.Straight:
                    layout = new StraightEdgeLayout(settings.EdgesAboveBlocks);
                    break;
                case SEECity.EdgeLayouts.Spline:
                    layout = new SplineEdgeLayout(settings.EdgesAboveBlocks);
                    break;
                case SEECity.EdgeLayouts.Bundling:
                    layout = new BundledEdgeLayout(settings.EdgesAboveBlocks);
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
            // game objects for the leaves
            Dictionary<Node, GameObject> nodeMap = CreateBlocks(nodes);
            // the layout to be applied
            NodeLayout nodeLayout = GetLayout();
            // for a hierarchical layout, we need to add the game objects for inner nodes
            Dictionary<Node, GameObject>.ValueCollection gameNodes;
            ICollection<ILayoutNode> layoutNodes;

            Performance p;
            if (settings.NodeLayout.GetModel().CanApplySublayouts)
            {

                ICollection<SublayoutNode>  sublayoutNodes = AddInnerNodesForSublayouts(nodeMap, nodes);
                gameNodes = nodeMap.Values;
                layoutNodes = ToLayoutNodes(nodeMap, sublayoutNodes);

                List<SublayoutLayoutNode> sublayoutLayoutNodes = ConvertSublayoutToLayoutNodes(sublayoutNodes.ToList());
                foreach (SublayoutLayoutNode layoutNode in sublayoutLayoutNodes)
                {
                    Sublayout sublayout = new Sublayout(layoutNode, groundLevel, GetInnerNodeFactory(layoutNode.InnerNodeKind));
                    sublayout.Layout();
                }

                p = Performance.Begin("layout name" + settings.NodeLayout + ", layout of nodes");
                if (nodeLayout.UsesEdgesAndSublayoutNodes())
                {
                    nodeLayout.Apply(layoutNodes, graph.Edges(), sublayoutLayoutNodes);
                }
                p.End();

                NodeLayout.Move(layoutNodes, settings.origin);
                AddToParent(gameNodes, parent);

                // add the decorations, too
                if (sublayoutLayoutNodes.Count <= 0)
                {
                    AddToParent(AddDecorations(gameNodes), parent);
                }
                else
                {
                    AddDecorationsForSublayouts(layoutNodes, sublayoutLayoutNodes, parent);
                }
            }
            else
            {
                if (nodeLayout.IsHierarchical())
                {
                    AddInnerNodes(nodeMap, nodes); // and inner nodes
                }

                // calculate and apply the node layout
                gameNodes = nodeMap.Values;
                layoutNodes = ToLayoutNodes(gameNodes);
                p = Performance.Begin("layout name" + settings.NodeLayout + ", layout of nodes");
                nodeLayout.Apply(layoutNodes);
                p.End();
                NodeLayout.Move(layoutNodes, settings.origin);

                AddToParent(gameNodes, parent);
                // add the decorations, too
                AddToParent(AddDecorations(gameNodes), parent);
            }

            EdgeDistCalculation(graph, layoutNodes);
            // create the laid out edges
            AddToParent(EdgeLayout(graph, layoutNodes), parent);
            // add the plane surrounding all game objects for nodes

            BoundingBox(layoutNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            GameObject plane = NewPlane(leftFrontCorner, rightBackCorner);
            AddToParent(plane, parent);

            Measurements measurements = new Measurements(layoutNodes.Cast<GameNode>().ToList(), graph, settings, leftFrontCorner, rightBackCorner);
            measurements.NodesPerformance(p);
        }


        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="gameNodes"></param>
        protected void EdgeDistCalculation(Graph graph, ICollection<ILayoutNode> layoutNodes)
        {

            foreach (Edge edge in graph.Edges())
            {
                Vector3 sourcePosition = layoutNodes.Where(node => node.LinkName == edge.Source.LinkName).First().CenterPosition;

                Vector3 targetPosition = layoutNodes.Where(node => node.LinkName == edge.Target.LinkName).First().CenterPosition;

                edge.dist = Vector3.Distance(sourcePosition, targetPosition);
            }
        }


        /// <summary>
        /// TODo
        /// </summary>
        /// <param name="layoutNodes"></param>
        /// <param name="sublayoutLayoutNodes"></param>
        /// <param name="parent"></param>
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
                AddToParent(AddDecorations(gameObjects, layoutNode.InnerNodeKind, layoutNode.NodeLayout), parent);
                remainingLayoutNodes.RemoveAll(node => layoutNode.Nodes.Contains(node));
            }

            ICollection<GameObject> remainingGameObjects = new List<GameObject>();
            foreach (GameNode gameNode in remainingLayoutNodes)
            {
                remainingGameObjects.Add(gameNode.GetGameObject());
            }

            AddToParent(AddDecorations(remainingGameObjects), parent);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="nodeMap"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private List<SublayoutNode> AddInnerNodesForSublayouts(Dictionary<Node, GameObject> nodeMap, List<Node> nodes)
        {
            List<SublayoutNode> coseSublayoutNodes = CreateCoseSublayoutNodes(nodes);

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
        /// TODO
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private List<SublayoutNode> CreateCoseSublayoutNodes(List<Node> nodes)
        {
            List<SublayoutNode> coseSublayoutNodes = new List<SublayoutNode>();
            foreach (KeyValuePair<string, bool> dir in settings.CoseGraphSettings.ListDirToggle)
            {
                if (dir.Value)
                {
                    var name = dir.Key;
                    if (settings.CoseGraphSettings.DirNodeLayout.ContainsKey(name) && settings.CoseGraphSettings.DirShape.ContainsKey(name))
                    {
                        IEnumerable<Node> matches = nodes.Where(i => i.LinkName.Equals(name));
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
        /// TODO
        /// </summary>
        /// <param name="coseSublayoutNodes"></param>
        private void CalculateNodesSublayout(List<SublayoutNode> coseSublayoutNodes)
        {
            foreach (SublayoutNode sublayoutNode in coseSublayoutNodes)
            {
                List<Node> children = WithAllChildren(sublayoutNode.Node);
                List<Node> childrenToRemove = new List<Node>();

                foreach (Node child in children)
                {
                    SublayoutNode sublayout = CoseHelper.CheckIfNodeIsSublayouRoot(coseSublayoutNodes, child.LinkName);

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
            sublayouts.ForEach(sublayoutNode => {

                SublayoutLayoutNode sublayout = new SublayoutLayoutNode(to_layout_node[sublayoutNode.Node], sublayoutNode.InnerNodeKind, sublayoutNode.NodeLayout);
                sublayoutNode.Nodes.ForEach(n => sublayout.Nodes.Add(to_layout_node[n]));
                sublayoutNode.RemovedChildren.ForEach(n => sublayout.RemovedChildren.Add(to_layout_node[n]));
                sublayoutLayoutNodes.Add(sublayout);
            });
            return sublayoutLayoutNodes;
        }


        /// <summary>
        /// TODO
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
                case SEECity.NodeLayouts.FlatRectanglePacking:
                    return new RectanglePacker(groundLevel, leafNodeFactory.Unit);
                case SEECity.NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory.Unit);
                case SEECity.NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, 1000.0f * Unit(), 1000.0f * Unit());
                case SEECity.NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel);
                case SEECity.NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel);
                case SEECity.NodeLayouts.CompoundSpringEmbedder:
                    return new CoseLayout(groundLevel, settings, leafNodeFactory);
                default:
                    throw new Exception("Unhandled node layout " + settings.NodeLayout.ToString());
            }
        }

        /// <summary>
        /// Creates and returns a new plane enclosing all given <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">the game objects to be enclosed by the new plane</param>
        /// <returns>new plane enclosing all given <paramref name="gameNodes"/></returns>
        public GameObject NewPlane(ICollection<GameObject> gameNodes)
        {
            BoundingBox(gameNodes, out Vector2 leftFrontCorner, out Vector2 rightBackCorner);
            // Place the plane somewhat under ground level.
            return NewPlane(leftFrontCorner, rightBackCorner); //PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, settings.origin.y - 0.5f, Color.gray);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public GameObject NewPlane(Vector2 leftFrontCorner, Vector2 rightBackCorner)
        {
            return PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, settings.origin.y - 0.5f, Color.gray);
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


        protected ICollection<GameObject> AddDecorations(ICollection<GameObject> gameNodes)
        {
            return AddDecorations(gameNodes, settings.InnerNodeObjects, settings.NodeLayout);
        }

        /// <summary>
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        /// <returns>the game objects added for the decorations; may be an empty collection</returns>
        private ICollection<GameObject> AddDecorations(ICollection<GameObject> gameNodes, InnerNodeKinds innerNodeKinds, NodeLayouts nodeLayout)
        {
            var innerNodeFactory = GetInnerNodeFactory(innerNodeKinds);

            // Decorations must be applied after the blocks have been placed, so that
            // we also know their positions.
            List<GameObject> decorations = new List<GameObject>();

            // Add software erosion decorators for all leaf nodes if requested.
            if (settings.ShowErosions)
            {
                ErosionIssues issueDecorator = new ErosionIssues(settings.LeafIssueMap(), leafNodeFactory, scaler);
                decorations.AddRange(issueDecorator.Add(LeafNodes(gameNodes)));
            }

            // Add text labels for all inner nodes
            if (nodeLayout == SEECity.NodeLayouts.Balloon 
                || nodeLayout == SEECity.NodeLayouts.EvoStreets)
            {
                decorations.AddRange(AddLabels(InnerNodes(gameNodes)));
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
                        CircleDecorator decorator = new CircleDecorator(innerNodeFactory, Color.white);
                        // the circle decorator does not create new game objects; it justs adds a line
                        // renderer to the list of nodes; that is why we do not add the result to decorations.
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
                case SEECity.InnerNodeKinds.Rectangles:
                    {
                        RectangleDecorator decorator = new RectangleDecorator(innerNodeFactory, Color.white);
                        decorator.Add(InnerNodes(gameNodes));
                    }
                    break;
                case SEECity.InnerNodeKinds.Blocks:
                    // TODO
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds " + settings.InnerNodeObjects);
            }
            return decorations;
        }

        /// <summary>
        /// Transforms the given <paramref name="gameNodes"/> to a collection of LayoutNodes.
        /// Sets the node levels of all <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        protected ICollection<ILayoutNode> ToLayoutNodes(ICollection<GameObject> gameObjects)
        {
            return ToLayoutNodes(gameObjects, leafNodeFactory, innerNodeFactory);
        }

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
        /// Adds the source name as a label to the center of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes whose source name is to be added</param>
        /// <returns>the game objects created for the text labels</returns>
        private ICollection<GameObject> AddLabels(ICollection<GameObject> gameNodes)
        {
            IList<GameObject> result = new List<GameObject>();

            foreach (GameObject node in gameNodes)
            {
                Vector3 size = innerNodeFactory.GetSize(node);
                float length = Mathf.Min(size.x, size.z);
                // The text may occupy up to 30% of the length.
                GameObject text = TextFactory.GetText(node.GetComponent<NodeRef>().node.SourceName, 
                                                      node.transform.position, length * 0.3f);
                result.Add(text);
            }
            return result;
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
                leafNodeFactory.SetSize(gameNode, layout.Scale);
                // FIXME: Must adjust layout.CenterPosition.y
                leafNodeFactory.SetGroundPosition(gameNode, layout.CenterPosition);
            }
            else
            {
                // Inner nodes were created by innerNodeFactory.
                innerNodeFactory.SetSize(gameNode, layout.Scale);
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
            int style = SelectStyle(node);
            GameObject block = leafNodeFactory.NewBlock(style);
            block.name = node.LinkName;
            AttachNode(block, node);
            AdjustScaleOfLeaf(block);
            return block;
        }

        /// <summary>
        /// Returns a style index as a linear interpolation of X for range [0..M-1]
        /// where M is the number of available styles of the leafNodeFactory (if
        /// the node is a leaf) or innerNodeFactory (if it is an inner node)
        /// and X = C / metricMaximum and C is the normalized metric value of 
        /// <paramref name="node"/> for the attribute chosen for the style
        /// and metricMaximum is the maximal value of the style metric.
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
            int style = isLeaf ? leafNodeFactory.NumberOfStyles() : innerNodeFactory.NumberOfStyles();
            string styleMetric = isLeaf ? settings.LeafStyleMetric : settings.InnerNodeStyleMetric;
            float metricMaximum = scaler.GetNormalizedMaximum(styleMetric);
            return Mathf.RoundToInt(Mathf.Lerp(0.0f,
                                               (float)(style - 1),
                                                scaler.GetNormalizedValue(styleMetric, node)
                                                         / metricMaximum));
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
                    Vector3 scale = new Vector3(scaler.GetNormalizedValue(settings.WidthMetric, node),
                                                scaler.GetNormalizedValue(settings.HeightMetric, node),
                                                scaler.GetNormalizedValue(settings.DepthMetric, node));

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
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// The inner <paramref name="node"/> is attached to that new game object
        /// via a NodeRef component. The style of resulting game object is adjusted
        /// according to the selected InnerNodeStyleMetric but not its scale.
        /// 
        /// Precondition: <paramref name="node"/> must be an inner node of the node
        /// hierarchy.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        public GameObject NewInnerNode(Node node, InnerNodeFactory innerNodeFactory = null)
        {
            if (innerNodeFactory == null)
            {
                innerNodeFactory = this.innerNodeFactory;
            }

            GameObject innerGameObject = innerNodeFactory.NewBlock();
            innerGameObject.name = node.LinkName;
            innerGameObject.tag = Tags.Node;
            AttachNode(innerGameObject, node);
            AdjustStyle(innerGameObject, innerNodeFactory);
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
       /// TODO
       /// </summary>
       /// <param name="layoutNodes"></param>
       /// <param name="leftLowerCorner"></param>
       /// <param name="rightUpperCorner"></param>
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
                    Vector3 extent = layoutNode.Scale / 2.0f;
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
