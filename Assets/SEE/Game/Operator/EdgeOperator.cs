using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using TinySpline;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the edge it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public partial class EdgeOperator : GraphElementOperator<(Color start, Color end)>
    {
        /// <summary>
        /// Operation handling edge morphing.
        /// </summary>
        private MorphismOperation morphism;

        /// <summary>
        /// Operation handling the construction of edges from subsplines.
        /// </summary>
        private TweenOperation<bool> construction;

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
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<TweenCallback> MorphTo(SEESpline target, float factor = 1)
        {
            // We deactivate the target edge first so it's not visible.
            target.gameObject.SetActive(false);
            // We now use the MorphismOperation to actually move the edge.
            return morphism.AnimateTo((target.Spline, target.gameObject), ToDuration(factor));
        }

        /// <summary>
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<TweenCallback> MorphTo(BSpline target, float factor = 1)
        {
            return morphism.AnimateTo((target, null), ToDuration(factor));
        }

        /// <summary>
        /// Construct the edge from subsplines.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Construct(float factor = 1)
        {
            return construction.AnimateTo(true, ToDuration(factor));
        }

        /// <summary>
        /// Destruct the edge from subsplines.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Destruct(float factor = 1)
        {
            return construction.AnimateTo(false, ToDuration(factor));
        }

        /// <summary>
        /// Show the edge, revealing it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to reveal the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Show(EdgeAnimationKind animationKind, float factor = 1)
        {
            return ShowOrHide(true, animationKind, factor);
        }

        /// <summary>
        /// Hide the edge, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to hide the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        public IOperationCallback<Action> Hide(EdgeAnimationKind animationKind, float factor = 1)
        {
            return ShowOrHide(false, animationKind, factor);
        }

        /// <summary>
        /// Show or hide the edge, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="show">Whether to show or hide the edge.</param>
        /// <param name="animationKind">In which way to animate the edge.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="animationKind"/>
        /// is unknown.</exception>
        public IOperationCallback<Action> ShowOrHide(bool show, EdgeAnimationKind animationKind, float factor = 1)
        {
            return animationKind switch
            {
                EdgeAnimationKind.None => new DummyOperationCallback<Action>(),
                EdgeAnimationKind.Fading => FadeTo(show ? 1.0f : 0.0f, factor),
                EdgeAnimationKind.Buildup => show ? Construct(factor) : Destruct(factor),
                _ => throw new ArgumentOutOfRangeException(nameof(animationKind), "Unknown edge animation kind supplied.")
            };
        }

        /// <summary>
        /// Enables or disables the data flow animation to indicate edge direction.
        /// </summary>
        /// <param name="enable">Enable or disable animation.</param>
        public void AnimateDataFlow(bool enable = true)
        {
            if (enable)
            {
                gameObject.AddOrGetComponent<EdgeDirectionVisualizer>();
            }
            else
            {
                EdgeDirectionVisualizer edfv = gameObject.GetComponent<EdgeDirectionVisualizer>();
                Destroyer.Destroy(edfv);
            }
        }

        #endregion

        protected override IEnumerable<Color> AsEnumerable((Color start, Color end) color)
        {
            yield return color.start;
            yield return color.end;
        }

        protected override void OnEnable()
        {
            // Assigned so that the expensive getter isn't called everytime.
            GameObject go = gameObject;
            spline = go.MustGetComponent<SEESpline>();
            base.OnEnable();

            morphism = new MorphismOperation(AnimateToMorphismAction, spline.Spline, null);
            construction = new TweenOperation<bool>(ConstructAction, spline.SubsplineEndT >= 1);
            return;

            SplineMorphism AnimateToMorphismAction((BSpline targetSpline, GameObject temporaryGameObject) s, float d)
            {
                SplineMorphism animator = go.AddOrGetComponent<SplineMorphism>();

                if (animator.IsActive())
                {
                    // A tween already exists, we simply need to change its target.
                    animator.ChangeTarget(s.targetSpline);
                }
                else
                {
                    SEESpline sourceSpline = go.MustGetComponent<SEESpline>();
                    animator.CreateTween(sourceSpline.Spline, s.targetSpline, d)
                            .OnComplete(() =>
                            {
                                if (s.temporaryGameObject != null)
                                {
                                    Destroyer.Destroy(s.temporaryGameObject);
                                }
                            }).Play();
                }

                return animator;
            }

            Tween[] ConstructAction(bool extending, float duration)
            {
                return new Tween[]
                {
                    DOTween.To(() => spline.SubsplineEndT,
                               u => spline.SubsplineEndT = u,
                               extending ? 1.0f : 0.0f,
                               duration).SetEase(Ease.InOutCubic).Play()
                };
            }
        }

        protected override TweenOperation<(Color start, Color end)> InitializeColorOperation()
        {
            Tween[] AnimateToColorAction((Color start, Color end) colors, float d)
            {
                Tween startTween = DOTween.To(() => spline.GradientColors.start,
                                              c => spline.GradientColors = (c, spline.GradientColors.end),
                                              colors.start, d);
                Tween endTween = DOTween.To(() => spline.GradientColors.end,
                                            c => spline.GradientColors = (spline.GradientColors.start, c),
                                            colors.end, d);
                return new[] { startTween.Play(), endTween.Play() };
            }

            return new TweenOperation<(Color start, Color end)>(AnimateToColorAction, spline.GradientColors);
        }

        protected override Tween[] BlinkAction(int count, float duration)
        {
            // If we're interrupting another blinking, we need to make sure the color still has the correct value.
            spline.GradientColors = Color.TargetValue;

            if (count != 0)
            {
                Color newStart = Color.TargetValue.start.Invert();
                Color newEnd = Color.TargetValue.end.Invert();
                float loopDuration = duration / (2 * Mathf.Abs(count));

                Tween startTween = DOTween.To(() => spline.GradientColors.start,
                                              c => spline.GradientColors = (c, spline.GradientColors.end),
                                              newStart, loopDuration);
                Tween endTween = DOTween.To(() => spline.GradientColors.end,
                                            c => spline.GradientColors = (spline.GradientColors.start, c),
                                            newEnd, loopDuration);
                return new[] { startTween, endTween }.Select(x => x.SetEase(Ease.Linear).SetLoops(2 * count, LoopType.Yoyo).Play()).ToArray();
            }
            else
            {
                return new Tween[] { };
            }
        }

        protected override (Color start, Color end) ModifyColor((Color start, Color end) color, Func<Color, Color> modifier)
        {
            return (modifier(color.start), modifier(color.end));
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            morphism.KillAnimator();
            morphism = null;
            construction.KillAnimator();
            construction = null;
        }
    }
}
