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

using SEE.Game.Operator;
using SEE.GO;
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
        /// Creates a new animator with a given maximal animation time.
        /// </summary>
        /// <param name="maxAnimationTime">The maximum time the animation is allowed to run.</param>
        public MoveScaleShakeAnimator(float maxAnimationTime = DefaultAnimationTime)
            : base(maxAnimationTime)
        { }

        /// <summary>
        /// Moves, scales, and shakes <paramref name="gameObject"/> as a sequence of animations.
        /// </summary>
        /// <param name="gameObject">the game object to be animated</param>
        /// <param name="position">the final destination of <paramref name="gameObject"/></param>
        /// <param name="localScale">the final scale of <paramref name="gameObject"/></param>
        /// <param name="duration">the duration of the whole animation in seconds</param>
        /// <param name="callbackWhenAnimationFinished">the method to be called when the animation has finished</param>
        /// <param name="moveCallback">the method to be called when the move animation is about to start</param>
        protected override void InternalAnimateTo(GameObject gameObject,
                                                Vector3 position,
                                                Vector3 localScale,
                                                float duration,
                                                Action<object> callbackWhenAnimationFinished,
                                                Action<float> moveCallback = null)
        {
            if (gameObject.IsNode())
            {
                NodeOperator nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                nodeOperator.MoveTo(position, duration).SetOnStart(() => moveCallback?.Invoke(duration));
                nodeOperator.ScaleTo(localScale, duration).SetOnComplete(() => callbackWhenAnimationFinished?.Invoke(gameObject));
            }
            else
            {
                throw new Exception($"MoveScaleShakeRotate.InternalAnimateTo({gameObject.name}) applied although the argument is not a node.\n");
            }
        }
    }
}

