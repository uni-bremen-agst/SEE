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
using UnityEngine;

namespace SEE.Animation
{
    /// <summary>
    /// An abstract animator that makes it simple to swap an existing type of animation.
    /// For example there could be a RotationAnimator and BounceAnimator for different
    /// kind of states.
    /// </summary>
    public abstract class AbstractAnimator
    {
        /// <summary>
        /// Defines the default time an animation takes in seconds.
        /// </summary>
        public const int DefaultAnimationTime = 2;

        /// <summary>
        /// Defines the maximum time an animation is allowed to take in seconds.
        /// </summary>
        private float _maxAnimationTime;
        /// <summary>
        /// If true animations are skipped and the new values are applied instantly.
        /// </summary>
        private bool _animationsDisabled = false;

        /// <summary>
        /// Defines the maximum time an animation is allowed to take in seconds.
        /// </summary>
        public float MaxAnimationTime { get => _maxAnimationTime; set => _maxAnimationTime = value; }

        /// <summary>
        /// If set to true animations are skipped and the new values are applied instantly.
        /// </summary>
        public bool AnimationsDisabled { get => _animationsDisabled; set => _animationsDisabled = value; }

        /// <summary>
        /// Creates a new animator with a given maximal animation time.
        /// </summary>
        /// <param name="maxAnimationTime">The maximum time the animation is allowed to run.</param>
        public AbstractAnimator(float maxAnimationTime = DefaultAnimationTime)
        {
            this.MaxAnimationTime = maxAnimationTime;
        }

        /// <summary>
        /// Animates the node transformation of a given GameObject. If needed, a callback that is called
        /// after the animation is finished can be defined. The animation is implemented by
        /// <see cref="AnimateToInternal(Node, GameObject, NodeTransform)"/>
        /// </summary>
        /// <param name="node">Node of the given GameObject</param>
        /// <param name="gameObject">GameObject to animate</param>
        /// <param name="nodeTransform">the node transformation to be applied</param>
        /// <param name="callback">An optional callback</param>
        public void AnimateTo(Node node, GameObject gameObject, NodeTransform nodeTransform, Action<object> callback = null)
        {
            Debug.LogFormat("AnimateTo {0}: from (position={1}, scale={2}) to (position={3}, scale={4})\n",
                             node.LinkName,
                             gameObject.transform.position, gameObject.transform.localScale,
                             nodeTransform.position, nodeTransform.scale);
            node.AssertNotNull("node");
            gameObject.AssertNotNull("gameObject");
            nodeTransform.AssertNotNull("nodeTransform");

            if (AnimationsDisabled)
            {
                gameObject.transform.position = nodeTransform.position;
                gameObject.transform.localScale = nodeTransform.scale;
                callback?.Invoke(gameObject);
            }
            else if (callback == null)
            {
                AnimateToInternalWithCallback(node, gameObject, nodeTransform, null, "");
            }
            else
            {
                AnimateToInternalWithCallback(node, gameObject, nodeTransform, ((MonoBehaviour)callback.Target).gameObject, callback.Method.Name);
            }
        }

        /// <summary>
        /// Abstract method, called by <see cref="AnimateTo"/> for an animation with a callback.
        /// </summary>
        /// <param name="node">Node of the given GameObject</param>
        /// <param name="gameObject">GameObject to animate</param>
        /// <param name="nodeTransform">the node transformation to be applied</param>
        /// <param name="callback">An optional callback</param>
        /// <param name="callbackName">name of the callback</param>
        protected abstract void AnimateToInternalWithCallback
            (Node node, 
            GameObject gameObject, 
            NodeTransform nodeTransform,  
            GameObject callBackTarget, 
            string callbackName);
    }
}