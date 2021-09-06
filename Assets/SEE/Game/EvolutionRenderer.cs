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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SEE.DataModel.DG;
using SEE.Game.Charts;
using SEE.Game.Evolution;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Layout.Utils;
using SEE.Utils;
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
    /// city that has another component of type SEECityEvolution.
    /// </summary>
    public class EvolutionRenderer : MonoBehaviour
    {
        /// <summary>
        /// Constructors for MonoBehaviours are meaningless. We need to initialize everything
        /// at Awake() time.
        /// </summary>
        private void Awake()
        {
            if (gameObject.TryGetComponent(out SEECityEvolution cityEvolution))
            {
                // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
                // So we cannot make the following assignment in the constructor. Neither
                // can we assign this value at the declaration of graphRenderer because
                // we need the city argument, which comes only later. Anyhow, whenever we
                // assign a new city, we also need a new graph renderer for that city.
                // So in fact this is the perfect place to assign graphRenderer.
                graphRenderer = new GraphRenderer(cityEvolution, null);
                Assert.IsNotNull(graphRenderer);
                diff = new NumericAttributeDiff(cityEvolution.AllMetricAttributes());
                Vector3 beamScale = new Vector3(cityEvolution.MarkerWidth, cityEvolution.MarkerHeight, cityEvolution.MarkerWidth);
                Dictionary<Difference, Color> beamColor = new Dictionary<Difference, Color>
                {
                    { Difference.Added, cityEvolution.AdditionBeamColor },
                    { Difference.Changed, cityEvolution.ChangeBeamColor },
                    { Difference.Deleted, cityEvolution.DeletionBeamColor },
                };

                moveScaleShakeAnimator = new MoveScaleShakeAnimator(beamColor, beamScale);
                objectManager = new ObjectManager(graphRenderer, gameObject, cityEvolution.DeletionBeamColor, beamScale);
                marker = new Marker(graphRenderer,
                                    markerWidth: cityEvolution.MarkerWidth,
                                    markerHeight: cityEvolution.MarkerHeight,
                                    additionColor: cityEvolution.AdditionBeamColor,
                                    changeColor: cityEvolution.ChangeBeamColor,
                                    deletionColor: cityEvolution.DeletionBeamColor,
                                    duration: AnimationLag);
                RegisterAllAnimators(animators);
            }
            else
            {
                Debug.LogError($"This EvolutionRenderer attached to {name} has no sibling component of type {nameof(SEECityEvolution)}.\n");
                enabled = false;
            }
        }

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
        /// The marker used to mark the new and removed game objects.
        /// </summary>
        private Marker marker;  // not serialized by Unity; will be set in CityEvolution property

        /// <summary>
        /// The kind of comparison to determine whether there any differences between
        /// two corresponding graph elements (corresponding by their ID) in
        /// two different graphs of the graph series.
        /// </summary>
        private GraphElementDiff diff;  // not serialized by Unity; will be set in CityEvolution property

        /// <summary>
        /// The city evolution to be drawn by this renderer.
        /// </summary>
        //public SEECityEvolution CityEvolution
        //{
        //    set
        //    {
        //        if (gameObject.TryGetComponent<SEECityEvolution>(out SEECityEvolution cityEvolution))
        //        {
        //            // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
        //            // So we cannot make the following assignment in the constructor. Neither
        //            // can we assign this value at the declaration of graphRenderer because
        //            // we need the city argument, which comes only later. Anyhow, whenever we
        //            // assign a new city, we also need a new graph renderer for that city.
        //            // So in fact this is the perfect place to assign graphRenderer.
        //            graphRenderer = new GraphRenderer(value, null);
        //            Assert.IsNotNull(graphRenderer);
        //            diff = new NumericAttributeDiff(value.AllMetricAttributes());
        //            Vector3 beamScale = new Vector3(cityEvolution.MarkerWidth, cityEvolution.MarkerHeight, cityEvolution.MarkerWidth);
        //            objectManager = new ObjectManager(graphRenderer, gameObject, cityEvolution.DeletionBeamColor, beamScale);
        //            marker = new Marker(graphRenderer,
        //                                markerWidth: cityEvolution.MarkerWidth,
        //                                markerHeight: cityEvolution.MarkerHeight,
        //                                additionColor: cityEvolution.AdditionBeamColor,
        //                                changeColor: cityEvolution.ChangeBeamColor,
        //                                deletionColor: cityEvolution.DeletionBeamColor,
        //                                duration: AnimationLag);
        //        }
        //        else
        //        {
        //            Debug.LogError($"This EvolutionRenderer attached to {name} has no sibling component of type {nameof(SEECityEvolution)}.\n");
        //            enabled = false;
        //        }
        //    }
        //}

        /// <summary>
        /// Shortest time period in which an animation can be run in seconds.
        /// </summary>
        private const float MinimalWaitTimeForNextRevision = 0.1f;

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
        /// An event fired upon the start of an animation.
        /// </summary>
        public readonly UnityEvent AnimationStartedEvent = new UnityEvent();

        /// <summary>
        /// An event fired upon the end of an animation.
        /// </summary>
        public readonly UnityEvent AnimationFinishedEvent = new UnityEvent();

        /// <summary>
        /// Used to store whether a node has been born or changed.
        /// </summary>
        private enum MarkerType { Changed, Born }

        /// <summary>
        /// The animator used when an inner node is removed from the scene.
        /// </summary>
        protected readonly AbstractAnimator moveAnimator = new MoveAnimator();

        /// <summary>
        /// An animator used for all other occasions. Will be set in Start().
        /// </summary>
        protected AbstractAnimator moveScaleShakeAnimator;

        /// <summary>
        /// Whether the edge animation is ongoing.
        /// </summary>
        private bool moveEdges;

        /// <summary>
        /// Saves pairs of old and new edges.
        /// </summary>
        private IList<(GameObject, GameObject)> matchedEdges;

        /// <summary>
        /// Timer for edge animation
        /// </summary>
        private float timer;

        /// <summary>
        /// True if animation is still ongoing.
        /// </summary>
        public bool IsStillAnimating { get; private set; }

        /// <summary>
        /// The collection of registered <see cref="AbstractAnimator"/> to be updated
        /// automatically for changes during the animation time period.
        /// </summary>
        private readonly List<AbstractAnimator> animators = new List<AbstractAnimator>();

        /// <summary>
        /// The duration of an animation. This value can be controlled by the user.
        /// </summary>
        private float _animationDuration = AbstractAnimator.DefaultAnimationTime;  // not serialized by Unity

        /// <summary>
        /// The duration of an animation.
        /// </summary>
        public float AnimationDuration
        {
            get => _animationDuration;
            set
            {
                if (value >= 0)
                {
                    _animationDuration = value;
                    animators.ForEach(animator =>
                    {
                        animator.MaxAnimationTime = value;
                        animator.AnimationsDisabled = value == 0;
                    });
                }
            }
        }

        /// <summary>
        /// The city (graph + layout) currently shown.
        /// </summary>
        private LaidOutGraph _currentCity;  // not serialized by Unity

        /// <summary>
        /// The underlying graph of the city currently shown.
        /// </summary>
        protected Graph CurrentGraphShown => _currentCity?.Graph;
        /// <summary>
        /// The layout of the city currently shown. The layout is a mapping of the graph
        /// nodes' IDs onto their layout nodes.
        /// </summary>
        protected Dictionary<string, ILayoutNode> CurrentLayoutShown => _currentCity?.Layout;  // not serialized by Unity

        /// <summary>
        /// Evaluates the performance of the edge animation.
        /// </summary>
        private int edgeAnimationPerfScore = 10;

        /// <summary>
        /// Saves how many edges were moved during the last animation.
        /// </summary>
        private int lastMovedEdgesCount = 0;

        /// <summary>
        /// The city (graph + layout) to be shown next.
        /// </summary>
        private LaidOutGraph _nextCity;  // not serialized by Unity
        /// <summary>
        /// The next city (graph + layout) to be shown.
        /// Note: 'next' does not necessarily mean that it is a graph coming later in the
        /// series of the graph evolution. It just means that this is the next graph to
        /// be shown. If the user goes backward in time, _nextCity is actually an older
        /// graph.
        /// </summary>
        protected Graph NextGraphToBeShown => _nextCity?.Graph;  // not serialized by Unity
        /// <summary>
        /// The layout of _nextGraph. The layout is a mapping of the graph
        /// nodes' IDs onto their ILayoutNodes.
        /// </summary>
        protected Dictionary<string, ILayoutNode> NextLayoutToBeShown => _nextCity?.Layout;  // not serialized by Unity

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private readonly NodeEqualityComparer nodeEqualityComparer = new NodeEqualityComparer();

        /// <summary>
        /// Saves the names of the game objects representing nodes that were not moved during an iteration.
        /// </summary>
        private ISet<string> negligibleNodes = new HashSet<string>();

        /// <summary>
        /// List for saving the copied nodes. Is used for animation.
        /// </summary>
        private readonly IList<GameObject> animationNodes = new List<GameObject>();

        /// <summary>
        /// List for saving the deactivated nodes. Is used for animation.
        /// </summary>
        private readonly IList<GameObject> currentNodes = new List<GameObject>();

        /// <summary>
        /// List to add markers to the animated nodes afterwards.
        /// </summary>
        private readonly IList<(GameObject, MarkerType)> animationMarker = new List<(GameObject, MarkerType)>();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private readonly EdgeEqualityComparer edgeEqualityComparer = new EdgeEqualityComparer();

        /// <summary>
        /// All pre-computed layouts for the whole graph series.
        /// </summary>
        private Dictionary<Graph, Dictionary<string, ILayoutNode>> Layouts { get; }
             = new Dictionary<Graph, Dictionary<string, ILayoutNode>>();  // not serialized by Unity

        /// <summary>
        /// Creates and saves the layouts for all given <paramref name="graphs"/>. This will
        /// also create all necessary game objects -- even those game objects that are not
        /// present in the first graph in this list.
        /// </summary>
        private void CalculateAllGraphLayouts(List<Graph> graphs)
        {
            // Determine the layouts of all loaded graphs upfront.
            Performance p = Performance.Begin("Layouting all " + graphs.Count + " graphs");
            graphs.ForEach(graph =>
            {
                Layouts[graph] = CalculateLayout(graph);
            });
            objectManager.Clear();
            p.End(true);
        }

        /// <summary>
        /// If true, inner nodes should not be rendered. This will be true if a non-hierarchical
        /// layout is applied.
        /// </summary>
        private bool ignoreInnerNodes = true;  // not serialized by Unity

        /// <summary>
        /// Calculates the layout data for <paramref name="graph"/> using the graphRenderer.
        /// All the game objects created for the nodes of <paramref name="graph"/> will
        /// be created by the objectManager, thus, be available for later use. The layout
        /// is not actually applied.
        /// </summary>
        /// <param name="graph">graph for which the layout is to be calculated</param>
        /// <returns>the node layout for all nodes in <paramref name="graph"/></returns>
        private Dictionary<string, ILayoutNode> CalculateLayout(Graph graph)
        {
            // The following code assumes that a leaf node remains a leaf across all
            // graphs of the graph series and an inner node remains an inner node.
            // This may not necessarily be true. For instance, an empty directory could
            // get subdirectories in the course of the evolution.

            // Collecting all game objects corresponding to nodes of the given graph.
            // If the node existed in a previous graph, we will re-use its corresponding
            // game object created earlier.
            List<GameObject> gameObjects = new List<GameObject>();

            // The layout to be applied.
            NodeLayout nodeLayout = graphRenderer.GetLayout(gameObject);

            // Gather all nodes for the layout.
            ignoreInnerNodes = !nodeLayout.IsHierarchical();
            foreach (Node node in graph.Nodes().Where(node => !ignoreInnerNodes || node.IsLeaf()))
            {
                // All layouts (flat and hierarchical ones) must be able to handle leaves;
                // hence, leaves can be added at any rate. For a hierarchical layout, we
                // need to add the game objects for inner nodes, too. To put it differently,
                // inner nodes are added only if we apply a hierarchical layout.
                objectManager.GetNode(node, out GameObject gameNode);
                // Now after having attached the new node to the game object,
                // we must adjust the scale of it according to the newly attached node so
                // that the layouter has these. We need to adjust the scale only for leaves,
                // however, because the layouter will select the scale for inner nodes.
                if (node.IsLeaf())
                {
                    graphRenderer.AdjustScaleOfLeaf(gameNode);
                }
                gameObjects.Add(gameNode);
            }

            // Calculate and apply the node layout
            ICollection<ILayoutNode> layoutNodes = ToLayoutNodes(gameObjects);
            // Note: Apply applies its results only on the layoutNodes but not on the game objects
            // these layoutNodes represent. Here, we leave the game objects untouched. The layout
            // must be later applied when we render a city. Here, we only store the layout for later use.
            nodeLayout.Apply(layoutNodes);
            GraphRenderer.Fit(gameObject, layoutNodes);
            return ToNodeIDLayout(layoutNodes);

            // Note: The game objects for leaf nodes are already properly scaled by the call to
            // objectManager.GetNode() above. Yet, inner nodes are generally not scaled by
            // the layout and there may be layouts that may shrink leaf nodes. For instance,
            // TreeMap shrinks leaves so that they fit into the available space.
            // Anyhow, we do not need to apply the layout already now. That can be deferred
            // to the point in time when the city is actually visualized. Here, we just calculate
            // the layout for every graph in the graph series for later use.
        }

        /// <summary>
        /// Yields the collection of LayoutNodes corresponding to the given <paramref name="gameNodes"/>.
        /// Each LayoutNode has the position, scale, and rotation of the game node. The graph node
        /// attached to the game node is passed on to the LayoutNode so that the graph node data is
        /// available to the node layout (e.g., Parent or Children).
        /// Sets also the node levels of all resulting LayoutNodes.
        /// </summary>
        /// <param name="gameNodes">collection of game objects created to represent inner nodes or leaf nodes of a graph</param>
        /// <returns>collection of LayoutNodes representing the information of <paramref name="gameNodes"/> for layouting</returns>
        private ICollection<ILayoutNode> ToLayoutNodes(List<GameObject> gameNodes)
        {
            IList<ILayoutNode> result = new List<ILayoutNode>();
            Dictionary<Node, ILayoutNode> to_layout_node = new Dictionary<Node, ILayoutNode>();

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().Value;
                LayoutNode layoutNode = new LayoutNode(node, to_layout_node);
                // We must transfer the scale from gameObject to layoutNode.
                // Rotation and CenterPosition are all zero. They will be computed by the layout,
                // but the layout needs the game object's scale.
                layoutNode.LocalScale = graphRenderer.GetSize(gameObject);
                result.Add(layoutNode);
            }
            LayoutNodes.SetLevels(result);
            return result;
        }

        /// <summary>
        /// Returns a mapping of graph-node IDs onto their corresponding <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="layoutNodes">collection of layout nodes to be mapped</param>
        /// <returns>mapping indexed by the IDs of the nodes corresponding to the layout nodes</returns>
        private static Dictionary<string, ILayoutNode> ToNodeIDLayout(ICollection<ILayoutNode> layoutNodes)
        {
            Dictionary<string, ILayoutNode> result = new Dictionary<string, ILayoutNode>();
            foreach (ILayoutNode layoutNode in layoutNodes)
            {
                result[layoutNode.ID] = layoutNode;
            }
            return result;
        }

        /// <summary>
        /// Retrieves the pre-computed stored layout for given <paramref name="graph"/>
        /// in output parameter <paramref name="layout"/> if one can be found. If a
        /// layout was actually found, true is returned; otherwise false.
        /// </summary>
        /// <param name="graph">the graph for which to determine the layout</param>
        /// <param name="layout">the retrieved layout or null</param>
        /// <returns>true if a layout could be found</returns>
        public bool TryGetLayout(Graph graph, out Dictionary<string, ILayoutNode> layout)
        {
            return Layouts.TryGetValue(graph, out layout);
        }

        /// <summary>
        /// Displays the given graph instantly if all animations are finished. This is
        /// called when we jump directly to a specific graph in the graph series: when
        /// we start the visualization of the graph evolution initially and when the user
        /// selects a specific graph revision.
        /// The graph is drawn from scratch.
        /// </summary>
        /// <param name="graph">graph to be drawn initially</param>
        public void DisplayGraphAsNew(LaidOutGraph graph)
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
            RenderGraph(_currentCity, graph);
        }

        /// <summary>
        /// Starts the animations to transition from the current to the next graph.
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
            MoveScaleShakeAnimator.DeletePowerBeams();
            RenderGraph(current, next);
        }

        /// <summary>
        /// Renders the animation from CurrentGraphShown to NextGraphToBeShown.
        /// </summary>
        /// <param name="current">the graph currently shown that is to be migrated into the next graph; may be null</param>
        /// <param name="next">the new graph to be shown, in which to migrate the current graph; must not be null</param>
        private void RenderGraph(LaidOutGraph current, LaidOutGraph next)
        {
            negligibleNodes.Clear();
            next.AssertNotNull("next");
            IsStillAnimating = true;
            // First remove all markings of the previous animation cycle.
            marker.Clear();
            AnimationStartedEvent.Invoke();
            if (current != null)
            {
                // For all nodes of the current graph not in the next graph; that is, all
                // nodes removed: remove those. Note: The comparison is based on the
                // IDs of the nodes because nodes between two graphs must be different
                // even if they denote the "logically same" node.
                current.Graph?
                    .Nodes().Except(next.Graph.Nodes(), nodeEqualityComparer).ToList()
                    .ForEach(node =>
                    {
                        RenderRemovedNode(node);
                    });

                // For all edges of the current graph not in the next graph; that is, all
                // edges removed: remove those. As above, edges are compared by their
                // IDs.
                current.Graph?
                    .Edges().Except(next.Graph.Edges(), edgeEqualityComparer).ToList()
                    .ForEach(RenderRemovedOldEdge);
            }
            // We need to assign _nextCity because the callback RenderPlane, RenderInnerNode, RenderLeaf, and
            // RenderEdge will access it.
            _nextCity = next;
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

            objectManager.NegligibleNodes = negligibleNodes;
            // FOR ANIMATION: next.Graph.Edges().ForEach(RenderEdge);

            // We have made the transition to the next graph.
            _currentCity = next;
            RenderPlane();
            MoveEdges();
            Invoke("OnAnimationsFinished", Math.Max(AnimationDuration, MinimalWaitTimeForNextRevision));
        }

        /// <summary>
        /// Current graph revision counter
        /// </summary>
        private int currentGraphRevisionCounter;

        /// <summary>
        /// Event function triggered when all animations are finished. Animates the transition of the edges
        /// and renders all edges as new and notifies everyone that the animation is finished.
        ///
        /// Note: This method is a callback called from the animation framework (DoTween). It is
        /// passed to this animation framework in <see cref="RenderGraph"/>.
        /// </summary>
        private void OnAnimationsFinished()
        {
            // Activates the nodes that were deactivated for the animation
            foreach (GameObject currentNode in currentNodes)
            {
                currentNode.SetActive(true);
            }
            // Adds a marker to the nodes
            foreach ((GameObject, MarkerType) nodeMarker in animationMarker)
            {
                switch (nodeMarker.Item2)
                {
                    case MarkerType.Changed:
                        marker.MarkChanged(nodeMarker.Item1);
                        break;
                    case MarkerType.Born:
                        marker.MarkBorn(nodeMarker.Item1);
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled case {nodeMarker.Item2}.");
                }
            }
            // Clears all lists relevant for the animation of the nodes
            animationNodes.Clear();
            currentNodes.Clear();
            animationMarker.Clear();

            // Destroy all previous edges and draw all edges of next graph. This can only
            // be done when nodes have reached their final position, that is, at the end
            // of the animation cycle.
            objectManager.RenderEdges();

            // Stops the edge animation
            moveEdges = false;
            IsStillAnimating = false;
            AnimationFinishedEvent.Invoke();
            NodeChangesBuffer.GetSingleton().currentRevisionCounter = currentGraphRevisionCounter;
            NodeChangesBuffer.GetSingleton().addedNodeIDsCache = new List<string>(NodeChangesBuffer.GetSingleton().addedNodeIDs);
            NodeChangesBuffer.GetSingleton().addedNodeIDs.Clear();
            NodeChangesBuffer.GetSingleton().changedNodeIDsCache = new List<string>(NodeChangesBuffer.GetSingleton().changedNodeIDs);
            NodeChangesBuffer.GetSingleton().changedNodeIDs.Clear();
            NodeChangesBuffer.GetSingleton().removedNodeIDsCache = new List<string>(NodeChangesBuffer.GetSingleton().removedNodeIDs);
            NodeChangesBuffer.GetSingleton().removedNodeIDs.Clear();
        }

        /// <summary>
        /// Is called by Constructor the register all given <paramref name="animators"/>,
        /// so they can be updated accordingly.
        /// </summary>
        /// <param name="animators">list of animators to be informed</param>
        protected virtual void RegisterAllAnimators(IList<AbstractAnimator> animators)
        {
            animators.Add(moveScaleShakeAnimator);
            animators.Add(moveAnimator);
        }

        /// <summary>
        /// Renders a plane enclosing all game objects of the currently shown graph.
        /// </summary>
        protected virtual void RenderPlane()
        {
            bool isPlaneNew = !objectManager.GetPlane(out GameObject plane);
            if (!isPlaneNew)
            {
                // We are re-using the existing plane, hence, we animate its change
                // (new position and new scale).
                objectManager.GetPlaneTransform(out Vector3 centerPosition, out Vector3 scale);

                Tweens.Scale(plane, scale, moveAnimator.MaxAnimationTime);
                Tweens.Move(plane, centerPosition, moveAnimator.MaxAnimationTime);
            }
        }

        /// <summary>
        /// Checks whether two edges are equal.
        /// </summary>
        /// <param name="left">First edge to be checked</param>
        /// <param name="right">Second edge to be checked</param>
        /// <returns>true if both edges are equal</returns>
        protected virtual bool AreEqualGameEdges(GameObject left, GameObject right)
        {
            return left.TryGetComponent(out EdgeRef leftEdgeRef)
                && right.TryGetComponent(out EdgeRef rightEdgeRef)
                && leftEdgeRef.Value.ID == rightEdgeRef.Value.ID;
        }

        /// <summary>
        /// Combines the edges of the old and the new graph by their ID. Called by the MoveEdges method
        /// </summary>
        /// <param name="oldEdges">List of currently drawn edges</param>
        /// <param name="newEdges">List of new edges to be drawn</param>
        /// <returns>List of related edges</returns>
        protected virtual void EdgeMatcher(IList<GameObject> oldEdges, IList<GameObject> newEdges)
        {
            matchedEdges = new List<(GameObject, GameObject)>();
            foreach (GameObject newEdge in newEdges)
            {
                GameObject oldEdge = oldEdges.ToList().Find(i => AreEqualGameEdges(i, newEdge));
                if (oldEdge != null)
                {
                    matchedEdges.Add((oldEdge, newEdge));
                }
            }
        }

        /// <summary>
        /// Calculates the control points of the edges of the next graph and generates their actual line points from them.
        /// </summary>
        protected virtual void MoveEdges()
        {
            try
            {
                // Calculates the edges for the next graph.
                IList<GameObject> newEdges = objectManager.CalculateNewEdgeControlPoints().ToList();
                IList<GameObject> oldEdges = objectManager.GetEdges().ToList();

                // Searches for pairs between old and new edge.
                EdgeMatcher(oldEdges, newEdges);

                // Case distinction in case the layout does not need sample points.
                if (graphRenderer.settings.edgeLayoutSettings.kind != EdgeLayoutKind.Straight && newEdges.Count() != 0)
                {
                    foreach ((GameObject oldEdge, GameObject newEdge) in matchedEdges)
                    {
                        oldEdge.TryGetComponent(out Points oP);
                        newEdge.TryGetComponent(out Points nP);

                        // Approximates the length of the edge over the control points to save computing power.
                        float dist = Vector3.Distance(nP.controlPoints[0], nP.controlPoints[nP.controlPoints.Count() - 1]);

                        // The AdjustedSamplerate is determined by the performance of the last animation
                        // and tries to achieve a balance between performance and aesthetics
                        // by giving all edges a number of points according to their length,
                        // the total number of edges, and the performance of the last animation.
                        double adjustedSampleRate = Math.Floor(edgeAnimationPerfScore * dist * 10 * lastMovedEdgesCount / matchedEdges.Count());

                        lastMovedEdgesCount = matchedEdges.Count();

                        //In order to use DynamicSampleRateReduction, all edges should have a number of points that is divisible by two.
                        if (adjustedSampleRate % 2 != 0)
                        {
                            adjustedSampleRate++;
                        }

                        // No edge should have more than 75, or less than 2 points.
                        adjustedSampleRate = Math.Min(Math.Max(adjustedSampleRate, 2), 75);

                        // Creates new line points from the control points
                        oP.linePoints = LinePoints.BSplineLinePointsSampleRate(oP.controlPoints, (uint)adjustedSampleRate);
                        nP.linePoints = LinePoints.BSplineLinePointsSampleRate(nP.controlPoints, (uint)adjustedSampleRate);

                        // Saves the new line points to the LineRenderer
                        oldEdge.TryGetComponent(out LineRenderer lineRenderer);
                        lineRenderer.positionCount = oP.linePoints.Count();
                        lineRenderer.SetPositions(oP.linePoints);
                    }
                }
                // Sets the timer for the animation to zero
                timer = 0f;
                // Starts the animation of the edges
                moveEdges = true;
                // Resets performance Score
                edgeAnimationPerfScore = 10;
            }
            catch (ArgumentNullException)
            {
                moveEdges = false;
            }
        }

        /// <summary>
        /// Reduces the number of points on the edge by half to improve performance in particularly complex cases.
        /// </summary>
        /// <returns>Whether the reduction was successful.</returns>
        private bool DynamicSampleRateReduction()
        {
            try
            {
                // Copies every second point.
                foreach ((GameObject oldEdge, GameObject newEdge) in matchedEdges)
                {
                    oldEdge.TryGetComponent<Points>(out Points oP);
                    newEdge.TryGetComponent<Points>(out Points nP);

                    if (oP.linePoints.Count() <= 2) return true;

                    Vector3[] oldLinePointsHalf = oP.linePoints.Where((_, index) => index % 2 == 0).ToArray();
                    Vector3[] newLinePointsHalf = nP.linePoints.Where((_, index) => index % 2 == 0).ToArray();
                    nP.linePoints = newLinePointsHalf;

                    // Saves the new line points to the LineRenderer
                    if (oldEdge.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
                    {
                        lineRenderer.positionCount = oP.linePoints.Count() / 2;
                        lineRenderer.SetPositions(oldLinePointsHalf);
                    }
                }
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        /// <summary>
        /// Interpolates the points of the old edges with those of the new edges over time.
        /// </summary>
        private void Update()
        {
            if (moveEdges)
            {
                timer += Time.deltaTime;

                // We try to keep the animation between 30 and 60 FPS, so we adjust the PerformanceScore at each iteration.
                if (Time.deltaTime > 0.033f)
                {
                    edgeAnimationPerfScore -= 2;
                }
                else if (Time.deltaTime < 0.016f)
                {
                    edgeAnimationPerfScore += 1;
                }

                // If the performance drops too much, we halve the number of points to be drawn by half.
                if (edgeAnimationPerfScore < -200)
                {
                    DynamicSampleRateReduction();

                }
                RedrawEdges();
            }

            void RedrawEdges()
            {
                foreach ((GameObject oldEdge, GameObject newEdge) in matchedEdges)
                {
                    if (oldEdge.TryGetComponent(out LineRenderer lineRenderer)
                        && newEdge.TryGetComponent(out Points newLinePoints))
                    {
                        for (int i = 0; i < lineRenderer.positionCount; i++)
                        {
                            lineRenderer.SetPosition(i, Vector3.Lerp(lineRenderer.GetPosition(i),
                                                                     newLinePoints.linePoints[i],
                                                                     timer / AnimationDuration));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event function that adjusts the given <paramref name="gameNode"/>
        /// according to is attached node's color (style) metric.
        /// It will be called as a callback after the animation of a node to be
        /// rendered has been finished (see RenderNode()). The animation will
        /// adjust the game object's scale and position, but not its style.
        /// Here we adjust the style.
        /// </summary>
        /// <param name="gameNode">game node object that was just modified by the animation</param>
        public void OnRenderNodeFinishedAnimation(object gameNode)
        {
            if (gameNode is GameObject node)
            {
                graphRenderer.AdjustStyle(node);
            }
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
        protected void IgnoreNode(Node node)
        {
            // intentionally left blank
        }

        /// <summary>
        /// Renders the game object corresponding to the given <paramref name="graphNode"/>
        /// by creating a copy of the GameObject that is used during the animation.
        /// </summary>
        /// <param name="graphNode">graph node to be displayed</param>
        protected virtual void RenderNode(Node graphNode)
        {
            // The layout to be applied to graphNode
            ILayoutNode layoutNode = NextLayoutToBeShown[graphNode.ID];
            // The game node representing the graphNode if there is any; null if there is none
            Node formerGraphNode = objectManager.GetNode(graphNode, out GameObject currentGameNode);

            // Copy of the currentGameNode. This copy will be used during the animation on behalf of currentGameNode.
            GameObject animationNode = Instantiate(currentGameNode);
            // The copied node is added to list animationNodes so that it can be deleted after animation.
            animationNodes.Add(animationNode);
            // The actual node is added to list currentNodes so that it can be reactivated after animation.
            currentNodes.Add(currentGameNode);
            // Hide the actual node during the animation.
            currentGameNode.SetActive(false);

            Difference difference;
            if (formerGraphNode == null)
            {
                // The node is new. It has no layout applied to it yet.
                // If the node is new, we animate it by moving it out of the ground.
                // Note layoutNode.position.y denotes the ground position of
                // a game object, not its center.
                Vector3 position = layoutNode.CenterPosition;
                position.y -= layoutNode.LocalScale.y;
                layoutNode.CenterPosition = position;

                // Revert the change to the y co-ordindate.
                position.y += layoutNode.LocalScale.y;
                layoutNode.CenterPosition = position;
                animationMarker.Add((currentGameNode, MarkerType.Born));
                difference = Difference.Added;

                // Set the layout for the copied node.
                animationNode.transform.localScale = layoutNode.LocalScale;
                animationNode.transform.position = layoutNode.CenterPosition;
            }
            else
            {
                // Edge was not moved
                if (XZAreEqual(layoutNode.CenterPosition, currentGameNode.transform.position))
                {
                    negligibleNodes.Add(currentGameNode.ID());
                }
                // Node existed before.
                if (diff.AreDifferent(formerGraphNode, graphNode))
                {
                    difference = Difference.Changed;
                    animationMarker.Add((currentGameNode, MarkerType.Changed));
                }
                else
                {
                    difference = Difference.None;
                }
                // Set the layout for the copied node.
                animationNode.transform.localScale = new Vector3(currentGameNode.transform.localScale.x, 0.0001f, currentGameNode.transform.localScale.z);
                animationNode.transform.position = currentGameNode.transform.position;
            }
            // The actual node is shifted to its new position.
            graphRenderer.Apply(currentGameNode, gameObject, layoutNode);
            // The copied node is animated.
            moveScaleShakeAnimator.AnimateTo(animationNode, layoutNode, difference, OnAnimationNodeAnimationFinished);
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// It will be called as a callback after the animation of a node to be
        /// removed has been finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        private void OnRemovedNodeFinishedAnimation(object gameObject)
        {
            DestroyGameObject(gameObject);
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// Called as a callback and deletes the <paramref name="gameObject"/>
        /// after the animation is finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        private void OnAnimationNodeAnimationFinished(object gameObject)
        {
            DestroyGameObject(gameObject);
        }

        /// <summary>
        /// Destroys <paramref name="gameObject"/> if it is an instance of <see cref="GameObject"/>.
        /// </summary>
        /// <param name="gameObject">object to be destroyed</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DestroyGameObject(object gameObject)
        {
            if (gameObject is GameObject go)
            {
                Destroy(go);
            }
        }

        /// <summary>
        /// Removes the given node. The removal is animated by sinking the
        /// node. The node is not destroyed.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        protected virtual void RenderRemovedNode(Node node)
        {
            if (objectManager.RemoveNode(node, out GameObject block))
            {
                // if the node needs to be removed, mark it dead and let it sink into the ground
                marker.MarkDead(block);
                Vector3 newPosition = block.transform.position;
                newPosition.y = -block.transform.localScale.y;
                ILayoutNode nodeTransform = new AnimationNode(newPosition, block.transform.localScale);
                moveScaleShakeAnimator.AnimateTo(block, nodeTransform, Difference.Deleted, OnRemovedNodeFinishedAnimation);
            }
        }

        /// <summary>
        /// Rendes given <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">edge to be rendered</param>
        /// FOR ANIMATION: protected virtual void RenderEdge(Edge edge)
        /// FOR ANIMATION: {
        /// FOR ANIMATION: // FIXME.
        /// FOR ANIMATION: }

        /// <summary>
        /// Removes the given edge. The edge is not destroyed, however.
        /// </summary>
        /// <param name="edge"></param>
        protected virtual void RenderRemovedOldEdge(Edge edge)
        {
            objectManager.RemoveEdge(edge);
        }

        // **********************************************************************

        /// <summary>
        /// The series of underlying graphs to be rendered.
        /// </summary>
        private List<Graph> graphs;  // not serialized by Unity

        /// <summary>
        /// The number of graphs of the graph series to be rendered.
        /// </summary>
        public int GraphCount => graphs.Count;

        /// <summary>
        /// The time in seconds for showing a single graph revision during auto-play animation.
        /// </summary>
        public float AnimationLag
        {
            get => AnimationDuration;
            set
            {
                AnimationDuration = value;
                marker?.SetDuration(value);
                shownGraphHasChangedEvent.Invoke();
            }
        }

        /// <summary>
        /// The index of the currently visualized graph.
        /// </summary>
        private int currentGraphIndex;  // not serialized by Unity

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
        private readonly UnityEvent shownGraphHasChangedEvent = new UnityEvent();

        /// <summary>
        /// Whether the user has selected auto-play mode.
        /// </summary>
        private bool _isAutoplay;  // not serialized by Unity

        /// <summary>
        /// Whether the user has selected reverse auto-play mode.
        /// </summary>
        private bool _isAutoplayReverse;  // not serialized by Unity

        /// <summary>
        /// Returns true if automatic animations are active.
        /// </summary>
        public bool IsAutoPlay
        {
            get => _isAutoplay;
            private set
            {
                shownGraphHasChangedEvent.Invoke();
                _isAutoplay = value;
            }
        }

        /// <summary>
        /// Returns true if automatic reverse animations are active.
        /// </summary>
        public bool IsAutoPlayReverse
        {
            get => _isAutoplayReverse;
            private set
            {
                shownGraphHasChangedEvent.Invoke();
                _isAutoplayReverse = value;
            }
        }

        /// <summary>
        /// Sets the evolving series of <paramref name="graphs"/> to be visualized.
        /// The actual visualization is triggered by <see cref="ShowGraphEvolution"/>
        /// that can be called next.
        /// </summary>
        /// <param name="graphs">series of graphs to be visualized</param>
        public void SetGraphEvolution(List<Graph> graphs)
        {
            this.graphs = graphs;
        }

        /// <summary>
        /// Initiates the visualization of the evolving series of graphs
        /// provided earlier by <see cref="SetGraphEvolution(List{Graph})"/>
        /// (the latter function must have been called before).
        /// </summary>
        public void ShowGraphEvolution()
        {
            CurrentGraphIndex = 0;
            _currentCity = null;
            _nextCity = null;

            graphRenderer.SetScaler(graphs);
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
                Debug.LogErrorFormat("The value {0} is no valid index.\n", index);
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
                Debug.LogErrorFormat("Could not retrieve a layout for graph with index {0}.\n", index);
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
                Debug.LogErrorFormat("There ist no graph available for graph with index {0}\n", index);
                return false;
            }
            bool hasLayout = TryGetLayout(graph, out Dictionary<string, ILayoutNode> layout);
            if (layout == null || !hasLayout)
            {
                Debug.LogErrorFormat("There ist no layout available for graph with index {0}", index);
                return false;
            }
            laidOutGraph = new LaidOutGraph(graph, layout);
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
            if (currentGraphIndex == graphs.Count - 1)
            {
                return false;
            }
            CurrentGraphIndex++;

            if (HasCurrentLaidOutGraph(out LaidOutGraph newlyShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex - 1, out LaidOutGraph currentlyShownGraph))
            {
                currentGraphRevisionCounter++;
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
                currentGraphRevisionCounter--;
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
        internal void SetAutoPlayReverse(bool enabled)
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
        /// An implementation of ILayoutNode that is used for animation purposes only.
        /// The only features it supports are the position and scale of node.
        /// That is what is currently needed by the animators.
        /// </summary>
        private class AnimationNode : ILayoutNode
        {
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
