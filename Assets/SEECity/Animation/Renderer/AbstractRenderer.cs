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
        /// Renders the as active set graph.
        /// </summary>
        private void RenderGraph()
        {
            IsStillAnimating = true;
            AnimationStartedEvent.Invoke();

            // For all nodes 
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

            CurrentGraphShown?
                .Edges().Except(NextGraphToBeShown.Edges(), edgeEqualityComparer).ToList()
                .ForEach(RenderRemovedOldEdge);

            NextGraphToBeShown.Traverse(RenderRoot, RenderInnerNode, RenderLeaf);
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
        protected abstract void RegisterAllAnimators(List<AbstractAnimator> animators);

        /// <summary>
        ///Determines how the main node of the active graph is displayed.
        /// </summary>
        /// <param name="node"></param>
        protected abstract void RenderRoot(Node node);

        /// <summary>
        /// Determines how an inner node that contains other nodes is displayed.
        /// </summary>
        /// <param name="node"></param>
        protected abstract void RenderInnerNode(Node node);

        /// <summary>
        /// Determines how a leaf node is displayed.
        /// </summary>
        /// <param name="node"></param>
        protected abstract void RenderLeaf(Node node);

        /// <summary>
        /// Determines how an edge is displayed.
        /// </summary>
        /// <param name="edge"></param>
        protected abstract void RenderEdge(Edge edge);

        /// <summary>
        /// Object is not auto destroyed
        /// </summary>
        /// <param name="node"></param>
        protected abstract void RenderRemovedOldInnerNode(Node node);
        /// <summary>
        /// Object is not auto destroyed
        /// </summary>
        /// <param name="node"></param>
        protected abstract void RenderRemovedOldLeaf(Node node);

        /// <summary>
        /// Object is not auto destroyed
        /// </summary>
        /// <param name="edge"></param>
        protected abstract void RenderRemovedOldEdge(Edge edge);

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