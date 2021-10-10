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

using System;
using SEE.Layout;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Animates the move of a given GameObject over the full <see cref="AbstractAnimator.MaxAnimationTime"/>.
    /// The scale is instantly applied.
    /// </summary>
    public class MoveAnimator : AbstractAnimator
    {
        /// <summary>
        /// Creates a new animator with a given maximal animation time.
        /// </summary>
        /// <param name="maxAnimationTime">The maximum time the animation is allowed to run.</param>
        public MoveAnimator(float maxAnimationTime = DefaultAnimationTime)
            : base (maxAnimationTime)
        { }

        /// <summary>
        /// Moves the game object to its target location through animation. The scale and style
        /// are instantly applied.
        /// At the end of the animation, the <see cref="Action"/> <paramref name="callback"/>
        /// will be called with <paramref name="gameObject"/> as parameter if <paramref name="callback"/>
        /// is not null. If <paramref name="callback"/> equals null, no callback happens.
        /// </summary>
        /// <param name="gameObject">game object to be animated</param>
        /// <param name="layout">the node transform to be applied</param>
        /// <param name="difference">whether the node attached to <paramref name="gameObject"/> was added,
        /// modified, or deleted w.r.t. to the previous graph</param>
        /// <param name="callback">method to be called when the animation has finished</param>
        protected override void AnimateToInternalWithCallback
                  (GameObject gameObject,
                   ILayoutNode layout,
                   Difference difference,
                   Action<object> callback)
        {
            gameObject.transform.localScale = layout.LocalScale;
            Tweens.Move(gameObject, layout.CenterPosition, MaxAnimationTime, callback);
        }
    }
}