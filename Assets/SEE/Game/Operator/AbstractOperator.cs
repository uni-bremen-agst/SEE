using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on a single game object it is attached to, such as a node or an edge.
    /// Operations are represented by the inner <see cref="Operation"/> class, and include things like
    /// movement, color changes, etc.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    /// <remarks>
    /// Operator classes may also expose several "target" values, which are values the object is supposed to have. 
    /// "Supposed" means the object might have this value already, but it also might still animate towards it.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class AbstractOperator : MonoBehaviour
    {
        /// <summary>
        /// Interface for <see cref="Operation{T,V}"/> which collects all non-generic methods.
        /// <see cref="Operation{T,V}"/> should be preferred over this if generic parameters can be used.
        /// </summary>
        protected interface IOperation
        {
            /// <summary>
            /// Kills (i.e., stops) all active animators.
            /// </summary>
            /// <param name="complete">Whether to stop at the current value (<c>false</c>)
            /// or at the target (<c>true</c>)</param>
            void KillAnimator(bool complete = false);
        }

        /// <summary>
        /// Represents an animated operation to a value of type <typeparamref name="T"/>
        /// carried out via an animator of type <typeparamref name="T"/>.
        /// The operation must be properly initialized by setting <see cref="AnimateToAction"/> as well as
        /// an initial <see cref="TargetValue"/> before calling any methods.
        /// To then execute the operation, <see cref="AnimateTo"/> will be called, which will in turn
        /// call <see cref="ChangeAnimatorTarget"/> to replace any active animation with the newly created one.
        /// </summary>
        /// <typeparam name="T">Type of the animator</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        protected class Operation<T, V> : IOperation
        {
            /// <summary>
            /// The function that is called when the animation shall be constructed and played.
            /// The first parameter is the target value, and the second parameter is the duration of the animation.
            /// The return value is the animator controlling this animation.
            /// </summary>
            protected readonly Func<V, float, T> AnimateToAction;

            /// <summary>
            /// The animator that is controlling the current animation.
            /// May be <c>null</c> if no animation is running.
            /// </summary>
            protected T Animator;
            
            /// <summary>
            /// Any operations that are composited (i.e., running together) with this one.
            /// Any entries here will be killed when this operation is killed.
            /// <b>NOTE: This property may not necessarily be kept here.</b>
            /// </summary>
            private IList<IOperation> CompositedOperations = new List<IOperation>();
            
            /// <summary>
            /// The target value that we're animating towards.
            /// </summary>
            public V TargetValue { get; private set; }

            /// <summary>
            /// Instantiates a new operation.
            /// </summary>
            /// <param name="animateToAction">The function that starts the animation.</param>
            /// <param name="targetValue">The initial target value (i.e., the current value).</param>
            protected Operation(Func<V, float, T> animateToAction, V targetValue)
            {
                AnimateToAction = animateToAction;
                TargetValue = targetValue;
            }

            /// <summary>
            /// Kills (i.e., stops) all active animators.
            /// </summary>
            /// <param name="complete">Whether to stop at the current value (<c>false</c>)
            /// or at the target (<c>true</c>)</param>
            public virtual void KillAnimator(bool complete = false)
            {
                // Kill all old animators, including those composited with this tween
                foreach (IOperation operation in CompositedOperations)
                {
                    operation.KillAnimator(complete);
                }
            }

            /// <summary>
            /// Changes the target of the animation from the current target value to <paramref name="newTarget"/>.
            /// </summary>
            /// <param name="newTarget">The new target value.</param>
            /// <param name="duration">The duration of the new animation.</param>
            protected virtual void ChangeAnimatorTarget(V newTarget, float duration)
            {
                // Usual approach: Kill old animator and replace it with new one
                KillAnimator();
                Animator = AnimateToAction(newTarget, duration);
            }

            /// <summary>
            /// Animate to the new <paramref name="target"/> value, taking <paramref name="duration"/> seconds.
            /// If the target value should be set immediately (without an animation),
            /// set the <paramref name="duration"/> to 0. 
            /// </summary>
            /// <param name="target">The new target value that shall be animated towards.</param>
            /// <param name="duration">The desired length of the animation.</param>
            /// <param name="compositedOperations">Any operations running in tandem with this one. They will
            /// be killed once this operation is killed. Parameter may be removed in the future.</param>
            /// <exception cref="ArgumentOutOfRangeException">If <paramref name="duration"/> is negative.</exception>
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

        /// <summary>
        /// An <see cref="Operation{T,V}"/> which uses one or multiple <see cref="Tween"/>s as the animator.
        /// Use this operation for any animations doable with <see cref="DOTween"/>.
        /// Note that the type of the target value <typeparamref name="V"/> still has to be specified.
        /// </summary>
        /// <typeparam name="V">The type of the target value</typeparam>
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
