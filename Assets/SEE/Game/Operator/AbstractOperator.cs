using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace SEE.Game.Operator
{
    public abstract class AbstractOperator : MonoBehaviour
    {
        // Collects all non-generic methods
        protected interface IOperation
        {
            void KillAnimator(bool complete = false);
        }

        protected class Operation<T, V> : IOperation
        {
            public Func<V, float, T> AnimateToAction { protected get; set; }

            protected T Animator;
            private IList<IOperation> CompositedOperations = new List<IOperation>();
            public V TargetValue { get; set; } // note: should only be set at beginning!

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
                if (EqualityComparer<V>.Default.Equals(target, TargetValue))
                {
                    // Nothing to be done, we're already where we want to be.
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
        }
    }
}