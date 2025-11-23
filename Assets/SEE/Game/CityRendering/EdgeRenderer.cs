using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using MoreLinq.Extensions;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.EdgeLayouts;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Implements the functions of the <see cref="GraphRenderer"/> related to edges.
    /// </summary>
    public partial class GraphRenderer
    {
        /// <summary>
        /// Returns an edge layout for the given <paramref name="gameEdges"/>.
        ///
        /// The result is a mapping of the names of the game objects in <paramref name="gameEdges"/>
        /// onto the layout for those edges.
        ///
        /// Precondition: The game objects in <paramref name="gameEdges"/> represent graph edges.
        /// </summary>
        /// <param name="gameEdges">the edges for which to create a layout</param>
        /// <returns>mapping of the names of the game objects in <paramref name="gameEdges"/> onto
        /// their layout information</returns>
        /// <remarks>Implements <see cref="IGraphRenderer.LayoutEdges(ICollection{GameObject})"/></remarks>
        public IDictionary<string, ILayoutEdge<ILayoutNode>> LayoutEdges(ICollection<GameObject> gameEdges)
        {
            // Save edge layout so that we can restore it if we need to select a default layout.
            EdgeLayoutKind savedEdgeLayout = Settings.EdgeLayoutSettings.Kind;
            if (savedEdgeLayout == EdgeLayoutKind.None)
            {
                Debug.LogWarning($"Edges should be laid out, but no edge layout was chosen. Using default {IGraphRenderer.EdgeLayoutDefault}.\n");
                // Select default layout
                Settings.EdgeLayoutSettings.Kind = IGraphRenderer.EdgeLayoutDefault;
            }

            // All source and target game nodes of the edges.
            ISet<GameObject> gameNodes = new HashSet<GameObject>();

            // Gather all sources and targets of all gameEdges.
            foreach (GameObject gameEdge in gameEdges)
            {
                if (gameEdge.TryGetEdge(out Edge edge))
                {
                    gameNodes.Add(edge.Source.GameObject(mustFind: true));
                    gameNodes.Add(edge.Target.GameObject(mustFind: true));
                }
            }

            // In case of a hierarchical edge layout, we need to add the ascendants of all gameNodes.
            if (Settings.EdgeLayoutSettings.Kind == EdgeLayoutKind.Bundling)
            {
                ISet<GameObject> ascendants = new HashSet<GameObject>();
                foreach (GameObject gameNode in gameNodes)
                {
                    AddAscendants(gameNode, ascendants);
                }
                gameNodes.UnionWith(ascendants);
            }

            // One layout game node for each game node.
            IDictionary<Node, LayoutGameNode> layoutNodes = ToLayoutNodes(gameNodes, NewLayoutNode);

            // Now we have all nodes (game nodes and layout nodes). Next we gather the layout edges.

            // The layout edges for all gameEdges.
            ICollection<LayoutGraphEdge<ILayoutNode>> layoutEdges = new List<LayoutGraphEdge<ILayoutNode>>(gameEdges.Count);
            foreach (GameObject gameEdge in gameEdges)
            {
                if (gameEdge.TryGetEdge(out Edge edge))
                {
                    layoutEdges.Add(new LayoutGraphEdge<ILayoutNode>(layoutNodes[edge.Source], layoutNodes[edge.Target], edge));
                }
            }

            // All (graph/layout) nodes and edges are known.
            // We can now calculate the edge layout.
            GetEdgeLayout().Create(layoutNodes.Values, layoutEdges);

            // Finally, we can prepare the result.
            IDictionary<string, ILayoutEdge<ILayoutNode>> result = new Dictionary<string, ILayoutEdge<ILayoutNode>>(layoutEdges.Count);
            foreach (LayoutGraphEdge<ILayoutNode> layoutEdge in layoutEdges)
            {
                result[layoutEdge.ItsEdge.ID] = layoutEdge;
            }

            // Reset original edge layout.
            Settings.EdgeLayoutSettings.Kind = savedEdgeLayout;

            return result;
        }

        /// <summary>
        /// Creates and returns a game object representing the given graph <paramref name="edge"/>
        /// from <paramref name="from"/> to <paramref name="to"/>. If <paramref name="addToGraphElementIDMap"/>
        /// is true, the returned edge will be added to <see cref="GraphElementIDMap"/>.
        /// </summary>
        /// <param name="edge">graph edge to be presented by the resulting game object</param>
        /// <param name="from">the game node representing the source of <paramref name="edge"/></param>
        /// <param name="to">the game node representing the target of <paramref name="edge"/></param>
        /// <param name="addToGraphElementIDMap">whether the returned edge should be added to <see cref="GraphElementIDMap"/></param>
        /// <returns>The new game object representing the new edge from <paramref name="source"/> to <paramref name="target"/>.</returns>
        private GameObject DrawEdge(Edge edge, GameObject from, GameObject to, bool addToGraphElementIDMap)
        {
            Assert.IsNotNull(from);
            Assert.IsNotNull(to);
            // Save edge layout so that we can restore it if we need to select a default layout.
            EdgeLayoutKind savedEdgeLayout = Settings.EdgeLayoutSettings.Kind;
            if (savedEdgeLayout == EdgeLayoutKind.None)
            {
                Debug.LogWarning($"An edge {edge.ID} from {edge.Source.ID} to {edge.Target.ID} was added to the graph, but no edge layout was chosen.\n");
                // Select default layout
                Settings.EdgeLayoutSettings.Kind = IGraphRenderer.EdgeLayoutDefault;
            }

            // Creating the game object representing the edge.
            // The edge layout will be calculated for the following gameNodes. This list will
            // contain the source and target of the edge but also all their ascendants. The
            // ascendants are needed for hierarchical layouts.
            HashSet<GameObject> gameNodes = new();
            // We add the ascendants of the source and target nodes in case the edge layout is hierarchical.
            AddAscendants(from, gameNodes);
            AddAscendants(to, gameNodes);
            // The layout nodes corresponding to those game nodes.
            IDictionary<Node, LayoutGameNode> layoutNodes = ToLayoutNodes(gameNodes, NewLayoutNode);

            LayoutGameNode fromLayoutNode = null; // layout node in layoutNodes corresponding to source node
            LayoutGameNode toLayoutNode = null; // layout node in layoutNodes corresponding to target node
            // We need fromLayoutNode and toLayoutNode to create a single layout edge to be passed
            // to the edge layouter.
            foreach (LayoutGameNode layoutNode in layoutNodes.Values)
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
                { new(fromLayoutNode, toLayoutNode, edge) };

            // Calculate the edge layout (for the single edge only).
            GameObject resultingEdge = EdgeLayout(layoutNodes.Values, layoutEdges, addToGraphElementIDMap).Single();

            // The edge becomes a child of the root node of the game-node hierarchy
            GameObject codeCity = SceneQueries.GetCodeCity(from.transform).gameObject;
            GameObject rootNode = SceneQueries.GetCityRootNode(codeCity);
            resultingEdge.transform.SetParent(rootNode.transform);
            // The portal of the new edge is inherited from the codeCity.
            Portal.SetPortal(root: codeCity, gameObject: resultingEdge);
            // Reset original edge layout.
            Settings.EdgeLayoutSettings.Kind = savedEdgeLayout;
            return resultingEdge;
        }

        /// <summary>
        /// Creates and returns a new game edge between <paramref name="source"/> and <paramref name="target"/>
        /// based on the current settings. A new graph edge will be added to the underlying graph, too.
        ///
        /// Note: The default edge layout <see cref="IGraphRenderer.EdgeLayoutDefault"/> will be used if no edge layout,
        /// i.e., <see cref="EdgeLayoutKind.None>"/>, was chosen in the settings.
        ///
        /// Precondition: <paramref name="source"/> and <paramref name="target"/> must have a valid
        /// node reference. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="source">source of the new edge</param>
        /// <param name="target">target of the new edge</param>
        /// <param name="edgeType">the type of the edge to be created</param>
        /// <returns>The new game object representing the new edge from <paramref name="source"/> to <paramref name="target"/>.</returns>
        /// <exception cref="System.Exception">thrown if <paramref name="source"/> or <paramref name="target"/>
        /// are not contained in any graph or contained in different graphs</exception>
        public GameObject DrawEdge(GameObject source, GameObject target, string edgeType)
        {
            Node fromNode = source.GetNode();
            if (fromNode == null)
            {
                throw new Exception($"The source {source.name} of the edge is not contained in any graph.");
            }

            Node toNode = target.GetNode();
            if (toNode == null)
            {
                throw new Exception($"The target {target.name} of the edge is not contained in any graph.");
            }

            if (fromNode.ItsGraph != toNode.ItsGraph)
            {
                throw new Exception($"The source {source.name} and target {target.name} of the edge are in different graphs.");
            }

            // Creating the edge in the underlying graph
            Edge edge = new(fromNode, toNode, edgeType);
            fromNode.ItsGraph.AddEdge(edge);

            return DrawEdge(edge, source, target, true);
        }

        /// <summary>
        /// Draws and returns a new game edge <paramref name="edge"/>
        /// based on the current settings.
        ///
        /// Note: The default edge layout <see cref="IGraphRenderer.EdgeLayoutDefault"/> will be used if no edge layout,
        /// i.e., <see cref="EdgeLayoutKind.None>"/>, was chosen in the settings.
        ///
        /// Precondition: <paramref name="source"/> and <paramref name="target"/> must have a valid
        /// node reference. The corresponding graph nodes must be in the same graph.
        /// </summary>
        /// <param name="edge">the edge to be drawn</param>
        /// <param name="sourceNode">GameObject of source of the new edge</param>
        /// <param name="targetNode">GameObject of target of the new edge</param>
        /// <returns>The new game object representing the given edge.</returns>
        public GameObject DrawEdge(Edge edge, GameObject sourceNode = null, GameObject targetNode = null)
        {
            if (sourceNode == null)
            {
                sourceNode = GraphElementIDMap.Find(edge.Source.ID);
            }

            if (targetNode == null)
            {
                targetNode = GraphElementIDMap.Find(edge.Target.ID);
            }

            return DrawEdge(edge, sourceNode, targetNode, true);
        }

        /// <summary>
        /// Adds <paramref name="node"/> and all its transitive parent game objects tagged by
        /// <see cref="Tags.Node"/> to <paramref name="gameNodes"/>.
        /// </summary>
        /// <param name="node">the game objects whose ascendant game nodes are to be added to <paramref name="gameNodes"/></param>
        /// <param name="gameNodes">where to add the ascendants</param>
        private static void AddAscendants(GameObject node, ISet<GameObject> gameNodes)
        {
            GameObject cursor = node;
            while (cursor != null && cursor.CompareTag(Tags.Node))
            {
                gameNodes.Add(cursor);
                cursor = cursor.transform.parent != null ? cursor.transform.parent.gameObject : null;
            }
        }

        /// <summary>
        /// Applies the edge layout according to the user's choice (settings) for
        /// all edges in between nodes in <paramref name="gameNodes"/>. The resulting
        /// edges are added to <paramref name="parent"/> as children.
        ///
        /// This method should be chosen if a synchronous context is required. Otherwise,
        /// prefer <see cref="EdgeLayoutAsync"/> for performance reasons.
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
            IDictionary<Node, LayoutGameNode> layoutNodes = ToLayoutNodes(gameNodes, NewLayoutNode);
            ICollection<GameObject> result = EdgeLayout(layoutNodes.Values, ConnectingEdges(layoutNodes.Values), addToGraphElementIDMap);
            AddToParent(result, parent);
            return result;
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
        /// <param name="updateProgress">callback to update the progress of the operation</param>
        /// <param name="token">token to cancel the operation</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private async UniTask<ICollection<GameObject>> EdgeLayoutAsync(ICollection<LayoutGameNode> gameNodes,
                                                                       GameObject parent,
                                                                       bool addToGraphElementIDMap,
                                                                       Action<float> updateProgress = null,
                                                                       CancellationToken token = default)
        {
            ICollection<GameObject> result = await EdgeLayoutAsync(gameNodes, ConnectingEdges(gameNodes),
                                                                   addToGraphElementIDMap, updateProgress, token);
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
            where T : AbstractLayoutNode
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
        /// <param name="updateProgress">callback to update the progress of the operation</param>
        /// <param name="token">token to cancel the operation</param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private async UniTask<ICollection<GameObject>> EdgeLayoutAsync<T>(ICollection<T> gameNodes,
                                                                          ICollection<LayoutGraphEdge<T>> layoutEdges,
                                                                          bool addToGraphElementIDMap,
                                                                          Action<float> updateProgress = null,
                                                                          CancellationToken token = default)
            where T : LayoutGameNode
        {
            IEdgeLayout layout = GetEdgeLayout();
            if (layout == null)
            {
                // No layout selected, no edges will be created.
                return new List<GameObject>();
            }

            EdgeFactory edgeFactory = new(layout, Settings.EdgeLayoutSettings.EdgeWidth);
            // The resulting game objects representing the edges.
            int totalEdges = layoutEdges.Count;
            float i = 0;
            ICollection<GameObject> result = await edgeFactory.DrawEdges(gameNodes, layoutEdges)
                                                              .Pipe(_ => updateProgress?.Invoke(0.5f * ++i / totalEdges))
                                                              .BatchPerFrame(cancellationToken: token)
                                                              .ToListAsync(cancellationToken: token);
            if (addToGraphElementIDMap)
            {
                GraphElementIDMap.Add(result);
            }

            await InteractionDecorator.PrepareForInteractionAsync(result, x => updateProgress?.Invoke(0.5f + x*0.5f), token);
            AddLOD(result);

            return result;
        }

        /// <summary>
        /// Applies the edge layout according to the the user's choice (settings) synchronously.
        ///
        /// This method should be chosen if a synchronous context is required. Otherwise,
        /// prefer <see cref="EdgeLayoutAsync"/> for performance reasons.
        /// </summary>
        /// <param name="gameNodes">the set of layout nodes for which to create game edges</param>
        /// <param name="layoutEdges">the edges to be laid out</param>
        /// <param name="addToGraphElementIDMap">if true, all newly created edges will be added
        /// to <see cref="GraphElementIDMap"/></param>
        /// <returns>all game objects created to represent the edges; may be empty</returns>
        private ICollection<GameObject> EdgeLayout<T>(ICollection<T> gameNodes,
                                                      ICollection<LayoutGraphEdge<T>> layoutEdges,
                                                      bool addToGraphElementIDMap)
            where T : LayoutGameNode
        {
            IEdgeLayout layout = GetEdgeLayout();
            EdgeFactory edgeFactory = new(layout, Settings.EdgeLayoutSettings.EdgeWidth);
            // The resulting game objects representing the edges.
            IList<GameObject> resultingEdges = edgeFactory.DrawEdges(gameNodes, layoutEdges).ToList();
            if (addToGraphElementIDMap)
            {
                GraphElementIDMap.Add(resultingEdges);
            }

            resultingEdges.ForEach(InteractionDecorator.PrepareGraphElementForInteraction);
            AddLOD(resultingEdges);
            return resultingEdges;
        }

        /// <summary>
        /// Yields the edge layout as specified in the <see cref="Settings"/>.
        /// </summary>
        /// <returns>specified edge layout</returns>
        private IEdgeLayout GetEdgeLayout()
        {
            float minimalEdgeLevelDistance = 2.5f * Settings.EdgeLayoutSettings.EdgeWidth;
            bool edgesAboveBlocks = Settings.EdgeLayoutSettings.EdgesAboveBlocks;
            switch (Settings.EdgeLayoutSettings.Kind)
            {
                case EdgeLayoutKind.Straight:
                    return new StraightEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance);
                case EdgeLayoutKind.Spline:
                    return new SplineEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance);
                case EdgeLayoutKind.Bundling:
                    return new BundledEdgeLayout(edgesAboveBlocks, minimalEdgeLevelDistance,
                                                 Settings.EdgeLayoutSettings.Tension);
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
