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
using SEE.DataModel;
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

                objectManager = new ObjectManager(graphRenderer, gameObject, cityEvolution.DeletionBeamColor, beamScale);
                marker = new Marker(graphRenderer,
                                    markerWidth: cityEvolution.MarkerWidth,
                                    markerHeight: cityEvolution.MarkerHeight,
                                    additionColor: cityEvolution.AdditionBeamColor,
                                    changeColor: cityEvolution.ChangeBeamColor,
                                    deletionColor: cityEvolution.DeletionBeamColor,
                                    duration: AnimationLag);
                RegisterAllAnimators(animators);
                phase1AnimationWatchDog = new Phase1AnimationWatchDog(this);
                phase2AnimationWatchDog = new Phase2AnimationWatchDog(this);
            }
            else
            {
                Debug.LogError($"This EvolutionRenderer attached to {name} has no sibling component of type {nameof(SEECityEvolution)}.\n");
                enabled = false;
            }
        }

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
        /// The animator used when an inner node is removed from the scene
        /// and also for animating the new plane.
        ///
        /// Note: We split the animation in halves for the first phase of
        /// removing nodes and the second phase of drawing the nodes of
        /// the next graph.
        /// </summary>
        protected readonly AbstractAnimator moveAnimator
            = new MoveAnimator(AbstractAnimator.DefaultAnimationTime / 2);

        /// <summary>
        /// An animator used for all other occasions (new nodes, existing nodes, changed nodes).
        ///
        /// Note: We split the animation in halves for the first phase of
        /// removing nodes and the second phase of drawing the nodes of
        /// the next graph.
        /// </summary>
        protected AbstractAnimator changeAndBirthAnimator
            = new MoveScaleShakeAnimator(AbstractAnimator.DefaultAnimationTime / 2);

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
        private readonly ISet<string> negligibleNodes = new HashSet<string>();

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
            GraphRenderer.Stack(gameObject, layoutNodes);
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
            Phase1RemoveDeletedGraphElements(current, next);
        }

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

        /// <summary>
        /// Implements the second phase in the transition from the current to the <paramref name="next"/>
        /// graph. In this phase, all nodes in <paramref name="next"/> will be drawn. These may be
        /// either new or existing nodes (the latter being nodes that have been present in the
        /// currently drawn graph. When this phase has been completed, <see cref="OnAnimationsFinished"/>
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
            /// We need to assign _nextCity because the callbacks <see cref="RenderPlane"/>
            /// and <see cref="RenderNode(Node)"/> will access it.
            _nextCity = next;

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

            objectManager.NegligibleNodes = negligibleNodes;

            // We have made the transition to the next graph.
            _currentCity = next;
            MoveEdges();

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
        /// A watchdog for outstanding animations whose completion needs to be awaited
        /// until a particular method can be triggered.
        /// </summary>
        private abstract class AnimationWatchDog
        {
            /// <summary>
            /// The number of outstanding animation that need to be awaited before
            /// a particular method can be called.
            /// </summary>
            protected int outstandingAnimations;

            /// <summary>
            /// The <see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations.
            /// </summary>
            protected readonly EvolutionRenderer evolutionRenderer;

            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public AnimationWatchDog(EvolutionRenderer evolutionRenderer)
            {
                this.evolutionRenderer = evolutionRenderer;
                outstandingAnimations = 0;
            }

            /// <summary>
            /// Sets the <paramref name="numberOfAnimations"/> to be waited for until the
            /// particular method should be called.
            /// </summary>
            /// <param name="numberOfAnimations">the number of animations to be awaited</param>
            public void Await(int numberOfAnimations)
            {
                outstandingAnimations = numberOfAnimations;
            }

            /// <summary>
            /// Signals this <see cref="AnimationWatchDog"/> that one animation has been completed.
            /// If there are no more other animations to be awaited, the particular method will be
            /// called. That method depends upon the concrete subclass.
            /// </summary>
            public abstract void Finished();
        }

        /// <summary>
        /// A watchdog awaiting all animations of the first phase to be finished. The first
        /// phase is dedicated to the deletion of graph elements not present in the next graph.
        /// When all deletion animations have completed, <see cref="Phase2AddNewAndExistingGraphElements"/>
        /// will be called.
        /// </summary>
        private class Phase1AnimationWatchDog : AnimationWatchDog
        {
            /// <summary>
            /// The next graph to be shown.
            /// </summary>
            private LaidOutGraph next;

            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public Phase1AnimationWatchDog(EvolutionRenderer evolutionRenderer)
                : base(evolutionRenderer)
            { }

            /// <summary>
            /// Sets the <paramref name="numberOfAnimations"/> to be waited for until the
            /// <see cref="Phase2AddNewAndExistingGraphElements"/> should be called.
            /// </summary>
            /// <param name="numberOfAnimations">the number of animations to be awaited</param>
            public void Await(int numberOfAnimations, LaidOutGraph next)
            {
                Await(numberOfAnimations);
                this.next = next;
            }

            /// <summary>
            /// Signals this <see cref="Phase1AnimationWatchDog"/> that one animation has been completed.
            /// If there are no more other animations to be awaited, <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called.
            /// </summary>
            public override void Finished()
            {
                outstandingAnimations--;
                if (outstandingAnimations == 0)
                {
                    evolutionRenderer.Phase2AddNewAndExistingGraphElements(next);
                }
            }

            /// <summary>
            /// Tells this <see cref="Phase1AnimationWatchDog"/> to skip the waiting for
            /// outstanding animations. <see cref="Phase2AddNewAndExistingGraphElements"/>
            /// will be called immediately. <paramref name="next"/> will be passed as
            /// argument to <see cref="Phase2AddNewAndExistingGraphElements"/>.
            /// </summary>
            /// <param name="next">the next graph to be shown</param>
            public void Skip(LaidOutGraph next)
            {
                outstandingAnimations = 0;
                evolutionRenderer.Phase2AddNewAndExistingGraphElements(next);
            }
        }

        /// <summary>
        /// A watchdog awaiting all animations of the second phase to be finished. The second
        /// phase is dedicated to the drawing all graph elements present in the graph next to
        /// be drawn.When all deletion animations have completed, <see cref="OnAnimationsFinished"/>
        /// will be called.
        /// </summary>
        private class Phase2AnimationWatchDog : AnimationWatchDog
        {
            /// <summary>
            /// Constructor setting the <see cref="EvolutionRenderer"/> whose method should
            /// be called when there are no more outstanding animations. The number of
            /// outstanding animations is assumed to be zero at this point. The correct
            /// value can be set by <see cref="Await(int)"/> later.
            /// </summary>
            /// <param name="evolutionRenderer"><see cref="EvolutionRenderer"/> whose method should be called
            /// when there are no more outstanding animations</param>
            public Phase2AnimationWatchDog(EvolutionRenderer evolutionRenderer)
                : base(evolutionRenderer)
            { }

            /// <summary>
            /// Signals this <see cref="Phase2AnimationWatchDog"/> that one animation has been completed.
            /// If there are no more other animations to be awaited, <see cref="OnAnimationsFinished"/>
            /// will be called.
            /// </summary>
            public override void Finished()
            {
                outstandingAnimations--;
                if (outstandingAnimations == 0)
                {
                    evolutionRenderer.OnAnimationsFinished();
                }
            }
        }

        /// <summary>
        /// Updates the hierarchy of game nodes such that is isomorphic to the node
        /// hierarchy of the underlying graph.
        /// </summary>
        private void UpdateGameNodeHierarchy()
        {
            Dictionary<Node, GameObject> nodeMap = new Dictionary<Node, GameObject>();
            CollectNodes(gameObject, nodeMap);
            Check(nodeMap);
            GraphRenderer.CreateGameNodeHierarchy(nodeMap, gameObject);
        }

        /// <summary>
        /// Checks whether all graph nodes and game nodes in <paramref name="nodeMap"/> are
        /// members of the same graph.
        /// </summary>
        /// <param name="nodeMap">mapping of graph nodes onto their corresponding game nodes</param>
        private void Check(Dictionary<Node, GameObject> nodeMap)
        {
            HashSet<Graph> graphs = new HashSet<Graph>();

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
        private static void CollectNodes(GameObject root, Dictionary<Node, GameObject> nodeMap)
        {
            if (root != null)
            {
                foreach (Transform childTransform in root.transform)
                {
                    GameObject child = childTransform.gameObject;
                    /// If a game node was deleted, it was marked inactive in
                    /// <see cref="OnRemovedNodeFinishedAnimation"/>. We need to ignored such
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
            // Destroy all previous edges and draw all edges of next graph. This can only
            // be done when nodes have reached their final position, that is, at the end
            // of the animation cycle.
            objectManager.RenderEdges();

            // Stops the edge animation
            moveEdges = false;

            NodeChangesBuffer nodeChangesBuffer = NodeChangesBuffer.GetSingleton();
            nodeChangesBuffer.currentRevisionCounter = currentGraphRevisionCounter;
            nodeChangesBuffer.addedNodeIDsCache = new List<string>(nodeChangesBuffer.addedNodeIDs);
            nodeChangesBuffer.addedNodeIDs.Clear();
            nodeChangesBuffer.changedNodeIDsCache = new List<string>(nodeChangesBuffer.changedNodeIDs);
            nodeChangesBuffer.changedNodeIDs.Clear();
            nodeChangesBuffer.removedNodeIDsCache = new List<string>(nodeChangesBuffer.removedNodeIDs);
            nodeChangesBuffer.removedNodeIDs.Clear();

            UpdateGameNodeHierarchy();
            RenderPlane();
            DestroyDeletedNodes();

            IsStillAnimating = false;
            AnimationFinishedEvent.Invoke();
        }

        /// <summary>
        /// Is called by Constructor the register all given <paramref name="animators"/>,
        /// so they can be updated accordingly.
        /// </summary>
        /// <param name="animators">list of animators to be informed</param>
        protected virtual void RegisterAllAnimators(IList<AbstractAnimator> animators)
        {
            animators.Add(changeAndBirthAnimator);
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
            phase2AnimationWatchDog.Finished();
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
                difference = Difference.Added;

                // Set the layout for the copied node.
                currentGameNode.transform.localScale = layoutNode.LocalScale;
                currentGameNode.transform.position = layoutNode.CenterPosition;
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
                    marker.MarkChanged(currentGameNode);
                    break;
                case Difference.Added:
                    marker.MarkBorn(currentGameNode);
                    break;
            }
            // we want the animator to move each node separately, which is why we
            // remove each from the hierarchy; later the node hierarchy will be
            // re-established
            RemoveFromNodeHierarchy(currentGameNode);
            // currentGameNode is shifted to its new position through the animator.
            changeAndBirthAnimator.AnimateTo(currentGameNode, layoutNode, difference, OnAnimationNodeAnimationFinished);
        }

        private static void RemoveFromNodeHierarchy(GameObject currentGameNode)
        {
            currentGameNode.transform.SetParent(null);
            foreach (Transform child in currentGameNode.transform)
            {
                if (child.CompareTag(Tags.Node))
                {
                    child.SetParent(null);
                }
            }
        }

        /// <summary>
        /// The list of game nodes that were removed from the current graph to the next
        /// graph. They will need to be destroyed at the end of phase 1 (or at the beginning
        /// of phase 2, respectively).
        /// <seealso cref="DestroyDeletedNodes"/>.
        /// </summary>
        private IList<GameObject> toBeDestroyed = new List<GameObject>();

        /// <summary>
        /// Destroys all game nodes in <see cref="toBeDestroyed"/>. <see cref="toBeDestroyed"/>
        /// will be cleared at the end.
        /// </summary>
        private void DestroyDeletedNodes()
        {
            foreach (GameObject gameObject in toBeDestroyed)
            {
                DestroyGameObject(gameObject);
            }
            toBeDestroyed.Clear();
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// It will be called as a callback after the animation of a node to be
        /// removed has been finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        private void OnRemovedNodeFinishedAnimation(object gameObject)
        {
            GameObject go = gameObject as GameObject;
            /// The gameObject must not be destroyed immediately, because the animator still
            /// accesses it. Therefore, we defer the destruction and just add it to
            /// <see cref="toBeDestroyed"/>.
            toBeDestroyed.Add(go);
            /// We set the game object inactive. This will later be considered in
            /// <see cref="CollectNodes(GameObject, Dictionary{Node, GameObject})"/>,
            /// where inactive objects will be ignored (because they are considered
            /// deleted).
            go.SetActive(false);
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
            if (gameNode is GameObject go && go.transform.parent == null)
            {
                /// We will just put this game object under <see cref="gameObject"/>
                /// (the game object representing the city as a whole) as a child. When
                /// the animation is over and all nodes have reached their destination,
                /// <see cref="UpdateGameNodeHierarchy"/> will put this node to its
                /// actual logical game-node parent.
                go.transform.SetParent(gameObject.transform);
            }
            phase2AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Destroys <paramref name="gameObject"/> if it is an instance of <see cref="GameObject"/>.
        /// The ancestors of <paramref name="gameObject"/> will not be destroyed; their parent will
        /// just be set to null.
        /// </summary>
        /// <param name="gameObject">object to be destroyed</param>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                Destroy(go);
            }
        }

        /// <summary>
        /// Removes the given node. The removal is animated by raising the
        /// node to <see cref="SkyLevel"/> (y dimension). The node is not
        /// destroyed when the animation has finished.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        protected virtual void RenderRemovedNode(Node node)
        {
            if (objectManager.RemoveNode(node, out GameObject block))
            {
                Assert.IsNotNull(block);
                block.transform.SetParent(null);
                /// if the node needs to be removed, mark it dead and let it raise to <see cref="SkyLevel"/>
                marker.MarkDead(block);
                Vector3 newPosition = block.transform.position;
                newPosition.y = SkyLevel;
                ILayoutNode nodeTransform = new AnimationNode(newPosition, block.transform.localScale);
                moveAnimator.AnimateTo(block, nodeTransform, Difference.Deleted, OnRemovedNodeFinishedAnimation);
            }
            else
            {
                phase1AnimationWatchDog.Finished();
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
        protected virtual void RenderRemovedEdge(Edge edge)
        {
            objectManager.RemoveEdge(edge);
            phase1AnimationWatchDog.Finished();
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
                Debug.LogError($"There is no graph available for graph with index {index}\n");
                return false;
            }
            bool hasLayout = TryGetLayout(graph, out Dictionary<string, ILayoutNode> layout);
            if (layout == null || !hasLayout)
            {
                Debug.LogError($"There is no layout available for graph with index {index}");
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
