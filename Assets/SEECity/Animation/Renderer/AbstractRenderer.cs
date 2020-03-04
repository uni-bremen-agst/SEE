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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Animation.Internal
{
    /// <summary>
    /// Abstract Render class that serves as an interface to optimally animate different 
    /// display formats or layouts.
    /// </summary>
    public abstract class AbstractRenderer : MonoBehaviour
    {
        /// <summary>
        /// Shortest time period in which an animation can be run.
        /// </summary>
        private float MinimalWaitTimeForNextRevision = 0.1f;

        /// <summary>
        /// An event fired upon the start of an animation.
        /// </summary>
        public readonly UnityEvent AnimationStartedEvent = new UnityEvent();

        /// <summary>
        /// An event fired upon the end of an animation.
        /// </summary>
        public readonly UnityEvent AnimationFinishedEvent = new UnityEvent();

        /// <summary>
        /// A SimpleAnimator used for animation.
        /// </summary>
        protected readonly AbstractAnimator SimpleAnim = new SimpleAnimator();

        /// <summary>
        /// A MoveAnimator used for move animations.
        /// </summary>
        protected readonly AbstractAnimator MoveAnim = new MoveAnimator();

        /// <summary>
        /// Whether the animation is still ongoing.
        /// </summary>
        private bool _isStillAnimating = false;

        /// <summary>
        /// True if animation is still ongoing.
        /// </summary>
        public bool IsStillAnimating { get => _isStillAnimating; set => _isStillAnimating = value; }

        /// <summary>
        /// The collection of registered <see cref="AbstractAnimator"/> to be updated
        /// automatically for changes during the animation time period.
        /// </summary>
        private readonly List<AbstractAnimator> animators = new List<AbstractAnimator>();

        private float _animationTime = AbstractAnimator.DefaultAnimationTime;

        /// <summary>
        /// Maximal time of the lifetime of animation after they started.
        /// </summary>
        public float AnimationTime
        {
            get => _animationTime;
            set
            {
                if (value >= 0)
                {
                    _animationTime = value;
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
        /// The layout of the city currently shown.
        /// </summary>
        protected Layout CurrentLayoutShown => _currentCity?.Layout;

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
        /// The layout of _nextGraph.
        /// </summary>
        protected Layout NextLayoutToBeShown => _nextCity?.Layout;

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/>.
        /// </summary>
        private readonly NodeEqualityComparer nodeEqualityComparer = new NodeEqualityComparer();

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Edge"/>.
        /// </summary>
        private readonly EdgeEqualityComparer edgeEqualityComparer = new EdgeEqualityComparer();

        private AbstractObjectManager _objectManager;

        protected enum GraphDirection { First, Next, Previous };

        /// <summary>
        /// Can be null if not set
        /// </summary>
        public AbstractObjectManager ObjectManager
        {
            set
            {
                value.AssertNotNull("ObjectManager");
                _objectManager = value;
            }
            get => _objectManager;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public AbstractRenderer()
        {
            RegisterAllAnimators(animators);
        }

        /// <summary>
        /// Displays the given graph instantly if all animations are finished.
        /// </summary>
        /// <param name="graph"></param>
        public void DisplayGraph(LaidOutGraph graph)
        {
            graph.AssertNotNull("loadedGraph");

            if (IsStillAnimating)
            {
                Debug.LogWarning("Graph changes are blocked while animations are running.");
                return;
            }

            ClearGraphObjects();
            _nextCity = graph;
            RenderGraph();
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
                Debug.LogError("Graph changes are not allowed while animations are running.");
                return;
            }

            _currentCity = current;
            _nextCity = next;
            RenderGraph();
        }

        /// <summary>
        /// Renders the animation from CurrentGraphShown to NextGraphToBeShown.
        /// </summary>
        private void RenderGraph()
        {
            IsStillAnimating = true;
            AnimationStartedEvent.Invoke();

            // For all nodes of the current graph not in the next graph; that is, all
            // nodes removed:
            CurrentGraphShown?
                .Nodes().Except(NextGraphToBeShown.Nodes(), nodeEqualityComparer).ToList()
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
            // edges removed:
            CurrentGraphShown?
                .Edges().Except(NextGraphToBeShown.Edges(), edgeEqualityComparer).ToList()
                .ForEach(RenderRemovedOldEdge);

            // Draw all nodes of NextGraphToBeShown.
            NextGraphToBeShown.Traverse(RenderRoot, RenderInnerNode, RenderLeaf);
            // Draw all edges of NextGraphToBeShown.
            NextGraphToBeShown.Edges().ForEach(RenderEdge);
            Invoke("OnAnimationsFinished", Math.Max(AnimationTime, MinimalWaitTimeForNextRevision));
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
            animators.Add(SimpleAnim);
            animators.Add(MoveAnim);
        }

        /// <summary>
        /// Determines how the root node of the active graph is displayed.
        /// </summary>
        /// <param name="node">the node to be displayed</param>
        protected virtual void RenderRoot(Node node)
        {
            // FIXME: The root node is either a leaf or inner node.
            // So just dispatch to either RenderInnerNode or RenderLeaf-

            var isPlaneNew = !ObjectManager.GetRoot(out GameObject root);
            var nodeTransform = NextLayoutToBeShown.GetNodeTransform(node);
            if (isPlaneNew)
            {
                // if the plane is new instantly apply the position and size
                root.transform.position = Vector3.zero;
                root.transform.localScale = nodeTransform.scale;
            }
            else
            {
                // if the tranform of the plane changed animate it
                SimpleAnim.AnimateTo(node, root, Vector3.zero, nodeTransform.scale);
            }
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

            var isCircleNew = !ObjectManager.GetInnerNode(node, out GameObject circle);
            var nodeTransform = NextLayoutToBeShown.GetNodeTransform(node);

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
                SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            else if (node.WasModified())
            {
                SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            else if (node.WasRelocated(out string oldLinkageName))
            {
                SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
            }
            else
            {
                SimpleAnim.AnimateTo(node, circle, circlePosition, circleRadius);
            }
        }

        /// <summary>
        /// Renders a leaf node.
        /// </summary>
        /// <param name="node">leaf node to be rendered</param>
        protected abstract void RenderLeaf(Node node);

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
            if (ObjectManager.RemoveNode(node, out GameObject gameObject))
            {
                // if the node needs to be removed, let it sink into the ground
                var nextPosition = gameObject.transform.position;
                nextPosition.y = -2;
                MoveAnim.AnimateTo(node, gameObject, nextPosition, gameObject.transform.localScale,
                                   OnRemovedNodeFinishedAnimation);
            }
        }
        /// <summary>
        /// Removes the given leaf node. The node is not auto destroyed.
        /// </summary>
        /// <param name="node">leaf node to be removed</param>
        protected abstract void RenderRemovedOldLeaf(Node node);

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
            ObjectManager?.Clear();
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