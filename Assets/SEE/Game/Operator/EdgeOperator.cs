using System;
using System.Collections.Generic;
using DG.Tweening;
using SEE.GO;
using TinySpline;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the edge it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public class EdgeOperator : AbstractOperator
    {
        /// <summary>
        /// Operation handling edge morphing.
        /// </summary>
        private MorphismOperation morphism;
        
        /// <summary>
        /// Operation handling changes to the color gradient of the edge.
        /// </summary>
        private TweenOperation<(Color start, Color end)> color;

        /// <summary>
        /// The <see cref="SEESpline"/> represented by this edge.
        /// </summary>
        private SEESpline spline;

        #region Public API

        /// <summary>
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline,
        /// destroying the associated game object of <paramref name="target"/> once the animation is complete.
        /// This will also disable the <paramref name="target"/>'s game object immediately so it's invisible.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        public void MorphTo(SEESpline target, float duration)
        {
            // We deactivate the target edge first so it's not visible.
            target.gameObject.SetActive(false);
            // We now use the MorphismOperation to actually move the edge.
            morphism.AnimateTo((target.Spline, target.gameObject), duration);
        }

        /// <summary>
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        public void MorphTo(BSpline target, float duration)
        {
            morphism.AnimateTo((target, null), duration);
        }

        /// <summary>
        /// Change the color gradient of the edge to a new gradient from <paramref name="newStartColor"/> to
        /// <paramref name="newEndColor"/>.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        public void ChangeColorsTo(Color newStartColor, Color newEndColor, float duration)
        {
            color.AnimateTo((newStartColor, newEndColor), duration);
        }

        #endregion

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
        /// <summary>
        /// An <see cref="Operation{T,V}"/> which uses a <see cref="SplineMorphism"/> as the animator.
        /// Use this operation for morphing edges.
        /// The target value consists of the target spline, as well as an optional temporary game object associated
        /// to the spline. If the latter is given, it will be destroyed upon completion.
        /// </summary>
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
                    // be implemented when control is returned to them, the same way it would work when
                    // setting the target value manually.
                    Animator.tween.ManualUpdate(Time.deltaTime, Time.unscaledDeltaTime);
                }
            }

            public MorphismOperation(Func<(BSpline targetSpline, GameObject temporaryGameObject), float, SplineMorphism> animateToAction, 
                                     BSpline targetSpline, GameObject temporaryGameObject) : base(animateToAction, (targetSpline, temporaryGameObject))
            {
            }
        }
    }
}