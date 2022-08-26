using System;
using System.Collections.Generic;
using DG.Tweening;
using SEE.GO;
using TinySpline;
using UnityEngine;

namespace SEE.Game.Operator
{
    public class EdgeOperator : AbstractOperator
    {
        private MorphismOperation morphism;
        private TweenOperation<(Color start, Color end)> color;

        private SEESpline spline;

        public void MorphTo(SEESpline target, float duration)
        {
            // We deactivate the target edge first so it's not visible.
            target.gameObject.SetActive(false);
            // We now use the MorphismOperation to actually move the edge.
            morphism.AnimateTo((target.Spline, target.gameObject), duration);
        }

        public void MorphTo(BSpline target, float duration)
        {
            morphism.AnimateTo((target, null), duration);
        }

        public void FadeColorsTo(Color newStartColor, Color newEndColor, float duration)
        {
            color.AnimateTo((newStartColor, newEndColor), duration);
        }

        private void Awake()
        {
            SplineMorphism AnimateToMorphismAction((BSpline targetSpline, GameObject temporaryGameObject) s, float d)
            {
                SplineMorphism Animator = gameObject.AddOrGetComponent<SplineMorphism>();

                if (Animator.IsActive())
                {
                    // A tween already exists, we simply need to change its target.
                    Animator.ChangeTarget(s.targetSpline);
                }
                else
                {
                    gameObject.MustGetComponent(out SEESpline sourceSpline);
                    Animator.CreateTween(sourceSpline.Spline, s.targetSpline, d)
                            .OnComplete(() =>
                            {
                                if (s.temporaryGameObject != null)
                                {
                                    Destroy(s.temporaryGameObject);
                                }
                            }).Play();
                }

                return Animator;
            };

            gameObject.MustGetComponent(out spline);
            morphism = new MorphismOperation(AnimateToMorphismAction, spline.Spline, null);

            List<Tween> AnimateToColorAction((Color start, Color end) colors, float d)
            {
                Tween startTween = DOTween.To(() => spline.GradientColors.start,
                                              c => spline.GradientColors = (c, spline.GradientColors.end),
                                              colors.start, d);
                Tween endTween = DOTween.To(() => spline.GradientColors.end,
                                            c => spline.GradientColors = (spline.GradientColors.start, c),
                                            colors.end, d);
                return new List<Tween> { startTween, endTween };
            };
            color = new TweenOperation<(Color start, Color end)>(AnimateToColorAction, spline.GradientColors);
        }

        // TODO: Maybe refactor this? Type signature is somewhat complex
        protected class MorphismOperation : Operation<SplineMorphism, (BSpline targetSpline, GameObject temporaryGameObject)>
        {
            public override void KillAnimator(bool complete = false)
            {
                if (Animator.IsActive())
                {
                    Animator.tween.Kill();
                    Destroy(Animator);
                    Animator = null;
                }

                base.KillAnimator(complete);
            }

            protected override void ChangeAnimatorTarget((BSpline targetSpline, GameObject temporaryGameObject) newTarget, float duration)
            {
                // No need to kill any old animators, the spline morphism can change its target.
                Animator = AnimateToAction(newTarget, duration);
                if (duration == 0)
                {
                    // We execute the first step immediately. This way, callers can expect the change to
                    // be implemented when control is returned to them, as it would work when
                    // setting the target value manually.
                    Animator.tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }

            public MorphismOperation(Func<(BSpline targetSpline, GameObject temporaryGameObject), float, SplineMorphism> animateToAction, BSpline targetSpline, GameObject temporaryGameObject) : base(animateToAction, (targetSpline, temporaryGameObject))
            {
            }
        }
    }
}