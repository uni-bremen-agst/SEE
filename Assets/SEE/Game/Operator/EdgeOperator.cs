using System;
using DG.Tweening;
using SEE.DataModel;
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
    public partial class EdgeOperator : GraphElementOperator, IObserver<ChangeEvent>
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
        /// Change the color gradient of the edge to a new gradient from <paramref name="newStartColor"/> to
        /// <paramref name="newEndColor"/>.
        /// </summary>
        /// <param name="newStartColor">The starting color of the gradient this edge should animate towards.</param>
        /// <param name="newEndColor">The ending color of the gradient this edge should animate towards.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <param name="useAlpha">Whether to incorporate the alpha values from the given colors.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ChangeColorsTo(Color newStartColor, Color newEndColor,
                                                         float duration, bool useAlpha = true)
        {
            if (!useAlpha)
            {
                newStartColor.a = color.TargetValue.start.a;
                newEndColor.a = color.TargetValue.end.a;
            }
            return color.AnimateTo((newStartColor, newEndColor), duration);
        }

        /// <summary>
        /// Flashes an edge in its inverted color for a short <paramref name="duration"/>.
        /// Note that this animation is not controlled by an operation and thus not necessarily synchronized.
        /// </summary>
        /// <param name="duration">Amount of time the flashed color shall fade out for.</param>
        public void HitEffect(float duration = 0.5f)
        {
            // NOTE: This is not controlled by an operation. HighlightEffect itself controls the animation.
            //       Should be alright because overlapping animations aren't a big problem here.
            highlightEffect.hitFxFadeOutDuration = duration;
            highlightEffect.hitFxColor = Color.Lerp(color.TargetValue.start, color.TargetValue.end, 0.5f).Invert();
            highlightEffect.HitFX();
        }

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
        protected override IOperationCallback<Action> Construct(float duration)
        {
            return construction.AnimateTo(true, duration);
        }

        /// <summary>
        /// Destruct the edge from subsplines.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        protected override IOperationCallback<Action> Destruct(float duration)
        {
            return construction.AnimateTo(false, duration);
        }

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();

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

            color = new TweenOperation<(Color start, Color end)>(AnimateToColorAction, spline.GradientColors);

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

            if (go.TryGetComponentOrLog(out EdgeRef edge) && edge.Value != null)
            {
                // When the hierarchy changes, we need to refresh the glow effect properties.
                edge.Value.Subscribe(this);
            }

            Tween[] ConstructAction(bool extending, float duration)
            {
                return new[] { DOTween.To(() => spline.SubsplineEndT,
                                          u => spline.SubsplineEndT = u,
                                          extending ? 1.0f : 0.0f,
                                          duration).SetEase(Ease.InOutExpo).Play() };
            }

            construction = new TweenOperation<bool>(ConstructAction, spline.SubsplineEndT >= 1);

        }

        protected override void OnDisable()
        {
            base.OnDisable();

            morphism.KillAnimator();
            morphism = null;
        }

        public void OnNext(ChangeEvent value)
        {
            // As stated in the documentation of Highlight Plus, whenever the hierarchy of an object changes,
            // we need to call Refresh() on it or it will stop working.
            if (value is HierarchyEvent)
            {
                RefreshGlow().Forget();
            }
        }

        public void OnCompleted()
        {
            // Nothing to be done.
        }

        public void OnError(Exception error)
        {
            throw error;
        }

    }
}
