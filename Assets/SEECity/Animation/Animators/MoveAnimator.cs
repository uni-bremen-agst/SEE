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

namespace SEE.Animation
{
    /// <summary>
    /// Animates the position of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
    /// The scale is instantly applied.
    /// </summary>
    public class MoveAnimator : AbstractAnimator
    {
        /// <summary>
        /// See <see cref="AbstractAnimator.AnimateToInternalWithCallback(Node, GameObject, Vector3, Vector3, GameObject, string)"/>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="gameObject"></param>
        /// <param name="position"></param>
        /// <param name="scale"></param>
        /// <param name="callBackTarget"></param>
        /// <param name="callbackName"></param>
        protected override void AnimateToInternalWithCallback(Node node, GameObject gameObject, Vector3 position, Vector3 scale, GameObject callBackTarget, string callbackName)
        {
            gameObject.transform.localScale = scale;
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
                iTween.MoveTo(gameObject, iTween.Hash(
                                          "position", position,
                                          "time", MaxAnimationTime
                ));
            }
        }
    }
}