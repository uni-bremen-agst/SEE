using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using UnityEngine;
using UnityEngine.Assertions;
using Edge = SEE.DataModel.DG.Edge;
using Node = SEE.DataModel.DG.Node;
using Plane = SEE.GO.Plane;

namespace SEE.Game.Architecture
{
    
    /// <summary>
    /// A renderer for hierarchical architecture graphs. 
    /// </summary>
    public class ArchitectureRenderer
    {
        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="settings">the settings for the visualization</param>
        /// <param name="loadedGraph">the graph to be rendered</param>
        /// <exception cref="Exception">When a unknown <see cref="ArchitectureElementType"/>
        /// is found an exception is thrown.</exception>
        public ArchitectureRenderer(SEECityArchitecture settings, Graph loadedGraph)
        {
            this.settings = settings;
            for (int i = 0; i < (int)ArchitectureElementType.Count; i++)
            {
                ColorRange colorRange = this.settings.ArchitectureElementSettings[i].ColorRange;

                switch (this.settings.ArchitectureElementSettings[i].ElementType)
                {
                    case ArchitectureElementType.Cluster:
                        nodeFactories[i] = new CubeFactory(ShaderType, colorRange);
                        break;
                    case ArchitectureElementType.Component:
                        nodeFactories[i] = new CylinderFactory(ShaderType, colorRange);
                        break;
                    default:
                        throw new Exception("Unhandled ArchitectureElementType");
                }
                this.loadedGraph = loadedGraph;
            }

        }


        /// <summary>
        /// Settings for the graph visualization.
        /// </summary>
        private SEECityArchitecture settings;
        
        /// <summary>
        /// The loaded graph to be rendered.
        /// </summary>
        private Graph loadedGraph;
        
        /// <summary>
        /// The used Material shader type used to render the nodes and edges.
        /// </summary>
        private const Materials.ShaderType ShaderType = Materials.ShaderType.Opaque;
        
        /// <summary>
        /// The factories used to create the architecture node elements.
        /// </summary>
        private readonly NodeFactory[] nodeFactories = new NodeFactory[(int) ArchitectureElementType.Count];

        /// <summary>
        /// The ground levle of the nodes.
        /// </summary>
        private const float GroundLevel = 0.0f;
        
        /// <summary>
        /// The distance between each node level
        /// </summary>
        private const float LevelDistance = 0.001f;

        
        /// <summary>
        /// The maximum allowed block width.
        /// </summary>
        private const float MaxBlockWidth = 100f;

        /// <summary>
        /// The maximum allowed block depth.
        /// </summary>
        private const float MaxBlockDepth = 100f;
        
        /// <summary>
        /// The mapping for the node style by id.
        /// </summary>
        private Dictionary<string, int> style_mapping = new Dictionary<string, int>();


        /// <summary>
        /// Mapping of the string graph node types onto the <see cref="ArchitectureElementType"/>.
        /// </summary>
        private static readonly Dictionary<string, ArchitectureElementType> typeToElementType =
            new Dictionary<string, ArchitectureElementType>()
            {
                {"Cluster", ArchitectureElementType.Cluster},
                {"Component", ArchitectureElementType.Component},
                {"ROOTTYPE", ArchitectureElementType.Cluster}
            };


        /// <summary>
        /// Renders an edge between two nodes.
        /// </summary>
        /// <param name="from">The source node game object</param>
        /// <param name="to">The target node game object</param>
        /// <param name="id">The id of the edge</param>
        public GameObject DrawEdge(GameObject from, GameObject to, string id)
        {
            // get the source node
            Node fromNode = from.GetNode();
            if (fromNode == null)
            {
                throw new Exception($"The source {from.name} of the edge is not contained in any graph.\n");
            }
            
            // get the target node
            Node toNode = to.GetNode();
            if (toNode == null)
            {
                throw new Exception($"The target {to.name} of the edge is not contained in any graph.\n");
            }
                
            // Make sure both node elements exist in the same graph instance.
            if (fromNode.ItsGraph != toNode.ItsGraph)
            {
                throw new Exception(
                    $"The source {from.name} and target {to.name} of the edge are in different graphs.\n");
            }

            // initialize graph edge element
            Edge edge = string.IsNullOrEmpty(id) ? new Edge {Source = fromNode, Target = toNode, Type = Graph.UnknownType} : new Edge {ID = id, Source = fromNode, Target = toNode, Type = Graph.UnknownType};
            Graph graph = fromNode.ItsGraph;
            graph.AddEdge(edge);

            HashSet<GameObject> gameObjects = new HashSet<GameObject>();
            
            // Gather node ascendants and add them to the list
            RendererUtils.AddAscendants(from, gameObjects);
            RendererUtils.AddAscendants(to, gameObjects);
            Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();
            //Wraps all game objects nodes with the GameNode wrapper class
            ICollection<GameNode> layoutNodes = ToLayoutNodes(gameObjects, to_layout_node);
            GameNode fromlayoutNode = null;
            GameNode toLayoutNode = null;

            //Find source and target GameNodes from the list
            foreach (GameNode layoutNode in layoutNodes)
            {
                if (layoutNode.ItsNode == fromNode)
                {
                    fromlayoutNode = layoutNode;
                }

                if (layoutNode.ItsNode == toNode)
                {
                    toLayoutNode = layoutNode;
                }
            }
            
            Assert.IsNotNull(fromlayoutNode, $"Source node {fromNode.ID} does not have a layout node.\n");
            Assert.IsNotNull(toLayoutNode, $"Target node {toNode.ID} does not have a layout node.\n");
    
            ICollection<LayoutEdge> layoutEdges = new List<LayoutEdge>()
                {new LayoutEdge(fromlayoutNode, toLayoutNode, edge)};
            
            //Apply the Edge Layout
            ICollection<GameObject> edges = ApplyEdgeLayout(layoutNodes, layoutEdges);
            GameObject resultingEdge = edges.FirstOrDefault();
            //Find the SEECityArchitecture
            GameObject architectureCity = SceneQueries.FindArchitectureCity().gameObject;
            Transform rootNode = SceneQueries.GetCityRootNode(architectureCity);
            //Add the resulting edge as an parent to the root node
            resultingEdge.transform.SetParent(rootNode);
            //Sets the portal size to the extents of the architecture city
            Portal.SetPortal(root: architectureCity, gameObject: resultingEdge);
            return resultingEdge;

        }
    
        /// <summary>
        /// Connects the game nodes with their respective edges and applies the selected edge layout.
        /// </summary>
        /// <param name="gameNodes">The game nodes</param>
        /// <returns>List of edges</returns>
        private ICollection<GameObject> ApplyEdgeLayout(ICollection<GameNode> gameNodes)
        {
            return ApplyEdgeLayout(gameNodes, RendererUtils.ConnectingEdges(gameNodes));
        }
        
        /// <summary>
        /// Creates and renders the edges that connect the passed game nodes described with <paramref name="layoutEdges"/>.
        /// </summary>
        /// <param name="gameNodes">The game nodes</param>
        /// <param name="layoutEdges">The edges</param>
        /// <returns>List of the created edge game objects</returns>
        /// <exception cref="Exception">Thrown when an unhandled edge layout kind was found.</exception>
        private ICollection<GameObject> ApplyEdgeLayout(ICollection<GameNode> gameNodes,
            ICollection<LayoutEdge> layoutEdges)
        {
            float minimalEdgeLevelDistance = 2.5f * settings.edgeLayoutSettings.edgeWidth;
            bool edgesAboveBlocks = settings.edgeLayoutSettings.edgesAboveBlocks;
            float rdp = settings.edgeLayoutSettings.rdp;
            IEdgeLayout layout;
            switch (settings.edgeLayoutSettings.kind)
            {
                case EdgeLayoutKind.Straight:
                    layout = new StraightEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance);
                    break;
                case EdgeLayoutKind.Spline:
                    layout = new SplineEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance, rdp);
                    break;
                case EdgeLayoutKind.Bundling:
                    layout = new BundledEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance,
                        settings.edgeLayoutSettings.tension, rdp);
                    break;
                case EdgeLayoutKind.None:
                    return new List<GameObject>();
                default:
                    throw new Exception($"Unhandled edge layout was found {settings.edgeLayoutSettings.kind}");
            }

            EdgeFactory edgeFactory = new EdgeFactory(
                layout,
                settings.edgeLayoutSettings.edgeWidth,
                settings.edgeLayoutSettings.tubularSegments,
                settings.edgeLayoutSettings.radius,
                settings.edgeLayoutSettings.radialSegments,
                settings.edgeLayoutSettings.isEdgeSelectable);
            ICollection<GameObject> result = edgeFactory.DrawEdges(gameNodes.Cast<ILayoutNode>().ToList(), layoutEdges);
            //TODO Decorate edges
            RendererUtils.AddLOD(result);
            return result;
        }
        
        

        /// <summary>
        /// Renders the nodes and edges of the loaded graph and attaches these to the given parent game object.
        /// </summary>
        /// <param name="parent">The parent game object.</param>
        public void Draw(GameObject parent)
        {
            List<Node> nodes = loadedGraph.Nodes();
            
            
            if (nodes.Count == 0)
            {
                Debug.Log("Supplied GXL graph does not contain any graph nodes.\n Adding the base root node.\n");
                //TODO add base root node.
                AddAndRenderArtificialRootNode(loadedGraph, new Dictionary<Node, GameObject>());
            }

            // Draw the graph nodes as GameObjects
            Dictionary<Node, GameObject> nodeToGO = DrawNodes(nodes);
            // Add the root node that acts as a drawing plane, if it not already exists within the graph.
            Node artificialRoot = AddAndRenderArtificialRootNode(loadedGraph, nodeToGO);
            // Loads the layout from the supplied layout file
            NodeLayout layout = GetLayout();

            ICollection<GameNode> gameNodes = new List<GameNode>();
            Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();
            
            //Wraps all game object nodes with the GameNode wrapper class
            gameNodes = ToLayoutNodes(nodeToGO.Values, to_layout_node);
            //Converts the list of created game object nodes to ILayoutNodes
            ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();
            //Applies the loaded layout to the nodes.
            layout.Apply(layoutNodes);
            //Scales the nodes to fit within the parent transform
            NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
            //Moves the nodes to the center of the parent.
            NodeLayout.MoveTo(layoutNodes, parent.transform.position);

            Dictionary<Node, GameObject>.ValueCollection nodeToGameObject = nodeToGO.Values;
            //Creates a new portal plane to position the nodes and edges on
            GameObject plane = NewPlane(nodeToGameObject, parent.transform.position.y + parent.transform.lossyScale.y / 2.0f + LevelDistance);
            //Add the plane as a child to the parent
            RendererUtils.AddToParent(plane, parent);
            
            //Currently all nodes are layed out flat, therefore they need to be stacked on top of each other
            NodeLayout.Stack(layoutNodes, plane.transform.position.y + plane.transform.lossyScale.y / 2.0f + LevelDistance);
            // Scale the artificial root node to the maximum allowed block size.
            GameObject artificialRootGO = nodeToGO[artificialRoot];
            Vector3 scaling = artificialRootGO.transform.lossyScale;
            artificialRootGO.transform.localScale = new Vector3(MaxBlockWidth, scaling.y, MaxBlockDepth);
            nodeToGO[artificialRoot] = artificialRootGO;
            //Creates the game object object hierarchy
            RendererUtils.CreateObjectHierarchy(nodeToGO, parent);
            GameObject rootNode = RendererUtils.RootGameNode(parent);
            RendererUtils.AddToParent(ApplyEdgeLayout(gameNodes), rootNode);
            RefreshNodeStyle(parent, nodeToGameObject);
            //Sets the portal size to the extents of this parent.
            Portal.SetPortal(parent);
        }

        
        /// <summary>
        ///  Creates and returns a new plane enclosing all given <paramref name="gameNodes"/>
        /// </summary>
        /// <param name="gameNodes">The gameobjects</param>
        /// <param name="yLevel">The level of plane y-Axis</param>
        /// <returns>The plane</returns>
        public GameObject NewPlane(ICollection<GameObject> gameNodes, float yLevel)
        {
            RendererUtils.ArchitectureBoundingBox(gameNodes, out Vector2 leftFrontCorner, 
                out Vector2 rightBackCorner, nodeFactories, typeToElementType);
            return PlaneFactory.NewPlane(ShaderType, leftFrontCorner, rightBackCorner, yLevel, Color.gray, LevelDistance);
        }

        
        /// <summary>
        /// Prepares a new empty architecture graph that only contains a root drawing node with max block size.
        /// The created root is then added as a child game object to the passed parent.
        /// </summary>
        /// <param name="parent"></param>
        public void PrepareNewArchitectureGraph(GameObject parent)
        {
            Dictionary<Node, GameObject> nodeToGO = new Dictionary<Node, GameObject>();
            Node artificialRoot = AddAndRenderArtificialRootNode(loadedGraph, nodeToGO);
            Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();
            
            ICollection<GameNode> gamNodes = ToLayoutNodes(nodeToGO.Values, to_layout_node);
            ICollection<ILayoutNode> layoutNodes = gamNodes.Cast<ILayoutNode>().ToList();
            NodeLayout.Scale(layoutNodes, parent.transform.lossyScale.x);
            NodeLayout.MoveTo(layoutNodes, parent.transform.position);
            Dictionary<Node, GameObject>.ValueCollection nodeToGameObject = nodeToGO.Values;
            GameObject plane = NewPlane(nodeToGameObject, parent.transform.position.y + parent.transform.lossyScale.y / 2.0f + LevelDistance);
            RendererUtils.AddToParent(plane, parent);
            
            NodeLayout.Stack(layoutNodes, plane.transform.position.y + plane.transform.lossyScale.y / 2.0f + LevelDistance);
            RendererUtils.CreateObjectHierarchy(nodeToGO, parent);
            GameObject artificialRootGO = nodeToGO[artificialRoot];
            Vector3 scaling = artificialRootGO.transform.lossyScale;
            Debug.Log($"Scaling {scaling}");
            artificialRootGO.transform.localScale = new Vector3(MaxBlockWidth, scaling.y, MaxBlockDepth);
            nodeToGO[artificialRoot] = artificialRootGO;
            
            Portal.SetPortal(parent);
            
        }

        
        /// <summary>
        /// Transforms the given <paramref name="gameObjects"/> to a collection of <see cref="LayoutNode"/>.
        /// Sets the node levels of all <paramref name="gameObjects"/>.
        /// </summary>
        /// <param name="gameObjects">collection of game objects representing graph nodes</param>
        /// <param name="to_layout_node">mapping from graph node to layout node</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameObjects"/>
        /// </returns>
        private ICollection<GameNode> ToLayoutNodes(ICollection<GameObject> gameObjects, Dictionary<Node, ILayoutNode> to_layout_node)
        {
            IList<GameNode> result = new List<GameNode>(gameObjects.Count);
            foreach (GameObject gameObject in gameObjects)
            {
                Node node = gameObject.GetNode();
                NodeFactory factory = GetFactoryByType(node.Type);
                result.Add(new GameNode(to_layout_node, gameObject, factory));
            }

            return result;
        }

        
        /// <summary>
        /// Retrieves the <see cref="NodeFactory"/> by the given string node type.
        /// </summary>
        /// <param name="nodeType">The node type to find the mapping.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Thrown when an unhandled node type is found.</exception>
        private NodeFactory GetFactoryByType(string nodeType)
        {
            if (typeToElementType.TryGetValue(nodeType, out ArchitectureElementType type))
            {
                return nodeFactories[(int) type];
            }
            throw new Exception($"Caught unhandled node type {nodeType}.\n");
        }
    
        
        /// <summary>
        /// Loads the Layout
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private NodeLayout GetLayout()
        {
            if (File.Exists(settings.ArchitectureLayoutPath.Path))
            {
                return new LoadedNodeLayout(GroundLevel, settings.ArchitectureLayoutPath.Path);
            }
            Debug.LogError("No Layout file was found for the Architecture Layout.\n");
            throw new Exception("The supplied path to the ArchitectureLayout file does not contain any valid layout.");
        }


        
        /// <summary>
        /// Renders the visual representation of the given architecture graph nodes.
        /// </summary>
        /// <param name="nodes">The architecture graph nodes.</param>
        /// <returns>Mapping from graph nodes onto GameObjects</returns>
        public Dictionary<Node, GameObject> DrawNodes(ICollection<Node> nodes)
        {
            Dictionary<Node, GameObject> result = new Dictionary<Node, GameObject>();
            foreach (Node node in nodes)
            {
                GameObject go = DrawNode(node);
                result.Add(node, go);
            }

            return result;
        }

        /// <summary>
        /// Linear interpolation for the styling.
        /// </summary>
        /// <param name="node">The graph node to calculate the style for.</param>
        /// <returns>The style index</returns>
        /// <exception cref="Exception">Thrown when an unhandled <see cref="ArchitectureElementType"/> was found.</exception>
        public int SelectStyle(Node node)
        {
            NodeFactory factory = GetFactoryByType(node.Type);
            float maxGraphDepth = node.ItsGraph.MaxDepth;
            uint numberOfStyles = factory.NumberOfStyles();
            int result = Mathf.RoundToInt(Mathf.Lerp(0.0f, numberOfStyles, (node.Level) / maxGraphDepth));
            Debug.Log($"Calculated style index {result} for node {node.SourceName}");
            return result;
        }
        
        /// <summary>
        /// Renders a <see cref="GameObject"/> for the given graph node.
        /// </summary>
        /// <param name="node">The graph node to render</param>
        /// <returns>The rendered graph node as <see cref="GameObject"/></returns>
        /// <exception cref="Exception">Thrown when an unhandled <see cref="ArchitectureElementType"/> was found.</exception>
        public GameObject DrawNode(Node node)
        {
            NodeFactory factory = GetFactoryByType(node.Type);
            
            GameObject go = factory.NewBlock(SelectStyle(node), node.Level);
            go.name = node.ID;
            go.tag = Tags.Node;
            go.AddComponent<NodeRef>().Value = node;
            Vector3 scaling = go.transform.localScale;
            go.transform.localScale =
                new Vector3(scaling.x, settings.ArchitectureElementSettings[(int) typeToElementType[node.Type]].ElementHeight, scaling.z);
            ArchitectureDecorator.DecorateForInteraction(go);
            return go;
        }
        
        /// <summary>
        ///  Adds a root node to the architecture to act as a drawing sheet.
        /// </summary>
        /// <param name="graph">The graph to add the artificial root node to.</param>
        /// <param name="nodeMap">The node gameobject map.</param>
        /// <returns>The artificial root</returns>
        public Node AddAndRenderArtificialRootNode(Graph graph, IDictionary<Node, GameObject> nodeMap)
        {
            Node artificialRoot = AddArtificialRootNode(graph);
            nodeMap[artificialRoot] = DrawNode(artificialRoot);
            return artificialRoot;
        }

        
        /// <summary>
        /// Adds an artificial root node to the graph. This node represents the drawing plane on which the nodes are drawn.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public Node AddArtificialRootNode(Graph graph)
        {
            if (graph.ContainsNode(graph.Name + "#ROOT"))
            {
                Debug.Log($"Graph already contains drawing root with id {graph.Name + "#ROOT"}. Skipping adding of new root");
                return graph.GetNode(graph.Name + "#ROOT");
            }
            ICollection<Node> roots = graph.GetRoots();
            Node artificialRoot = new Node
            {
                ID = graph.Name + "#ROOT",
                SourceName = graph.Name + "#ROOT",
                Type = "ROOTTYPE"
            };
            graph.AddNode(artificialRoot);
            if (roots.Count > 1)
            {
                foreach (Node root in roots)
                {
                    artificialRoot.AddChild(root);
                }
            }

            return artificialRoot;
        }

        
        /// <summary>
        /// Refreshes the node style for each entry in the passed list of game nodes.
        /// Precondition: All game objects have an <see cref="NodeRef"/> component attached.
        /// Afterwards the Portal set again to the parent extents.
        /// </summary>
        /// <param name="parent">The parent object for correcting the portal extents.</param>
        /// <param name="gameNodes">The game objects.</param>
        public void RefreshNodeStyle(GameObject parent, ICollection<GameObject> gameNodes)
        {
            foreach (GameObject o in gameNodes)
            {
                RefreshNodeStyle(o);
            }
            Portal.SetPortal(parent);
        }
        
        /// <summary>
        /// Refreshes the styling of a given game node.
        /// </summary>
        /// <param name="nodeGO">The game node</param>
        public void RefreshNodeStyle(GameObject nodeGO)
        {
            if (nodeGO.TryGetNode(out Node node))
            {
                NodeFactory factory = GetFactoryByType(node.Type);
                factory.SetStyle(nodeGO, SelectStyle(node));
                return;
            }
            throw new Exception(
                $"Tried to refresh the node style for {nodeGO.name} which has no NodeRef component attached.\n");
        }
    }
}