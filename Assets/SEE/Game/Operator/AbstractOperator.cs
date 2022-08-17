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
        protected class OperationCategory<T>
        {
            public Func<T, float, Tween> AnimateToAction { protected get; set; }

            private Tween Tween;
            private IList<Tween> CompositedTweens = new List<Tween>();
            public T TargetValue { get; set; } // note: should only be set at beginning!

            private void CleanTweens(bool complete = false)
            {
                // Kill all old tweens, including those composited with this tween
                if (Tween.IsActive())
                {
                    Tween.Kill(complete);
                }

                foreach (Tween tween in CompositedTweens)
                {
                    if (tween.IsActive())
                    {
                        tween.Kill(complete);
                    }
                }
            }

            public void AnimateTo(T target, float duration, IList<Tween> compositedTweens = null)
            {
                if (EqualityComparer<T>.Default.Equals(target, TargetValue))
                {
                    // Nothing to be done, we're already where we want to be.
                    return;
                }

                CleanTweens();
                Tween = AnimateToAction(target, duration);
                TargetValue = target;
                CompositedTweens = compositedTweens ?? new List<Tween>();
            }
        }
    }
}