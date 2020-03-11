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

using UnityEngine;
using SEE.Layout;

namespace SEE.Animation
{
    /// <summary>
    /// Animates the position and scale of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
    /// If a node was modified, the GameObject will be shaken to indicate its modification.
    /// </summary>
    public class MoveScaleShakeAnimator : AbstractAnimator
    {
        /// <summary>
        /// Shakes (if <paramref name="wasModified"/>), scales, and then moves the animated game object.
        /// At the end of the animation, the method <paramref name="callbackName"/> will be called for the
        /// game object <paramref name="callBackTarget"/> with <paramref name="gameObject"/> as 
        /// parameter if <paramref name="callBackTarget"/> is not null. If <paramref name="callBackTarget"/>
        /// equals null, no callback happens.
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="nodeTransform">the node transformation to be applied</param>
        /// <param name="wasModified">whether the node attached to <paramref name="gameObject"/> was modified w.r.t. to the previous graph</param>
        /// <param name="callBackTarget">an optional game object that should receive the callback</param>
        /// <param name="callbackName">the method name of the callback</param>
        protected override void AnimateToInternalWithCallback(GameObject gameObject, NodeTransform nodeTransform, 
                                                              bool wasModified, GameObject callBackTarget, string callbackName)
        {
            // Note: nodeTransform.position.y denotes the ground of the game object, not
            // the center as normally in Unity. The following position is the one in
            // Unity's term where the y co-ordinate denotes the center.
            Vector3 position = nodeTransform.position;
            position.y += nodeTransform.scale.y / 2;

            // First shake the object.
            if (wasModified)
            {
                iTween.ShakeRotation(gameObject, iTween.Hash(
                    "amount", new Vector3(0, 10, 0),
                    "time", MaxAnimationTime / 2,
                    "delay", MaxAnimationTime / 2
                ));
            }
            // Move the object.
            if (callBackTarget != null)
            {
                iTween.MoveTo(gameObject, iTween.Hash(
                    "position", position,
                    "time", MaxAnimationTime,
                    "oncompletetarget", callBackTarget,
                    "oncomplete", callbackName,
                    "oncompleteparams", gameObject
                ));
            }
            else
            {
                iTween.MoveTo(gameObject, iTween.Hash("position", position, "time", MaxAnimationTime));
            }
            // Scale the object.
            iTween.ScaleTo(gameObject, iTween.Hash(
                "scale", nodeTransform.scale,
                "time", MaxAnimationTime
            ));

        }
    }
}