using System;
using DG.Tweening;
using SEE.GO;
using SEE.Utils;
using TinySpline;
using UnityEngine;

namespace SEE.Game.Operator
{
    public partial class EdgeOperator
    {
        // TODO: Maybe refactor this? Type signature is somewhat complex
        /// <summary>
        /// An <see cref="Operation{T,V}"/> which uses a <see cref="SplineMorphism"/> as the animator.
        /// Use this operation for morphing edges.
        /// The target value consists of the target spline, as well as an optional temporary game object associated
        /// to the spline. If the latter is given, it will be destroyed upon completion.
        /// </summary>
        protected class MorphismOperation : Operation<SplineMorphism, (BSpline targetSpline, GameObject temporaryGameObject), TweenCallback>
        {
            public override void KillAnimator(bool complete = false)
            {
                if (Animator != null && Animator.IsActive())
                {
                    Animator.tween.Kill(complete);
                    Destroyer.Destroy(Animator);
                    Animator = null;
                }
            }

            protected override void ChangeAnimatorTarget((BSpline targetSpline, GameObject temporaryGameObject) newTarget, float duration, bool complete = false)
            {
                // No need to kill any old animators, the spline morphism can change its target.
                Animator = AnimateToAction(newTarget, duration);
                if (duration == 0)
                {
                    // We execute the first step immediately. This way, callers can expect the change to
                    // be implemented when control is returned to them, the same way it would work when
                    // setting the target value manually.
                    Animator.tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }

            protected override IOperationCallback<TweenCallback> AnimatorCallback => new TweenOperationCallback(Animator.tween);

            public MorphismOperation(Func<(BSpline targetSpline, GameObject temporaryGameObject), float, SplineMorphism> animateToAction,
                                     BSpline targetSpline, GameObject temporaryGameObject)
                : base(animateToAction, (targetSpline, temporaryGameObject))
            {
            }
        }
    }
}