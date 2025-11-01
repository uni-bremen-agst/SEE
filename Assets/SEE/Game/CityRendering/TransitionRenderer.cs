using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GameObjects;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.UI.Notification;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Renders a transition between an old and a new graph.
    /// </summary>
    public class TransitionRenderer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="markerAttributes">attributes for rendering the node markers</param>
        public TransitionRenderer(MarkerAttributes markerAttributes)
        {
            markerFactory = new MarkerFactory(markerAttributes);
        }

        #region Node Marking
        /// <summary>
        /// <see cref="MarkerFactory"> for generating node markers.
        /// </summary>
        private readonly MarkerFactory markerFactory;

        /// <summary>
        /// Marks all <paramref name="addedNodes"/> as born and all <paramref name="changedNodes"/>
        /// as changed using <see cref="markerFactory"/>.
        /// </summary>
        private void MarkNodes(ISet<Node> addedNodes, ISet<Node> changedNodes)
        {
            foreach (Node node in addedNodes)
            {
                markerFactory.MarkBorn(GraphElementIDMap.Find(node.ID, true));
            }
            foreach (Node node in changedNodes)
            {
                markerFactory.MarkChanged(GraphElementIDMap.Find(node.ID, true));
            }
        }

        #endregion Node Marking

        #region Edge Marking

        /// <summary>
        /// A queue of <see cref="EdgeOperator"/>s associated with edges which are currently highlighted, that is,
        /// edges which have changed compared to the previous rendering.
        /// </summary>
        private readonly Queue<EdgeOperator> highlightedEdgeOperators = new();

        /// <summary>
        /// Fade out the highlights for each previously marked edge in <see cref="highlightedEdgeOperators"/>.
        /// </summary>
        private void DeleteEdgeMarking()
        {
            while (highlightedEdgeOperators.Count > 0)
            {
                // Fade out the highlights for each previously marked edge.
                EdgeOperator edgeOperator = highlightedEdgeOperators.Dequeue();
                if (edgeOperator != null)
                {
                    edgeOperator.GlowOut();
                }
            }
        }

        /// <summary>
        /// Marks all <paramref name="addedEdges"/> as born and all <paramref name="changedEdges"/>
        /// as changed using glow effects. Every marked edge is added to <see cref="highlightedEdgeOperators"/>.
        /// Marking is expressed by a glow effect.
        ///
        /// In case a game edge does not yet have a mesh (i.e., is currently rendered by
        /// a <see cref="LineRenderer"/>, we cannot let it glow immediately because the
        /// glow effect requires a mesh. In that case, we postpone the effect until the
        /// <see cref="LineRenderer"/> has been turned into a mesh by the <see cref="EdgeMeshScheduler"/>.
        /// </summary>
        /// <param name="addedEdges">added edges</param>
        /// <param name="changedEdges">changed edges</param>
        private void MarkEdges(ISet<Edge> addedEdges, ISet<Edge> changedEdges)
        {
            GlowIn(addedEdges);
            GlowIn(changedEdges);

            void GlowIn(ISet<Edge> edges)
            {
                if (edges == null)
                {
                    return;
                }

                foreach (Edge edge in edges)
                {
                    GameObject go = GraphElementIDMap.Find(edge.ID, false);

                    if (go != null)
                    {
                        if (go.TryGetComponent(out LineRenderer _))
                        {
                            // We do not have a mesh yet. Glow effect will currently not work.
                            if (go.TryGetComponentOrLog(out SEESpline spline))
                            {
                                // We need to wait until the mesh is created.
                                spline.OnMeshCreated += OnMeshCreated;
                            }
                        }
                        else
                        {
                            MarkEdge(go);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Edge {edge.ID} could not be found.\n");
                    }
                }
            }

            void OnMeshCreated(SEESpline spline)
            {
                spline.OnMeshCreated -= OnMeshCreated;
                MarkEdge(spline.gameObject);
            }
        }

        /// <summary>
        /// Marks the <paramref name="gameEdge"/> as changed or new by
        /// letting it glow. Adds its <see cref="EdgeOperator"/> to
        /// <see cref="highlightedEdgeOperators"/> so that the glow effect
        /// can be stopped when we move to the next graph.
        /// </summary>
        /// <param name="gameEdge">the game edge to be marked</param>
        private void MarkEdge(GameObject gameEdge)
        {
            EdgeOperator edgeOperator = gameEdge.EdgeOperator();
            edgeOperator.GlowIn();
            edgeOperator.HitEffect();
            highlightedEdgeOperators.Enqueue(edgeOperator);
        }

        #endregion Edge Marking

        /// <summary>
        /// The last calculated <see cref="NodeLayout"/> (needed for incremental layouts).
        /// </summary>
        private NodeLayout oldLayout = null;

        /// <summary>
        /// Renders the transition from <paramref name="oldGraph"/> to <paramref name="newGraph"/>.
        /// If <paramref name="edgesAreDrawn"/> is true, edges will be rendered as well.
        /// </summary>
        /// <param name="oldGraph">the previously rendered graph</param>
        /// <param name="newGraph">the new graph to be rendered anew</param>
        /// <param name="edgesAreDrawn">whether edges should be drawn</param>
        /// <param name="codeCity">the game object under which to place the nodes and edges
        /// for the new graph</param>
        /// <param name="renderer">the graph renderer to obtain the layouts from</param>
        /// <returns>task</returns>
        internal async UniTask RenderAsync
            (Graph oldGraph,
             Graph newGraph,
             bool edgesAreDrawn,
             GameObject codeCity,
             IGraphRenderer renderer)
        {
            // Remove markers from previous rendering.
            markerFactory.Clear();
            DeleteEdgeMarking();

            newGraph.Diff(oldGraph,
                g => g.Nodes(),
                (g, id) => g.GetNode(id),
                GraphExtensions.AttributeDiff(newGraph, oldGraph),
                nodeEqualityComparer,
                out ISet<Node> addedNodes,   // nodes belong to newGraph
                out ISet<Node> removedNodes, // nodes belong to oldGraph
                out ISet<Node> changedNodes, // nodes belong to newGraph
                out ISet<Node> equalNodes);  // nodes belong to newGraph

            // Before we can calculate the new layout, we must ensure that all game objects
            // representing nodes or edges that are still present in the newGraph are reattached.
            equalNodes.UnionWith(changedNodes);
            Reattach(equalNodes);

            ISet<Edge> addedEdges = null;   // edges belong to newGraph
            ISet<Edge> removedEdges = null; // edges belong to oldGraph
            ISet<Edge> changedEdges = null; // edges belong to newGraph
            ISet<Edge> equalEdges = null;   // edges belong to newGraph

            if (edgesAreDrawn)
            {
                newGraph.Diff(oldGraph,
                g => g.Edges(),
                (g, id) => g.GetEdge(id),
                GraphExtensions.AttributeDiff(newGraph, oldGraph),
                edgeEqualityComparer,
                out addedEdges,   // edges belong to newGraph
                out removedEdges, // edges belong to oldGraph
                out changedEdges, // edges belong to newGraph
                out equalEdges);  // edges belong to newGraph

                equalEdges.UnionWith(changedEdges);
                Reattach(equalEdges);
            }

            // Note: No matter what the value of edgesAreDrawn is, we do not
            // calculate the edge layout here. Existing edges will be moved
            // along with their nodes using the NodeOperators. Only the layout
            // of new edges needs to be calculated, but that will be done
            // by IGraphRenderer.DrawEdge called in AddNewEdges.
            NextLayout.Calculate(newGraph,
                                 GetGameNode,
                                 renderer,
                                 false, // We do not need to calculate the edge layout.
                                 codeCity,
                                 out Dictionary<string, ILayoutNode> newNodelayout,
                                 out Dictionary<string, ILayoutEdge<ILayoutNode>> _,
                                 ref oldLayout);
            // Note: At this point, game nodes for new nodes have been created already.
            // Their NodeRefs refer to nodes in the newly loaded graph.
            // The removedNodes, changedNodes, and equalNodes have NodeRefs still
            // referencing nodes in the oldGraph.
            // That is no problem for removedNodes as these will be removed anyway.
            // Yet, we need to update the game nodes with NodeRefs to all changedNodes
            // and equalNodes.
            // The same holds for game edges.

            if (edgesAreDrawn)
            {
                ShowRemovedEdges(removedEdges);
                await AnimateDeathAsync(removedEdges, AnimateEdgeDeath);
            }

            ShowRemovedNodes(removedNodes);
            await AnimateDeathAsync(removedNodes, AnimateNodeDeath);

            await AnimateNodeMoveByLevelAsync(equalNodes, newNodelayout);
            if (edgesAreDrawn)
            {
                ShowChangedEdges(changedEdges);
            }

            ShowChangedNodes(changedNodes);
            // Even the equal nodes need adjustments because the layout could have
            // changed their dimensions. The treemap layout, for instance, may do that.
            // Note that equalNodes is the union of the really equal nodes passed initially
            // as a parameter and the changedNodes.
            await AnimateNodeChangeAsync(equalNodes, changedNodes, newNodelayout, markerFactory, renderer);

            ShowAddedNodes(addedNodes);
            // The temporary parent object for the new nodes. A new node must have
            // a parent object with a Portal component; otherwise the NodeOperator
            // will not work. Later, we will set the correct parent of the new node.
            GameObject parent = codeCity;
            await AnimateNodeBirthAsync(addedNodes, newNodelayout, GetGameNode, parent);

            if (edgesAreDrawn)
            {
                ShowAddedEdges(addedEdges);
                AddNewEdges(addedEdges, renderer);
            }

            MarkNodes(addedNodes, changedNodes);
            MarkEdges(addedEdges, changedEdges);

            IOperationCallback<Action> AnimateNodeDeath(GameObject go)
            {
                markerFactory.MarkDead(go);
                return go.NodeOperator().MoveYTo(AbstractSEECity.SkyLevel, updateEdges: false);
            }

            IOperationCallback<Action> AnimateEdgeDeath(GameObject go)
            {
                return go.EdgeOperator().Blink(5);
            }

            /// <summary>
            /// If a game node with the ID of the given <paramref name="node"/> exists, it is returned.
            /// Otherwise, a new game node is created and returned with the given <paramref name="node"/>
            /// attached to it.
            /// </summary>
            /// <param name="node">node for which a game node is requested</param>
            /// <returns>existing or new game node</returns>
            GameObject GetGameNode(Node node)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, false);
                if (go != null)
                {
                    return go;
                }
                return renderer.DrawNode(node, codeCity);
            }
        }

        /// <summary>
        /// Animates the death of <paramref name="toBeRemoved"/> and destroys their game objects
        /// after the animation has finished.
        /// </summary>
        /// <param name="toBeRemoved">nodes to be removed</param>
        /// <param name="AnimateDeath">method to animate the death of a game object</param>
        /// <returns>task</returns>
        private async UniTask AnimateDeathAsync<T>
            (ISet<T> toBeRemoved, Func<GameObject,
             IOperationCallback<Action>> AnimateDeath)
            where T : GraphElement
        {
            HashSet<GameObject> deads = new();

            foreach (T element in toBeRemoved)
            {
                GameObject go = GraphElementIDMap.Find(element.ID, true);
                if (go != null)
                {
                    deads.Add(go);
                    IOperationCallback<Action> animation = AnimateDeath(go);
                    animation.OnComplete(() => OnComplete(go));
                    animation.OnKill(() => OnComplete(go));
                }
            }

            await UniTask.WaitUntil(() => deads.Count == 0);

            void OnComplete(GameObject go)
            {
                deads.Remove(go);
                Destroyer.Destroy(go);
            }
        }

        /// <summary>
        /// Creates new game objects for <paramref name="addedNodes"/> and renders them.
        /// </summary>
        /// <param name="addedNodes">nodes to be added</param>
        /// <param name="newNodelayout">the layout to be applied to the new nodes</param>
        /// <param name="getGameNode">method to get or create the game object for a given node</param>
        /// <param name="codeCity">the temporary parent object for the new nodes (should be the code city)</param>
        /// <returns>task</returns>
        private static async UniTask AnimateNodeBirthAsync
            (ISet<Node> addedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             Func<Node, GameObject> getGameNode,
             GameObject codeCity)
        {
            Assert.IsNotNull(codeCity);
            // The set of nodes whose birth is still being animated.
            HashSet<GameObject> births = new();

            // First create new game objects for the new nodes, mark them as born,
            // and add the NodeOperator component to them. The NodeOperator will
            // be enabled only at the end of the current frame. We cannot use it
            // earlier.
            foreach (Node node in addedNodes)
            {
                GameObject go = getGameNode(node);
                Assert.IsNotNull(go);
                // The NodeOperator requires that the node has a parent with a Portal component.
                // That is why we set the parent to the code city here. We cannot use
                // the actual parent (the game node corresponding to the parent of the
                // node in the graph) yet because the actual parent game object may not
                // exist yet (it may be new, too). The real parent will be set later in OnComplete.
                go.transform.SetParent(codeCity.transform);
                // We need the NodeOperator component to animate the birth of the node.
                go.AddOrGetComponent<NodeOperator>();
                births.Add(go);
            }

            // Let the frame be finished so that all game nodes are really added to the scene
            // and their NodeOperator component is enabled.
            // Note: UniTask.Yield() works only while the game is playing.
            await UniTask.Yield();

            // Game nodes for all new nodes now exist.

            // Now we can animate the birth of the new nodes.
            foreach (GameObject go in births)
            {
                // go.name and node.ID are the same.
                ILayoutNode layoutNode = newNodelayout[go.name];
                if (layoutNode != null)
                {
                    Add(go, layoutNode);
                }
                else
                {
                    Debug.LogError($"No layout for node {go.name}.\n");
                }
            }

            await UniTask.WaitUntil(() => births.Count == 0);

            void Add(GameObject gameNode, ILayoutNode layoutNode)
            {
                // A new node has no layout applied to it yet.
                // If the node is new, we animate it by moving it out from the sky.
                Vector3 initialPosition = layoutNode.CenterPosition;
                initialPosition.y = AbstractSEECity.SkyLevel + layoutNode.AbsoluteScale.y;
                gameNode.transform.position = initialPosition;
                gameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);

                // We know the NodeOperator component exists and is enabled because
                // we added it before and waited at least one frame.
                IOperationCallback<Action> animation = gameNode.NodeOperator()
                        .MoveTo(layoutNode.CenterPosition, updateEdges: false);
                animation.OnComplete(() => OnComplete(gameNode));
                animation.OnKill(() => OnComplete(gameNode));
            }

            void OnComplete(GameObject go)
            {
                // All nodes exist now. We can set the correct parent of the new node.
                Node node = go.GetNode();
                if (!node.IsRoot())
                {
                    GameObject parent = GraphElementIDMap.Find(node.Parent.ID, false);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
                    }
                }
                births.Remove(go);
            }
        }

        /// <summary>
        /// Creates and adds new game objects for <paramref name="addedEdges"/>.
        /// </summary>
        /// <param name="addedEdges">the new edges</param>
        /// <param name="renderer">the graph renderer to draw the new edges</param>
        /// <returns>task</returns>
        private void AddNewEdges
            (ISet<Edge> addedEdges,
            IGraphRenderer renderer)
        {

            foreach (Edge edge in addedEdges)
            {
                // The new edge will be created with the correct layout.
                GetNewEdge(edge);
            }

            /// <summary>
            /// Creates a new game edge for the given <paramref name="edge"/>.
            /// It is assumed that the source and target game objects exist already.
            /// The new edge will be created according to the current settings
            /// including the edge layout.
            /// </summary>
            /// <param name="edge">graph edge for which to create a game edge</param>
            /// <returns>new game edge</returns>
            GameObject GetNewEdge(Edge edge)
            {
                // Source and target game objects of the new edge will be looked up
                // in the GraphElementIDMap by DrawEdge.
                return renderer.DrawEdge(edge);
            }
        }

        /// <summary>
        /// Reattaches the given <paramref name="elements"/> to their corresponding game objects.
        /// The graph elements belong to the new graph but have corresponding game objects still representing
        /// the same graph elements (according to the ID) of the former graph.
        /// </summary>
        /// <param name="elements">graph elements of the new graph to be reattached</param>
        private void Reattach<T>(ISet<T> elements) where T : GraphElement
        {
            foreach (GraphElement element in elements)
            {
                GameObject go = GraphElementIDMap.Find(element.ID, true);
                if (go != null)
                {
                    GraphElementReattacher.Reattach(go, element);
                }
            }
        }

        /// <summary>
        /// Animates the movement of <paramref name="movedNodes"/> to their new positions
        /// according to <paramref name="newNodelayout"/>.
        /// </summary>
        /// <param name="movedNodes">game nodes to be moved</param>
        /// <param name="newNodelayout">new positions</param>
        /// <returns>task</returns>
        private static async UniTask AnimateNodeMoveByLevelAsync
                                (ISet<Node> movedNodes,
                                 Dictionary<string, ILayoutNode> newNodelayout)
        {
            if (movedNodes.Count == 0)
            {
                return;
            }

            // Partition all at the same hierarchy level.
            UnionFind<Node, int> unionFind = new(movedNodes, n => n.Level);
            unionFind.PartitionByValue();
            // For each partition sorted ascendingly by the hierarchy level:
            // move all the nodes in the same partition together.
            // Note that a partition is a list of Nodes. For the sorting, we
            // take the level of the first node of each list. Each list is
            // guaranteed to have at least one node. The partitions were defined
            // be the node levels.
            foreach (IList<Node> partition in unionFind.GetPartitions().ToList().OrderBy(l => l.First().Level))
            {
                await MoveAsync(partition, newNodelayout);
            }
        }

        /// <summary>
        /// Moves all <paramref name="movedNodes"/> to their target position looked up in
        /// <paramref name="newNodelayout"/>. Incoming and outgoing edges are moved along
        /// with the nodes.
        /// </summary>
        /// <param name="movedNodes">Nodes to be moved.</param>
        /// <param name="newNodelayout">Target position of the nodes to be moved.</param>
        /// <returns>task</returns>
        private static async UniTask MoveAsync(IList<Node> movedNodes, Dictionary<string, ILayoutNode> newNodelayout)
        {
            HashSet<GameObject> moved = new();

            foreach (Node node in movedNodes)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    ILayoutNode layoutNode = newNodelayout[node.ID];
                    if (layoutNode != null)
                    {
                        if (PositionHasChanged(go, layoutNode))
                        {
                            moved.Add(go);
                            // Move the node to its new position. The edge layout will not be
                            // be updated because we just set the node's parent to cityTransform.
                            // As a consequence, the node hierarchy is temporarily flat, which
                            // will distort hierarchical edge layouts, such as edge bundling.
                            IOperationCallback<Action> animation = go.NodeOperator()
                              .MoveTo(layoutNode.CenterPosition, updateEdges: true);
                            animation.OnComplete(() => OnComplete(go));
                            animation.OnKill(() => OnComplete(go));
                        }
                    }
                    else
                    {
                        Debug.LogError($"No layout for node {node.ID}.\n");
                    }
                }
            }

            await UniTask.WaitUntil(() => moved.Count == 0);

            void OnComplete(GameObject go)
            {
                moved.Remove(go);
            }
        }

        /// <summary>
        /// The distance between a current position and a new position of a game node
        /// at which we consider that the position has actually changed.
        /// </summary>
        private const float relevantMovementMargin = 0.001f;

        /// <summary>
        /// True if the position of the given <paramref name="gameNode"/> has actually changed
        /// by a relevant margin.
        /// </summary>
        /// <param name="gameNode">game node to be moved</param>
        /// <param name="layoutNode">the intended new position of <paramref name="gameNode"/></param>
        /// <returns>True if the position of the given <paramref name="gameNode"/> has actually changed
        /// by a relevant margin.</returns>
        private static bool PositionHasChanged(GameObject gameNode, ILayoutNode layoutNode)
        {
            Vector3 currentPosition = gameNode.transform.position;
            Vector3 newPosition = layoutNode.CenterPosition;
            return Vector3.Distance(currentPosition, newPosition) > relevantMovementMargin;
        }

        /// <summary>
        /// Animates the change of <paramref name="nodesToAdjust"/> to their new
        /// scale and style. The nodes in <paramref name="changedNodes"/> will
        /// be marked as changed using <paramref name="markerFactory"/>.
        ///
        /// </summary>
        /// <param name="nodesToAdjust">nodes whose dimensions, dimensions, and
        /// markers need to be adjusted; we assume <paramref name="changedNodes"/>
        /// is a subset of <paramref name="nodesToAdjust"/></param>
        /// <param name="changedNodes">nodes to be marked for a change</param>
        /// <param name="newNodelayout">new layout determining the new scale</param>
        /// <param name="markerFactory">factory for marking as changed</param>
        /// <param name="renderer">the graph renderer to adjust node styles and antennas</param>
        /// <returns>task</returns>
        private static async UniTask AnimateNodeChangeAsync
            (ISet<Node> nodesToAdjust,
             ISet<Node> changedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             MarkerFactory markerFactory,
             IGraphRenderer renderer)
        {
            HashSet<GameObject> changed = new();

            foreach (Node node in nodesToAdjust)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    ILayoutNode layoutNode = newNodelayout[node.ID];
                    if (layoutNode != null)
                    {
                        // Apply MarkChanged only to the really changed nodes.
                        // nodesToAdjust is the union of changedNodes and equalNodes.
                        if (changedNodes.Contains(node))
                        {
                            // There is a change. It may or may not be the metric determining the style.
                            // We will not further check that and just call the following method.
                            // If there is no change, this method does not need to be called because then
                            // we know that the metric values determining the styles of the former
                            // and the new graph node are the same. A style may include color, material,
                            // and other visual properties of the node itself, exluding size and decorations
                            // such as antenna and marker.
                            renderer.AdjustStyle(go);
                        }
                        changed.Add(go);
                        ScaleTo(go, layoutNode);
                    }
                    else
                    {
                        Debug.LogError($"No layout for node {node.ID}.\n");
                    }
                }
            }

            await UniTask.WaitUntil(() => changed.Count == 0);

            void ScaleTo(GameObject gameNode, ILayoutNode layoutNode)
            {
                // layoutNode.AbsoluteScale is in world space, while the animation by iTween
                // is in local space. Our game objects may be nested in other game objects,
                // hence, the two spaces may be different.
                // We may need to transform layoutNode.AbsoluteScale from world space to local space.
                Assert.IsNotNull(gameNode);
                Assert.IsNotNull(layoutNode);

                Vector3 localScale = gameNode.transform.parent == null ?
                                         layoutNode.AbsoluteScale
                                       : gameNode.transform.parent.InverseTransformVector(layoutNode.AbsoluteScale);

                IOperationCallback<Action> animation = gameNode.NodeOperator()
                        .ScaleTo(localScale, updateEdges: true);
                animation.OnComplete(() => OnComplete(gameNode));
                animation.OnKill(() => OnComplete(gameNode));
            }

            void OnComplete(GameObject go)
            {
                // Adjust the antenna and marker position after the scaling has finished;
                // otherwise they would be scaling along with their parent.
                if (changedNodes.Contains(go.GetNode()))
                {
                    // The adjustment of the antenna is needed only if the node has changed.
                    // Similarly to the adjustment of the style, we do not check whether
                    // the metrics determining the antenna have changed or not.
                    renderer.AdjustAntenna(go);
                }
                markerFactory.AdjustMarkerY(go);
                changed.Remove(go);
            }
        }

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private static readonly EdgeEqualityComparer edgeEqualityComparer = new();

        #region User Notifications

        /// <summary>
        /// Verb used to indicate to the user that an existing graph element was removed.
        /// </summary>
        private const string removed = "removed";
        /// <summary>
        /// Verb used to indicate to the user that a new graph element was added.
        /// </summary>
        private const string added = "added";
        /// <summary>
        /// Verb used to indicate to the user that an existing graph element was changed.
        /// </summary>
        private const string changed = "changed";
        /// <summary>
        /// Term used to indicate to the user that a graph element is an edge.
        /// We use "Relation" instead of "Edge" because "Edge" may be confusing
        /// to non-technical users.
        /// </summary>
        private const string edgeKind = "Relation";
        /// <summary>
        /// Term used to indicate to the user that a graph element is a node.
        /// We use "Entity" instead of "Node" because "Node" may be confusing
        /// to non-technical users.
        /// </summary>
        private const string nodeKind = "Entity";

        /// <summary>
        /// Notifies the user about added, changed, or removed graph elements.
        /// </summary>
        /// <param name="elements">the graph elements that have been updated</param>
        /// <param name="kind">a Relation (edge) or an Entity (node)</param>
        /// <param name="change">the kind of change</param>
        private static void ShowUpdated(IEnumerable<GraphElement> elements, string kind, string change)
        {
            return;
            foreach (GraphElement element in elements)
            {
                ShowNotification.Info($"{kind} {change}", $"{kind} {element.ID} was {change}.");
            }
        }

        /// <summary>
        /// Notifies the user about removed edges.
        /// </summary>
        /// <param name="edges">The list of removed edges.</param>
        private static void ShowRemovedEdges(ISet<Edge> edges)
        {
            ShowUpdated(edges, edgeKind, removed);
        }

        /// <summary>
        /// Notifies the user about added edges.
        /// </summary>
        /// <param name="edges">The list of added edges.</param>
        private static void ShowAddedEdges(ISet<Edge> edges)
        {
            ShowUpdated(edges, edgeKind, added);
        }

        /// <summary>
        /// Notifies the user about changed edges.
        /// </summary>
        /// <param name="edges">The list of changed edges.</param>
        private static void ShowChangedEdges(ISet<Edge> edges)
        {
            ShowUpdated(edges, edgeKind, changed);
        }

        /// <summary>
        /// Notifies the user about removed nodes.
        /// </summary>
        /// <param name="nodes">The list of removed nodes.</param>
        private static void ShowRemovedNodes(ISet<Node> nodes)
        {
            ShowUpdated(nodes, nodeKind, removed);
        }

        /// <summary>
        /// Notifies the user about added nodes.
        /// </summary>
        /// <param name="nodes">The list of added nodes.</param>
        private static void ShowAddedNodes(ISet<Node> nodes)
        {
            ShowUpdated(nodes, nodeKind, added);
        }

        /// <summary>
        /// Notifies the user about changed nodes.
        /// </summary>
        /// <param name="nodes">The list of changed nodes.</param>
        private void ShowChangedNodes(ISet<Node> nodes)
        {
            ShowUpdated(nodes, nodeKind, changed);
        }
        #endregion User Notifications
    }
}
