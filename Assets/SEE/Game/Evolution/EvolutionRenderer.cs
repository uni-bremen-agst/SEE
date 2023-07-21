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

using SEE.DataModel.DG;
using SEE.Game.Charts;
using SEE.Game.City;
using SEE.Game.UI.Notification;
using SEE.GO;
using SEE.Layout;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.Game.Evolution
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
        /// Watchdog triggering the next animation phase when the previous phase has been
        /// completed, that is, if all awaited events have occurred.
        /// </summary>
        private CountingJoin animationWatchDog;

        /// <summary>
        /// The series of underlying graphs to be rendered.
        /// </summary>
        private IList<Graph> graphs;

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
        private int currentGraphIndex = 0;

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
        /// True if edges are actually drawn, that is, if the user has selected an
        /// edge layout different from <see cref="EdgeLayoutKind.None"/>.
        /// </summary>
        private bool edgesAreDrawn = false;

        /// <summary>
        /// The y co-ordinate in world space where to move deleted nodes to and to move
        /// added nodes from. Deleted nodes will be lifted to this level and then disappear.
        /// Added nodes will appear there and then be moved to their location in the code
        /// city.
        /// </summary>
        private const float SkyLevel = 2.0f;

        /// <summary>
        /// The manager of the game objects created for the city.
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the graphRenderer, which in turn depends upon the city, which is set by
        /// this setter.
        /// </summary>
        private ObjectManager objectManager;

        /// <summary>
        /// The marker factory used to mark the new and removed game objects.
        /// </summary>
        private MarkerFactory markerFactory;

        /// <summary>
        /// An event fired when the view graph has changed.
        /// </summary>
        private readonly UnityEvent shownGraphHasChangedEvent = new();

        /// <summary>
        /// Registers <paramref name="action"/> to be called back when the shown
        /// graph has changed.
        /// </summary>
        /// <param name="action">action to be called back</param>
        internal void RegisterOnNewGraph(UnityAction action)
        {
            shownGraphHasChangedEvent.AddListener(action);
        }

        /// <summary>
        /// An event fired upon the end of an animation.
        /// </summary>
        private readonly UnityEvent AnimationFinishedEvent = new();

        /// <summary>
        /// True if animation is still ongoing.
        /// </summary>
        public bool IsStillAnimating { get; private set; }

        /// <summary>
        /// The default factor for the complete graph transition animation.
        /// </summary>
        private const float DefaultAnimationFactor = 1.0f;

        /// <summary>
        /// A factor controlling the speed of an animation (higher = slower). This value can be controlled by the user.
        /// </summary>
        [FormerlySerializedAs("animationDuration")]
        [SerializeField]
        private float animationFactor = DefaultAnimationFactor;

        /// <summary>
        /// The animation factor for showing a single graph revision during auto-play.
        /// </summary>
        public float AnimationLagFactor
        {
            get => animationFactor;
            set
            {
                if (value >= 0)
                {
                    animationFactor = value;
                    shownGraphHasChangedEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// The transition from the current graph to the next one is organized in
        /// the following phases:
        ///
        /// (1) Remove deleted nodes and edges from the scene.
        /// (2) Move existing nodes and edges to their new position in the scene.
        /// (3) Adjust existing changed nodes and edges in the scene.
        /// (4) Add newly created nodes to the scene.
        /// (5) Add newly created edges to the scene.
        ///
        /// This constant is the number of phases.
        /// </summary>
        private const int NumberOfPhases = 5;  // TODO: Do we still need this constant?

        /// <summary>
        /// The city (graph + layout) currently shown.
        /// </summary>
        private LaidOutGraph currentCity;  // not serialized by Unity

        /// <summary>
        /// The city (graph + layout) to be shown next.
        /// </summary>
        private LaidOutGraph nextCity;

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
        private bool ignoreInnerNodes = true;

        #region User Messages

        /// <summary>
        /// The title of user notifications used here.
        /// </summary>
        private const string NotificationTitle = "City Evolution";

        /// <summary>
        /// Informs the user that graph transition is currently blocked.
        /// </summary>
        private static void UserInfoGraphTransitionIsBlocked()
        {
            ShowNotification.Info(NotificationTitle, "Graph transition is blocked while animations are running.");
        }

        /// <summary>
        /// Informs the user that the renderer is currently occupied with an animation.
        /// </summary>
        private static void UserInfoStillOccupied()
        {
            ShowNotification.Info(NotificationTitle, "The renderer is already occupied with animating, wait until animations are finished.");
        }

        /// <summary>
        /// Informs the user that we already reached the begin of the graph series.
        /// </summary>
        private static void UserInfoFirstGraph()
        {
            ShowNotification.Info(NotificationTitle, "This is already the first graph revision.");
        }

        /// <summary>
        /// Informs the user that we already reached the end of the graph series.
        /// </summary>
        private static void UserInfoLastGraph()
        {
            ShowNotification.Info(NotificationTitle, "This is already the last graph revision.");
        }

        /// <summary>
        /// Informs the user that auto play is on.
        /// </summary>
        private static void UserInfoAutoPlayIsOn()
        {
            ShowNotification.Info(NotificationTitle, "Auto-play mode is turned on. You cannot move to the next graph manually.");
        }

        /// <summary>
        /// Informs the user about an error when attempting to load a layout.
        /// </summary>
        private static Notification UserInfoNoLayout()
        {
            return ShowNotification.Error(NotificationTitle, "Could not retrieve a layout for the graph.");
        }

        #endregion

        /// <summary>
        /// Sets the evolving series of <paramref name="graphs"/> to be visualized.
        /// The actual visualization is triggered by <see cref="ShowGraphEvolution"/>
        /// that can be called next.
        /// This method is expected to be called before attemption to draw any graph.
        /// </summary>
        /// <param name="graphs">series of graphs to be visualized</param>
        public void SetGraphEvolution(IList<Graph> graphs)
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
                Renderer = new GraphRenderer(cityEvolution, graphs);
                edgesAreDrawn = Renderer.AreEdgesDrawn();

                objectManager = new ObjectManager(Renderer, gameObject);
                markerFactory = new MarkerFactory(markerWidth: cityEvolution.MarkerWidth,
                                    markerHeight: cityEvolution.MarkerHeight,
                                    additionColor: cityEvolution.AdditionBeamColor,
                                    changeColor: cityEvolution.ChangeBeamColor,
                                    deletionColor: cityEvolution.DeletionBeamColor);
                animationWatchDog = new CountingJoin();
            }
            else
            {
                Debug.LogError($"This EvolutionRenderer attached to {name} has no sibling component of type {nameof(SEECityEvolution)}.\n");
                enabled = false;
            }
            Renderer.SetScaler(graphs);
        }

        /// <summary>
        /// Set of added nodes from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Node> addedNodes;
        /// <summary>
        /// Set of removed nodes from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Node> removedNodes;
        /// <summary>
        /// Set of changed nodes from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> changedNodes;
        /// <summary>
        /// Set of equal nodes (i.e., nodes without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is a node in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Node> equalNodes;

        /// <summary>
        /// Set of added edges from the current to the next graph.
        /// They are contained in the next graph.
        /// </summary>
        private ISet<Edge> addedEdges;
        /// <summary>
        /// Set of removed edges from the current to the next graph.
        /// They are contained in the current graph.
        /// </summary>
        private ISet<Edge> removedEdges;
        /// <summary>
        /// Set of changed edges from the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> changedEdges;
        /// <summary>
        /// Set of equal edges (i.e., edges without any changes) from
        /// the current to the next graph.
        /// They are contained in the next graph (and logically also
        /// in the current graph, that is, there is an edge in the
        /// current graph that has the same ID).
        /// </summary>
        private ISet<Edge> equalEdges;

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
                UserInfoNoLayout();
            }
        }

        #region Transition between graphs in the series

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
                UserInfoGraphTransitionIsBlocked();
                return;
            }
            // The upfront calculation of the node layout for all graphs has filled
            // objectManager with game objects for those nodes. Likewise, when we jump
            // to a graph directly in the version history, the nodes of its predecessors
            // may still be contained in the scene and objectManager. We need to clean up
            // first.
            objectManager?.Clear();
            RenderGraph(currentCity, graph);
        }

        /// <summary>
        /// Starts the animations to transition from the <paramref name="current"/> graph
        /// to the <paramref name="next"/> graph.
        /// </summary>
        /// <param name="current">the currently shown graph</param>
        /// <param name="next">the next graph to be shown</param>
        private void TransitionToNextGraph(LaidOutGraph current, LaidOutGraph next)
        {
            current.AssertNotNull("current");
            next.AssertNotNull("next");
            if (IsStillAnimating)
            {
                UserInfoGraphTransitionIsBlocked();
                return;
            }
            RenderGraph(current, next);
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
                UserInfoStillOccupied();
                return false;
            }
            if (IsAutoPlay || IsAutoPlayReverse)
            {
                UserInfoAutoPlayIsOn();
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
        /// If animation is still ongoing, auto-play mode is turned on, or we are at
        /// the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its
        /// direct successor graph in the graph series.
        /// </summary>
        public void ShowNextGraph()
        {
            if (IsStillAnimating)
            {
                UserInfoStillOccupied();
                return;
            }
            if (IsAutoPlay || IsAutoPlayReverse)
            {
                UserInfoAutoPlayIsOn();
                return;
            }
            if (!ShowNextIfPossible())
            {
                UserInfoLastGraph();
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
                NodeChangesBuffer.Instance().RevisionChanged = true;
                // Note: newlyShownGraph is the very next future of currentlyShownGraph
                TransitionToNextGraph(currentlyShownGraph, newlyShownGraph);
            }
            else
            {
                UserInfoNoLayout();
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
                UserInfoFirstGraph();
                return false;
            }
            CurrentGraphIndex--;

            if (HasCurrentLaidOutGraph(out LaidOutGraph newlyShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex + 1, out LaidOutGraph currentlyShownGraph))
            {
                NodeChangesBuffer.Instance().RevisionChanged = true;
                // Note: newlyShownGraph is the most recent past of currentlyShownGraph
                TransitionToNextGraph(currentlyShownGraph, newlyShownGraph);
            }
            else
            {
                UserInfoNoLayout();
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
                UserInfoStillOccupied();
                return;
            }
            if (!ShowPreviousIfPossible())
            {
                UserInfoFirstGraph();
            }
        }

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

            Graph oldGraph = current?.Graph;
            Graph newGraph = next?.Graph;

            // Node comparison.
            newGraph.Diff(oldGraph,
                          g => g.Nodes(),
                          (g, id) => g.GetNode(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          nodeEqualityComparer,
                          out addedNodes,
                          out removedNodes,
                          out changedNodes,
                          out equalNodes);

            // Edge comparison.
            newGraph.Diff(oldGraph,
                          g => g.Edges(),
                          (g, id) => g.GetEdge(id),
                          GraphExtensions.AttributeDiff(newGraph, oldGraph),
                          edgeEqualityComparer,
                          out addedEdges,
                          out removedEdges,
                          out changedEdges,
                          out equalEdges);

            Phase1RemoveDeletedGraphElements(next);
        }

        /// <summary>
        /// Event function triggered when all animations are finished.
        /// Marks the new and changed nodes.
        /// Updates <see cref="NodeChangesBuffer.Instance()"/>.
        /// Restores the game node hierarchy.
        /// Renders the plane under the code city.
        ///
        /// Note: This method is a callback called from the animation framework (DoTween). It is
        /// passed to this animation framework in <see cref="RenderGraph"/>.
        /// </summary>
        private void OnAnimationsFinished()
        {
            Debug.Log("Animation cycle has finished.\n");
            MarkNodes();
            UpdateNodeChangeBuffer();
            UpdateGameNodeHierarchy();
            RenderPlane();

            IsStillAnimating = false;
            AnimationFinishedEvent.Invoke();

            // We have made the transition to the next graph.
            currentCity = nextCity;

            /// <summary>
            /// Updates the hierarchy of game nodes so that it is isomorphic to the node
            /// hierarchy of the underlying graph.
            /// </summary>
            void UpdateGameNodeHierarchy()
            {
                Dictionary<Node, GameObject> nodeMap = new();
                CollectNodes(gameObject, nodeMap);
                //Check(nodeMap);
                GraphRenderer.CreateGameNodeHierarchy(nodeMap, gameObject);

                /// <summary>
                /// Collects all graph nodes and their corresponding game nodes that are (transitive)
                /// descendants of <paramref name="root"/>. The result is added to <paramref name="nodeMap"/>,
                /// where the <paramref name="root"/> itself will not be added.
                /// </summary>
                /// <param name="root">root of the game-node hierarchy whose hierarchy members are to be collected</param>
                /// <param name="nodeMap">the mapping of graph nodes onto their corresponding game nodes</param>
                /// <exception cref="Exception">thrown if a game node has no valid node reference</exception>
                static void CollectNodes(GameObject root, IDictionary<Node, GameObject> nodeMap)
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
                /// Checks whether all graph nodes and game nodes in <paramref name="nodeMap"/> are
                /// members of the same graph. Emits warnings and asserts that they are all in
                /// the same graph.
                /// Used only for debugging.
                /// </summary>
                /// <param name="nodeMap">mapping of graph nodes onto their corresponding game nodes</param>
                static void Check(Dictionary<Node, GameObject> nodeMap)
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
            }

            /// <summary>
            /// Updates <see cref="NodeChangesBuffer"/> with the <see cref="addedNodes"/>,
            /// <see cref="changedNodes"/>, and <see cref="removedNodes"/>.
            /// </summary>
            void UpdateNodeChangeBuffer()
            {
                NodeChangesBuffer nodeChangesBuffer = NodeChangesBuffer.Instance();
                nodeChangesBuffer.CurrentRevisionCounter = CurrentGraphIndex;
                nodeChangesBuffer.AddedNodeIDsCache = new List<string>(addedNodes.Select(n => n.ID));
                nodeChangesBuffer.ChangedNodeIDsCache = new List<string>(changedNodes.Select(n => n.ID));
                nodeChangesBuffer.RemovedNodeIDsCache = new List<string>(removedNodes.Select(n => n.ID));
            }

            /// <summary>
            /// Markes all <see cref="addedNodes"/> and <see cref="changedNodes"/>.
            /// </summary>
            void MarkNodes()
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

            /// <summary>
            /// Renders a plane enclosing all game objects of the currently shown graph.
            /// </summary>
            void RenderPlane()
            {
                bool isPlaneNew = !objectManager.GetPlane(out GameObject plane);
                if (!isPlaneNew)
                {
                    // We are re-using the existing plane, hence, we animate its change
                    // (new position and new scale).
                    objectManager.GetPlaneTransform(out Vector3 centerPosition, out Vector3 scale);
                    Tweens.Scale(plane, scale, AnimationLagFactor);
                    Tweens.Move(plane, centerPosition, AnimationLagFactor / 2);
                }
            }
        }
        #endregion

        #region auto play mode

        /// <summary>
        /// Whether the user has selected auto-play mode.
        /// </summary>
        private bool isAutoplay;

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
        /// Whether the user has selected reverse auto-play mode.
        /// </summary>
        private bool isAutoplayReverse;

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
                    UserInfoLastGraph();
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
                    UserInfoFirstGraph();
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

        #endregion

        /// <summary>
        /// Yields a graph renderer that can draw code cities for the each graph of
        /// the graph series.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public GraphRenderer Renderer { get; private set; }

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
            return Renderer.DrawEdge(source, target, edgeType);
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
            return Renderer.DrawNode(node, city);
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
            return Renderer.LayoutEdges(gameEdges);
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
            if (currentCity?.Graph == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return currentCity.Graph.AllNumericNodeAttributes();
            }
        }

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
    }
}
