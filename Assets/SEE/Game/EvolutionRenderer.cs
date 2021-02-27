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
using SEE.Game.Evolution;
using SEE.Game.Charts;
using SEE.GO;
using SEE.Layout;
using SEE.Layout.NodeLayouts;
using SEE.Utils;
using System;
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
    /// city that has another component of type SEECityEvolution.
    /// </summary>
    public class EvolutionRenderer : MonoBehaviour
    {
        /// <summary>
        /// Constructors for MonoBehaviours are meaningless. We need to initialize everything
        /// at Start() time.
        /// </summary>
        public void Start()
        {
            RegisterAllAnimators(animators);
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
        public SEECityEvolution CityEvolution
        {
            set
            {
                // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
                // So we cannot make the following assignment in the constructor. Neither
                // can we assign this value at the declaration of graphRenderer because
                // we need the city argument, which comes only later. Anyhow, whenever we
                // assign a new city, we also need a new graph renderer for that city.
                // So in fact this is the perfect place to assign graphRenderer.
                graphRenderer = new GraphRenderer(value, null);
                Assert.IsNotNull(graphRenderer);
                diff = new NumericAttributeDiff(value.AllMetricAttributes());
                objectManager = new ObjectManager(graphRenderer, gameObject);
                if (gameObject.TryGetComponent<SEECityEvolution>(out SEECityEvolution cityEvolution))
                {
                    marker = new Marker(graphRenderer,
                                        markerWidth: cityEvolution.MarkerWidth,
                                        markerHeight: cityEvolution.MarkerHeight,
                                        additionColor: cityEvolution.AdditionBeamColor,
                                        changeColor: cityEvolution.ChangeBeamColor,
                                        deletionColor: cityEvolution.DeletionBeamColor,
                                        duration: AnimationLag);
                }
                else
                {
                    Debug.LogErrorFormat("This EvolutionRenderer attached to {0} has no sibling component of type SEECityEvolution",
                                         name);
                    enabled = false;
                }
            }
        }

        /// <summary>
        /// Shortest time period in which an animation can be run in seconds.
        /// </summary>
        private readonly float MinimalWaitTimeForNextRevision = 0.1f;

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
        private enum MarkerType {Changed, Born};

        /// <summary>
        /// The animator used when an inner node is removed from the scene.
        /// </summary>
        protected readonly AbstractAnimator moveAnimator = new MoveAnimator();

        /// <summary>
        /// An animator used for all other occasions.
        /// </summary>
        protected readonly AbstractAnimator moveScaleShakeAnimator = new MoveScaleShakeAnimator();

        /// <summary>
        /// Whether the animation is still ongoing.
        /// </summary>
        private bool _isStillAnimating = false;  // serialized by Unity

        /// <summary>
        /// Whether the edge animation is ongoing.
        /// </summary>
        private bool moveEdges = false;

        /// <summary>
        /// Saves pairs of old and new edges.
        /// </summary>
        private IList<(GameObject, GameObject)> matchedEdges;

        /// <summary>
        /// Timer for edge animation
        /// </summary>
        private float timer = 0f;

        /// <summary>
        /// True if animation is still ongoing.
        /// </summary>
        public bool IsStillAnimating
        {
            get => _isStillAnimating;
            set => _isStillAnimating = value;
        }

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
        /// List for saving the copied nodes. Is used for animation.
        /// </summary>
        private List<GameObject> animationNodes = new List<GameObject>();

        /// <summary>
        /// List for saving the deactivated nodes. Is used for animation.
        /// </summary>
        private List<GameObject> currentNodes = new List<GameObject>();

        /// <summary>
        /// List to add markers to the animated nodes afterwards
        /// </summary>
        private List<(GameObject, MarkerType)> animationMarker = new List<(GameObject, MarkerType)>();

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
            NodeLayout nodeLayout = graphRenderer.GetLayout();

            // Gather all nodes for the layout.
            ignoreInnerNodes = !nodeLayout.IsHierarchical();
            foreach (Node node in graph.Nodes())
            {
                if (!ignoreInnerNodes || node.IsLeaf())
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
        private int currentGraphRevisionCounter = 0;

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
            // Deletes the nodes created during animation
            foreach (GameObject animationNode in animationNodes)
            {
                Destroyer.DestroyGameObject(animationNode);
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
        /// Combines the edges of the old and the new graph by their ID. Called by the MoveEdges method
        /// </summary>
        /// <param name="oldEdges">List of currently drawn edges</param>
        /// <param name="newEdges">List of new edges to be drawn</param>
        /// <returns>List of related edges</returns>
        protected virtual IList<(GameObject, GameObject)> EdgeMatcher(IList<GameObject> oldEdges, IList<GameObject> newEdges)
        {
            IList<(GameObject, GameObject)> result = new List<(GameObject, GameObject)>();
            foreach (GameObject oldEdgeGameObject in oldEdges)
            {
                foreach (GameObject newEdgeGameObject in newEdges)
                {                    
                    if (oldEdgeGameObject.TryGetComponent<EdgeRef>(out EdgeRef oldEdgeRef) 
                        && newEdgeGameObject.TryGetComponent<EdgeRef>(out EdgeRef newEdgeRef)
                        && oldEdgeRef.edge.Equals(newEdgeRef.edge))
                    {
                        result.Add((oldEdgeGameObject, newEdgeGameObject));
                    }
                }
            }
            return result;
        }
        
        /// <summary>
        /// Calculates the control points of the edges of the next graph and generates their actual line points from them. 
        /// </summary>
        protected virtual void MoveEdges()
        {
           try
           {
                // Calculates the edges for the next graph
                IList<GameObject> newEdges = objectManager.CalculateNewEdgeControlPoints().ToList();
                IList<GameObject> oldEdges = objectManager.GetEdges().ToList();
                
                // Searches for pairs between old and new edges
                matchedEdges =  EdgeMatcher(oldEdges,newEdges);              
                // Case distinction in case the layout does not need sample points
                if(!graphRenderer.GetSettings().EdgeLayout.Equals(SEE.Layout.EdgeLayouts.EdgeLayoutKind.Straight))
                {
                    foreach((GameObject oldEdge, GameObject newEdge) in matchedEdges)
                    {
                        oldEdge.TryGetComponent<Points>(out Points oP);
                        newEdge.TryGetComponent<Points>(out Points nP);

                        uint sampleRate = (uint)Math.Max(oP.linePoints.Count(),nP.linePoints.Count());

                        // Creates new line points from the control points 
                        oP.linePoints = SEE.Layout.Utils.LinePoints.BSplineLinePointsSampleRate(oP.controlPoints, sampleRate);
                        nP.linePoints = SEE.Layout.Utils.LinePoints.BSplineLinePointsSampleRate(nP.controlPoints, sampleRate);

                        // Saves the new line points to the LineRenderer
                        oldEdge.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer);
                        lineRenderer.positionCount = oP.linePoints.Count();
                        lineRenderer.SetPositions(oP.linePoints);
                    }
                }
                    // Sets the timer for the animation to zero
                    timer = 0f;
                    // Starts the animation of the edges
                    moveEdges = true;
            }
            catch (ArgumentNullException)
            {
                moveEdges = false;
            }
        }
        
        /// <summary>
        /// Interpolates the points of the old edges with those of the new edges over time.
        /// </summary>
        void Update()
        {
            if (moveEdges) 
            {
                timer += Time.deltaTime;
                 foreach ((GameObject oldEdge, GameObject newEdge) in matchedEdges)
                 {
                    if (oldEdge.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer)
                        && newEdge.TryGetComponent<Points>(out Points newLinePoints))
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
            if (gameNode is GameObject)
            {
                graphRenderer.AdjustStyle(gameNode as GameObject);
            }
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

                // Set the layout for the copied node
                animationNode.transform.localScale = layoutNode.LocalScale;
                animationNode.transform.position = layoutNode.CenterPosition;
            }
            else
            {
                // node existed before
                if (diff.AreDifferent(formerGraphNode, graphNode))
                {
                    difference = Difference.Changed;
                    animationMarker.Add((currentGameNode, MarkerType.Changed));
                }
                else
                {
                    difference = Difference.None;
                }
                // Set the layout for the copied node
                animationNode.transform.localScale = new Vector3(currentGameNode.transform.localScale.x, 0.0001f, currentGameNode.transform.localScale.z);
                animationNode.transform.position = currentGameNode.transform.position;
            }
            // The actual node is shifted to its new position.
            graphRenderer.Apply(currentGameNode, gameObject, layoutNode);
            // The copied node is animated.
            moveScaleShakeAnimator.AnimateTo(animationNode, layoutNode, difference, null);
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// It will be called as a callback after the animation of a node to be 
        /// removed has been finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        private void OnRemovedNodeFinishedAnimation(object gameObject)
        {
            if (gameObject != null && gameObject is GameObject)
            {
                Destroy((GameObject)gameObject);
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
        /// An event fired when the viewn graph has changed.
        /// </summary>
        private readonly UnityEvent shownGraphHasChangedEvent = new UnityEvent();

        /// <summary>
        /// Whether the user has selected auto-play mode.
        /// </summary>
        private bool _isAutoplay = false;  // not serialized by Unity

        /// <summary>
        /// Whether the user has selected reverse auto-play mode.
        /// </summary>
        private bool _isAutoplayReverse = false;  // not serialized by Unity

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
        /// Initiates the visualization of the evolving series of <paramref name="graphs"/>.
        /// </summary>
        /// <param name="graphs">series of graphs to be visualized</param>
        public void ShowGraphEvolution(List<Graph> graphs)
        {
            this.graphs = graphs;
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
                Debug.LogError("Evolution renderer could not show the inital graph.\n");
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
            CurrentGraphIndex = index;

            if (HasCurrentLaidOutGraph(out LaidOutGraph loadedGraph))
            {
                DisplayGraphAsNew(loadedGraph);
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
                return;
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
                return;
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
        /// That is what is currenly needed by the animators.
        /// </summary>
        private class AnimationNode : ILayoutNode
        {
            private Vector3 centerPosition;
            private Vector3 scale;

            public AnimationNode(Vector3 centerPosition, Vector3 scale)
            {
                this.centerPosition = centerPosition;
                this.scale = scale;
            }

            public Vector3 LocalScale
            {
                get => scale;
                set => scale = value;
            }

            public Vector3 AbsoluteScale
            {
                get => scale;
            }

            public Vector3 CenterPosition
            {
                get => centerPosition;
                set => centerPosition = value;
            }

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
