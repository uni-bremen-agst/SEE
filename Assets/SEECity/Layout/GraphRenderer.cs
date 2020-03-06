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
            switch (this.settings.InnerNodeObjects)
            {
                case SEECity.InnerNodeKinds.Empty:
                case SEECity.InnerNodeKinds.Donuts:
                    innerNodeFactory = new VanillaFactory();
                    break;
                case SEECity.InnerNodeKinds.Circles:
                    innerNodeFactory = new CircleFactory(leafNodeFactory.Unit);
                    break;
                case SEECity.InnerNodeKinds.Cylinders:
                    innerNodeFactory = new CylinderFactory();
                    break;
                case SEECity.InnerNodeKinds.Rectangles:
                    innerNodeFactory = new RectangleFactory(leafNodeFactory.Unit);
                    break;
                case SEECity.InnerNodeKinds.Blocks:
                    innerNodeFactory = new CubeFactory();
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds");
            }
        }

        /// <summary>
        /// Settings for the visualization.
        /// </summary>
        private readonly AbstractSEECity settings;

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
        /// <param name="graph">the graph to be drawn</param>
        /// <param name="parent">every game object drawn for this graph will be added to this parent</param>
        public void Draw(Graph graph, GameObject parent)
        {
            SetScaler(graph);
            graph.SortHierarchyByName();
            DrawCity(graph, parent);
        }

        /// <summary>
        /// Sets the scaler to be used to map metric values onto graphical attributes
        /// (e.g., width, height, depth, color) across all given <paramref name="graphs"/>
        /// based on the user's choice (settings).
        /// </summary>
        /// <param name="graphs"></param>
        public void SetScaler(ICollection<Graph> graphs)
        {
            // FIXME: Implement this.
            throw new NotImplementedException();
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
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private ICollection<GameObject> EdgeLayout(Graph graph, ICollection<GameObject> gameNodes)
        {
            IEdgeLayout layout;
            switch (settings.EdgeLayout)
            {
                case SEECity.EdgeLayouts.Straight:
                    layout = new StraightEdgeLayout(leafNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case SEECity.EdgeLayouts.Spline:
                    layout = new SplineEdgeLayout(leafNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case SEECity.EdgeLayouts.Bundling:
                    layout = new BundledEdgeLayout(leafNodeFactory, settings.EdgeWidth, settings.EdgesAboveBlocks);
                    break;
                case SEECity.EdgeLayouts.None:
                    // nothing to be done
                    return new List<GameObject>();
                default:
                    throw new Exception("Unhandled edge layout " + settings.EdgeLayout.ToString());
            }
            Performance p = Performance.Begin(layout.Name + " layout of edges");
            ICollection<GameObject> result = layout.DrawEdges(graph, gameNodes);
            p.End();
            return result;
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
        /// <param name="parent">every game object drawn for this graph will be added to this parent</param>
        protected void DrawCity(Graph graph, GameObject parent)
        {
            // all nodes of the graph
            List<Node> nodes = graph.Nodes();
            // game objects for the leaves
            Dictionary<Node, GameObject> nodeMap = CreateBlocks(nodes);
            // the layout to be applied
            NodeLayout nodeLayout = GetLayout();
            // for a hierarchical layout, we need to add the game objects for inner nodes
            if (nodeLayout.IsHierarchical())
            {
                AddInnerNodes(nodeMap, nodes); // and inner nodes
            }
            // calculate the layout
            Dictionary<GameObject, NodeTransform> layout = nodeLayout.Layout(nodeMap.Values);
            // apply the layout
            Apply(layout, settings.origin);
            // add all game nodes as children to parent
            ICollection<GameObject> gameNodes = layout.Keys;
            AddToParent(gameNodes, parent);
            // add the decorations, too
            AddToParent(AddDecorations(gameNodes), parent);
            // create the laid out edges
            AddToParent(EdgeLayout(graph, gameNodes), parent);
            // add the plane surrounding all game objects for nodes
            GameObject plane = NewPlane(gameNodes);
            AddToParent(plane, parent);
        }

        /// <summary>
        /// Returns the node layouter according to the settings.
        /// </summary>
        /// <returns>node layout selected</returns>
        public NodeLayout GetLayout()
        {
            switch (settings.NodeLayout)
            {
                case SEECity.NodeLayouts.Manhattan:
                    return new ManhattanLayout(groundLevel, leafNodeFactory);
                case SEECity.NodeLayouts.FlatRectanglePacking:
                    return new RectanglePacker(groundLevel, leafNodeFactory);
                case SEECity.NodeLayouts.EvoStreets:
                    return new EvoStreetsNodeLayout(groundLevel, leafNodeFactory);
                case SEECity.NodeLayouts.Treemap:
                    return new TreemapLayout(groundLevel, leafNodeFactory, 1000.0f * Unit(), 1000.0f * Unit());
                case SEECity.NodeLayouts.Balloon:
                    return new BalloonNodeLayout(groundLevel, leafNodeFactory);
                case SEECity.NodeLayouts.CirclePacking:
                    return new CirclePackingNodeLayout(groundLevel, leafNodeFactory);
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
            GameObject plane = PlaneFactory.NewPlane(leftFrontCorner, rightBackCorner, groundLevel - 0.01f, Color.gray);
            return plane;
        }

        /// <summary>
        /// Adds <paramref name="child"/> as a child to <paramref name="parent"/>.
        /// </summary>
        /// <param name="child">child to be added</param>
        /// <param name="parent">new parent of child</param>
        private static void AddToParent(GameObject child, GameObject parent)
        {
            child.transform.SetParent(parent.transform, true);
            //child.transform.parent = parent.transform;
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
        /// Draws the decorations of the given game nodes.
        /// </summary>
        /// <param name="gameNodes">game nodes to be decorated</param>
        /// <returns>the game objects added for the decorations; may be an empty collection</returns>
        private ICollection<GameObject> AddDecorations(ICollection<GameObject> gameNodes)
        {
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
            if (settings.NodeLayout == SEECity.NodeLayouts.Balloon 
                || settings.NodeLayout == SEECity.NodeLayouts.EvoStreets)
            {
                decorations.AddRange(AddLabels(InnerNodes(gameNodes)));
            }

            // Add decorators specific to the shape of inner nodes (circle decorators for circles
            // and donut decorators for donuts.
            switch (settings.InnerNodeObjects)
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
                case SEECity.InnerNodeKinds.Blocks:
                    // TODO
                    break;
                default:
                    throw new Exception("Unhandled GraphSettings.InnerNodeKinds " + settings.InnerNodeObjects);
            }
            return decorations;
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
        /// Applies the layout to all nodes at given origin.
        /// </summary>
        /// <param name="layout">node layout to be applied</param>
        /// <param name="origin">the center origin where the graph should be placed in the world scene</param>
        public void Apply(Dictionary<GameObject, NodeTransform> layout, Vector3 origin)
        {      
            foreach (var entry in NodeLayout.Move(layout, origin))
            {
                GameObject gameNode = entry.Key;
                NodeTransform transform = entry.Value;
                Apply(gameNode, transform);
            }
        }

        /// <summary>
        /// Applies the given <paramref name="transform"/> to the given <paramref name="gameNode"/>,
        /// i.e., sets its size and position according to the <paramref name="transform"/>. The
        /// game node can represent a leaf or inner node of the node hierarchy.
        /// 
        /// Precondition: <paramref name="gameNode"/> must have NodeRef component referencing a
        /// graph node.
        /// </summary>
        /// <param name="gameNode">the game node the transform should be applied to</param>
        /// <param name="transform">transform to be applied to the game node</param>
        public void Apply(GameObject gameNode, NodeTransform transform)
        {
            Node node = gameNode.GetComponent<NodeRef>().node;

            if (node.IsLeaf())
            {
                // We need to first scale the game node and only afterwards set its
                // position because transform.scale refers to the center position.
                if (settings.NodeLayout == SEECity.NodeLayouts.Treemap)
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
            // Rotate the game object.
            Rotate(gameNode, transform.rotation);
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
                    GameObject block = NewLeafNode(node, metricMaximum);
                    result[node] = block;
                }
            }
            return result;
        }

        /// <summary>
        /// Create and returns a new game object for representing the given <paramref name="node"/>.
        /// The exact kind of representation depends upon the leaf-node factory. The node is 
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings. 
        /// Its color is determined by ColorMetric (linerar interpolation of a color gradient).
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// 
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        public GameObject NewLeafNode(Node node)
        {
            return NewLeafNode(node, scaler.GetNormalizedMaximum(settings.ColorMetric));
        }

        /// <summary>
        /// Create and returns a new game object for representing the given <paramref name="node"/>.
        /// The exact kind of representation depends upon the leaf-node factory. The node is 
        /// scaled according to the WidthMetric, HeightMetric, and DepthMetric of the current settings. 
        /// Its color is determined by ColorMetric devided by <paramref name="metricMaximum"/>.
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// 
        /// Precondition: <paramref name="node"/> must be a leaf node in the node hierarchy.
        /// </summary>
        /// <param name="node">leaf node</param>
        /// <param name="metricMaximum">the maximal value the color metric can have;
        /// used to devide the color metric's value so that it stays in the range [0,1]</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        private GameObject NewLeafNode(Node node, float metricMaximum)
        {
            int material = SelectStyle(node, metricMaximum);
            GameObject block = leafNodeFactory.NewBlock(material);
            block.name = node.LinkName;
            AttachNode(block, node);
            AdjustVisualsOfBlock(node, block);
            return block;
        }

        /// <summary>
        /// Returns a style index as a linear interpolation of X for range [0..M-1]
        /// where M is the number of available styles of the leafNodeFactory
        /// and X = C / metricMaximum and C is the normalized metric value of 
        /// <paramref name="node"/> for the attribute chosen for the color.
        /// </summary>
        /// <param name="node">node for which to determine the style index</param>
        /// <param name="metricMaximum">the maximal value of the metric chosen for color</param>
        /// <returns>style index</returns>
        private int SelectStyle(Node node, float metricMaximum)
        {
            return Mathf.RoundToInt(Mathf.Lerp(0.0f,
                                               (float)(leafNodeFactory.NumberOfStyles() - 1),
                                                scaler.GetNormalizedValue(settings.ColorMetric, node)
                                                         / metricMaximum));
        }

        /// <summary>
        /// Adjusts the scale and style of the given <paramref name="block"/> according
        /// to the metric values of the graph node attached to <paramref name="block"/>
        /// chosen to determine scale and color.
        /// 
        /// Precondition: <paramref name="node"/> is a leaf.
        /// </summary>
        /// <param name="block">a block representing a leaf graph node</param>
        public void AdjustVisualsOfBlock(GameObject block)
        {
            NodeRef noderef = block.GetComponent<NodeRef>();
            if (noderef == null)
            {
                throw new Exception("block game object " + block.name + " does not have a graph node attached to it.");
            }
            else
            {
                Node node = noderef.node;
                if (node.IsLeaf())
                {
                    float metricMaximum = scaler.GetNormalizedMaximum(settings.ColorMetric);
                    int material = SelectStyle(node, metricMaximum);
                    AdjustVisualsOfBlock(node, block, material);
                }
                else
                {
                    throw new Exception("block game object " + block.name + " is not a leaf.");
                }
            }
        }

        /// <summary>
        /// Adjusts the scale and style of the given <paramref name="block"/> according
        /// to the metric values of the <paramref name="node"/> attached to 
        /// <paramref name="block"/>. The scale is determined by the node's
        /// width, height, and depth metrics (which are determined by the settings).
        /// The style of <paramref name="block"/> will be determined by the given 
        /// parameter <paramref name="style"/> if (and only if) <paramref name="style"/>
        /// is equal to or greater than 0. If <paramref name="style"/> is negative,
        /// the style of <paramref name="block"/> will not be changed.
        /// 
        /// Precondition: <paramref name="node"/> is a leaf.
        /// 
        /// Assumption: <paramref name="node"/> is attached to <paramref name="block"/>
        /// and has the width, height, and depth metrics set.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="block"></param>
        /// <param name="style"></param>
        private void AdjustVisualsOfBlock(Node node, GameObject block, int style = -1)
        {
            if (style > 0)
            {
                leafNodeFactory.SetStyle(block, style);
            }
            // Scaled metric values for the three dimensions.
            Vector3 scale = new Vector3(scaler.GetNormalizedValue(settings.WidthMetric, node),
                                        scaler.GetNormalizedValue(settings.HeightMetric, node),
                                        scaler.GetNormalizedValue(settings.DepthMetric, node));

            // Scale according to the metrics.
            if (settings.NodeLayout == SEECity.NodeLayouts.Treemap)
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
        }

        /// <summary>
        /// Creates a new game object for an inner node using innerNodeFactory.
        /// The inner <paramref name="node"/> is attached to that new game object
        /// via a NodeRef component.
        /// 
        /// Precondition: <paramref name="node"/> must be an inner node of the node
        /// hierarchy.
        /// </summary>
        /// <param name="node">graph node for which to create the game node</param>
        /// <returns>new game object for the inner node</returns>
        public GameObject NewInnerNode(Node node)
        {
            GameObject innerGameObject = innerNodeFactory.NewBlock();
            innerGameObject.name = node.LinkName;
            innerGameObject.tag = Tags.Node;
            AttachNode(innerGameObject, node);
            return innerGameObject;
        }

        /// <summary>
        /// Adds game objects for all inner nodes in given list of nodes to nodeMap.
        /// Note: added game objects for inner nodes are not scaled.
        /// </summary>
        /// <param name="nodeMap">nodeMap to which the game objects are to be added</param>
        /// <param name="nodes">list of nodes for which to create blocks</param>
        private void AddInnerNodes(Dictionary<Node, GameObject> nodeMap, IList<Node> nodes)
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
