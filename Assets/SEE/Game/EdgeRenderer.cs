using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.EdgeLayouts;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    /// <summary>
    /// Implements the functions of the <see cref="GraphRenderer"/> related to edges.
    /// </summary>
    public partial class GraphRenderer
    {
        public GameObject DrawEdge(Edge edge, GameObject from = null, GameObject to = null)
        {
            from ??= edge.Source.RetrieveGameNode();
            to ??= edge.Target.RetrieveGameNode();

            // Save edge layout so that we can restore it if we need to select a default layout.
            EdgeLayoutKind savedEdgeLayout = Settings.EdgeLayoutSettings.Kind;
            if (savedEdgeLayout == EdgeLayoutKind.None)
            {
                Debug.LogWarning($"An edge {edge.ID} from {edge.Source.ID} to {edge.Target.ID} was added to the graph, but no edge layout was chosen.\n");
                // Select default layout
                Settings.EdgeLayoutSettings.Kind = EdgeLayoutKind.Spline;
            }

            // Creating the game object representing the edge.
            // The edge layout will be calculated for the following gameNodes. This list will
            // contain the source and target of the edge but also all their ascendants. The
            // ascendants are needed for hierarchical layouts.
            HashSet<GameObject> gameNodes = new HashSet<GameObject>();
            // We add the ascendants of the source and target nodes in case the edge layout is hierarchical.
            AddAscendants(from, gameNodes);
            AddAscendants(to, gameNodes);
            Dictionary<Node, ILayoutNode> nodeToLayoutNode = new Dictionary<Node, ILayoutNode>();
            // The layout nodes corresponding to those game nodes.
            ICollection<LayoutGameNode> layoutNodes = ToLayoutNodes(gameNodes, nodeToLayoutNode);

            LayoutGameNode fromLayoutNode = null; // layout node in layoutNodes corresponding to source node
            LayoutGameNode toLayoutNode = null; // layout node in layoutNodes corresponding to target node
            // We need fromLayoutNode and toLayoutNode to create a single layout edge to be passed
            // to the edge layouter.
            foreach (LayoutGameNode layoutNode in layoutNodes)
            {
                if (layoutNode.ItsNode == edge.Source)
                {
                    fromLayoutNode = layoutNode;
                }

                // note: fromNode = toNode is possible, hence, there is no 'else' here.
                if (layoutNode.ItsNode == edge.Target)
                {
                    toLayoutNode = layoutNode;
                }
            }

            Assert.IsNotNull(fromLayoutNode, $"Source node {edge.Source.ID} does not have a layout node.\n");
            Assert.IsNotNull(toLayoutNode, $"Target node {edge.Target.ID} does not have a layout node.\n");
            // The single layout edge between source and target. We want the layout only for this edge.
            ICollection<LayoutGraphEdge<LayoutGameNode>> layoutEdges = new List<LayoutGraphEdge<LayoutGameNode>>
                { new LayoutGraphEdge<LayoutGameNode>(fromLayoutNode, toLayoutNode, edge) };
            // Calculate the edge layout (for the single edge only).
            ICollection<GameObject> edges = EdgeLayout(layoutNodes, layoutEdges, true);
            GameObject resultingEdge = edges.First();
            InteractionDecorator.PrepareForInteraction(resultingEdge);
            // The edge becomes a child of the root node of the game-node hierarchy
            GameObject codeCity = SceneQueries.GetCodeCity(from.transform).gameObject;
            GameObject rootNode = SceneQueries.GetCityRootNode(codeCity).gameObject;
            resultingEdge.transform.SetParent(rootNode.transform);
            // The portal of the new edge is inherited from the codeCity.
            Portal.SetPortal(root: codeCity, gameObject: resultingEdge);
            // Reset original edge layout.
            Settings.EdgeLayoutSettings.Kind = savedEdgeLayout;
            return resultingEdge;
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
        /// <param name="edgeType">the type of the edge to be created</param>
        /// <param name="existingEdge">If non-null, we'll use this as the edge in the underlying graph
        /// instead of creating a new one</param>
        /// <exception cref="Exception">thrown if <paramref name="from"/> or <paramref name="to"/>
        /// are not contained in any graph or contained in different graphs</exception>
        public GameObject DrawEdge(GameObject from, GameObject to, string edgeType, Edge existingEdge = null)
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

            Edge edge = existingEdge;
            if (edge == null)
            {
                // Creating the edge in the underlying graph
                edge = new Edge(fromNode, toNode, edgeType);
                fromNode.ItsGraph.AddEdge(edge);
            }

            return DrawEdge(edge, from, to);
        }

        /// <summary>
        /// Adds <paramref name="node"/> and all its transitive parent game objects tagged by
        /// Tags.Node to <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="node">the game objects whose ascendant game nodes are to be added to <paramref name="gameNodes"/></param>
        /// <param name="gameNodes">where to add the ascendants</param>
        private static void AddAscendants(GameObject node, ISet<GameObject> gameNodes)
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
        /// <param name="addToGraphElementIDMap">if true, all newly created edges will be
        /// added to <see cref="GraphElementIDMap"/></param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        public ICollection<GameObject> EdgeLayout(ICollection<GameObject> gameNodes,
                                                  GameObject parent,
                                                  bool addToGraphElementIDMap)
        {
            return EdgeLayout(ToLayoutNodes(gameNodes), parent, addToGraphElementIDMap);
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings) for
        /// all edges in between nodes in <paramref name="gameNodes"/>. The resulting
        /// edges are added to <paramref name="parent"/> as children.
        /// </summary>
        /// <param name="gameNodes">the subset of nodes for which to draw the edges</param>
        /// <param name="parent">the object the new edges are to become children of</param>
        /// <param name="addToGraphElementIDMap">if true, all newly created edges will be
        /// added to <see cref="GraphElementIDMap"/></param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private ICollection<GameObject> EdgeLayout(ICollection<LayoutGameNode> gameNodes,
                                        GameObject parent,
                                        bool addToGraphElementIDMap)
        {
            ICollection<GameObject> result = EdgeLayout(gameNodes, ConnectingEdges(gameNodes), addToGraphElementIDMap);
            AddToParent(result, parent);
            return result;
        }

        /// <summary>
        /// Returns the connecting edges among <paramref name="layoutNodes"/> laid out by the
        /// selected edge layout.
        /// If <paramref name="layoutNodes"/> is null or empty or if no layout was selected
        /// by the user, the empty collection is returned.
        /// </summary>
        /// <param name="layoutNodes">nodes whose connecting edges are to be laid out</param>
        /// <returns>laid out edges</returns>
        public ICollection<LayoutGraphEdge<T>> LayoutEdges<T>(ICollection<T> layoutNodes)
            where T : AbstractLayoutNode, IHierarchyNode<ILayoutNode>
        {
            if (layoutNodes == null || layoutNodes.Count == 0)
            {
                // no nodes, no edges, no layout
                return new List<LayoutGraphEdge<T>>();
            }

            IEdgeLayout layout = GetEdgeLayout();
            if (layout == null)
            {
                // No layout selected, no edges will be created.
                return new List<LayoutGraphEdge<T>>();
            }

            ICollection<LayoutGraphEdge<T>> edges = ConnectingEdges(layoutNodes);
            layout.Create(layoutNodes, edges);
            return edges;
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings).
        /// </summary>
        /// <param name="gameNodes">the set of layout nodes for which to create game edges</param>
        /// <param name="layoutEdges">the edges to be laid out</param>
        /// <param name="addToGraphElementIDMap">if true, all newly created edges will be added
        /// to <see cref="GraphElementIDMap"/></param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private ICollection<GameObject> EdgeLayout<T>(ICollection<T> gameNodes,
                                                      ICollection<LayoutGraphEdge<T>> layoutEdges,
                                                      bool addToGraphElementIDMap)
            where T : LayoutGameNode, IHierarchyNode<ILayoutNode>
        {
            IEdgeLayout layout = GetEdgeLayout();
            if (layout == null)
            {
                // No layout selected, no edges will be created.
                return new List<GameObject>();
            }
#if UNITY_EDITOR
            Performance p = Performance.Begin("edge layout " + layout.Name);
#endif
            EdgeFactory edgeFactory = new EdgeFactory(layout, Settings.EdgeLayoutSettings.EdgeWidth);
            // The resulting game objects representing the edges.
            ICollection<GameObject> result;
            // Calculate and draw edges
            result = edgeFactory.DrawEdges(gameNodes, layoutEdges);
            if (addToGraphElementIDMap)
            {
                GraphElementIDMap.Add(result);
            }
            InteractionDecorator.PrepareForInteraction(result);
            AddLOD(result);

#if UNITY_EDITOR
            p.End();
            Debug.Log($"Calculated \"  {Settings.EdgeLayoutSettings.Kind} \" edge layout for {gameNodes.Count}"
                      + $" nodes and {result.Count} edges in {p.GetElapsedTime()} [h:m:s:ms].\n");
#endif
            return result;
        }

        /// <summary>
        /// Yields the edge layout as specified in the <see cref="Settings"/>.
        /// </summary>
        /// <returns>specified edge layout</returns>
        private IEdgeLayout GetEdgeLayout()
        {
            float minimalEdgeLevelDistance = 2.5f * Settings.EdgeLayoutSettings.EdgeWidth;
            bool edgesAboveBlocks = Settings.EdgeLayoutSettings.EdgesAboveBlocks;
            float rdp = Settings.EdgeLayoutSettings.RDP;
            switch (Settings.EdgeLayoutSettings.Kind)
            {
                case EdgeLayoutKind.Straight:
                    return new StraightEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance);
                case EdgeLayoutKind.Spline:
                    return new SplineEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance, rdp);
                case EdgeLayoutKind.Bundling:
                    return new BundledEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance,
                                                 Settings.EdgeLayoutSettings.Tension, rdp);
                case EdgeLayoutKind.None:
                    // nothing to be done
                    return null;
                default:
                    throw new Exception("Unhandled edge layout " + Settings.EdgeLayoutSettings.Kind);
            }
        }

        /// <summary>
        /// Returns the list of layout edges for all edges in between <paramref name="gameNodes"/>.
        /// Note that this will skip "virtual" edges, i.e., edges which exist in the underlying graph
        /// but which shall not be layouted or drawn.
        /// </summary>
        /// <param name="gameNodes">set of game nodes whose connecting edges are requested</param>
        /// <returns>list of layout edges</returns>
        private static ICollection<LayoutGraphEdge<T>> ConnectingEdges<T>(ICollection<T> gameNodes)
            where T : AbstractLayoutNode
        {
            ICollection<LayoutGraphEdge<T>> edges = new List<LayoutGraphEdge<T>>();
            Dictionary<Node, T> map = NodeToGameNodeMap(gameNodes);

            foreach (T source in gameNodes)
            {
                Node sourceNode = source.ItsNode;

                foreach (Edge edge in sourceNode.Outgoings.Where(x => !x.HasToggle(Edge.IsVirtualToggle)))
                {
                    Node target = edge.Target;
                    edges.Add(new LayoutGraphEdge<T>(source, map[target], edge));
                }
            }

            return edges;
        }
    }
}