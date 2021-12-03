using OdinSerializer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.GameObjects
{
    /// <summary>
    /// This class implements edege animation. To do so, an
    /// <see cref="IEvaluator"/> instance is processed by calling
    /// <see cref="IEvaluator.Eval(float)"/> in <see cref="Update"/> with a
    /// time parameter that is linear to the elapsed time.
    ///
    /// How to use:
    ///
    /// 1.  Set the evaluator to be processed (<see cref="Evaluator"/>).
    /// 2.  Start the animation with <see cref="DoAnimation(float)"/>.
    /// 3a. If the animation must be ended prematurely, call
    ///     <see cref="FinalizeAnimation"/>. This forwards the evaluator to
    ///     its end (time parameter 1) and inactivates the animation (i.e.,
    ///     <see cref="Update"/> doesn't process <see cref="Evaluator"/> until
    ///     <see cref="DoAnimation(float)"/> is called again).
    /// 3b. <see cref="Update"/> automatically finalizes the animation when
    ///     the duration passed to <see cref="DoAnimation(float)"/> (2.) has
    ///     been exceeded.
    ///
    /// Instances of this class can be reused. That is, after 3a./3b., one can
    /// start again with 1./2.
    /// </summary>
    public class EdgeAnimator : SerializedMonoBehaviour
    {
        /// <summary>
        /// This interface serves as an abstraction layer to decouple the
        /// evaluation process of edge animation (the part carried out by
        /// <see cref="EdgeAnimator"/>) from the implementation of a single
        /// evaluation step (the part carried out by implementors of this
        /// interface).
        /// </summary>
        public interface IEvaluator
        {
            /// <summary>
            /// Evaluate the edge animation at time parameter
            /// <paramref name="t"/>. The domain of <paramref name="t"/> is
            /// [0, 1] where 0 evaluates to the start of the animation and 1
            /// to the end. It is not necessary for the animation to be linear
            /// to the domain of <paramref name="t"/>.
            /// </summary>
            /// <param name="t">Time parameter with domain [0, 1]</param>
            public void Eval(float t);
        }

        /// <summary>
        /// Evaluator to be processed in <see cref="Update"/> when
        /// <see cref="DoAnimation(float)"/> is called.
        /// </summary>
        public IEvaluator Evaluator;

        /// <summary>
        /// The time parameter passed to <see cref="IEvaluator.Eval(float)"/>.
        /// </summary>
        [SerializeField]
        private float time;

        /// <summary>
        /// Time that has elapsed since the last call of
        /// <see cref="DoAnimation(float)"/>. It is incremented by
        /// <see cref="Time.deltaTime"/> in <see cref="Update"/>.
        /// Incrementation is stopped when the animation is finalized.
        /// </summary>
        [SerializeField]
        private float timer;

        /// <summary>
        /// Duration of the animation (> 0).
        /// </summary>
        [SerializeField]
        private float duration;

        /// <summary>
        /// Whether the animation is active. A finalized animation
        /// (<see cref="FinalizeAnimation"/>) is inactive.
        /// </summary>
        private bool active = false;

        /// <summary>
        /// Starts the animation (i.e., <see cref="Update"/> processes
        /// <see cref="Evaluator"/>).
        /// </summary>
        /// <param name="duration">Duration of the animation; lower bound is
        /// clamped to 0.01</param>
        public void DoAnimation(float duration)
        {
            timer = 0;
            // Avoid division by zero (and negative durations).
            this.duration = (float)Math.Max(duration, 0.01);
            active = true;
        }

        /// <summary>
        /// Fast-forward <see cref="Evaluator"/> to its end (i.e., call
        /// <see cref="IEvaluator.Eval(float)"/> with time parameter 1) and
        /// prepare the internal state for the next animation. Can be called
        /// explicitly. Is called by <see cref="Update"/> when the animation
        /// duration has been exceeded. Subsequent calls have no effect until
        /// <see cref="DoAnimation(float)"/> is called again.
        /// </summary>
        public void FinalizeAnimation()
        {
            if (active)
            {
                time = 1; // Fast-forward to end.
                Evaluator.Eval(time);
                active = false;
            }
        }

        /// <summary>
        /// Called by Unity. Processes <see cref="Evaluator"/> (i.e., calls
        /// <see cref="IEvaluator.Eval(float)"/> with corresponding time
        /// parameter). If the animation duration has been exceeded, the
        /// animation is finalized.
        /// (<see cref="FinalizeAnimation"/>).
        /// </summary>
        void Update()
        {
            if (active)
            {
                timer += Time.deltaTime;
                time = timer / duration; // duration > 0
                if (time >= 1) // => timer > duration
                {
                    FinalizeAnimation();
                    // => active = false
                }
                else
                {
                    Evaluator.Eval(time);
                }
            }
        }
    }
}