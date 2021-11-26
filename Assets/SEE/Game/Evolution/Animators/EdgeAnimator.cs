using Assets.SEE.GameObjects;
using OdinSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game.Evolution.Animators
{
    /// <summary>
    /// This class implements the edege animation (through spline morphism)
    /// of <see cref="SEE.Game.EvolutionRenderer"/>. To do so, it stores a
    /// list of <see cref="SplineMorphism"/> instances and processes them by
    /// calling <see cref="SplineMorphism.Eval(double)"/> in
    /// <see cref="Update"/> (with corresponding time parameter).
    ///
    /// How to use:
    ///
    /// 1.  Add the spline morphisms to be processed with
    ///     <see cref="Add(SplineMorphism)"/>.
    /// 2.  Start the animation with <see cref="DoAnimation(float)"/>.
    /// 3a. If the animation must be ended prematurely, call
    ///     <see cref="FinalizeAnimation"/>. This forwards all morphisms to
    ///     their target, clears the list of registered morphisms (1.), and
    ///     inactivates the animation (i.e., <see cref="Update"/> doesn't
    ///     process any morphism until <see cref="DoAnimation(float)"/> is
    ///     called again).
    /// 3b. <see cref="Update"/> automatically finalizes the animation when
    ///     the duration passed to <see cref="DoAnimation(float)"/> (2.) has
    ///     been exceeded.
    ///
    /// Instances of this class can be reused. That is, After 3a./3b., one can
    /// start again with 1. It is therefore sufficient to create this
    /// component once at the start and then use it whenever one or more edges
    /// need to be animated (following the pattern described above).
    /// </summary>
    public class EdgeAnimator : SerializedMonoBehaviour
    {
        /// <summary>
        /// Morphisms to be processed in <see cref="Update"/>.
        /// </summary>
        [SerializeField]
        private List<SplineMorphism> morphisms = new List<SplineMorphism>();

        /// <summary>
        /// The time parameter passed to
        /// <see cref="SplineMorphism.Eval(double)"/>.
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
        [SerializeField]
        private bool active = false;

        /// <summary>
        /// Registers the given morphism.
        /// </summary>
        /// <param name="morphism"></param>
        public void Add(SplineMorphism morphism)
        {
            morphisms.Add(morphism);
        }

        /// <summary>
        /// Starts the animation (i.e., <see cref="Update"/> processes all
        /// registered morphisms).
        /// </summary>
        /// <param name="duration">Duration of the animation; lower bound is clamped to 0.01</param>
        public void DoAnimation(float duration)
        {
            timer = 0;
            // Avoid divition by zero (and negative durations).
            this.duration = (float)Math.Max(duration, 0.01);
            active = true;
        }

        /// <summary>
        /// Fast-forward all spline morphisms to their target and prepare the
        /// internal state for the next animation. Can be called explicitly.
        /// Is called by <see cref="Update"/> when the animation duration has
        /// been exceeded. Subsequent calls have no effect until
        /// <see cref="DoAnimation(float)"/> is called again.
        /// </summary>
        public void FinalizeAnimation()
        {
            if (active)
            {
                time = 1; // Fast-forward to target.
                foreach (var m in morphisms)
                {
                    m.Eval(time);
                }
                // Prepare for the next animation.
                morphisms.Clear();
                active = false;
            }
        }


        /// <summary>
        /// Processes all registered morphisms (i.e., calls
        /// <see cref="SplineMorphism.Eval(double)"/> with corresponding time
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
                if (time >= 1)
                {
                    FinalizeAnimation();
                    // => active = false
                }
                else
                {
                    foreach (var m in morphisms)
                    {
                        m.Eval(time);
                    }
                }
            }
        }
    }
}