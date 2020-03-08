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

using System.Runtime.CompilerServices;
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
        /// The city evolution to be drawn by this renderer.
        /// </summary>
        private SEECityEvolution cityEvolution;

        /// <summary>
        /// The city evolution to be drawn by this renderer.
        /// </summary>
        protected SEECityEvolution City => cityEvolution;

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
            get => cityEvolution;
            set {
                cityEvolution = value;
                // A constructor with a parameter is meaningless for a class that derives from MonoBehaviour.
                // So we cannot make the following assignment in the constructor. Neither
                // can we assign this value at the declaration of graphRenderer because
                // we need the city argument, which comes only later. Anyhow, whenever we
                // assign a new city, we also need a new graph renderer for that city.
                // So in fact this is the perfect place to assign graphRenderer.
                graphRenderer = new GraphRenderer(cityEvolution);
                graphRenderer.SetScaler(cityEvolution.Graphs);
                objectManager = new ObjectManager(graphRenderer);
            }
        }

        /// <summary>
        /// Shortest time period in which an animation can be run in seconds.
        /// </summary>
        private readonly float MinimalWaitTimeForNextRevision = 0.1f;

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
        private Dictionary<Graph, Dictionary<GameObject, NodeTransform>> Layouts { get; }
             =  new Dictionary<Graph, Dictionary<GameObject, NodeTransform>>();

        /// <summary>
        /// Creates and saves the layouts for all given <paramref name="graphs"/>. This will 
        /// also create all necessary game objects -- even those game objects that are not 
        /// present in the first graph in this list.
        /// </summary>
        public void CalculateAllGraphLayouts(List<Graph> graphs)
        {
            // Determine the layouts of all loaded graphs upfront.
            var p = Performance.Begin("Layouting all " + graphs.Count + " graphs");
            graphs.ForEach(graph =>
            {
                Layouts[graph] = CalculateLayout(graph);
            });
            p.End();
        }

        /// <summary>
        /// Calculates the layout data for <paramref name="graph"/> using the graphRenderer.
        /// All the game objects created for the nodes of <paramref name="graph"/> will
        /// be created by the objectManager, thus, be available for later use. The layout
        /// is not actually applied.
        /// </summary>
        /// <param name="graph">graph for which the layout is to be calculated</param>
        /// <returns>the node layout for all nodes in <paramref name="graph"/></returns>
        private Dictionary<GameObject, NodeTransform> CalculateLayout(Graph graph)
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
            bool isHierarchicalLayout = nodeLayout.IsHierarchical();
            foreach (Node node in graph.Nodes())
            {
                if (isHierarchicalLayout || node.IsLeaf())
                {
                    // All layouts (flat and hierarchical ones) must be able to handle leaves; 
                    // hence, leaves can be added at any rate. For a hierarchical layout, we 
                    // need to add the game objects for inner nodes, too. To put it differently,
                    // inner nodes are added only if we apply a hierarchical layout.
                    objectManager.GetNode(node, out var gameNode);
                    gameObjects.Add(gameNode);
                }
            }

            // Calculate and return the layout for the collected game objects.
            return nodeLayout.Layout(gameObjects);

            // Note: The game objects for leaf nodes are already properly scaled by the call to 
            // objectManager.GetNode() above. Yet, inner nodes are generally not scaled by
            // the layout and there may be layouts that may shrink leaf nodes. For instance,
            // TreeMap shrinks leaves so that they fit into the available space.
            // Anyhow, we do not need to apply the layout already now. That can be deferred
            // to the point in time when the city is actually visualized. Here, we just calculate
            // the layout for every graph in the graph series for later use.
        }

        /// <summary>
        /// Retrieves the pre-computed stored layout for given <paramref name="graph"/>
        /// in output parameter <paramref name="layout"/> if one can be found. If a
        /// layout was actually found, true is returned; otherwise false.
        /// </summary>
        /// <param name="graph">the graph for which to determine the layout</param>
        /// <param name="layout">the retrieved layout or null</param>
        /// <returns>true if a layout could be found</returns>
        public bool TryGetLayout(Graph graph, out Dictionary<GameObject, NodeTransform> layout)
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
                Debug.LogWarning("Graph changes are blocked while animations are running.");
                return;
            }
            if (_currentCity != null)
            {
                ClearGraphObjects(); // FIXME: Why do we need to clear the objects anyhow?
            }
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
                Debug.LogError("Graph changes are blocked while animations are running.");
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
                // nodes removed: remove those.
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
                // edges removed: remove those.
                current.Graph?
                    .Edges().Except(next.Graph.Edges(), edgeEqualityComparer).ToList()
                    .ForEach(RenderRemovedOldEdge);
            }
            // We need to assign _nextCity because the callback RenderPlane, RenderInnerNode, RenderLeaf, and 
            // RenderEdge will access it.
            _nextCity = next;
            // Draw all nodes of next graph.
            next.Graph.Traverse(RenderPlane, RenderInnerNode, RenderLeaf);
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
        /// Determines how an inner node that contains other nodes is displayed.
        /// </summary>
        /// <param name="node">node to be displayed</param>
        protected virtual void RenderInnerNode(Node node)
        {
            // FIXME: The form of inner nodes depends upon the user's choice
            // and possibly the kind of layout.

            // Currently, we have the following kinds of InnerNodeKinds:
            // Blocks, Rectangles, Donuts, Circles, Empty, Cylinders.

            /*
            var isCircleNew = !objectManager.GetInnerNode(node, out GameObject circle);
            var nodeTransform = NextLayoutToBeShown[circle];

            var circlePosition = nodeTransform.position;
            circlePosition.y = 0.5F;

            var circleRadius = nodeTransform.scale;
            circleRadius.x += 2;
            circleRadius.z += 2;

            if (isCircleNew)
            {
                // if the node is new, animate it by moving it out of the ground
                circlePosition.y = -3;
                circle.transform.position = circlePosition;
                circle.transform.localScale = circleRadius;

                circlePosition.y = 0.5F;
                moveScaleShakeAnimator.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            else if (node.WasModified())
            {
                moveScaleShakeAnimator.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            else if (node.WasRelocated(out string oldLinkageName))
            {
                moveScaleShakeAnimator.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            else
            {
                moveScaleShakeAnimator.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            */
        }

        /// <summary>
        /// Renders a leaf node.
        /// </summary>
        /// <param name="node">leaf node to be rendered</param>
        protected virtual void RenderLeaf(Node node)
        {
            bool isNewLeaf = !objectManager.GetLeaf(node, out GameObject leaf);
            if (leaf == null)
            {
                Debug.LogErrorFormat("Leaf node {0} does not have an associated game object in the object manager.\n", node.LinkName);
                return;
            }
            NodeTransform nodeTransform;

            try
            {
                nodeTransform = NextLayoutToBeShown[node.LinkName];
            } catch (Exception e)
            {
                Debug.LogErrorFormat("Leaf node named {0} does not have a layout.\n", node.LinkName);
                DumpLayout(NextLayoutToBeShown);
                return;
            }

            if (isNewLeaf)
            {
                // if the leaf node is new, animate it by moving it out of the ground

                // FIXME: CScape buildings have a different notion of position than cubes.
                // We need to use graphRenderer.Apply().
                var newPosition = nodeTransform.position;
                newPosition.y = -nodeTransform.scale.y;
                leaf.transform.position = newPosition;
            }
            moveScaleShakeAnimator.AnimateTo(node, leaf, nodeTransform.position, nodeTransform.scale);
        }

        private void DumpLayout(Dictionary<string, NodeTransform> layout)
        {
            foreach (var entry in layout)
            {
                Debug.LogFormat("layout of node named {0}: {1}.\n", entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Determines how an edge is displayed.
        /// </summary>
        /// <param name="edge"></param>
        protected virtual void RenderEdge(Edge edge)
        {
        }

        /// <summary>
        /// Removes the given inner node. The node is not auto destroyed.
        /// </summary>
        /// <param name="node">inner node to be removed</param>
        protected virtual void RenderRemovedOldInnerNode(Node node)
        {
            if (objectManager.TryGetNode(node, out GameObject gameObject))
            {
                // if the node needs to be removed, let it sink into the ground
                var nextPosition = gameObject.transform.position;
                nextPosition.y = -2;
                moveAnimator.AnimateTo(node, gameObject, nextPosition, gameObject.transform.localScale,
                                   OnRemovedNodeFinishedAnimation);
            }
        }
        /// <summary>
        /// Removes the given leaf node. The removal is animating by sinking the
        /// node. The node is not auto destroyed.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        protected virtual void RenderRemovedOldLeaf(Node node)
        {
            if (objectManager.TryGetNode(node, out GameObject leaf))
            {
                // if the node needs to be removed, let it sink into the ground
                var newPosition = leaf.transform.position;
                newPosition.y = -leaf.transform.localScale.y;

                moveScaleShakeAnimator.AnimateTo(node, leaf, newPosition, leaf.transform.localScale, OnRemovedNodeFinishedAnimation);
            }
        }

        /// <summary>
        /// Removes the given edge. The edge is not auto destroyed, however.
        /// </summary>
        /// <param name="edge"></param>
        protected virtual void RenderRemovedOldEdge(Edge edge)
        {
        }

        /// <summary>
        /// Clears all GameObjects created by the used ObjectManager
        /// </summary>
        private void ClearGraphObjects()
        {
            objectManager?.Clear();
            foreach (string tag in SEE.DataModel.Tags.All)
            {
                foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
                {
                    DestroyImmediate(o);
                }
            }
        }

        /// <summary>
        /// Event function that destroys a given GameObject.
        /// </summary>
        /// <param name="gameObject"></param>
        public void OnRemovedNodeFinishedAnimation(object gameObject)
        {
            if (gameObject != null && gameObject is GameObject)
            {
                Destroy((GameObject)gameObject);
            }
        }
    }
}