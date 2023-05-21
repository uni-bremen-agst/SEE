using System;
using System.Collections.Generic;
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
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<TweenCallback> MorphTo(SEESpline target, float duration)
        {
            // We deactivate the target edge first so it's not visible.
            target.gameObject.SetActive(false);
            // We now use the MorphismOperation to actually move the edge.
            return morphism.AnimateTo((target.Spline, target.gameObject), duration);
        }

        /// <summary>
        /// Morph the spline represented by this edge to the given <paramref name="target"/> spline.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<TweenCallback> MorphTo(BSpline target, float duration)
        {
            return morphism.AnimateTo((target, null), duration);
        }

        /// <summary>
        /// Construct the edge from subsplines.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Construct(float duration)
        {
            return construction.AnimateTo(true, duration);
        }

        /// <summary>
        /// Destruct the edge from subsplines.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Destruct(float duration)
        {
            return construction.AnimateTo(false, duration);
        }

        /// <summary>
        /// Show the edge, revealing it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to reveal the edge.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Show(EdgeAnimationKind animationKind, float duration)
        {
            return ShowOrHide(true, animationKind, duration);
        }

        /// <summary>
        /// Hide the edge, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to hide the edge.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Hide(EdgeAnimationKind animationKind, float duration)
        {
            return ShowOrHide(false, animationKind, duration);
        }

        /// <summary>
        /// Show or hide the edge, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="show">Whether to show or hide the edge.</param>
        /// <param name="animationKind">In which way to animate the edge.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="animationKind"/>
        /// is unknown.</exception>
        public IOperationCallback<Action> ShowOrHide(bool show, EdgeAnimationKind animationKind, float duration)
        {
            return animationKind switch
            {
                EdgeAnimationKind.None => new DummyOperationCallback<Action>(),
                EdgeAnimationKind.Fading => FadeTo(show ? 1.0f : 0.0f, duration),
                EdgeAnimationKind.Buildup => show ? Construct(duration) : Destruct(duration),
                _ => throw new ArgumentOutOfRangeException(nameof(animationKind), "Unknown edge animation kind supplied.")
            };
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

            SplineMorphism AnimateToMorphismAction((BSpline targetSpline, GameObject temporaryGameObject) s, float d)
            {
                SplineMorphism Animator = go.AddOrGetComponent<SplineMorphism>();

                if (Animator.IsActive())
                {
                    // A tween already exists, we simply need to change its target.
                    Animator.ChangeTarget(s.targetSpline);
                }
                else
                {
                    go.MustGetComponent(out SEESpline sourceSpline);
                    Animator.CreateTween(sourceSpline.Spline, s.targetSpline, d)
                            .OnComplete(() =>
                            {
                                if (s.temporaryGameObject != null)
                                {
                                    Destroyer.Destroy(s.temporaryGameObject);
                                }
                            }).Play();
                }

                return Animator;
            }

            go.MustGetComponent(out spline);
            morphism = new MorphismOperation(AnimateToMorphismAction, spline.Spline, null);

            Tween[] ConstructAction(bool extending, float duration)
            {
                return new Tween[]
                {
                    DOTween.To(() => spline.SubsplineEndT,
                               u => spline.SubsplineEndT = u,
                               extending ? 1.0f : 0.0f,
                               duration).SetEase(Ease.InOutExpo).Play()
                };
            }

            construction = new TweenOperation<bool>(ConstructAction, spline.SubsplineEndT >= 1);
            base.OnEnable();
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

        protected override (Color start, Color end) ModifyColor((Color start, Color end) color, Func<Color, Color> modifier)
        {
            return (modifier(color.start), modifier(color.end));
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            morphism.KillAnimator();
            morphism = null;
        }
    }
}