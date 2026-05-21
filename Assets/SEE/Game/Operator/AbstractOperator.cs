using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using DG.Tweening;
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
        /// The duration of the animation in seconds to use as a basis.
        /// Operator methods will apply factors to this value to determine the actual duration.
        /// </summary>
        protected abstract float BaseAnimationDuration
        {
            get;
        }

        /// <summary>
        /// Converts the given <paramref name="factor"/> to the effective duration an animation
        /// with this factor would take.
        /// </summary>
        /// <param name="factor">The factor to convert.</param>
        /// <returns>The effective duration.</returns>
        [Pure]
        public float ToDuration(float factor)
        {
            return factor * BaseAnimationDuration;
        }

        /// <summary>
        /// Converts the given <paramref name="duration"/> to the factor that would be used
        /// for an animation of this duration.
        /// </summary>
        /// <param name="duration">The duration to convert.</param>
        /// <returns>The factor that would be used for an animation of this duration.</returns>
        [Pure]
        public float ToFactor(float duration)
        {
            return BaseAnimationDuration > 0 ? duration / BaseAnimationDuration : 0;
        }

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
            /// or at the target (<c>true</c>).</param>
            void KillAnimator(bool complete = false);

            /// <summary>
            /// Whether the operation is currently running.
            /// </summary>
            bool IsRunning { get; }

            /// <summary>
            /// A list of all operations that are conflicting with this one.
            /// If an operation in this list is running at the time this operation is started,
            /// the conflicting operation will be killed.
            /// </summary>
            public IList<IOperation> ConflictingOperations { get; }
        }

        /// <summary>
        /// Represents an animated operation to a value of type <typeparamref name="T"/>
        /// carried out via an animator of type <typeparamref name="T"/>.
        /// The operation must be properly initialized by setting <see cref="AnimateToAction"/> as well as
        /// an initial <see cref="TargetValue"/> before calling any methods.
        /// In order to execute the operation, the user will call <see cref="AnimateTo"/>, which will itself
        /// call <see cref="ChangeAnimatorTarget"/> to replace any active animation with the newly created one.
        /// </summary>
        /// <typeparam name="T">Type of the animator</typeparam>
        /// <typeparam name="V">Type of the value</typeparam>
        /// <typeparam name="C">Type of the callback delegate</typeparam>
        protected abstract class Operation<T, V, C> : IOperation where C : MulticastDelegate
        {
            /// <summary>
            /// The function that is called when the animation shall be constructed and played.
            /// The first parameter is the target value, and the second parameter is the duration of the animation.
            /// The return value is the animator controlling this animation.
            /// </summary>
            protected readonly Func<V, float, T> AnimateToAction;

            /// <summary>
            /// A set of all operations that are conflicting with this one.
            /// If an operation in this set is running at the time this operation is started,
            /// the conflicting operation will be killed.
            /// </summary>
            public IList<IOperation> ConflictingOperations { get; }

            /// <summary>
            /// The animator that is controlling the current animation.
            /// May be <c>null</c> if no animation is running.
            /// </summary>
            protected T Animator { get; set; }

            /// <summary>
            /// The equality comparer used to check whether the target value has changed.
            /// </summary>
            protected IEqualityComparer<V> EqualityComparer { get; }

            /// <summary>
            /// The target value that we're animating towards.
            /// </summary>
            public V TargetValue { get; private set; }

            /// <summary>
            /// Whether the operation is currently running.
            /// </summary>
            public abstract bool IsRunning { get; }

            /// <summary>
            /// Instantiates a new operation.
            /// </summary>
            /// <param name="animateToAction">The function that starts the animation.</param>
            /// <param name="targetValue">The initial target value (i.e., the current value).</param>
            /// <param name="equalityComparer">
            /// The equality comparer used to check whether the target value has changed.
            /// If <c>null</c>, the default equality comparer for <typeparamref name="V"/> is used.
            /// </param>
            /// <param name="conflictingOperations">
            /// The operations that are conflicting with this one.
            /// Note that this operation will also be added to the conflicting operations of the given operations.
            /// Hence, this is always a bidirectional relationship.
            /// </param>
            protected Operation(Func<V, float, T> animateToAction, V targetValue,
                                IEqualityComparer<V> equalityComparer = null,
                                IEnumerable<IOperation> conflictingOperations = null)
            {
                AnimateToAction = animateToAction;
                TargetValue = targetValue;
                EqualityComparer = equalityComparer ?? EqualityComparer<V>.Default;
                ConflictingOperations = conflictingOperations?.ToList() ?? new List<IOperation>();
                foreach (IOperation conflictingOperation in ConflictingOperations)
                {
                    conflictingOperation.ConflictingOperations.Add(this);
                }
            }

            /// <summary>
            /// Kills (i.e., stops) all active animators.
            /// </summary>
            /// <param name="complete">Whether to stop at the current value (<c>false</c>)
            /// or at the target (<c>true</c>).</param>
            public abstract void KillAnimator(bool complete = false);

            /// <summary>
            /// Changes the target of the animation from the current target value to <paramref name="newTarget"/>.
            /// </summary>
            /// <param name="newTarget">The new target value.</param>
            /// <param name="duration">The duration of the new animation.</param>
            /// <param name="complete">Whether to complete any existing animations before starting this one.</param>
            protected virtual void ChangeAnimatorTarget(V newTarget, float duration, bool complete = false)
            {
                // Usual approach: Kill old animator and replace it with new one
                KillAnimator(complete);
                // We also need to kill any currently running, conflicting operations.
                foreach (IOperation operation in ConflictingOperations.Where(x => x.IsRunning))
                {
                    operation.KillAnimator(true);
                }
                Animator = AnimateToAction(newTarget, duration);
            }

            /// <summary>
            /// Animate to the new <paramref name="target"/> value, taking <paramref name="duration"/> seconds.
            /// If the target value should be set immediately (without an animation),
            /// set the <paramref name="duration"/> to 0.
            /// </summary>
            /// <param name="target">The new target value that shall be animated towards.</param>
            /// <param name="duration">The desired length of the animation.</param>
            /// <exception cref="ArgumentOutOfRangeException">If <paramref name="duration"/> is negative.</exception>
            /// <returns>An operation callback for the requested animation.</returns>
            public IOperationCallback<C> AnimateTo(V target, float duration)
            {
                if (duration < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero!");
                }

                if (EqualityComparer.Equals(target, TargetValue) && duration > 0)
                {
                    // Nothing to be done, we're already where we want to be.
                    // If duration is 0, however, we must trigger the change immediately.
                    return new DummyOperationCallback<C>();
                }

                ChangeAnimatorTarget(target, duration);
                TargetValue = target;
                // If the duration is zero, the change has already been applied, so the callback never triggers.
                // Hence, we create a dummy callback that triggers its respective methods immediately on registration.
                return duration == 0 ? new DummyOperationCallback<C>() : AnimatorCallback;
            }

            /// <summary>
            /// The callback that shall be returned at the end of <see cref="AnimateTo"/>.
            /// </summary>
            protected abstract IOperationCallback<C> AnimatorCallback { get; }
        }

        /// <summary>
        /// An <see cref="Operation{T,V}"/> which uses one or multiple <see cref="Tween"/>s as the animator.
        /// Use this operation for any animations doable with <see cref="DOTween"/>.
        /// Note that the type of the target value <typeparamref name="V"/> still has to be specified.
        /// </summary>
        /// <typeparam name="V">The type of the target value</typeparam>
        protected class TweenOperation<V> : Operation<IList<Tween>, V, Action>
        {
            public override bool IsRunning => Animator?.Any(x => x.IsActive()) ?? false;

            public override void KillAnimator(bool complete = false)
            {
                if (Animator != null)
                {
                    foreach (Tween tween in Animator)
                    {
                        if (tween != null && tween.IsActive())
                        {
                            tween.Kill(complete);
                        }
                    }
                }
            }

            protected override void ChangeAnimatorTarget(V newTarget, float duration, bool complete = false)
            {
                base.ChangeAnimatorTarget(newTarget, duration, complete);
                if (duration == 0)
                {
                    // We execute the first step immediately. This way, callers can expect the change to
                    // be implemented when control is returned to them, the same way it would work when
                    // setting the target value manually.
                    foreach (Tween tween in Animator)
                    {
                        tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                    }
                }
            }

            protected override IOperationCallback<Action> AnimatorCallback =>
                new AndCombinedOperationCallback<TweenCallback>(Animator.Select(x => new TweenOperationCallback(x)), x => new TweenCallback(x));

            public TweenOperation(Func<V, float, IList<Tween>> animateToAction, V targetValue,
                                  IEqualityComparer<V> equalityComparer = null, IEnumerable<IOperation> conflictingOperations = null)
                : base(animateToAction, targetValue, equalityComparer, conflictingOperations)
            {
            }
        }
    }
}
