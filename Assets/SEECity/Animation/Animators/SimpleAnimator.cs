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
using UnityEngine;
using SEE.Animation.Internal;

namespace SEE.Animation
{
    /// <summary>
    /// Animates the position and scale of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
    /// If <see cref="Node.WasModified()"/> is true, the GameObject will be shaken to indicate its modification.
    /// </summary>
    public class MoveScaleShakeAnimator : AbstractAnimator
    {
        /// <summary>
        /// See <see cref="AbstractAnimator.AnimateToInternalWithCallback(Node, GameObject, Vector3, Vector3, GameObject, string)"/>.
        /// Moves, scales, and shakes the animated game object.
        /// </summary>
        protected override void AnimateToInternalWithCallback(Node node, GameObject gameObject, Vector3 position, Vector3 scale, GameObject callBackTarget, string callbackName)
        {
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
                "scale", scale,
                "time", MaxAnimationTime
            ));
            // Shake the object.
            if (node.WasModified())
            {
                iTween.ShakeRotation(gameObject, iTween.Hash(
                    "amount", new Vector3(0, 10, 0),
                    "time", MaxAnimationTime / 2,
                    "delay", MaxAnimationTime / 2
                ));
            }
        }
    }
}