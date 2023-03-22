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

using SEE.Layout;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// The kind of differences for an animated object.
    /// </summary>
    public enum Difference
    {
        None,    // if there is no difference whatsoever
        Added,   // if the element was added
        Changed, // if the element was changed
        Deleted, // if the element was deleted
    }

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
        public const float DefaultAnimationTime = 5.0f;

        /// <summary>
        /// Defines the maximum time an animation is allowed to take in seconds.
        /// </summary>
        public float MaxAnimationTime { get; set; } = DefaultAnimationTime;

        /// <summary>
        /// If set to true animations are skipped and the new values are applied instantly.
        /// </summary>
        public bool AnimationsDisabled { get; set; } = false;

        /// <summary>
        /// Creates a new animator with a given maximal animation time.
        /// </summary>
        /// <param name="maxAnimationTime">The maximum time the animation is allowed to run.</param>
        public AbstractAnimator(float maxAnimationTime = DefaultAnimationTime)
        {
            MaxAnimationTime = maxAnimationTime;
        }

        /// <summary>
        /// Animates the node transformation of given <paramref name="gameObject"/>. If needed, a <paramref name="callbackWhenAnimationFinished"/>
        /// that is invoked with <paramref name="gameObject"/> as a parameter when the animation is finished can be passed.
        ///
        /// Precondition: the caller of this method AnimateTo() is a Monobehaviour instance attached to a game object.
        ///
        /// Let C be the caller (a Monobehaviour) of AnimateTo(). Then the actual callback will be as follows:
        ///   O.<paramref name="callbackWhenAnimationFinished"/>(<paramref name="gameObject"/>) where O is the game object caller C
        ///   is attached to (its gameObject).
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="layoutNode">the node transformation to be applied</param>
        /// <param name="callbackWhenAnimationFinished">an optional callback to be called when the animation has finished</param>
        /// <param name="moveCallback">an optional callback to be called when the move animation is about to start;
        /// the argument passed to it is <see cref="MaxAnimationTime"/></param>
        public void AnimateTo(GameObject gameObject,
                              ILayoutNode layoutNode,
                              Action<object> callbackWhenAnimationFinished = null,
                              Action<float> moveCallback = null)
        {
            gameObject.AssertNotNull("gameObject");
            layoutNode.AssertNotNull("nodeTransform");

            if (AnimationsDisabled)
            {
                // Note: nodeTransform.position.y denotes the ground of the game object, not
                // the center as normally in Unity. The following position is the one in
                // Unity's terms where the y co-ordinate denotes the center.
                Vector3 position = layoutNode.CenterPosition;
                position.y += layoutNode.LocalScale.y / 2;
                gameObject.transform.position = position;
                gameObject.transform.localScale = layoutNode.LocalScale;
                // FIXME: Shouldn't we also call moveCallback?
                callbackWhenAnimationFinished?.Invoke(gameObject);
            }
            else
            {
                // layoutNode.LocalScale is in world space, while the animation by iTween
                // is in local space. Our game objects may be nested in other game objects,
                // hence, the two spaces may be different.
                // We may need to transform layoutNode.LocalScale from world space to local space.
                Vector3 localScale = gameObject.transform.parent == null ?
                                         layoutNode.LocalScale
                                       : gameObject.transform.parent.InverseTransformVector(layoutNode.LocalScale);

                InternalAnimateTo(gameObject,
                     position: layoutNode.CenterPosition,
                     localScale: localScale,
                     duration: MaxAnimationTime,
                     callbackWhenAnimationFinished,
                     moveCallback);
            }
        }

        /// <summary>
        /// Moves and scales <paramref name="gameObject"/> as a sequence of animations.
        /// </summary>
        /// <param name="gameObject">the game object to be animated</param>
        /// <param name="position">the final destination of <paramref name="gameObject"/></param>
        /// <param name="localScale">the final scale of <paramref name="gameObject"/></param>
        /// <param name="duration">the duration of the whole animation in seconds</param>
        /// <param name="callbackWhenAnimationFinished">the method to be called when the animation has finished</param>
        /// <param name="moveCallback">the method to be called when the move animation is about to start</param>
        protected abstract void InternalAnimateTo(GameObject gameObject,
                                                  Vector3 position,
                                                  Vector3 localScale,
                                                  float duration,
                                                  Action<object> callbackWhenAnimationFinished,
                                                  Action<float> moveCallback = null);

    }
}