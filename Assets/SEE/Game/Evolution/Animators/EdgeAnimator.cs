using Assets.SEE.GameObjects;
using OdinSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game.Evolution.Animators
{
    /// <summary>
    /// This class implements the edege animation of
    /// <see cref="SEE.Game.EvolutionRenderer"/>. To do so, it stores a list
    /// of <see cref="IEvaluator"/> instances and processes them by calling
    /// <see cref="IEvaluator.Eval(float)"/> in <see cref="Update"/> with a
    /// time parameter that is linear to the elapsed time.
    ///
    /// How to use:
    ///
    /// 1.  Register the evaluators to be processed with
    ///     <see cref="Add(IEvaluator)"/>.
    /// 2.  Start the animation with <see cref="DoAnimation(float)"/>.
    /// 3a. If the animation must be ended prematurely, call
    ///     <see cref="FinalizeAnimation"/>. This forwards all evaluators to
    ///     their end (time parameter 1), clears the list of registered
    ///     evaluators (step 1.), and  inactivates the animation (i.e.,
    ///     <see cref="Update"/> doesn't process any evaluator until
    ///     <see cref="DoAnimation(float)"/> is called again).
    /// 3b. <see cref="Update"/> automatically finalizes the animation when
    ///     the duration passed to <see cref="DoAnimation(float)"/> (2.) has
    ///     been exceeded.
    ///
    /// Instances of this class can be reused. That is, after 3a./3b., one can
    /// start again with 1. It is therefore sufficient to create this
    /// component once at the start of the application and then use it
    /// whenever one or more edges need to be animated (following the pattern
    /// described above).
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
        /// Evaluators to be processed in <see cref="Update"/> when
        /// <see cref="DoAnimation(float)"/> is called.
        /// </summary>
        [SerializeField]
        private List<IEvaluator> evaluators = new List<IEvaluator>();

        /// <summary>
        /// The time parameter passed to <see cref="IEvaluator.Eval(float)"/>.
        /// </summary>
        [SerializeField]
        private float time;

        /// <summary>
        /// Time that has elapsed since the last call of
        /// <see cref="DoAnimation(float)"/>. Is incremented by
        /// <see cref="Time.deltaTime"/> in <see cref="Update"/>.
        /// Incrementation is stopped when animation is finalized.
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
        /// Registers the given evaluator. Evaluators cannot be registered if
        /// an animation is in progress (i.e., <see cref="active"/> is true).
        /// </summary>
        /// <param name="evaluator">Evaluator to be added</param>
        /// <returns>Whether the evaluator was registered</returns>
        public bool Add(IEvaluator evaluator)
        {
            if (!active)
            {
                evaluators.Add(evaluator);
            }
            return !active;
        }

        /// <summary>
        /// Starts the animation (i.e., <see cref="Update"/> processes all
        /// registered evaluators).
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
        /// Fast-forward all evaluators to their end (i.e., call
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
                foreach (var m in evaluators)
                {
                    m.Eval(time);
                }
                // Prepare for the next animation.
                evaluators.Clear();
                active = false;
            }
        }


        /// <summary>
        /// Processes all registered evaluators (i.e., calls
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
                    foreach (var m in evaluators)
                    {
                        m.Eval(time);
                    }
                }
            }
        }
    }
}