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

        protected override void InternalAnimateTo(GameObject gameObject, Vector3 position, Vector3 localScale, float duration, Action<object> callbackWhenAnimationFinished, Action<float> moveCallback = null)
        {
            IOperationCallback<Action> callback;
            if (gameObject.IsNode())
            {
                NodeOperator nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                callback = nodeOperator.MoveTo(position, duration, updateEdges: false);
            }
            else if (gameObject.IsEdge())
            {
                EdgeOperator edgeOperator = gameObject.AddOrGetComponent<EdgeOperator>();
                callback = edgeOperator.GlowIn(duration);
            }
            else
            {
                throw new Exception($"MoveAnimator.InternalAnimateTo({gameObject.name}) applied although the argument is neither node nor edge.\n");
            }
            callback.SetOnStart(() => moveCallback?.Invoke(duration));
            callback.SetOnComplete(() => callbackWhenAnimationFinished?.Invoke(gameObject));
        }
    }
}