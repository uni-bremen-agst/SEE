using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace SEE.Game.Operator
{
    [DisallowMultipleComponent]
    public abstract class AbstractOperator : MonoBehaviour
    {
        // Collects all non-generic methods
        protected interface IOperation
        {
            void KillAnimator(bool complete = false);
        }

        protected class Operation<T, V> : IOperation
        {
            protected readonly Func<V, float, T> AnimateToAction;

            protected T Animator;
            private IList<IOperation> CompositedOperations = new List<IOperation>();
            public V TargetValue { get; private set; } // note: should only be set at beginning!

            protected Operation(Func<V, float, T> animateToAction, V targetValue)
            {
                AnimateToAction = animateToAction;
                TargetValue = targetValue;
            }

            public virtual void KillAnimator(bool complete = false)
            {
                // Kill all old animators, including those composited with this tween
                foreach (IOperation operation in CompositedOperations)
                {
                    operation.KillAnimator(complete);
                }
            }

            protected virtual void ChangeAnimatorTarget(V newTarget, float duration)
            {
                // Usual approach: Kill old animator and replace it with new one
                KillAnimator();
                Animator = AnimateToAction(newTarget, duration);
            }

            public void AnimateTo(V target, float duration, IList<IOperation> compositedOperations = null)
            {
                if (duration < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero!");
                }
                if (EqualityComparer<V>.Default.Equals(target, TargetValue) && duration > 0)
                {
                    // Nothing to be done, we're already where we want to be.
                    // If duration is 0, however, we must trigger the change immediately.
                    return;
                }

                ChangeAnimatorTarget(target, duration);
                TargetValue = target;
                CompositedOperations = compositedOperations ?? new List<IOperation>();
            }
        }

        protected class TweenOperation<V> : Operation<IList<Tween>, V>
        {
            public override void KillAnimator(bool complete = false)
            {
                if (Animator != null)
                {
                    foreach (Tween tween in Animator)
                    {
                        if (tween.IsActive())
                        {
                            tween.Kill(complete);
                        }
                    }
                }

                base.KillAnimator(complete);
            }

            protected override void ChangeAnimatorTarget(V newTarget, float duration)
            {
                base.ChangeAnimatorTarget(newTarget, duration);
                if (duration == 0)
                {
                    foreach (Tween tween in Animator)
                    {
                        // We execute the first step immediately. This way, callers can expect the change to
                        // be implemented when control is returned to them, as it would work when
                        // setting the target value manually.
                        tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                    }
                }
            }

            public TweenOperation(Func<V, float, IList<Tween>> animateToAction, V targetValue) : base(animateToAction, targetValue)
            {
            }
        }
    }
}
