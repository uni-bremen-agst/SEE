using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GameObjects;
using SEE.GO;
using SEE.GO.Factories;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using Sirenix.Utilities;
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
        /// <param name="markerAttributes">Attributes for rendering the node markers.</param>
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
        /// A queue of <see cref="EdgeOperator"/>s associated with edges which are currently
        /// highlighted, that is, edges which have changed compared to the previous rendering.
        /// It will be used to revert the highlighting when rendering the next graph.
        /// </summary>
        private readonly Queue<EdgeOperator> highlightedEdgeOperators = new();

        /// <summary>
        /// Glow out the highlights for each previously marked edge in <see cref="highlightedEdgeOperators"/>.
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
        /// <param name="addedEdges">Added edges.</param>
        /// <param name="changedEdges">Changed edges.</param>
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
        /// <param name="gameEdge">The game edge to be marked.</param>
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
        /// <param name="oldGraph">The previously rendered graph.</param>
        /// <param name="newGraph">The new graph to be rendered anew.</param>
        /// <param name="edgesAreDrawn">Whether edges should be drawn.</param>
        /// <param name="codeCity">The game object under which to place the nodes and edges
        /// for the new graph.</param>
        /// <param name="renderer">The graph renderer to obtain the layouts from.</param>
        /// <returns>Task.</returns>
        internal async UniTask RenderAsync
            (Graph oldGraph,
             Graph newGraph,
             bool edgesAreDrawn,
             GameObject codeCity,
             IGraphRenderer renderer)
        {
            // Whether to animate a change. If false, changes are applied within the same frame.
            // FIXME: Make this a parameter.
            bool animate = false;

            Trace("Start");
            // Remove markers from previous rendering.
            markerFactory.Clear();
            DeleteEdgeMarking();
            Trace("Markers deleted.");

            newGraph.Diff(oldGraph,
                g => g.Nodes(),
                (g, id) => g.GetNode(id),
                GraphExtensions.AttributeDiff(newGraph, oldGraph),
                nodeEqualityComparer,
                out ISet<Node> addedNodes,   // nodes belong to newGraph
                out ISet<Node> removedNodes, // nodes belong to oldGraph
                out ISet<Node> changedNodes, // nodes belong to newGraph
                out ISet<Node> equalNodes);  // nodes belong to newGraph

            Trace("Nodes diffed");

            // Before we can calculate the new layout, we must ensure that all game objects
            // representing nodes or edges that are still present in the newGraph are reattached.
            equalNodes.UnionWith(changedNodes);
            Reattach(equalNodes);
            Trace("Nodes reattached");

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
                Trace("Edges diffed");
                equalEdges.UnionWith(changedEdges);
                // Note: From now on, equalEdges subsumes changedEdges.
                Reattach(equalEdges);
                Trace("Edges reattached");
            }

            // Note 1: We calculate the edge layout in advance (if needed
            // at all). Although a NodeOperator allows us to update the layout
            // of its edges, that does not work if both ends of an edge are
            // moved at the same time. In that case, one edge layout update
            // will kill the other one, resulting in one end of the edge not
            // properly updated.
            // Note 2: The following method call will also create the game nodes
            // for all graph nodes contained in the newGraph.
            NextLayout.Calculate(newGraph,
                                 GetGameNode,
                                 renderer,
                                 edgesAreDrawn,
                                 codeCity,
                                 out Dictionary<string, ILayoutNode> newNodelayout,
                                 out Dictionary<string, ILayoutEdge<ILayoutNode>> newEdgeLayout,
                                 ref oldLayout);
            Trace("Layout calculated");
            // Note: At this point, game nodes for new nodes have been created already.
            // Newly created nodes will have their visual properties already set.
            // Their NodeRefs refer to graph nodes in the newly loaded graph.
            // The removedNodes, changedNodes, and equalNodes have NodeRefs still
            // referencing nodes in the oldGraph.
            // That is no problem for removedNodes as these will be removed anyway.
            // Yet, we need to update the game nodes with NodeRefs to all changedNodes
            // and equalNodes.
            // The same holds for game edges.

            // We first remove the edges and then the nodes. That looks better.
            if (edgesAreDrawn)
            {
                ShowRemovedEdges(removedEdges);
                await AnimateDeathAsync(removedEdges, AnimateEdgeDeath, animate);
                Trace("Edges removed");
            }

            ShowRemovedNodes(removedNodes);
            await AnimateDeathAsync(removedNodes, AnimateNodeDeath, animate);
            Trace("Nodes removed");

            // Now we move the equal and changed nodes along with their edges to their new positions.
            await AnimateNodeMoveByLevelAsync(codeCity, equalNodes, equalEdges, newNodelayout, newEdgeLayout, animate);
            Trace("Movement of nodes finished");

            if (edgesAreDrawn)
            {
                ShowChangedEdges(changedEdges);
                Trace("Change of edges rendered");
            }
            ShowChangedNodes(changedNodes);
            Trace("Change of nodes rendered");

            // Even the equal nodes need adjustments because the layout could have
            // changed their dimensions. The treemap layout, for instance, may do that.
            // Note that equalNodes is the union of the really equal nodes passed initially
            // as a parameter and the changedNodes.
            MarkAndAdjustStyleAntenna(equalNodes, changedNodes, newNodelayout, markerFactory, renderer);
            Trace("Change finished");

            ShowAddedNodes(addedNodes);
            // The temporary parent object for the new nodes will be the codeCity. A new node
            // must have a parent object with a Portal component; otherwise the NodeOperator
            // will not work. Later, we will set the correct parent of the new node.
            await AnimateNodeBirthAsync(addedNodes, newNodelayout, GetGameNode, codeCity, animate);
            Trace("Birth of nodes animated");

            if (edgesAreDrawn)
            {
                ShowAddedEdges(addedEdges);
                AddNewEdges(addedEdges, renderer);
                Trace("New edges added");
            }

            MarkNodes(addedNodes, changedNodes);
            MarkEdges(addedEdges, changedEdges);
            Trace("Added and changed nodes and edges marked");

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
            /// It will have the same visual properties as before.
            ///
            /// Otherwise, a new game node is created and returned with the given <paramref name="node"/>
            /// attached to it. The returned game node will have its dimensions and other visual
            /// properties set according to the settings in <see cref="codeCity"/>. It will be added
            /// to <see cref="GraphElementIDMap"/>.
            /// </summary>
            /// <param name="node">Node for which a game node is requested.</param>
            /// <returns>Existing or new game node.</returns>
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
        ///
        /// Postcondition: all graph elements in <paramref name="toBeRemoved"/> are destroyed.
        /// </summary>
        /// <param name="toBeRemoved">Nodes to be removed.</param>
        /// <param name="animateDeath">Method to animate the death of a game object.</param>
        /// <param name="animate">Whether to animate. If false, the change is immediate.</param>
        /// <returns>Task.</returns>
        private async UniTask AnimateDeathAsync<T>
            (ISet<T> toBeRemoved,
             Func<GameObject, IOperationCallback<Action>> animateDeath,
             bool animate)
            where T : GraphElement
        {
            if (!animate)
            {
                DestroyAll(toBeRemoved);
                return;
            }

            HashSet<GameObject> deads = new();

            foreach (T element in toBeRemoved)
            {
                GameObject go = GraphElementIDMap.Find(element.ID, false);
                if (go != null)
                {
                    deads.Add(go);
                    IOperationCallback<Action> animation = animateDeath(go);
                    animation.OnComplete(() => OnComplete(go));
                    animation.OnKill(() => OnComplete(go));
                }
                else
                {
                    Debug.LogError($"Cannot retrieve {element.ID} from {nameof(GraphElementIDMap)} for destruction.\n");
                }
            }

            await UniTask.WaitUntil(() => deads.Count == 0);

            // We wait until all animations have finished before we destroy all game elements.
            // Otherwise the animation may be stalled.
            DestroyAll(toBeRemoved);

            void OnComplete(GameObject go)
            {
                deads.Remove(go);
            }

            // Destroys all elments in toBeRemoved.
            static void DestroyAll<T>(ISet<T> toBeRemoved) where T : GraphElement
            {
                foreach (T element in toBeRemoved)
                {
                    GameObject removable = GraphElementIDMap.Find(element.ID, false);
                    if (removable != null)
                    {
                        Destroyer.Destroy(removable, recurseIntoChildren: false);
                    }
                    else
                    {
                        Debug.LogError($"Cannot retrieve {element.ID} from {nameof(GraphElementIDMap)} for destruction.\n");
                    }
                }
            }
        }

        /// <summary>
        /// Creates new game objects for <paramref name="addedNodes"/> and renders them.
        ///
        /// Postcondition: New game objects have been created for all <paramref name="addedNodes"/>
        /// using <paramref name="getGameNode"/> and they are positioned and scaled according to the
        /// given <paramref name="newNodelayout"/>. Their parent will be the game node representing
        /// the parent of an added <see cref="Node"/> in the underlying <see cref="Graph"/>.
        /// </summary>
        /// <param name="addedNodes">Nodes to be added.</param>
        /// <param name="newNodelayout">The layout to be applied to the new nodes.</param>
        /// <param name="getGameNode">Method to get or create the game node for a given graph node.
        /// The assumption is that this method adds the game node to the <see cref="GraphElementIDMap"/>
        /// if not already present there.</param>
        /// <param name="codeCity">The temporary parent object for the new game nodes (should be the code city).</param>
        /// <param name="animate">Whether to animate. If false, the change is immediate.</param>
        /// <returns>Task.</returns>
        private static async UniTask AnimateNodeBirthAsync
            (ISet<Node> addedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             Func<Node, GameObject> getGameNode,
             GameObject codeCity,
             bool animate)
        {
            Assert.IsNotNull(codeCity);

            if (!animate)
            {
                /// Create the new game nodes and add them to the <see cref="GraphElementIDMap"/>.
                foreach (Node node in addedNodes)
                {
                    Assert.IsNotNull(getGameNode(node));
                }
                // Now we can be sure that all game nodes exist including the parent of every node.
                // We can set the correct (Unity) parent of every game node as well as the
                // position and scale.
                foreach (Node node in addedNodes)
                {
                    GameObject gameNode = GraphElementIDMap.Find(node.ID, true);

                    ApplyLayout(gameNode, newNodelayout);

                    if (!node.IsRoot())
                    {
                        GameObject parent = GraphElementIDMap.Find(node.Parent.ID, true);
                        gameNode.transform.SetParent(parent.transform);
                    }
                    else
                    {
                        // The root node is immediate child of the code city.
                        gameNode.transform.SetParent(codeCity.transform);
                    }
                }

                return;
            }

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

            // Game nodes for all new graph nodes now exist.

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
        /// Assigns the real-world position and real-world scale of <paramref name="gameNode"/>
        /// according to its entry in <paramref name="newNodelayout"/>.
        ///
        /// The changes are applied without any animation.
        /// </summary>
        /// <param name="gameNode">The game node whose scale and position are to be set.</param>
        /// <param name="newNodelayout">The layout from which to retrieve the new scale and position.</param>
        private static void ApplyLayout(GameObject gameNode, Dictionary<string, ILayoutNode> newNodelayout)
        {
            ILayoutNode layoutNode = newNodelayout[gameNode.name];
            if (layoutNode != null)
            {
                ApplyLayout(gameNode, layoutNode);
            }
            else
            {
                Debug.LogError($"No layout for node {gameNode.name}.\n");
            }
        }

        /// <summary>
        /// Assigns the real-world position and real-world scale of <paramref name="gameNode"/>
        /// according <paramref name="layoutNode"/>.
        ///
        /// The changes are applied without any animation.
        /// </summary>
        /// <param name="gameNode">The game node whose scale and position are to be set.</param>
        /// <param name="layoutNode">New position and scale.</param>
        private static void ApplyLayout(GameObject gameNode, ILayoutNode layoutNode)
        {
            gameNode.transform.position = layoutNode.CenterPosition;
            gameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);
        }

        /// <summary>
        /// Applies the layout given by <paramref name="layoutEdge"/> to the <paramref name="gameEdge"/>
        /// without any animation.
        /// </summary>
        /// <param name="gameEdge">Edge to which apply the layout.</param>
        /// <param name="layoutEdge">The layout to be applied.</param>
        private static void ApplyLayout(GameObject gameEdge, ILayoutEdge<ILayoutNode> layoutEdge)
        {
            gameEdge.EdgeOperator().MorphTo(layoutEdge.Spline, factor: 0);
        }

        /// <summary>
        /// Creates and adds new game edges for <paramref name="addedEdges"/>.
        /// It is not animated.
        ///
        /// Postcondition: Game edges are created, laid out and rendered for a <paramref name="addedEdges"/>.
        /// </summary>
        /// <param name="addedEdges">The new graph edges.</param>
        /// <param name="renderer">The graph renderer to draw the new game edges.</param>
        /// <returns>Task.</returns>
        private void AddNewEdges(ISet<Edge> addedEdges, IGraphRenderer renderer)
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
            /// <param name="edge">Graph edge for which to create a game edge.</param>
            /// <returns>New game edge.</returns>
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
        /// the same graph elements (according to the ID) of the former graph. That is why we
        /// need to reattach them to <paramref name="elements"/>
        /// </summary>
        /// <param name="elements">Graph elements of the new graph to be reattached.</param>
        private void Reattach<T>(ISet<T> elements) where T : GraphElement
        {
            foreach (GraphElement element in elements)
            {
                GameObject go = GraphElementIDMap.Find(element.ID, true);
                // No need to check for null because we must find the game object.
                // If the game object cannot be found, an exception is thrown.
                GraphElementReattacher.Reattach(go, element);
            }
        }

        /// <summary>
        /// Animates the movement of <paramref name="movedNodes"/> to their new positions
        /// according to <paramref name="newNodelayout"/>.
        ///
        /// Postcondition: All <paramref name="movedNodes"/> are at their final location
        /// according to <paramref name="newNodelayout"/>.
        /// </summary>
        /// <param name="codeCity">The code city currently being drawn.</param>
        /// <param name="movedNodes">Game nodes to be moved.</param>
        /// <param name="movedEdges">Existing or changed edge that might need to move
        /// along with the <paramref name="movedNodes"/>.</param>
        /// <param name="newNodelayout">New positions and scales for nodes.</param>
        /// <param name="newEdgeLayout">New layout for edges.</param>
        /// <param name="animate">Whether to animate. If false, the change is immediate.</param>
        /// <returns>Task.</returns>
        private static async UniTask AnimateNodeMoveByLevelAsync
                                (GameObject codeCity,
                                 ISet<Node> movedNodes,
                                 ISet<Edge> movedEdges,
                                 Dictionary<string, ILayoutNode> newNodelayout,
                                 Dictionary<string, ILayoutEdge<ILayoutNode>> newEdgeLayout,
                                 bool animate)
        {
            if (movedNodes.Count == 0)
            {
                return;
            }

            if (!animate)
            {
                MoveNodesImmediately(movedNodes, newNodelayout);
                if (newEdgeLayout.Count > 0)
                {
                    MoveEdgesImmediately(movedNodes, movedEdges, newEdgeLayout);
                }
                return;
            }

            // Partition all at the same hierarchy level.
            UnionFind<Node, int> unionFind = new(movedNodes, n => n.Level);
            unionFind.PartitionByValue();
            Trace("Nodes partitioned");
            // For each partition sorted ascendingly by the hierarchy level:
            // move all the nodes in the same partition together.
            // Note that a partition is a list of Nodes. For the sorting, we
            // take the level of the first node of each list. Each list is
            // guaranteed to have at least one node. The partitions were defined
            // be the node levels.
            int level = 0;
            foreach (IList<Node> partition in unionFind.GetPartitions().ToList().OrderBy(l => l.First().Level))
            {
                level++;
                await MoveAsync(codeCity, partition, newNodelayout);
                Trace($"Nodes at level {level} moved");
            }

            // Moves nodes immediately without any animation.
            static void MoveNodesImmediately(ISet<Node> movedNodes,
                                             Dictionary<string, ILayoutNode> newNodelayout)
            {
                // Save the original parent and set it to null temporarily such that we
                // can move all nodes independently from their parents.
                Dictionary<Transform, Transform> parent = new(movedNodes.Count);
                foreach (Node node in movedNodes)
                {
                    GameObject gameNode = GraphElementIDMap.Find(node.ID, true);
                    parent[gameNode.transform] = gameNode.transform.parent;
                    gameNode.transform.SetParent(null);
                }
                // Apply the layout.
                foreach (Transform gameNode in parent.Keys)
                {
                    ApplyLayout(gameNode.gameObject, newNodelayout);
                }
                // Restore the original parent.
                foreach (KeyValuePair<Transform, Transform> entry in parent)
                {
                    entry.Key.SetParent(entry.Value);
                }
            }
        }

        /// <summary>
        /// Morphs the edges connected to any node in <paramref name="movedNodes"/> and
        /// contained in <paramref name="movedEdges"/> to their form described in
        /// <paramref name="newEdgeLayout"/>.
        /// </summary>
        /// <param name="movedNodes">Nodes to be moved.</param>
        /// <param name="movedEdges">Edges to be moved.</param>
        /// <param name="newEdgeLayout">The edge layout to be applied.</param>
        private static void MoveEdgesImmediately(ISet<Node> movedNodes, ISet<Edge> movedEdges, Dictionary<string, ILayoutEdge<ILayoutNode>> newEdgeLayout)
        {
            ISet<Edge> associatedEdges = Edges(movedNodes);
            foreach (Edge edge in associatedEdges)
            {
                if (movedEdges.Contains(edge))
                {
                    if (GraphElementIDMap.TryGetValue(edge.ID, out GameObject gameEdge))
                    {
                        if (newEdgeLayout.TryGetValue(edge.ID, out ILayoutEdge<ILayoutNode> layoutEdge))
                        {
                            ApplyLayout(gameEdge, layoutEdge);
                        }
                        else
                        {
                            Debug.LogError($"No edge layout for {edge.ID}\n");
                        }
                    }
                    else
                    {
                        Debug.LogError($"No game edge for {edge.ID} in {nameof(GraphElementIDMap)}.\n");
                    }
                }
            }
        }

        /// <summary>
        /// Returns the incoming and outgoing edges for the given <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">Nodes whose edges are requested.</param>
        /// <returns>All edges for the given <paramref name="nodes"/>.</returns>
        private static ISet<Edge> Edges(ISet<Node> nodes)
        {
            HashSet<Edge> result = new();
            foreach (Node node in nodes)
            {
                result.AddRange(node.Edges);
            }
            return result;
        }

        /// <summary>
        /// Moves all <paramref name="movedNodes"/> to their target position looked up in
        /// <paramref name="newNodelayout"/>. Incoming and outgoing edges are moved along
        /// with the nodes.
        /// </summary>
        /// <param name="movedNodes">Nodes to be moved.</param>
        /// <param name="newNodelayout">Target position of the nodes to be moved.</param>
        /// <returns>Task.</returns>
        private static async UniTask MoveAsync(GameObject codeCity, IList<Node> movedNodes, Dictionary<string, ILayoutNode> newNodelayout)
        {
            HashSet<GameObject> moved = new();

            Portal.GetDimensions(codeCity, out Vector2 leftFront, out Vector2 rightBack);

            foreach (Node node in movedNodes)
            {
                Trace($"Trying to move node {node.ID}");
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                if (go != null)
                {
                    ILayoutNode layoutNode = newNodelayout[node.ID];
                    if (layoutNode != null)
                    {
                        if (!Portal.InPortal(layoutNode.CenterPosition, leftFront, rightBack))
                        {
                            Debug.LogError($"Attempt to move {node.ID} to world space {layoutNode.CenterPosition:F9} outside of portal {leftFront:F3}-{rightBack:F3}. Node will not be moved.\n");
                            continue;
                        }
                        // Animate the move only if the position has changed by a relevant margin.
                        if (PositionHasChanged(go, layoutNode))
                        {
                            Trace($"Trying to move {node.ID} from world space {go.transform.position} to world space {layoutNode.CenterPosition:F9} at distance {Vector3.Distance(go.transform.position, layoutNode.CenterPosition):F9} within portal {leftFront:F3}-{rightBack:F3}");

                            moved.Add(go);
                            // Move the node to its new position. The edge layout will be updated.
                            try
                            {
                                // What if there is an edge between two nodes that are both being moved?
                                IOperationCallback<Action> animation = go.NodeOperator()
                                  .ResizeTo(ToLocalScale(go, layoutNode), layoutNode.CenterPosition, updateEdges: true);
                                animation.OnComplete(() => OnComplete(go));
                                animation.OnKill(() => OnComplete(go));
                            }
                            catch (Exception e)
                            {
                                Trace($"Exception {e.Message} caught for {node.ID}");
                                moved.Remove(go);
                            }
                        }
                        else
                        {
                            // No animation. Apply layout in a single frame.
                            ApplyLayout(go, layoutNode);
                        }
                    }
                    else
                    {
                        Debug.LogError($"No layout for node {node.ID}.\n");
                    }
                }
                else
                {
                    Trace($"{node.ID} not found in the scene");
                }
            }

            await UniTask.WaitUntil(() => moved.Count <= 0);

            void OnComplete(GameObject go)
            {
                moved.Remove(go);
            }
        }

        /// <summary>
        /// The distance between a current position and a new position of a game node
        /// at which we consider that the position has actually changed. The unit is
        /// Unity world units.
        /// </summary>
        private const float relevantMovementMargin = 0.001f;

        /// <summary>
        /// Squared <see cref="relevantMovementMargin"/>.
        /// </summary>
        private const float squaredRelevantMovementMargin = relevantMovementMargin * relevantMovementMargin;

        /// <summary>
        /// True if the position of the given <paramref name="gameNode"/> has actually changed
        /// by a relevant margin. We consider only the distance between the current position of
        /// <paramref name="gameNode"/> and its target position according to <paramref name="layoutNode"/>
        /// in the 2D X/Z plane. That distance must be below <see cref="relevantMovementMargin"/>
        /// to be considered a relevant change.
        /// </summary>
        /// <param name="gameNode">Game node to be moved.</param>
        /// <param name="layoutNode">The intended new position of <paramref name="gameNode"/>.</param>
        /// <returns>True if the position of the given <paramref name="gameNode"/> has actually changed
        /// by a relevant margin.</returns>
        private static bool PositionHasChanged(GameObject gameNode, ILayoutNode layoutNode)
        {
            float deltaX = layoutNode.CenterPosition.x - gameNode.transform.position.x;
            float deltaZ = layoutNode.CenterPosition.z - gameNode.transform.position.z;
            float sqrMagnitude = (deltaX * deltaX) + (deltaZ * deltaZ);
            /// The 2D distance would be sqrt(sqrMagnitude), but since calculating sqrt
            /// is expensive we compare against the <see cref="squaredRelevantMovementMargin"/>.
            return sqrMagnitude > squaredRelevantMovementMargin;
        }

        /// <summary>
        /// Adjusts the antenna and style of <paramref name="nodesToAdjust"/>.
        /// The nodes in <paramref name="changedNodes"/> will
        /// be marked as changed using <paramref name="markerFactory"/>.
        ///
        /// We assume the game nodes have already their final position and scale.
        ///
        /// Postcondition: The style and antenna of <paramref name="nodesToAdjust"/>
        /// have been adjusted according to <paramref name="newNodelayout"/>. All
        /// <paramref name="changedNodes"/> are marked.
        /// </summary>
        /// <param name="nodesToAdjust">Nodes whose dimensions and markers need to be
        /// adjusted; we assume <paramref name="changedNodes"/>
        /// is a subset of <paramref name="nodesToAdjust"/>.</param>
        /// <param name="changedNodes">Nodes to be marked for a change.</param>
        /// <param name="newNodelayout">New layout determining the new scale.</param>
        /// <param name="markerFactory">Factory for marking as changed.</param>
        /// <param name="renderer">The graph renderer to adjust node styles and antennas.</param>
        /// <returns>Task.</returns>
        private static void MarkAndAdjustStyleAntenna
            (ISet<Node> nodesToAdjust,
             ISet<Node> changedNodes,
             Dictionary<string, ILayoutNode> newNodelayout,
             MarkerFactory markerFactory,
             IGraphRenderer renderer)
        {
            foreach (Node node in nodesToAdjust)
            {
                GameObject go = GraphElementIDMap.Find(node.ID, true);
                // Note: go cannot be null, otherwise an exception would be throw
                // since we request mustFindElement.
                ILayoutNode layoutNode = newNodelayout[node.ID];
                if (layoutNode != null)
                {
                    // Apply the adjustment only to the really changedNodes.
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
                    // The game node itself was already scaled.
                    AdjustAntennaAndMark(changedNodes, markerFactory, renderer, go);
                }
                else
                {
                    Debug.LogError($"No layout for node {node.ID}.\n");
                }
            }

            static void AdjustAntennaAndMark(ISet<Node> changedNodes, MarkerFactory markerFactory, IGraphRenderer renderer, GameObject go)
            {
                // Adjust the antenna and marker position after the scaling has finished;
                // otherwise they would be scaling along with their parent.
                if (changedNodes.Contains(go.GetNode()))
                {
                    // The adjustment of the antenna is needed only if the node has changed.
                    // Similarly to the adjustment of the style, we do not check whether
                    // the metrics determining the antenna have changed or not. Simply
                    // because we do not know here.
                    renderer.AdjustAntenna(go);
                }
                markerFactory.AdjustMarkerY(go);
            }
        }

        /// <summary>
        /// Returns the target local-space scale for <paramref name="gameNode"/> according
        /// to the <paramref name="layoutNode"/>. If <paramref name="gameNode"/> has not
        /// parent yet, it is just the absolute scale of the layout node. Otherwise
        /// the absolute scale of the layout node is transformed into the local space
        /// relative to the parent of <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">Game node whose target relative scale is to be
        /// computed.</param>
        /// <param name="layoutNode">Layout node from which to retrieve the
        /// absolute scale to be transformed.</param>
        /// <returns>Target local-space scale.</returns>
        private static Vector3 ToLocalScale(GameObject gameNode, ILayoutNode layoutNode)
        {
            return gameNode.transform.parent == null ?
                        layoutNode.AbsoluteScale
                      : gameNode.transform.parent.InverseTransformVector(layoutNode.AbsoluteScale);
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
        /// <param name="elements">The graph elements that have been updated.</param>
        /// <param name="kind">A Relation (edge) or an Entity (node).</param>
        /// <param name="change">The kind of change.</param>
        private static void ShowUpdated(IEnumerable<GraphElement> elements, string kind, string change)
        {
            foreach (GraphElement element in elements)
            {
                string message = $"{kind} {element.ID} was {change}.";
                Debug.Log(message + '\n');
                // ShowNotification.Info($"{kind} {change}", message);
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

        #region Tracing
        /// <summary>
        /// If true, tracing messages will be emitted.
        /// </summary>
        private const bool doTrace = true;

        /// <summary>
        /// Emits <paramref name="message"/> if <see cref="doTrace"/> is true.
        /// </summary>
        /// <param name="message">Message to be emitted.</param>
        private static void Trace(string message)
        {
            if (doTrace)
            {
                Debug.Log(message + '\n');
            }
        }
        #endregion Tracing
    }
}
