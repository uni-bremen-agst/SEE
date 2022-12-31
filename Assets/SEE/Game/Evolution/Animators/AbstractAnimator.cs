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
        private float maxAnimationTime;
        /// <summary>
        /// If true animations are skipped and the new values are applied instantly.
        /// </summary>
        private bool animationsDisabled = false;

        /// <summary>
        /// Defines the maximum time an animation is allowed to take in seconds.
        /// </summary>
        public float MaxAnimationTime { get => maxAnimationTime; set => maxAnimationTime = value; }

        /// <summary>
        /// If set to true animations are skipped and the new values are applied instantly.
        /// </summary>
        public bool AnimationsDisabled { get => animationsDisabled; set => animationsDisabled = value; }

        /// <summary>
        /// Creates a new animator with a given maximal animation time.
        /// </summary>
        /// <param name="maxAnimationTime">The maximum time the animation is allowed to run.</param>
        public AbstractAnimator(float maxAnimationTime = DefaultAnimationTime)
        {
            MaxAnimationTime = maxAnimationTime;
        }

        /// <summary>
        /// Animates the node transformation of given <paramref name="gameObject"/>. If needed, a <paramref name="callback"/>
        /// that is invoked with <paramref name="gameObject"/> as a parameter when the animation is finished can be passed.
        ///
        /// Precondition: the caller of this method AnimateTo() is a Monobehaviour instance attached to a game object.
        ///
        /// Let C be the caller (a Monobehaviour) of AnimateTo(). Then the actual callback will be as follows:
        ///   O.<paramref name="callback"/>(<paramref name="gameObject"/>) where O is the game object caller C
        ///   is attached to (its gameObject).
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="nodeTransform">the node transformation to be applied</param>
        /// <param name="callback">an optional callback to be called when the animation has finished</param>
        /// <param name="moveCallback">an optional callback to be called when the move animation is about to start</param>
        public void AnimateTo(GameObject gameObject,
                              ILayoutNode nodeTransform,
                              Action<object> callback = null,
                              Action<float> moveCallback = null)
        {
            gameObject.AssertNotNull("gameObject");
            nodeTransform.AssertNotNull("nodeTransform");

            if (AnimationsDisabled)
            {
                // Note: nodeTransform.position.y denotes the ground of the game object, not
                // the center as normally in Unity. The following position is the one in
                // Unity's terms where the y co-ordinate denotes the center.
                Vector3 position = nodeTransform.CenterPosition;
                position.y += nodeTransform.LocalScale.y / 2;
                gameObject.transform.position = position;
                gameObject.transform.localScale = nodeTransform.LocalScale;
                callback?.Invoke(gameObject);
            }
            else
            {
                AnimateToInternalWithCallback(gameObject, nodeTransform, callback, moveCallback);
            }
        }

        /// <summary>
        /// Returns the strength of shaking an animated game object. Each component
        /// is a degree and corresponds to one axis (x, y, z).
        /// </summary>
        /// <returns>the degree by which to shake an animated object</returns>
        protected abstract Vector3 ShakeStrength();

        /// <summary>
        /// Moves, scales, and then finally shakes (if <paramref name="difference"/>) the animated game object.
        /// At the end of the animation, the <see cref="Action"/> <paramref name="callback"/>
        /// will be called with <paramref name="gameObject"/> as parameter if <paramref name="callback"/>
        /// is not null. If <paramref name="callback"/> equals null, no callback happens.
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="layout">the node transformation to be applied</param>
        /// <param name="callback">method to be called when the animation has finished</param>
        /// <param name="moveCallback">method to be called when the move animation is about to start</param>
        private void AnimateToInternalWithCallback
                  (GameObject gameObject,
                   ILayoutNode layout,
                   Action<object> callback,
                   Action<float> moveCallback = null)
        {
            // layout.scale is in world space, while the animation by iTween
            // is in local space. Our game objects may be nested in other game objects,
            // hence, the two spaces may be different.
            // We may need to transform nodeTransform.scale from world space to local space.
            Vector3 localScale = gameObject.transform.parent == null ?
                                     layout.LocalScale
                                   : gameObject.transform.parent.InverseTransformVector(layout.LocalScale);

            Tweens.MoveScaleShakeRotate(gameObject, position: layout.CenterPosition, localScale: localScale,
                                        strength: ShakeStrength(), duration: MaxAnimationTime, callback,
                                        moveCallback);
        }
    }
}