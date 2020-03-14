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

using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// Renders the evolution of the graph series through animations. Incrementally updates
    /// the graph (removal/addition of nodes/edges).
    /// 
    /// Note: The renderer is a MonoBehaviour, thus, will be added as a component to a game
    /// object. As a consequence, a constructor will not be called and is meaningless.
    /// </summary>
    public class EvolutionRenderer : MonoBehaviour
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public EvolutionRenderer()
        {
            RegisterAllAnimators(animators);
        }

        /// <summary>
        /// The graph renderer used to draw a single graph and the later added nodes and edges.
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the city, which is set by this setter.
        /// </summary>
        private GraphRenderer graphRenderer;

        /// <summary>
        /// The manager of the game objects created for the city.         
        /// This attribute will be set in the setter of the attribute CityEvolution because it
        /// depends upon the graphRenderer, which in turn depends upon the city, which is set by 
        /// this setter.
        /// </summary>
        private ObjectManager objectManager;

        /// <summary>
        /// The city evolution to be drawn by this renderer.
        /// </summary>
        public SEECityEvolution CityEvolution
        {
            set {
                // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
                // So we cannot make the following assignment in the constructor. Neither
                // can we assign this value at the declaration of graphRenderer because
                // we need the city argument, which comes only later. Anyhow, whenever we
                // assign a new city, we also need a new graph renderer for that city.
                // So in fact this is the perfect place to assign graphRenderer.
                graphRenderer = new GraphRenderer(value);
                diff = new NumericAttributeDiff(value.AllMetricAttributes());
                objectManager = new ObjectManager(graphRenderer);
            }
        }

        /// <summary>
        /// The kind of comparison to determine whether there any differences between
        /// two corresponding graph elements (corresponding by their linknames) in
        /// two different graphs of the graph series.
        /// </summary>
        private GraphElementDiff diff;

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
        private bool _isStillAnimating = false;

        /// <summary>
        /// True if animation is still ongoing.
        /// </summary>
        public bool IsStillAnimating { get => _isStillAnimating;
                                       set => _isStillAnimating = value; }

        /// <summary>
        /// The collection of registered <see cref="AbstractAnimator"/> to be updated
        /// automatically for changes during the animation time period.
        /// </summary>
        private readonly List<AbstractAnimator> animators = new List<AbstractAnimator>();

        /// <summary>
        /// The duration of an animation. This value can be controlled by the user.
        /// </summary>
        private float _animationDuration = AbstractAnimator.DefaultAnimationTime;

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
        private LaidOutGraph _currentCity;

        /// <summary>
        /// The underlying graph of the city currently shown.
        /// </summary>
        protected Graph CurrentGraphShown => _currentCity?.Graph;
        /// <summary>
        /// The layout of the city currently shown. The layout is a mapping of the graph
        /// nodes' LinkName onto their NodeTransforms.
        /// </summary>
        protected Dictionary<string, NodeTransform> CurrentLayoutShown => _currentCity?.Layout;

        /// <summary>
        /// The city (graph + layout) to be shown next.
        /// </summary>
        private LaidOutGraph _nextCity;
        /// <summary>
        /// The next city (graph + layout) to be shown. 
        /// Note: 'next' does not necessarily mean that it is a graph coming later in the
        /// series of the graph evolution. It just means that this is the next graph to
        /// be shown. If the user goes backward in time, _nextCity is actually an older
        /// graph.
        /// </summary>
        protected Graph NextGraphToBeShown => _nextCity?.Graph;
        /// <summary>
        /// The layout of _nextGraph. The layout is a mapping of the graph
        /// nodes' LinkName onto their NodeTransforms.
        /// </summary>
        protected Dictionary<string, NodeTransform> NextLayoutToBeShown => _nextCity?.Layout;

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private readonly NodeEqualityComparer nodeEqualityComparer = new NodeEqualityComparer();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/> from different graphs.
        /// </summary>
        private readonly EdgeEqualityComparer edgeEqualityComparer = new EdgeEqualityComparer();

        /// <summary>
        /// All pre-computed layouts for the whole graph series.
        /// </summary>
        private Dictionary<Graph, Dictionary<string, NodeTransform>> Layouts { get; }
             =  new Dictionary<Graph, Dictionary<string, NodeTransform>>();

        /// <summary>
        /// Creates and saves the layouts for all given <paramref name="graphs"/>. This will 
        /// also create all necessary game objects -- even those game objects that are not 
        /// present in the first graph in this list.
        /// </summary>
        private void CalculateAllGraphLayouts(List<Graph> graphs)
        {
            // Determine the layouts of all loaded graphs upfront.
            var p = Performance.Begin("Layouting all " + graphs.Count + " graphs");
            graphs.ForEach(graph =>
            {
                Layouts[graph] = CalculateLayout(graph);
            });
            objectManager.Clear();
            p.End();
        }

        /// <summary>
        /// If true, inner nodes should not be rendered. This will be true if a non-hierarchical
        /// layout is applied.
        /// </summary>
        private bool ignoreInnerNodes = true;

        /// <summary>
        /// Calculates the layout data for <paramref name="graph"/> using the graphRenderer.
        /// All the game objects created for the nodes of <paramref name="graph"/> will
        /// be created by the objectManager, thus, be available for later use. The layout
        /// is not actually applied.
        /// </summary>
        /// <param name="graph">graph for which the layout is to be calculated</param>
        /// <returns>the node layout for all nodes in <paramref name="graph"/></returns>
        private Dictionary<string, NodeTransform> CalculateLayout(Graph graph)
        {
            // The following code assumes that a leaf node remains a leaf across all
            // graphs of the graph series and an inner node remains an inner node.
            // This may not necessarily be true. For instance, an empty directory could 
            // get subdirectories in the course of the evolution.

            // Collecting all game objects corresponding to nodes of the given graph.
            // If the node existed in a previous graph, we will re-use its corresponding
            // game object created earlier.
            var gameObjects = new List<GameObject>();

            // The layout to be applied.
            NodeLayout nodeLayout = graphRenderer.GetLayout();

            // Gather all nodes for the layout.
            ignoreInnerNodes = ! nodeLayout.IsHierarchical();
            foreach (Node node in graph.Nodes())
            {
                if (! ignoreInnerNodes || node.IsLeaf())
                {
                    // All layouts (flat and hierarchical ones) must be able to handle leaves; 
                    // hence, leaves can be added at any rate. For a hierarchical layout, we 
                    // need to add the game objects for inner nodes, too. To put it differently,
                    // inner nodes are added only if we apply a hierarchical layout.
                    objectManager.GetNode(node, out var gameNode);
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

            // Calculate and return the layout for the collected game objects.
            return ToLinkNameLayout(nodeLayout.Layout(gameObjects));

            // Note: The game objects for leaf nodes are already properly scaled by the call to 
            // objectManager.GetNode() above. Yet, inner nodes are generally not scaled by
            // the layout and there may be layouts that may shrink leaf nodes. For instance,
            // TreeMap shrinks leaves so that they fit into the available space.
            // Anyhow, we do not need to apply the layout already now. That can be deferred
            // to the point in time when the city is actually visualized. Here, we just calculate
            // the layout for every graph in the graph series for later use.
        }

        /// <summary>
        /// Transform the given <paramref name="layout"/> such that instead of the game objects, 
        /// the LinkName of the graph node attached to a game object is used as a key for
        /// the dictionary.
        /// </summary>
        /// <param name="layout">layout indexed by game objects</param>
        /// <returns>layout indexed by the LinkName of the node attached to the game objects</returns>
        private static Dictionary<string, NodeTransform> ToLinkNameLayout(Dictionary<GameObject, NodeTransform> layout)
        {
            Dictionary<string, NodeTransform> result = new Dictionary<string, NodeTransform>();
            foreach (var entry in layout)
            {
                NodeRef nodeRef = entry.Key.GetComponent<NodeRef>();
                result[nodeRef.node.LinkName] = entry.Value;
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
        public bool TryGetLayout(Graph graph, out Dictionary<string, NodeTransform> layout)
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
            next.AssertNotNull("next");
            IsStillAnimating = true;
            AnimationStartedEvent.Invoke();
            if (current != null)
            {
                // For all nodes of the current graph not in the next graph; that is, all
                // nodes removed: remove those. Note: The comparison is based on the
                // Linknames of the nodes because nodes between two graphs must be different
                // even if they denote the "logically same" node.
                current.Graph?
                    .Nodes().Except(next.Graph.Nodes(), nodeEqualityComparer).ToList()
                    .ForEach(node =>
                    {
                        if (node.IsLeaf())
                        {
                            RenderRemovedOldLeaf(node);
                        }
                        else
                        {
                            RenderRemovedOldInnerNode(node);
                        }
                    });

                // For all edges of the current graph not in the next graph; that is, all
                // edges removed: remove those. As above, edges are compared by their
                // linknames.
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
            // Draw all edges of next graph.
            next.Graph.Edges().ForEach(RenderEdge);
            // We have made the transition to the next graph.
            _currentCity = next;
            Invoke("OnAnimationsFinished", Math.Max(AnimationDuration, MinimalWaitTimeForNextRevision));
        }

        /// <summary>
        /// Event function triggered when alls animations are finished.
        /// </summary>
        private void OnAnimationsFinished()
        {
            IsStillAnimating = false;
            AnimationFinishedEvent.Invoke();
        }

        /// <summary>
        /// Is called on Constructor the register all given animator,
        /// so they can be updated accordingly.
        /// </summary>
        /// <param name="animators"></param>
        protected virtual void RegisterAllAnimators(List<AbstractAnimator> animators)
        {
            animators.Add(moveScaleShakeAnimator);
            animators.Add(moveAnimator);
        }

        /// <summary>
        /// Renders a plane enclosing all (transitive) descendant game objects of given
        /// <paramref name="node"/>.
        /// </summary>
        /// <param name="node">the node to be displayed</param>
        protected virtual void RenderPlane(Node node)
        {
            Debug.LogWarning("EvolutionRenderer.RenderPlane(Node) not implemented yet.\n");
            // FIXME. Code must be adjusted. Planes are not part of the layout.
            /*
            var isPlaneNew = !objectManager.GetPlane(out GameObject plane);
            var nodeTransform = NextLayoutToBeShown[node];
            if (isPlaneNew)
            {
                // if the plane is new instantly apply the position and size
                plane.transform.position = Vector3.zero;
                plane.transform.localScale = nodeTransform.scale;
            }
            else
            {
                // if the tranform of the plane changed animate it
                SimpleAnim.AnimateTo(node, plane, Vector3.zero, nodeTransform.scale);
            }
            */
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
            if (gameNode != null && gameNode is GameObject)
            {
                graphRenderer.AdjustStyle(gameNode as GameObject);
            }
        }

        /// <summary>
        /// Ignroes the given <paramref name="node"/> in rendering. This method can
        /// be used if inner or leaf nodes are to be ignored (e.g., for non-hierarchical
        /// layouts).
        /// </summary>
        /// <param name="node">node to be displayed</param>
        protected void IgnoreNode(Node node)
        {
            // intentionally left blank
        }

        /// <summary>
        /// Renders the game object corresponding to the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">node to be displayed</param>
        protected virtual void RenderNode(Node node)
        {
            NodeTransform nodeTransform = NextLayoutToBeShown[node.LinkName];
            Node formerNode = objectManager.GetNode(node, out GameObject gameObject);

            bool wasModified;
            if (formerNode == null)
            {
                // If the node is new, we animate it by moving it out of the ground.
                // Note nodeTransform.position.y denotes the ground position of
                // a game object, not its center.
                nodeTransform.position.y -= nodeTransform.scale.y;
                graphRenderer.Apply(gameObject, nodeTransform);
                // Revert the change to the y co-ordindate.
                nodeTransform.position.y += nodeTransform.scale.y;
                wasModified = false;
            }
            else
            {
                wasModified = diff.AreDifferent(formerNode, node);
            }
            moveScaleShakeAnimator.AnimateTo(gameObject, nodeTransform, wasModified, OnRenderNodeFinishedAnimation);
        }

        private void DumpLayout(Dictionary<string, NodeTransform> layout)
        {
            foreach (var entry in layout)
            {
                Debug.LogFormat("layout of node named {0}: {1}.\n", entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Rendes given <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">edge to be rendered</param>
        protected virtual void RenderEdge(Edge edge)
        {
            // FIXME.
        }

        /// <summary>
        /// Event function that destroys the given <paramref name="gameObject"/>.
        /// It will be called as a callback after the animation of a node to be 
        /// removed has been finished.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        public void OnRemovedNodeFinishedAnimation(object gameObject)
        {
            if (gameObject != null && gameObject is GameObject)
            {
                Destroy((GameObject)gameObject);
            }
        }

        /// <summary>
        /// Removes the given inner node. The node is not auto destroyed.
        /// </summary>
        /// <param name="node">inner node to be removed</param>
        protected virtual void RenderRemovedOldInnerNode(Node node)
        {
            if (objectManager.RemoveNode(node, out GameObject gameObject))
            {
                // if the node needs to be removed, let it sink into the ground
                var nextPosition = gameObject.transform.position;
                nextPosition.y = -2;
                NodeTransform nodeTransform = new NodeTransform(nextPosition, gameObject.transform.localScale);
                moveAnimator.AnimateTo(gameObject, nodeTransform, false, OnRemovedNodeFinishedAnimation);
            }
        }

        /// <summary>
        /// Removes the given leaf node. The removal is animated by sinking the
        /// node. The node is not destroyed.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        protected virtual void RenderRemovedOldLeaf(Node node)
        {
            if (objectManager.RemoveNode(node, out GameObject gameObject))
            {
                // if the node needs to be removed, let it sink into the ground
                var newPosition = gameObject.transform.position;
                newPosition.y = -gameObject.transform.localScale.y;
                NodeTransform nodeTransform = new NodeTransform(newPosition, gameObject.transform.localScale);
                moveScaleShakeAnimator.AnimateTo(gameObject, nodeTransform, false, OnRemovedNodeFinishedAnimation);
            }
        }

        /// <summary>
        /// Removes the given edge. The edge is not destroyed, however.
        /// </summary>
        /// <param name="edge"></param>
        protected virtual void RenderRemovedOldEdge(Edge edge)
        {
            // FIXME.
        }

        // **********************************************************************

        /// <summary>
        /// The series of underlying graphs to be rendered.
        /// </summary>
        private List<Graph> graphs;

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
                shownGraphHasChangedEvent.Invoke();
            }
        }

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
        /// An event fired when the viewn graph has changed.
        /// </summary>
        private UnityEvent shownGraphHasChangedEvent = new UnityEvent();

        /// <summary>
        /// Whether the user has selected auto-play mode.
        /// </summary>
        private bool _isAutoplay = false;

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
            if (IsAutoPlay)
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
            bool hasLayout = TryGetLayout(graph, out Dictionary<string, NodeTransform> layout);
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
            if (IsAutoPlay)
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
        /// If we are at the begin of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its 
        /// direct predecessor graph in the graph series. CurrentGraphIndex is decreased
        /// by one accordingly.
        /// </summary>
        public void ShowPreviousGraph()
        {
            if (IsStillAnimating || IsAutoPlay)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.\n");
                return;
            }
            if (CurrentGraphIndex == 0)
            {
                Debug.Log("This is already the first graph revision.\n");
                return;
            }
            CurrentGraphIndex--;

            if (HasCurrentLaidOutGraph(out LaidOutGraph newlyShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex + 1, out LaidOutGraph currentlyShownGraph))
            {
                // Note: newlyShownGraph is the most recent past of currentlyShownGraph
                TransitionToNextGraph(currentlyShownGraph, newlyShownGraph);
            }
            else
            {
                Debug.LogError("Could not retrieve a graph layout.\n");
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
        /// Sets auto-play mode to <paramref name="enabled"/>. If <paramref name="enabled"/>
        /// is true, the next graph in the series is shown and from there all other 
        /// following graphs until we reach the end of the graph series or auto-play
        /// mode is turned off again. If <paramref name="enabled"/> is false instead,
        /// the currently shown graph remains visible.
        /// </summary>
        /// <param name="enabled"></param>
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
    }
}