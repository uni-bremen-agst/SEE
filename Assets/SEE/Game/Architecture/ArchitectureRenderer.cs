using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Controls.Architecture;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.GO.NodeFactories;
using SEE.Layout;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using UnityEngine;
using UnityEngine.Assertions;
using Edge = SEE.DataModel.DG.Edge;
using Node = SEE.DataModel.DG.Node;

namespace SEE.Game.Architecture
{
    /// <summary>
    /// A renderer for hierarchical architecture graphs.
    ///
    /// FIXME: This should be a subclass of GraphRenderer. There is too much redundancy.
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
            }
            this.loadedGraph = loadedGraph;
            //Get the PenInteractionController from the SEECityArchitecture object. Should be set from PlayerSettingsEditor
            if (!settings.TryGetComponent(out PenInteractionController))
            {
                throw new Exception(
                    "City game object does not have the PenInteractionController component attached! Check your setup");
            }

        }

        /// <summary>
        /// Settings for the graph visualization.
        /// </summary>
        private SEECityArchitecture settings;

        /// <summary>
        /// The <see cref="PenInteractionController"/> component that is always attached to the <see cref="SEECityArchitecture"/>.
        /// </summary>
        private PenInteractionController PenInteractionController;

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
        /// Mapping of the string graph node types onto the <see cref="ArchitectureElementType"/>.
        /// </summary>
        private static readonly Dictionary<string, ArchitectureElementType> typeToElementType =
            new Dictionary<string, ArchitectureElementType>()
            {
                {"Cluster", ArchitectureElementType.Cluster},
                {"Component", ArchitectureElementType.Component}
            };


        /// <summary>
        /// Renders an edge between two nodes.
        /// </summary>
        /// <param name="from">The source node game object</param>
        /// <param name="to">The target node game object</param>
        /// <param name="id">The id of the edge</param>
        public GameObject DrawEdge(GameObject from, GameObject to, string id)
        {
            if (from.TryGetNode(out Node fromNode) && to.TryGetNode(out Node toNode))
            {
                Assert.AreEqual(fromNode.ItsGraph, toNode.ItsGraph);

                GraphRenderer graphRenderer = new GraphRenderer(settings, fromNode.ItsGraph);
                // Creating the edge in the underlying graph
                Edge edge = string.IsNullOrEmpty(id) ? new Edge { Source = fromNode, Target = toNode, Type = Graph.UnknownType }
                                                     : new Edge { ID = id, Source = fromNode, Target = toNode, Type = Graph.UnknownType };
                fromNode.ItsGraph.AddEdge(edge);
                GameObject gameEdge = graphRenderer.DrawEdge(edge, from, to);
                GameObject architectureCity = SceneQueries.FindArchitectureCity().gameObject;
                GameObject whiteboard = SceneQueries.FindWhiteboard();
                //Add the resulting edge as an parent to the root node
                gameEdge.transform.SetParent(whiteboard.transform);
                //Sets the portal size to the extents of the architecture city
                Portal.SetPortal(root: architectureCity, gameObject: gameEdge);
                return gameEdge;
            }
            else
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Connects the game nodes with their respective edges and applies the selected edge layout.
        /// </summary>
        /// <param name="gameNodes">The game nodes</param>
        /// <returns>List of edges</returns>
        private ICollection<GameObject> ApplyEdgeLayout(ICollection<ILayoutNode> gameNodes)
        {
            throw new NotImplementedException();
            //return ApplyEdgeLayout(gameNodes, RendererUtils.ConnectingEdges(gameNodes));
        }

        /// <summary>
        /// Creates and renders the edges that connect the passed game nodes described with <paramref name="layoutEdges"/>.
        /// </summary>
        /// <param name="gameNodes">The game nodes</param>
        /// <param name="layoutEdges">The edges</param>
        /// <returns>List of the created edge game objects</returns>
        /// <exception cref="Exception">Thrown when an unhandled edge layout kind was found.</exception>
        private ICollection<GameObject> ApplyEdgeLayout(ICollection<ILayoutNode> gameNodes,
            ICollection<LayoutGraphEdge<ILayoutNode>> layoutEdges)
        {
            throw new NotImplementedException();
            //float minimalEdgeLevelDistance = 2.5f * settings.EdgeLayoutSettings.EdgeWidth;
            //bool edgesAboveBlocks = settings.EdgeLayoutSettings.EdgesAboveBlocks;
            //IEdgeLayout layout = new ArchitectureEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance);
            //EdgeFactory edgeFactory = new EdgeFactory(layout, settings.EdgeLayoutSettings.EdgeWidth);
            //ICollection<LayoutGameNode> result = edgeFactory.DrawEdges(gameNodes.Cast<ILayoutNode>().ToList(), layoutEdges);
            //ArchitectureDecorator.DecorateForInteraction(result, PenInteractionController);
            //RendererUtils.AddLOD(result);
            //return result;
        }

        /// <summary>
        /// Renders the nodes and edges of the loaded graph and attaches these to the given parent game object.
        /// </summary>
        /// <param name="parent">The parent game object.</param>
        public void Draw(GameObject parent)
        {
            throw new NotImplementedException();

            //List<Node> nodes = loadedGraph.Nodes();
            //if (nodes.Count == 0)
            //{
            //    Debug.Log("Supplied GXL graph does not contain any graph nodes.\n Adding just a whiteboard.\n");
            //    PrepareNewArchitectureGraph(parent);
            //    return;
            //}
            //// Draw the graph nodes as GameObjects
            //Dictionary<Node, GameObject> nodeToGO = DrawNodes(nodes);
            //// Add the root node that acts as a drawing plane, if it not already exists within the graph.
            ////Node artificialRoot = AddAndRenderArtificialRootNode(loadedGraph, nodeToGO);
            //// Loads the layout from the supplied layout file
            //NodeLayout layout = GetLayout();
            //ICollection<ILayoutNode> gameNodes = new List<ILayoutNode>();
            //Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();
            ////Wraps all game object nodes with the GameNode wrapper class
            //gameNodes = ToLayoutNodes(nodeToGO.Values, to_layout_node);
            ////Converts the list of created game object nodes to ILayoutNodes
            //ICollection<ILayoutNode> layoutNodes = gameNodes.Cast<ILayoutNode>().ToList();
            ////Applies the loaded layout to the nodes.
            //layout.Apply(layoutNodes);
            //GameObject whiteboard = AddWhiteboardIfNecessary(parent);
            ////Scales the nodes to fit within the parent transform
            //NodeLayout.ScaleArchitecture(layoutNodes, parent.transform.lossyScale.x, parent.transform.lossyScale.z);
            ////Moves the nodes to the center of the parent.
            //NodeLayout.MoveTo(layoutNodes, whiteboard.transform.position);
            //Dictionary<Node, GameObject>.ValueCollection nodeToGameObject = nodeToGO.Values;
            ////Creates a new portal plane to position the nodes and edges on

            ////Add the plane as a child to the parent
            //RendererUtils.AddToParent(whiteboard, parent);
            ////Currently all nodes are layed out flat, therefore they need to be stacked on top of each other
            //NodeLayout.Stack(layoutNodes, whiteboard.transform.position.y + whiteboard.transform.lossyScale.y / 2.0f + LevelDistance);
            ////Creates the game object object hierarchy
            //RendererUtils.CreateObjectHierarchy(nodeToGO, whiteboard);
            ////GameObject rootNode = RendererUtils.RootGameNode(parent);
            //RendererUtils.AddToParent(ApplyEdgeLayout(gameNodes), whiteboard);
            //RefreshNodeStyle(parent, nodeToGameObject);
            ////Sets the portal size to the extents of this parent.
            //Portal.SetPortal(parent);
        }


        /// <summary>
        /// Creates a new whiteboard to place the graph elements on. If there is already a child of <see cref="parent"/>
        /// tagged with <see cref="Tags.Whiteboard"/> that game object is returned. Otherwise a new one is generated.
        /// </summary>
        /// <param name="city">The architecture city game object.</param>
        /// <returns>The whiteboard.</returns>
        private GameObject AddWhiteboardIfNecessary(GameObject city)
        {
            throw new NotImplementedException();
            //foreach (Transform child in city.transform)
            //{
            //    if (child.CompareTag(Tags.Whiteboard))
            //    {
            //        return child.gameObject;
            //    }
            //}
            //float yLevel = GetParentYLevel(city);
            //Vector3 center = city.transform.position;
            //// Calculate the whiteboard plane
            //Vector2 lFront = new Vector2(center.x - 100f, center.z - 100f);
            //Vector2 rBack = new Vector2(center.x + 100f, center.z + 100f);
            //GameObject whiteboard = PlaneFactory.NewPlane(ShaderType, lFront, rBack, yLevel, Color.white, LevelDistance);
            //whiteboard.tag = Tags.Whiteboard;
            //whiteboard.name = "Whiteboard";
            //whiteboard.AddComponent<PenInteraction>().controller = PenInteractionController;
            //return whiteboard;
        }

        /// <summary>
        /// Computes the y level of the parent object.
        /// </summary>
        /// <param name="parent">The parent object.</param>
        /// <param name="yLevel"></param>
        private float GetParentYLevel(GameObject parent)
        {
            Vector3 worldScale = parent.transform.lossyScale;
            Vector3 position = parent.transform.position;
            return position.y + worldScale.y / 2.0f + LevelDistance;
        }


        /// <summary>
        /// Prepares a new empty architecture graph that only contains the whiteboard plane.
        /// </summary>
        /// <param name="parent">The parent object to attach the whiteboard to.</param>
        public void PrepareNewArchitectureGraph(GameObject parent)
        {
            GameObject whiteboard = AddWhiteboardIfNecessary(parent);
            RendererUtils.AddToParent(whiteboard, parent);
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
        private ICollection<LayoutGraphNode> ToLayoutNodes(ICollection<GameObject> gameObjects, Dictionary<Node, ILayoutNode> to_layout_node)
        {
            throw new NotImplementedException();

            //IList<LayoutGraphNode> result = new List<LayoutGraphNode>(gameObjects.Count);
            //foreach (GameObject gameObject in gameObjects)
            //{
            //    Node node = gameObject.GetNode();
            //    NodeFactory factory = GetFactoryByType(node.Type);
            //    result.Add(new LayoutGraphNode(to_layout_node, gameObject, factory));
            //}

            //return result;
        }


        /// <summary>
        /// Retrieves the <see cref="NodeFactory"/> by the given string node type.
        /// If <paramref name="nodeType"/> has no registered factory, the Cluster factory should be used as default.
        /// </summary>
        /// <param name="nodeType">The node type to find the mapping.</param>
        /// <returns>The <see cref="NodeFactory"/> for the <paramref name="nodeType"/></returns>
        private NodeFactory GetFactoryByType(string nodeType)
        {
            if (typeToElementType.TryGetValue(nodeType, out ArchitectureElementType type))
            {
                return nodeFactories[(int) type];
            }
            return nodeFactories[(int) ArchitectureElementType.Cluster];
        }


        /// <summary>
        /// Loads the <see cref="LoadedNodeLayout">.
        /// </summary>
        /// <returns>The <see cref="LoadedNodeLayout"/> instance.</returns>
        /// <exception cref="Exception">Thrown when the path is invalid or empty.</exception>
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
            ArchitectureElementSettings elementSettings = GetSettingsByType(node.Type);

            GameObject go = factory.NewBlock(SelectStyle(node));
            go.name = node.ID;
            go.tag = Tags.Node;
            go.AddComponent<NodeRef>().Value = node;
            Vector3 scaling = go.transform.localScale;
            go.transform.localScale =
                new Vector3(scaling.x, elementSettings.ElementHeight, scaling.z);
            ArchitectureDecorator.DecorateForInteraction(go, PenInteractionController);
            return go;
        }

        /// <summary>
        /// Find the <see cref="ArchitectureElementSettings"/> for a given type of nodes.
        /// If none was found, the cluster settings are used as default.
        /// </summary>
        /// <param name="nodeType">The node type as string</param>
        /// <returns>The <see cref="ArchitectureElementSettings"/> for this <paramref name="nodeType"/>
        /// or the cluster settings as default</returns>
        private ArchitectureElementSettings GetSettingsByType(string nodeType)
        {
            if (typeToElementType.TryGetValue(nodeType,out  ArchitectureElementType type))
            {
                return settings.ArchitectureElementSettings[(int) type];
            }

            return settings.ArchitectureElementSettings[(int) ArchitectureElementType.Cluster];
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