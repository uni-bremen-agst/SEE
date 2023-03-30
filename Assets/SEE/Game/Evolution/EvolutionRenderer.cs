//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.

using DG.Tweening;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Charts;
using SEE.Game.City;
using SEE.Game.Evolution;
using SEE.GO;
using SEE.Layout;
using SEE.Utils;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace SEE.Game
{
    /// <summary>
    /// Renders the evolution of the graph series through animations. Incrementally updates
    /// the graph (removal/addition of nodes/edges).
    ///
    /// Note: The renderer is a MonoBehaviour, thus, will be added as a component to a game
    /// object. As a consequence, a constructor will not be called and is meaningless.
    ///
    /// Assumption: This EvolutionRenderer is attached to a game object representing a code
    /// city that has another component of type <see cref="SEECityEvolution"/>.
    /// </summary>
    public partial class EvolutionRenderer : MonoBehaviour, IGraphRenderer
    {
        /// <summary>
        /// Sets the evolving series of <paramref name="graphs"/> to be visualized.
        /// The actual visualization is triggered by <see cref="ShowGraphEvolution"/>
        /// that can be called next.
        /// This method is expected to be called before attemption to draw any graph.
        /// </summary>
        /// <param name="graphs">series of graphs to be visualized</param>
        public void SetGraphEvolution(List<Graph> graphs)
        {
            this.graphs = graphs;
            if (gameObject.TryGetComponent(out SEECityEvolution cityEvolution))
            {
                // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
                // So we cannot make the following assignment in the constructor. Neither
                // can we assign this value at the declaration of graphRenderer because
                // we need the city argument, which comes only later. Anyhow, whenever we
                // assign a new city, we also need a new graph renderer for that city.
                // So in fact this is the perfect place to assign graphRenderer.
                graphRenderer = new GraphRenderer(cityEvolution, graphs);
                edgesAreDrawn = graphRenderer.AreEdgesDrawn();

                objectManager = new ObjectManager(graphRenderer, gameObject);
                markerFactory = new MarkerFactory(markerWidth: cityEvolution.MarkerWidth,
                                    markerHeight: cityEvolution.MarkerHeight,
                                    additionColor: cityEvolution.AdditionBeamColor,
                                    changeColor: cityEvolution.ChangeBeamColor,
                                    deletionColor: cityEvolution.DeletionBeamColor);
                RegisterAllAnimators(animators);
                phase1AnimationWatchDog = new Phase1AnimationWatchDog(this);
                phase2AnimationWatchDog = new Phase2AnimationWatchDog(this);
            }
            else
            {
                Debug.LogError($"This EvolutionRenderer attached to {name} has no sibling component of type {nameof(SEECityEvolution)}.\n");
                enabled = false;
            }
            graphRenderer.SetScaler(graphs);
        }

        /// <summary>
        /// True if edges are actually drawn, that is, if the user has selected an
        /// edge layout different from <see cref="EdgeLayoutKind.None"/>.
        /// </summary>
        private bool edgesAreDrawn = false;

        /// <summary>
        /// The y co-ordinate where to to move deleted nodes in world space. Deleted nodes will
        /// be lifted to this level and then disappear.
        /// </summary>
        private const float SkyLevel = 2.0f;

        /// <summary>
        /// The graph renderer used to draw a single graph and the later added nodes and edges.
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the city, which is set by this setter.
        /// </summary>
        private GraphRenderer graphRenderer;  // not serialized by Unity; will be set in CityEvolution property

        /// <summary>
        /// The manager of the game objects created for the city.
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the graphRenderer, which in turn depends upon the city, which is set by
        /// this setter.
        /// </summary>
        private ObjectManager objectManager;  // not serialized by Unity; will be set in CityEvolution property

        /// <summary>
        /// The marker factory used to mark the new and removed game objects.
        /// </summary>
        private MarkerFactory markerFactory;  // not serialized by Unity; will be set in CityEvolution property

        /// <summary>
        /// The kind of comparison to determine whether there are any differences between
        /// two corresponding graph elements (corresponding by their ID) in
        /// two different graphs of the graph series.
        /// </summary>
        private GraphElementDiff diff;  // not serialized by Unity; will be set in CityEvolution property

        /// <summary>
        /// Registers <paramref name="action"/> to be called back when the shown
        /// graph has changed.
        /// </summary>
        /// <param name="action">action to be called back</param>
        internal void Register(UnityAction action)
        {
            shownGraphHasChangedEvent.AddListener(action);
        }

        /// <summary>
        /// An event fired upon the end of an animation.
        /// </summary>
        private readonly UnityEvent AnimationFinishedEvent = new();

        /// <summary>
        /// The animator used when an inner node is removed from the scene
        /// and also for animating the new plane.
        ///
        /// Note: We split the animation in halves for the first phase of
        /// removing nodes and the second phase of drawing the nodes of
        /// the next graph.
        /// </summary>
        private readonly AbstractAnimator moveAnimator
            = new MoveAnimator(AbstractAnimator.DefaultAnimationTime / 2.0f);

        /// <summary>
        /// An animator used for all other occasions (new nodes, existing nodes, changed nodes).
        ///
        /// Note: We split the animation in halves for the first phase of
        /// removing nodes and the second phase of drawing the nodes of
        /// the next graph.
        /// </summary>
        private readonly AbstractAnimator changeAndBirthAnimator
            = new MoveScaleShakeAnimator(AbstractAnimator.DefaultAnimationTime / 2.0f);

        /// <summary>
        /// Maps from the source of an edge to its tween. This storage is
        /// used by <see cref="RenderNode(Node)"/> to synchronize node
        /// animation with edge animation.
        /// </summary>
        private readonly Dictionary<Node, Tween> edgeTweens = new();

        /// <summary>
        /// True if animation is still ongoing.
        /// </summary>
        public bool IsStillAnimating { get; private set; }

        /// <summary>
        /// The collection of registered <see cref="AbstractAnimator"/> to be updated
        /// automatically for changes during the animation time period.
        /// </summary>
        private readonly List<AbstractAnimator> animators = new();

        /// <summary>
        /// The duration of an animation. This value can be controlled by the user.
        /// </summary>
        [SerializeField]
        private float animationDuration = AbstractAnimator.DefaultAnimationTime;

        /// <summary>
        /// The time in seconds for showing a single graph revision during auto-play animation.
        /// </summary>
        public float AnimationLag
        {
            get => animationDuration;
            set
            {
                if (value >= 0)
                {
                    animationDuration = value;
                    animators.ForEach(animator =>
                    {
                        animator.MaxAnimationTime = value;
                        animator.AnimationsDisabled = value == 0;
                    });

                    shownGraphHasChangedEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// The city (graph + layout) currently shown.
        /// </summary>
        private LaidOutGraph currentCity;  // not serialized by Unity

        /// <summary>
        /// The city (graph + layout) to be shown next.
        /// </summary>
        private LaidOutGraph nextCity;  // not serialized by Unity

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private static readonly EdgeEqualityComparer edgeEqualityComparer = new();

        /// <summary>
        /// If true, inner nodes should not be rendered. This will be true if a non-hierarchical
        /// layout is applied.
        /// </summary>
        private bool ignoreInnerNodes = true;  // not serialized by Unity



        /// <summary>
        /// Displays the given graph instantly if all animations are finished. This is
        /// called when we jump directly to a specific graph in the graph series: when
        /// we start the visualization of the graph evolution initially and when the user
        /// selects a specific graph revision.
        /// The graph is drawn from scratch.
        /// </summary>
        /// <param name="graph">graph to be drawn initially</param>
        private void DisplayGraphAsNew(LaidOutGraph graph)
        {
            graph.AssertNotNull("graph");

            if (IsStillAnimating)
            {
                Debug.LogWarning("Graph changes are blocked while animations are running.\n");
                return;
            }
            // The upfront calculation of the node layout for all graphs has filled
            // objectManager with game objects for those nodes. Likewise, when we jump
            // to a graph directly in the version history, the nodes of its predecessors
            // may still be contained in the scene and objectManager. We need to clean up
            // first.
            objectManager?.Clear();
            RenderGraph(currentCity, graph);
            //StartCoroutine(DelayedDisplayGraphAsNew(graph));
        }

        IEnumerator DelayedDisplayGraphAsNew(LaidOutGraph graph)
        {
            //wait for space to be pressed
            while (!Input.GetKeyDown(KeyCode.D))
            {
                yield return null;
            }

        }

        /// <summary>
        /// Starts the animations to transition from the <paramref name="current"/> graph
        /// to the <paramref name="next"/> graph.
        /// </summary>
        /// <param name="current">the currently shown graph</param>
        /// <param name="next">the next graph to be shown</param>
        public void TransitionToNextGraph(LaidOutGraph current, LaidOutGraph next)
        {
            current.AssertNotNull("current");
            next.AssertNotNull("next");
            if (IsStillAnimating)
            {
                Debug.Log("Graph changes are blocked while animations are running.\n");
                return;
            }
            RenderGraph(current, next);
        }

        private ISet<Node> addedNodes;
        private ISet<Node> removedNodes;
        private ISet<Node> changedNodes;
        private ISet<Node> equalNodes;

        private ISet<Edge> addedEdges;
        private ISet<Edge> removedEdges;
        private ISet<Edge> changedEdges;
        private ISet<Edge> equalEdges;

        /// <summary>
        /// Renders the animation from <paramref name="current"/> to <paramref name="next"/>.
        /// </summary>
        /// <param name="current">the graph currently shown that is to be migrated into the next graph; may be null</param>
        /// <param name="next">the new graph to be shown, in which to migrate the current graph; must not be null</param>
        private void RenderGraph(LaidOutGraph current, LaidOutGraph next)
        {
            next.AssertNotNull("next");

            IsStillAnimating = true;
            // First remove all markings of the previous animation cycle.
            markerFactory.Clear();

            Graph oldGraph = current != null ? current.Graph : null;
            Graph newGraph = next != null ? next.Graph : null;

            // Node comparison.
            {
                newGraph.Diff(oldGraph,
                              g => g.Nodes(),
                              (g, id) => g.GetNode(id),
                              GraphExtensions.AttributeDiff(newGraph, oldGraph),
                              nodeEqualityComparer,
                              out addedNodes,
                              out removedNodes,
                              out changedNodes,
                              out equalNodes);
            }

            // Edge comparison.
            {
                newGraph.Diff(oldGraph,
                              g => g.Edges(),
                              (g, id) => g.GetEdge(id),
                              GraphExtensions.AttributeDiff(newGraph, oldGraph),
                              edgeEqualityComparer,
                              out addedEdges,
                              out removedEdges,
                              out changedEdges,
                              out equalEdges);
            }
            if (OLD)
            {
                Phase1RemoveDeletedGraphElements(current, next);
            }
        }

        private bool OLD = false;

        /// <summary>
        /// Implements the first phase of the transition from the <paramref name="current"/> graph to
        /// the <paramref name="next"/> graph in which nodes and edges present in <paramref name="current"/>
        /// but not in <paramref name="next"/> (in other words, the deleted nodes and edges) are removed.
        /// When all animations triggered in this first phase have completed, execution will continue with
        /// <see cref="Phase2AddNewAndExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// The mechanism to call <see cref="Phase2AddNewAndExistingGraphElements(LaidOutGraph)"/> is
        /// as follows. The <see cref="phase1AnimationWatchDog"/> is set up to await the
        /// deletion of nodes and edges in <paramref name="current"/> but not in <paramref name="next"/>.
        /// Then the methods implementing this deletion will be called for each graph element to
        /// be deleted; these are <see cref="RenderRemovedNode(Node)"/> and <see cref="RenderRemovedEdge(Edge)"/>,
        /// respectively. When the animation of the deletion triggered by these has finished, each
        /// will signal the <see cref="phase1AnimationWatchDog"/> its completion. The <see cref="phase1AnimationWatchDog"/>
        /// awaits all outstanding deletions and then finally calls
        /// <see cref="Phase2AddNewAndExistingGraphElements(LaidOutGraph)"/>.
        ///
        /// Note: <paramref name="next"/> will be a graph for the previous revision of
        /// <paramref name="current"/> in the graph series when the evolution visualization
        /// is played backward.
        /// </summary>
        /// <param name="current">the currently shown graph</param>
        /// <param name="next">the next graph to be shown</param>
        private void Phase1RemoveDeletedGraphElements(LaidOutGraph current, LaidOutGraph next)
        {
            if (OLD)
            {
                if (current != null && current.Graph != null)
                {
                    // The set of nodes of the current graph not in the other graph, in other words,
                    // the set of deleted nodes.
                    // Note: The comparison is based on the IDs of the nodes because nodes between
                    // two graphs must be different even if they denote the "logically same" node.
                    List<Node> deletedNodes = current.Graph.Nodes().Except(next.Graph.Nodes(), nodeEqualityComparer).ToList();

                    // The set of edges of the current graph not in the next graph; that is, all
                    // edges removed. As above, edges are compared by their IDs.
                    List<Edge> deletedEdges = current.Graph.Edges().Except(next.Graph.Edges(), edgeEqualityComparer).ToList();

                    int deletedGraphElements = deletedNodes.Count + deletedEdges.Count;
                    if (deletedGraphElements > 0)
                    {
                        phase1AnimationWatchDog.Await(deletedGraphElements, next);

                        // Remove those nodes.
                        deletedNodes.ForEach(RenderRemovedNode);
                        // Remove those edges.
                        deletedEdges.ForEach(RenderRemovedEdge);
                    }
                    else
                    {
                        // To trigger the next phase where new and existing edges are to be drawn.
                        phase1AnimationWatchDog.Skip(next);
                    }
                }
                else
                {
                    // To trigger the next phase where new and existing edges are to be drawn.
                    phase1AnimationWatchDog.Skip(next);
                }
                /// Note: <see cref="Phase2AddNewAndExistingGraphElements"/> will be called by <see cref="phase1AnimationWatchDog"/>
                /// when phase 1 has completed (or skipped).
            }
            else
            {
                int deletedGraphElements = removedNodes.Count + removedEdges.Count;
                if (deletedGraphElements > 0)
                {
                    phase1AnimationWatchDog.Await(deletedGraphElements, next);

                    // Remove those nodes.
                    removedNodes.ForEach(RenderRemovedNode);
                    // Remove those edges.
                    removedEdges.ForEach(RenderRemovedEdge);
                }
                else
                {
                    // To trigger the next phase where new and existing edges are to be drawn.
                    phase1AnimationWatchDog.Skip(next);
                }
                /// Note: <see cref="Phase2AddNewAndExistingGraphElements"/> will be called by <see cref="phase1AnimationWatchDog"/>
                /// when phase 1 has completed (or skipped).
            }
        }

        /// <summary>
        /// Implements the second phase in the transition from the <see cref="currentCity"/>
        /// to the <paramref name="next"/> graph.
        /// In this phase, all nodes in <paramref name="next"/> will be drawn. These may be
        /// either new or existing nodes (the latter being nodes that have been present in the
        /// currently drawn graph). When this phase has been completed, <see cref="OnAnimationsFinished"/>
        /// will be called eventually.
        ///
        /// To make sure that <see cref="OnAnimationsFinished"/> is called only when all nodes
        /// have been drawn, <see cref="phase2AnimationWatchDog"/> will be used. It will be
        /// set up to await the number of nodes present in <paramref name="next"/>. Then
        /// <see cref="RenderNode(Node)"/> or <see cref="IgnoreNode(Node)"/>, respectively, will be
        /// called for each such node. These in turn will signal <see cref="phase2AnimationWatchDog"/>
        /// when they have finished. When all outstanding node additions have been completed,
        /// <see cref="phase2AnimationWatchDog"/> will call <see cref="OnAnimationsFinished"/>.
        ///
        /// Note: <paramref name="next"/> will be a graph for the previous revision of
        /// the currently drawn graph in the graph series when the evolution visualization
        /// is played backward.
        /// </summary>
        /// <param name="next">the next graph to be drawn</param>
        private void Phase2AddNewAndExistingGraphElements(LaidOutGraph next)
        {
            /// We need to assign nextCity because the callbacks <see cref="RenderPlane"/>
            /// and <see cref="RenderNode(Node)"/> will access it. More precisely,
            /// <see cref="RenderNode(Node)"/> uses <see cref="NextLayoutToBeShown"/>
            /// which in turn uses <see cref="nextCity"/>.
            nextCity = next;

            NodeAnimation(next);
            CreateEdges(next);
            if (edgesAreDrawn)
            {
                EdgeAnimation(next);
            }

            // We have made the transition to the next graph.
            currentCity = next;

            void CreateEdges(LaidOutGraph next)
            {
                foreach (Edge edge in next.Graph.Edges())
                {
                    objectManager.GetEdge(edge, out GameObject edgeObject);
                    if (edgeObject.TryGetComponent(out SEESpline spline))
                    {
                        spline.Spline = next.EdgeLayout[edge.ID].Spline;
                    }
                    edgeObject.SetActive(true); // Make visible
                }
            }

            void EdgeAnimation(LaidOutGraph next)
            {
                // Create (or read from cache) the edge objects of the next
                // visible graph, update their spline, and make the objects
                // visible.
                if (currentCity != null)
                {
                    // We are transitioning to another graph.
                    edgeTweens.Clear();
                    foreach (Edge edge in next.Graph.Edges())
                    {
                        if (!next.EdgeLayout.TryGetValue(edge.ID, out ILayoutEdge<ILayoutNode> target))
                        {
                            Debug.LogWarning($"Missing layout edge for graph edge with id '{edge.ID}'; skipping it.\n");
                            continue;
                        }
                        if (currentCity.EdgeLayout.TryGetValue(edge.ID, out ILayoutEdge<ILayoutNode> source))
                        {
                            objectManager.GetEdge(edge, out GameObject edgeObject);
                            if (!edgeObject.TryGetComponent(out SplineMorphism morphism))
                            {
                                morphism = edgeObject.AddComponent<SplineMorphism>();
                            }
                            // We can adjust the duration later using the timeScale attribute.
                            edgeTweens[edge.Source] = morphism.CreateTween(source.Spline, target.Spline, 1f);
                        }
                    }
                }
            }

            void NodeAnimation(LaidOutGraph next)
            {
                phase2AnimationWatchDog.Await(next.Graph.NodeCount);
                // Draw all nodes of next graph.
                if (ignoreInnerNodes)
                {
                    // FIXME: The root could be a leaf.
                    next.Graph.Traverse(IgnoreNode, IgnoreNode, RenderNode);
                }
                else
                {
                    next.Graph.Traverse(RenderNode, RenderNode, RenderNode);
                }
            }

            /// Note: <see cref="OnAnimationsFinished"/> will be called by <see cref="phase2AnimationWatchDog"/>
            /// when phase 2 has completed.
        }

        /// <summary>
        /// Watchdog triggering <see cref="Phase2AddNewAndExistingGraphElements"/> when phase 1 has been
        /// completed, in which the necessary nodes and edges are deleted.
        /// </summary>
        private Phase1AnimationWatchDog phase1AnimationWatchDog;

        /// <summary>
        /// Watchdog triggering <see cref="OnAnimationsFinished"/> when phase 2 has been
        /// completed, in which the nodes and edges of the next graph to be shown are drawn.
        /// </summary>
        private Phase2AnimationWatchDog phase2AnimationWatchDog;

        /// <summary>
        /// Updates the hierarchy of game nodes so that it is isomorphic to the node
        /// hierarchy of the underlying graph.
        /// </summary>
        private void UpdateGameNodeHierarchy()
        {
            Dictionary<Node, GameObject> nodeMap = new();
            CollectNodes(gameObject, nodeMap);
            // Check(nodeMap);
            GraphRenderer.CreateGameNodeHierarchy(nodeMap, gameObject);
        }

        /// <summary>
        /// Checks whether all graph nodes and game nodes in <paramref name="nodeMap"/> are
        /// members of the same graph. Emits warnings and asserts that they are all in
        /// the same graph.
        /// Used only for debugging.
        /// </summary>
        /// <param name="nodeMap">mapping of graph nodes onto their corresponding game nodes</param>
        private static void Check(Dictionary<Node, GameObject> nodeMap)
        {
            HashSet<Graph> graphs = new();

            foreach (GameObject go in nodeMap.Values)
            {
                graphs.Add(go.GetNode().ItsGraph);
            }
            foreach (Node node in nodeMap.Keys)
            {
                graphs.Add(node.ItsGraph);
            }
            if (graphs.Count > 1)
            {
                Debug.LogError("There are nodes from different graphs in the same game-node hierarchy!\n");
                foreach (GameObject go in nodeMap.Values)
                {
                    Node node = go.GetNode();
                    Debug.LogWarning($"Node {node.ID} contained in graph {node.ItsGraph.Name} from file {node.ItsGraph.Path}\n");
                }
            }
            Assert.AreEqual(1, graphs.Count);
        }

        /// <summary>
        /// Collects all graph nodes and their corresponding game nodes that are (transitive)
        /// descendants of <paramref name="root"/>. The result is added to <paramref name="nodeMap"/>,
        /// where the <paramref name="root"/> itself will not be added.
        /// </summary>
        /// <param name="root">root of the game-node hierarchy whose hierarchy members are to be collected</param>
        /// <param name="nodeMap">the mapping of graph nodes onto their corresponding game nodes</param>
        /// <exception cref="Exception">thrown if a game node has no valid node reference</exception>
        private static void CollectNodes(GameObject root, IDictionary<Node, GameObject> nodeMap)
        {
            if (root != null)
            {
                foreach (Transform childTransform in root.transform)
                {
                    GameObject child = childTransform.gameObject;
                    /// If a game node was deleted, it was marked inactive in
                    /// <see cref="OnRemoveFinishedAnimation"/>. We need to ignore such
                    /// game nodes.
                    if (child.activeInHierarchy && child.CompareTag(Tags.Node))
                    {
                        if (child.TryGetNodeRef(out NodeRef nodeRef))
                        {
                            nodeMap[nodeRef.Value] = child;
                            CollectNodes(child, nodeMap);
                        }
                        else
                        {
                            throw new Exception($"Game node {child.name} without valid node reference.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event function triggered when all animations are finished. Animates the transition of the edges
        /// and renders all edges as new and notifies everyone that the animation is finished.
        ///
        /// Note: This method is a callback called from the animation framework (DoTween). It is
        /// passed to this animation framework in <see cref="RenderGraph"/>.
        /// </summary>
        private void OnAnimationsFinished()
        {
            NodeChangesBuffer nodeChangesBuffer = NodeChangesBuffer.GetSingleton();
            nodeChangesBuffer.currentRevisionCounter = CurrentGraphIndex;
            nodeChangesBuffer.addedNodeIDsCache = new List<string>(nodeChangesBuffer.addedNodeIDs);
            nodeChangesBuffer.addedNodeIDs.Clear();
            nodeChangesBuffer.changedNodeIDsCache = new List<string>(nodeChangesBuffer.changedNodeIDs);
            nodeChangesBuffer.changedNodeIDs.Clear();
            nodeChangesBuffer.removedNodeIDsCache = new List<string>(nodeChangesBuffer.removedNodeIDs);
            nodeChangesBuffer.removedNodeIDs.Clear();

            UpdateGameNodeHierarchy();
            RenderPlane();

            IsStillAnimating = false;
            AnimationFinishedEvent.Invoke();
        }

        /// <summary>
        /// Is called by Constructor the register all given <paramref name="animators"/>,
        /// so they can be updated accordingly.
        /// </summary>
        /// <param name="animators">list of animators to be informed</param>
        private void RegisterAllAnimators(IList<AbstractAnimator> animators)
        {
            animators.Add(changeAndBirthAnimator);
            animators.Add(moveAnimator);
        }

        /// <summary>
        /// Renders a plane enclosing all game objects of the currently shown graph.
        /// </summary>
        private void RenderPlane()
        {
            bool isPlaneNew = !objectManager.GetPlane(out GameObject plane);
            if (!isPlaneNew)
            {
                // We are re-using the existing plane, hence, we animate its change
                // (new position and new scale).
                objectManager.GetPlaneTransform(out Vector3 centerPosition, out Vector3 scale);
                Tweens.Scale(plane, scale, moveAnimator.MaxAnimationTime / 2);
                Tweens.Move(plane, centerPosition, moveAnimator.MaxAnimationTime / 2);
            }
        }

        /// <summary>
        /// Checks whether two edges are equal.
        /// </summary>
        /// <param name="left">First edge to be checked</param>
        /// <param name="right">Second edge to be checked</param>
        /// <returns>true if both edges are equal</returns>
        private static bool AreEqualGameEdges(GameObject left, GameObject right)
        {
            return left.TryGetComponent(out EdgeRef leftEdgeRef)
                && right.TryGetComponent(out EdgeRef rightEdgeRef)
                && leftEdgeRef.Value.ID == rightEdgeRef.Value.ID;
        }

        /// <summary>
        /// Returns true if the x and z co-ordindates of the two vectors are approximately equal.
        /// </summary>
        /// <param name="v1">First vector</param>
        /// <param name="v2">Second vector</param>
        /// <returns>true if the x and z co-ordindates of the two vectors are approximately equal</returns>
        private bool XZAreEqual(Vector3 v1, Vector3 v2)
        {
            double x1, z1, x2, z2;

            x1 = Math.Round(v1.x, 2);
            z1 = Math.Round(v1.z, 2);
            x2 = Math.Round(v2.x, 2);
            z2 = Math.Round(v2.z, 2);

            return x1 == x2 && z1 == z2;
        }

        /// <summary>
        /// Ignores the given <paramref name="node"/> in rendering. This method can
        /// be used if inner or leaf nodes are to be ignored (e.g., for non-hierarchical
        /// layouts).
        /// </summary>
        /// <param name="node">node to be displayed</param>
        private void IgnoreNode(Node node)
        {
            phase2AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Renders the game object corresponding to the given <paramref name="graphNode"/>.
        /// </summary>
        /// <param name="graphNode">graph node to be displayed</param>
        private void RenderNode(Node graphNode)
        {
            // The layout to be applied to graphNode
            ILayoutNode layoutNode = NextLayoutToBeShown[graphNode.ID];
            // The game node representing the graphNode if there is any; null if there is none
            Node formerGraphNode = objectManager.GetNode(graphNode, out GameObject currentGameNode);
            Assert.IsTrue(currentGameNode.HasNodeRef());
            Debug.Log($"[RenderNode] {graphNode.ID} is new {formerGraphNode == null}: position={layoutNode.CenterPosition} scale={layoutNode.AbsoluteScale}\n");

            Difference difference;
            if (formerGraphNode == null)
            {
                Debug.Log($"[RenderNode] {graphNode.ID} is new: position={layoutNode.CenterPosition} scale={layoutNode.AbsoluteScale}\n");
                // The node is new. It has no layout applied to it yet.
                // If the node is new, we animate it by moving it out of the ground.
                // Note: layoutNode.position.y denotes the ground position of
                // a game object, not its center.
                Vector3 position = layoutNode.CenterPosition;
                position.y -= layoutNode.AbsoluteScale.y;
                layoutNode.CenterPosition = position;

                // Revert the change to the y co-ordindate.
                position.y += layoutNode.AbsoluteScale.y;
                layoutNode.CenterPosition = position;
                difference = Difference.Added;

                // Set the layout for the copied node.
                currentGameNode.SetAbsoluteScale(layoutNode.AbsoluteScale, animate: false);
                currentGameNode.transform.position = layoutNode.CenterPosition;
            }
            else
            {
                // Node existed before.
                if (diff.AreDifferent(formerGraphNode, graphNode))
                {
                    difference = Difference.Changed;
                }
                else
                {
                    difference = Difference.None;
                }
            }
            switch (difference)
            {
                case Difference.Changed:
                    NodeChangesBuffer.GetSingleton().changedNodeIDs.Add(currentGameNode.name);
                    markerFactory.MarkChanged(currentGameNode);
                    // There is a change. It may or may not be the metric determining the style.
                    // We will not further check that and just call the following method.
                    // If there is no change, this method does not need to be called because then
                    // we know that the metric values determining the style and antenna of the former
                    // and the new graph node are the same.
                    graphRenderer.AdjustStyle(currentGameNode);
                    break;
                case Difference.Added:
                    NodeChangesBuffer.GetSingleton().addedNodeIDs.Add(currentGameNode.name);
                    markerFactory.MarkBorn(currentGameNode);
                    break;
            }
            // We want the animator to move each node separately, which is why we
            // remove each from the hierarchy; later the node hierarchy will be
            // re-established. It still needs to be a child of the code city,
            // however, because methods called in the course of the animation
            // will try to retrieve the code city from the game node.
            currentGameNode.transform.SetParent(gameObject.transform);

            // currentGameNode is shifted to its new position through the animator.
            Action<float> onEdgeAnimationStart = null;
            if (edgeTweens.TryGetValue(graphNode, out Tween tween))
            {
                Debug.Log($"onEdgeAnimationStart set for {graphNode.ID} {currentGameNode.name}\n");
                onEdgeAnimationStart = duration => OnEdgeAnimationStart(tween, duration);
            }
            Debug.Log($"Move {currentGameNode.name} from {currentGameNode.transform.position} to {layoutNode.CenterPosition}.\n");
            changeAndBirthAnimator.AnimateTo(gameObject: currentGameNode,
                                             layoutNode: layoutNode,
                                             callbackWhenAnimationFinished: OnAnimationNodeAnimationFinished,
                                             moveCallback: onEdgeAnimationStart);
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// It will be called as a callback after the animation of a node or edge to be
        /// removed has been finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed (can be a node or edge)</param>
        private void OnRemoveFinishedAnimation(object gameObject)
        {
            GameObject go = gameObject as GameObject;
            Destroyer.Destroy(go);
            phase1AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Event function that adds the given <paramref name="gameNode"/>
        /// to <see cref="gameObject"/> as a child if <paramref name="gameNode"/>
        /// is a <see cref="GameObject"/> and has no parent yet. Informs
        /// <see cref="phase2AnimationWatchDog"/> that this animation has been finished.
        /// Called as a callback when the animation of new and existing
        /// nodes is finished. <see cref="RenderNode(Node)"/>.
        /// </summary>
        /// <param name="gameNode">new or existing game object representing a graph node</param>
        private void OnAnimationNodeAnimationFinished(object gameNode)
        {
            if (gameNode is GameObject go)
            {
                graphRenderer.AdjustAntenna(go);
                markerFactory.AdjustMarkerY(go);

                if (go.transform.parent == null)
                {
                    /// We will just put this game object under <see cref="gameObject"/>
                    /// (the game object representing the city as a whole) as a child. When
                    /// the animation is over and all nodes have reached their destination,
                    /// <see cref="UpdateGameNodeHierarchy"/> will put this node to its
                    /// actual logical game-node parent.
                    go.transform.SetParent(gameObject.transform);
                }
            }
            phase2AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Starts the animation of <paramref name="animatorTween"/> with given <paramref name="duration"/>.
        /// </summary>
        /// <param name="animatorTween">Animator tween to start</param>
        /// <param name="duration">Duration of the animation</param>
        private void OnEdgeAnimationStart(Tween animatorTween, float duration)
        {
            Debug.Log($"OnEdgeAnimationStart {animatorTween != null}\n");
            if (animatorTween != null)
            {
                // We previously set the duration to 1 second (i.e., setDuration = 1)
                // and now want to change it to `duration` (henceforth actualDuration):
                // actualDuration = setDuration / timeScale
                // <=> actualDuration = 1 / timeScale
                // <=> timeScale = 1 / actualDuration
                animatorTween.timeScale = 1.0f / duration;
                animatorTween.PlayForward();
            }
        }

        /// <summary>
        /// Destroys <paramref name="gameObject"/> if it is an instance of <see cref="GameObject"/>.
        /// The ancestors of <paramref name="gameObject"/> will not be destroyed; their parent will
        /// just be set to null.
        /// </summary>
        /// <param name="gameObject">object to be destroyed</param>
        private static void DestroyGameObject(object gameObject)
        {
            if (gameObject is GameObject go)
            {
                foreach (Transform child in go.transform)
                {
                    if (child.CompareTag(Tags.Node))
                    {
                        child.SetParent(null);
                    }
                }
                Destroyer.Destroy(go);
            }
        }

        /// <summary>
        /// Removes the given node. The removal is animated by raising the
        /// node to <see cref="SkyLevel"/> (y dimension). The node is not
        /// destroyed when the animation has finished.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        private void RenderRemovedNode(Node node)
        {
            if (objectManager.RemoveNode(node, out GameObject nodeObject))
            {
                Assert.IsNotNull(nodeObject);
                markerFactory.MarkDead(nodeObject);
                AnimateToDeath(nodeObject);
                // AnimateToDeath() will call phase1AnimationWatchDog.Finished();
                // hence, we must not call it here.
            }
            else
            {
                phase1AnimationWatchDog.Finished();
            }
        }

        /// <summary>
        /// Triggers an animation indicating that the given <paramref name="gameObject"/> was
        /// deleted by moving it up to the sky. When it reaches the sky at <see cref="SkyLevel"/>,
        /// it will disappear (via <see cref="OnRemoveFinishedAnimation"/>).
        /// </summary>
        /// <param name="gameObject">a game object representing either a node or edge</param>
        private void AnimateToDeath(GameObject gameObject)
        {
            gameObject.transform.SetParent(null);
            /// Let it raise to <see cref="SkyLevel"/>.
            Vector3 newPosition = gameObject.transform.position;
            newPosition.y = SkyLevel;
            ILayoutNode nodeTransform = new AnimationNode(newPosition, gameObject.transform.localScale);
            moveAnimator.AnimateTo(gameObject, nodeTransform, OnRemoveFinishedAnimation);
        }

        /// <summary>
        /// Removes the given edge. The edge is not destroyed immediately, however.
        /// Its destruction is postponed by <see cref="AnimateToDeath(GameObject)"/>
        /// to the point in time when the animation is finished in which case
        /// <see cref="OnRemoveFinishedAnimation"/> will be called.
        /// </summary>
        /// <param name="edge">removed edge</param>
        private void RenderRemovedEdge(Edge edge)
        {
            if (edgesAreDrawn && objectManager.RemoveEdge(edge, out GameObject edgeObject))
            {
                AnimateToDeath(edgeObject);
                // AnimateToDeath() will call phase1AnimationWatchDog.Finished();
                // hence, we must not call it here.
            }
            else
            {
                phase1AnimationWatchDog.Finished();
            }
        }

        // **********************************************************************

        /// <summary>
        /// The series of underlying graphs to be rendered.
        /// </summary>
        private List<Graph> graphs;  // not serialized by Unity

        /// <summary>
        /// Updates the base path of all graphs.
        /// </summary>
        /// <param name="basePath">the new base path to be set</param>
        internal void ProjectPathChanged(string basePath)
        {
            if (graphs != null)
            {
                foreach (Graph graph in graphs)
                {
                    graph.BasePath = basePath;
                }
            }
        }

        /// <summary>
        /// The number of graphs of the graph series to be rendered.
        /// </summary>
        public int GraphCount => graphs.Count;

        /// <summary>
        /// Current graph of the graph series to be rendered.
        /// </summary>
        public Graph GraphCurrent => graphs[currentGraphIndex];

        /// <summary>
        /// The index of the currently visualized graph.
        /// </summary>
        private int currentGraphIndex = 0;  // not serialized by Unity

        /// <summary>
        /// Returns the index of the currently shown graph.
        /// </summary>
        public int CurrentGraphIndex
        {
            get => currentGraphIndex;
            private set
            {
                currentGraphIndex = value;
                shownGraphHasChangedEvent.Invoke();
            }
        }

        /// <summary>
        /// An event fired when the view graph has changed.
        /// </summary>
        private readonly UnityEvent shownGraphHasChangedEvent = new();

        /// <summary>
        /// Whether the user has selected auto-play mode.
        /// </summary>
        private bool isAutoplay;  // not serialized by Unity

        /// <summary>
        /// Whether the user has selected reverse auto-play mode.
        /// </summary>
        private bool isAutoplayReverse;  // not serialized by Unity

        /// <summary>
        /// Returns true if automatic animations are active.
        /// </summary>
        public bool IsAutoPlay
        {
            get => isAutoplay;
            private set
            {
                shownGraphHasChangedEvent.Invoke();
                isAutoplay = value;
            }
        }

        /// <summary>
        /// Returns true if automatic reverse animations are active.
        /// </summary>
        public bool IsAutoPlayReverse
        {
            get => isAutoplayReverse;
            private set
            {
                shownGraphHasChangedEvent.Invoke();
                isAutoplayReverse = value;
            }
        }

        /// <summary>
        /// Initiates the visualization of the evolving series of graphs
        /// provided earlier by <see cref="SetGraphEvolution(List{Graph})"/>
        /// (the latter function must have been called before).
        /// </summary>
        public void ShowGraphEvolution()
        {
            CurrentGraphIndex = 0;
            currentCity = null;
            nextCity = null;

            CalculateAllGraphLayouts(graphs);

            shownGraphHasChangedEvent.Invoke();

            if (HasCurrentLaidOutGraph(out LaidOutGraph loadedGraph))
            {
                DisplayGraphAsNew(loadedGraph);
            }
            else
            {
                Debug.LogError("Evolution renderer could not show the initial graph.\n");
            }
        }

        /// <summary>
        /// If animations are still ongoing, auto-play mode is turned on, or <paramref name="index"/>
        /// does not denote a valid index in the graph series, false is returned and nothing else
        /// happens. Otherwise the graph with the given index in the graph series becomes the new
        /// currently shown graph.
        /// </summary>
        /// <param name="index">index of the graph to be shown in the graph series</param>
        /// <returns>true if that graph could be shown successfully</returns>
        public bool TryShowSpecificGraph(int index)
        {
            if (IsStillAnimating)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.\n");
                return false;
            }
            if (IsAutoPlay || IsAutoPlayReverse)
            {
                Debug.Log("Auto-play mode is turned on. You cannot move to the next graph manually.\n");
                return false;
            }
            if (index < 0 || index >= GraphCount)
            {
                Debug.LogError($"The value {index} is no valid index.\n");
                return false;
            }
            if (HasCurrentLaidOutGraph(out LaidOutGraph loadedGraph) && HasLaidOutGraph(index, out LaidOutGraph newGraph))
            {
                CurrentGraphIndex = index;
                TransitionToNextGraph(loadedGraph, newGraph);
                return true;
            }
            else
            {
                Debug.LogError($"Could not retrieve a layout for graph with index {index}.\n");
                return false;
            }
        }

        /// <summary>
        /// Returns true and a LoadedGraph if there is a LoadedGraph for the active graph index
        /// CurrentGraphIndex.
        /// </summary>
        /// <param name="loadedGraph"></param>
        /// <returns>true if there is graph to be visualized (index _openGraphIndex)</returns>
        private bool HasCurrentLaidOutGraph(out LaidOutGraph loadedGraph)
        {
            return HasLaidOutGraph(CurrentGraphIndex, out loadedGraph);
        }

        /// <summary>
        /// Returns true and a LaidOutGraph if there is a LaidOutGraph for the given graph index.
        /// </summary>
        /// <param name="index">index of the requested graph</param>
        /// <param name="laidOutGraph">the resulting graph with given index; defined only if this method returns true</param>
        /// <returns>true iff there is a graph at the given index</returns>
        private bool HasLaidOutGraph(int index, out LaidOutGraph laidOutGraph)
        {
            laidOutGraph = null;
            Graph graph = graphs[index];
            if (graph == null)
            {
                Debug.LogError($"There is no graph available for graph with index {index}.\n");
                return false;
            }
            Dictionary<string, ILayoutNode> nodeLayout = NodeLayouts[index];

            if (edgesAreDrawn)
            {
                Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout = EdgeLayouts[index];
                laidOutGraph = new LaidOutGraph(graph, nodeLayout, edgeLayout);
            }
            else
            {
                laidOutGraph = new LaidOutGraph(graph, nodeLayout, null);
            }
            return true;
        }

        /// <summary>
        /// If animation is still ongoing, auto-play mode is turned on, or we are at
        /// the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct successor graph in the graph series.
        /// </summary>
        public void ShowNextGraph()
        {
            if (IsStillAnimating)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.\n");
                return;
            }
            if (IsAutoPlay || IsAutoPlayReverse)
            {
                Debug.Log("Auto-play mode is turned on. You cannot move to the next graph manually.\n");
                return;
            }
            if (!ShowNextIfPossible())
            {
                Debug.Log("This is already the last graph revision.\n");
            }
        }

        /// <summary>
        /// If we are at the end of the graph series, false is returned and nothing else happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly.
        /// </summary>
        /// <returns>true iff we are not at the end of the graph series</returns>
        private bool ShowNextIfPossible()
        {
            if (CurrentGraphIndex == graphs.Count - 1)
            {
                return false;
            }
            CurrentGraphIndex++;

            if (HasCurrentLaidOutGraph(out LaidOutGraph newlyShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex - 1, out LaidOutGraph currentlyShownGraph))
            {
                NodeChangesBuffer.GetSingleton().revisionChanged = true;
                // Note: newlyShownGraph is the very next future of currentlyShownGraph
                TransitionToNextGraph(currentlyShownGraph, newlyShownGraph);
            }
            else
            {
                Debug.LogError("Could not retrieve a layout for the graph.\n");
            }
            return true;
        }

        /// <summary>
        /// If we are at the beginning of the graph series, false is returned and nothing else happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct predecessor graph in the graph series. CurrentGraphIndex is decreased
        /// by one accordingly.
        /// </summary>
        /// <returns>true iff we are not at the beginning of the graph series</returns>
        private bool ShowPreviousIfPossible()
        {
            if (CurrentGraphIndex == 0)
            {
                Debug.Log("This is already the first graph revision.\n");
                return false;
            }
            CurrentGraphIndex--;

            if (HasCurrentLaidOutGraph(out LaidOutGraph newlyShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex + 1, out LaidOutGraph currentlyShownGraph))
            {
                NodeChangesBuffer.GetSingleton().revisionChanged = true;
                // Note: newlyShownGraph is the most recent past of currentlyShownGraph
                TransitionToNextGraph(currentlyShownGraph, newlyShownGraph);
            }
            else
            {
                Debug.LogError("Could not retrieve a graph layout.\n");
            }
            return true;
        }

        /// <summary>
        /// If we are at the beginning of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct predecessor graph in the graph series. CurrentGraphIndex is decreased
        /// by one accordingly.
        /// </summary>
        public void ShowPreviousGraph()
        {
            if (IsStillAnimating || IsAutoPlay || IsAutoPlayReverse)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.\n");
                return;
            }
            if (!ShowPreviousIfPossible())
            {
                Debug.Log("This is already the first graph revision.\n");
            }
        }

        /// <summary>
        /// Toggles the auto-play mode. Equivalent to: SetAutoPlay(!IsAutoPlay)
        /// where IsAutoPlay denotes the current state of the auto-play mode.
        /// </summary>
        internal void ToggleAutoPlay()
        {
            SetAutoPlay(!IsAutoPlay);
        }

        /// <summary>
        /// Toggles the reverse auto-play mode. Equivalent to: SetAutoPlayReverse(!IsAutoPlayReverse)
        /// where IsAutoPlayReverse denotes the current state of the reverse auto-play mode.
        /// </summary>
        internal void ToggleAutoPlayReverse()
        {
            SetAutoPlayReverse(!IsAutoPlayReverse);
        }


        /// <summary>
        /// Sets auto-play mode to <paramref name="enabled"/>. If <paramref name="enabled"/>
        /// is true, the next graph in the series is shown and from there all other
        /// following graphs until we reach the end of the graph series or auto-play
        /// mode is turned off again. If <paramref name="enabled"/> is false instead,
        /// the currently shown graph remains visible.
        /// </summary>
        /// <param name="enabled"> Specifies whether reverse auto-play mode should be enabled. </param>
        internal void SetAutoPlay(bool enabled)
        {
            IsAutoPlay = enabled;
            if (IsAutoPlay)
            {
                AnimationFinishedEvent.AddListener(OnAutoPlayCanContinue);
                if (!ShowNextIfPossible())
                {
                    Debug.Log("This is already the last graph revision.\n");
                }
            }
            else
            {
                AnimationFinishedEvent.RemoveListener(OnAutoPlayCanContinue);
            }
            shownGraphHasChangedEvent.Invoke();
        }

        /// <summary>
        /// Sets reverse auto-play mode to <paramref name="enabled"/>. If <paramref name="enabled"/>
        /// is true, the previous graph in the series is shown and from there all other
        /// previous graphs until we reach the beginning of the graph series or reverse auto-play
        /// mode is turned off again. If <paramref name="enabled"/> is false instead,
        /// the currently shown graph remains visible.
        /// </summary>
        /// <param name="enabled"> Specifies whether reverse auto-play mode should be enabled. </param>
        private void SetAutoPlayReverse(bool enabled)
        {
            IsAutoPlayReverse = enabled;
            if (IsAutoPlayReverse)
            {
                AnimationFinishedEvent.AddListener(OnAutoPlayReverseCanContinue);
                if (!ShowPreviousIfPossible())
                {
                    Debug.Log("This is already the first graph revision.\n");
                }
            }
            else
            {
                AnimationFinishedEvent.RemoveListener(OnAutoPlayReverseCanContinue);
            }
            shownGraphHasChangedEvent.Invoke();
        }

        /// <summary>
        /// If we at the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its next
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly and auto-play mode is toggled (switched off actually).
        /// </summary>
        private void OnAutoPlayCanContinue()
        {
            if (!ShowNextIfPossible())
            {
                ToggleAutoPlay();
            }
        }

        /// <summary>
        /// If we are at the beginning of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its next
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly and auto-play mode is toggled (switched off actually).
        /// </summary>
        private void OnAutoPlayReverseCanContinue()
        {
            if (!ShowPreviousIfPossible())
            {
                ToggleAutoPlayReverse();
            }
        }

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph currently shown, that is, there is at least one node in the graph
        /// that has this metric.
        ///
        /// The metric names are derived from the graph currently drawn by the
        /// evolution renderer.
        /// If no graph has been loaded yet, the empty list will be returned.
        /// </summary>
        /// <returns>names of all existing node metrics</returns>
        internal ISet<string> AllExistingMetrics()
        {
            if (currentCity == null || currentCity.Graph == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return currentCity.Graph.AllNumericNodeAttributes();
            }
        }

        /// <summary>
        /// Yields a graph renderer that can draw this city.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public GraphRenderer Renderer => graphRenderer;

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
        /// <remarks>Implements <see cref="IGraphRenderer.DrawEdge(GameObject, GameObject, string, Edge)"/>.</remarks>
        public GameObject DrawEdge(GameObject source, GameObject target, string edgeType)
        {
            return graphRenderer.DrawEdge(source, target, edgeType);
        }

        /// <summary>
        /// Creates and returns a new game object for representing the given <paramref name="node"/>.
        /// The <paramref name="node"/> is attached to that new game object via a NodeRef component.
        /// LOD is added and the resulting node is prepared for interaction.
        /// </summary>
        /// <param name="node">graph node to be represented</param>
        /// <param name="city">the game object representing the city in which to draw this node;
        /// it has the information about how to draw the node and portal of the city</param>
        /// <returns>game object representing given <paramref name="node"/></returns>
        /// <remarks>Implements <see cref="IGraphRenderer.DrawNode(Node, GameObject)"/>.</remarks>
        public GameObject DrawNode(Node node, GameObject city = null)
        {
            return graphRenderer.DrawNode(node, city);
        }

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
        /// <remarks>Implements <see cref="IGraphRenderer.LayoutEdges(ICollection{GameObject})"/>.</remarks>
        public IDictionary<string, ILayoutEdge<ILayoutNode>> LayoutEdges(ICollection<GameObject> gameEdges)
        {
            return graphRenderer.LayoutEdges(gameEdges);
        }

        /// <summary>
        /// An implementation of ILayoutNode that is used for animation purposes only.
        /// The only features it supports are the position and scale of node.
        /// That is what is currently needed by the animators.
        /// </summary>
        private class AnimationNode : ILayoutNode
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="centerPosition">position of the center in world space</param>
            /// <param name="scale">local scale</param>
            public AnimationNode(Vector3 centerPosition, Vector3 scale)
            {
                this.CenterPosition = centerPosition;
                this.LocalScale = scale;
            }

            public Vector3 LocalScale { get; set; }

            public Vector3 AbsoluteScale => LocalScale;

            public Vector3 CenterPosition { get; set; }

            public void ScaleBy(float factor)
            {
                throw new NotImplementedException();
            }

            public ILayoutNode Parent => throw new NotImplementedException();

            public int Level { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public bool IsLeaf => throw new NotImplementedException();

            public string ID => throw new NotImplementedException();

            public float Rotation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Vector3 Roof => throw new NotImplementedException();

            public Vector3 Ground => throw new NotImplementedException();

            public ICollection<ILayoutNode> Successors => throw new NotImplementedException();

            public GameObject gameObject => throw new NotImplementedException();

            public Vector3 RelativePosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public bool IsSublayoutNode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public bool IsSublayoutRoot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Sublayout Sublayout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ILayoutNode SublayoutRoot { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public ICollection<ILayoutNode> Children()
            {
                throw new NotImplementedException();
            }

            public void SetOrigin()
            {
                throw new NotImplementedException();
            }

            public void SetRelative(ILayoutNode node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
