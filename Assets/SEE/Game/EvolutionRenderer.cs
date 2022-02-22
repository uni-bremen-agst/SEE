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
using SEE.Game.City;
using SEE.Game.Evolution;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
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
    public partial class EvolutionRenderer : MonoBehaviour
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
                edgesAreDrawn = graphRenderer.AreEdgesDrawn();
                Vector3 beamScale = new Vector3(cityEvolution.MarkerWidth, cityEvolution.MarkerHeight, cityEvolution.MarkerWidth);

                objectManager = new ObjectManager(graphRenderer, gameObject);
                marker = new Marker(markerWidth: cityEvolution.MarkerWidth,
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
        /// The marker used to mark the new and removed game objects.
        /// </summary>
        private Marker marker;  // not serialized by Unity; will be set in CityEvolution property

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
        private readonly UnityEvent AnimationFinishedEvent = new UnityEvent();

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
        private AbstractAnimator changeAndBirthAnimator
            = new MoveScaleShakeAnimator(AbstractAnimator.DefaultAnimationTime / 2.0f);

        /// <summary>
        /// Maps from the source of an edge to its animator. This storage is
        /// used by <see cref="RenderNode(Node)"/> to synchronize node
        /// animation with edge animation.
        /// </summary>
        private Dictionary<Node, EdgeAnimator> edgeAnimators
            = new Dictionary<Node, EdgeAnimator>();

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
        private float animationDuration = AbstractAnimator.DefaultAnimationTime;  // not serialized by Unity

        /// <summary>
        /// The duration of an animation.
        /// </summary>
        public float AnimationDuration
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
        /// The layout of _nextGraph. The layout is a mapping of the graph
        /// nodes' IDs onto their ILayoutNodes.
        /// </summary>
        private Dictionary<string, ILayoutNode> NextLayoutToBeShown => nextCity?.Layout;  // not serialized by Unity

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
        /// All pre-computed edge layouts for the whole graph series.
        /// </summary>
        private Dictionary<Graph, Dictionary<string, ILayoutEdge<ILayoutNode>>> EdgeLayouts { get; }
            = new Dictionary<Graph, Dictionary<string, ILayoutEdge<ILayoutNode>>>(); // not serialized by Unity

        /// <summary>
        /// Creates and saves the layouts for all given <paramref name="graphs"/>. This will
        /// also create all necessary game objects -- even those game objects that are not
        /// present in the first graph in this list.
        /// </summary>
        private void CalculateAllGraphLayouts(List<Graph> graphs)
        {
            // Determine the layouts of all loaded graphs upfront.
            Performance p = Performance.Begin("Layouting all " + graphs.Count + " graphs");
            ISet<string> numericNodeAttributes = new HashSet<string>();
            graphs.ForEach(graph =>
            {
                Layouts[graph] = CalculateLayout(graph);
                numericNodeAttributes.UnionWith(graph.AllNumericNodeAttributes());
            });
            diff = new NumericAttributeDiff(numericNodeAttributes);
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
            ICollection<AbstractLayoutNode> layoutNodes = ToLayoutNodes(gameObjects);
            // Note: Apply applies its results only on the layoutNodes but not on the game objects
            // these layoutNodes represent. Here, we leave the game objects untouched. The layout
            // must be later applied when we render a city. Here, we only store the layout for later use.
            nodeLayout.Apply(layoutNodes);
            GraphRenderer.Fit(gameObject, layoutNodes);
            GraphRenderer.Stack(gameObject, layoutNodes);

            if (edgesAreDrawn)
            {
                List<LayoutGraphEdge<AbstractLayoutNode>> layoutEdges = graphRenderer.LayoutEdges(layoutNodes).ToList();
                EdgeLayouts[graph] = new Dictionary<string, ILayoutEdge<ILayoutNode>>(layoutEdges.Count);
                foreach (LayoutGraphEdge<AbstractLayoutNode> le in layoutEdges)
                {
                    EdgeLayouts[graph].Add(le.ItsEdge.ID, le);
                }
            }
            return ToNodeIDLayout(layoutNodes.ToList<ILayoutNode>());

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
        private ICollection<AbstractLayoutNode> ToLayoutNodes(List<GameObject> gameNodes)
        {
            IList<AbstractLayoutNode> result = new List<AbstractLayoutNode>();
            Dictionary<Node, ILayoutNode> toLayoutNode = new Dictionary<Node, ILayoutNode>();

            foreach (GameObject gameObject in gameNodes)
            {
                Node node = gameObject.GetComponent<NodeRef>().Value;
                LayoutGraphNode layoutNode = new LayoutGraphNode(node, toLayoutNode)
                {
                    // We must transfer the scale from gameObject to layoutNode.
                    // but the layout needs the game object's scale.
                    // Rotation and CenterPosition are all zero. They will be computed by the layout,
                    LocalScale = graphRenderer.GetSize(gameObject)
                };
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
        private static Dictionary<string, T> ToNodeIDLayout<T>(ICollection<T> layoutNodes) where T : ILayoutNode
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            foreach (T layoutNode in layoutNodes)
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
            RenderGraph(currentCity, graph);
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
            /// We need to assign _nextCity because the callbacks <see cref="RenderPlane"/>
            /// and <see cref="RenderNode(Node)"/> will access it.
            nextCity = next;

            // Edge Animation must be set up before node animation because it
            // prepares the edge animators that are used during node
            // animation. If the other way around => BOOM!
            if (edgesAreDrawn)
            {
                EdgeAnimation(next);
            }
            NodeAnimation(next);

            // We have made the transition to the next graph.
            currentCity = next;

            void EdgeAnimation(LaidOutGraph next)
            {
                // Create (or read from cache) the edge objects of the next
                // visible graph, update their spline, and make the objects
                // visible.
                foreach (Edge edge in next.Graph.Edges())
                {
                    objectManager.GetEdge(edge, out GameObject edgeObject);
                    if (edgeObject.TryGetComponent(out SEESpline spline))
                    {
                        spline.Spline = next.EdgeLayout[edge.ID].Spline;
                    }
                    edgeObject.SetActive(true); // Make visible
                    if (gameObject.TryGetComponent(out EdgeMeshScheduler scheduler))
                    {
                        scheduler.Add(edgeObject); // Register for mesh creation
                    }
                }
                if (currentCity != null)
                {
                    // We are transitioning to another graph.
                    edgeAnimators.Clear();
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
                            morphism.Init(source.Spline, target.Spline);
                            if (!edgeObject.TryGetComponent(out EdgeAnimator animator))
                            {
                                animator = edgeObject.AddComponent<EdgeAnimator>();
                                animator.Evaluator = morphism;
                            }
                            edgeAnimators[edge.Source] = animator;
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

                objectManager.NegligibleNodes = negligibleNodes;
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
            Dictionary<Node, GameObject> nodeMap = new Dictionary<Node, GameObject>();
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
        private static void CollectNodes(GameObject root, IDictionary<Node, GameObject> nodeMap)
        {
            if (root != null)
            {
                foreach (Transform childTransform in root.transform)
                {
                    GameObject child = childTransform.gameObject;
                    /// If a game node was deleted, it was marked inactive in
                    /// <see cref="OnRemovedNodeFinishedAnimation"/>. We need to ignore such
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
            // intentionally left blank
            phase2AnimationWatchDog.Finished();
        }

        /// <summary>
        /// Renders the game object corresponding to the given <paramref name="graphNode"/>
        /// by creating a copy of the GameObject that is used during the animation.
        /// </summary>
        /// <param name="graphNode">graph node to be displayed</param>
        private void RenderNode(Node graphNode)
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
                    // There is a change. It may or may not be the metric determining the style.
                    // We will not further check that and just call the following method.
                    // If there is no change, this method does not to be called because then
                    // we know that the metric values determining the style of the former and
                    // the new graph node are the same.
                    graphRenderer.AdjustStyle(currentGameNode);
                    break;
                case Difference.Added:
                    NodeChangesBuffer.GetSingleton().addedNodeIDs.Add(currentGameNode.name);
                    marker.MarkBorn(currentGameNode);
                    break;
            }
            // we want the animator to move each node separately, which is why we
            // remove each from the hierarchy; later the node hierarchy will be
            // re-established
            RemoveFromNodeHierarchy(currentGameNode);
            // currentGameNode is shifted to its new position through the animator.
            Action<float> onEdgeAnimationStart = null;
            if (edgeAnimators.TryGetValue(graphNode, out EdgeAnimator animator))
            {
                onEdgeAnimationStart = duration =>
                    { OnEdgeAnimationStart(animator, duration); };
            }
            changeAndBirthAnimator.AnimateTo(currentGameNode, layoutNode,
                OnAnimationNodeAnimationFinished, onEdgeAnimationStart);
        }

        /// <summary>
        /// Removes <paramref name="currentGameNode"/> from the game-object hierarchy,
        /// that is, its parent will be set to <code>null</code> and all its immediate
        /// children tagged by <see cref="Tags.Node"/> will no longer be its children,
        /// that is, their parent is set to <code>null</code>, too.
        /// </summary>
        /// <param name="currentGameNode">a game node to be removed from the game-object hierarchy</param>
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
        /// Starts the animation of <paramref name="animator"/> with given <paramref name="duration"/>.
        /// </summary>
        /// <param name="animator">Animator to start</param>
        /// <param name="duration">Duration of the animation</param>
        private void OnEdgeAnimationStart(EdgeAnimator animator, float duration)
        {
            if (animator != null)
            {
                animator.DoAnimation(duration);
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
                Destroy(go);
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
            if (objectManager.RemoveNode(node, out GameObject block))
            {
                Assert.IsNotNull(block);
                block.transform.SetParent(null);
                /// if the node needs to be removed, mark it dead and let it raise to <see cref="SkyLevel"/>
                marker.MarkDead(block);
                Vector3 newPosition = block.transform.position;
                newPosition.y = SkyLevel;
                ILayoutNode nodeTransform = new AnimationNode(newPosition, block.transform.localScale);
                moveAnimator.AnimateTo(block, nodeTransform, OnRemovedNodeFinishedAnimation);
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
        private void RenderRemovedEdge(Edge edge)
        {
            if (edgesAreDrawn)
            {
                objectManager.GetEdge(edge, out GameObject edgeObject);
                edgeObject.SetActive(false);
            }
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
        /// Current graph of the graph series to be rendered.
        /// </summary>
        public Graph GraphCurrent => graphs[currentGraphIndex];

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
        /// Sets the evolving series of <paramref name="graphs"/> to be visualized.
        /// The actual visualization is triggered by <see cref="ShowGraphEvolution"/>
        /// that can be called next.
        /// </summary>
        /// <param name="graphs">series of graphs to be visualized</param>
        public void SetGraphEvolution(List<Graph> graphs)
        {
            this.graphs = graphs;
            graphRenderer.SetScaler(graphs);
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
                Debug.LogError($"There is no graph available for graph with index {index}.\n");
                return false;
            }
            if (!TryGetLayout(graph, out Dictionary<string, ILayoutNode> nodeLayout) || nodeLayout == null)
            {
                Debug.LogError($"There is no layout available for graph with index {index}.\n");
                return false;
            }
            if (edgesAreDrawn)
            {
                if (!EdgeLayouts.TryGetValue(graph, out Dictionary<string, ILayoutEdge<ILayoutNode>> edgeLayout) )
                {
                    Debug.LogError($"There is no edge layout available for graph with index {index}.\n");
                    return false;
                }
                else
                {
                    laidOutGraph = new LaidOutGraph(graph, nodeLayout, edgeLayout);
                }
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
        internal List<string> AllExistingMetrics()
        {
            if (currentCity == null || currentCity.Graph == null)
            {
                return new List<string>();
            }
            else
            {
                return currentCity.Graph.AllNumericNodeAttributes();
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
