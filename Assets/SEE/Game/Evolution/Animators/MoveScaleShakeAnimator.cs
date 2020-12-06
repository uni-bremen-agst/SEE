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
using System;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Animates the position and scale of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
    /// If a node was modified, the GameObject will be shaken to indicate its modification.
    /// </summary>
    public class MoveScaleShakeAnimator : AbstractAnimator
    {
        /// <summary>
        /// Moves, scales, and then finally shakes (if <paramref name="difference"/>) the animated game object.
        /// At the end of the animation, the method <paramref name="callbackName"/> will be called for the
        /// game object <paramref name="callBackTarget"/> with <paramref name="gameObject"/> as 
        /// parameter if <paramref name="callBackTarget"/> is not null. If <paramref name="callBackTarget"/>
        /// equals null, no callback happens.
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="layout">the node transformation to be applied</param>
        /// <param name="difference">whether the node attached to <paramref name="gameObject"/> was modified w.r.t. to the previous graph</param>
        /// <param name="callBackTarget">an optional game object that should receive the callback</param>
        /// <param name="callbackName">the method name of the callback</param>
        protected override void AnimateToInternalWithCallback
                  (GameObject gameObject,
                   ILayoutNode layout,
                   Difference difference,
                   GameObject callBackTarget,
                   string callbackName,
                   Action<object> callback)
        {
            bool mustCallBack = callBackTarget != null;

            Vector3 position = layout.CenterPosition;

            // layout.scale is in world space, while the animation by iTween
            // is in local space. Our game objects may be nested in other game objects,
            // hence, the two spaces may be different.
            // We may need to transform nodeTransform.scale from world space to local space.
            Vector3 localScale = gameObject.transform.parent == null ?
                                     layout.LocalScale
                                   : gameObject.transform.parent.InverseTransformVector(layout.LocalScale);

            if (gameObject.transform.localScale != localScale)
            {
                // Scale the object.
                if (mustCallBack)
                {
                    Tweens.Scale(gameObject, localScale, MaxAnimationTime);
                    // FIXME callback?.Invoke(callBackTarget);
                    callback?.Invoke(gameObject);
                    mustCallBack = false;
                }
                else
                {
                    Tweens.Scale(gameObject, localScale, MaxAnimationTime);
                }
            }

            if (gameObject.transform.position != position)
            {
                // Move the object.
                if (mustCallBack)
                {
                    Tweens.Move(gameObject, position, MaxAnimationTime);
                    // FIXME callback?.Invoke(callBackTarget);
                    callback?.Invoke(gameObject);
                    mustCallBack = false;
                }
                else
                {
                    Tweens.Move(gameObject, position, MaxAnimationTime);
                }
            }

            // Shake the object if it was modified.
            if (difference == Difference.Changed)
            {
                if (mustCallBack)
                {
                    Tweens.ShakeRotate(gameObject, MaxAnimationTime / 2, new Vector3(0, 10, 0));
                    // FIXME callback?.Invoke(callBackTarget);
                    callback?.Invoke(gameObject);
                    mustCallBack = false;
                }
                else
                {
                    Tweens.ShakeRotate(gameObject, MaxAnimationTime / 2, new Vector3(0, 10, 0));
                }
            }
            if (mustCallBack)
            {
                callback?.Invoke(gameObject);
            }
        }
    }
}
    