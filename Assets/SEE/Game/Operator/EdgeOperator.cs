using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using HighlightPlus;
using SEE.DataModel;
using SEE.GO;
using SEE.Utils;
using TinySpline;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the edge it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public partial class EdgeOperator : AbstractOperator, IObserver<ChangeEvent>
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
        /// Operation handling the glow effect around the edge.
        /// </summary>
        private TweenOperation<float> glow;

        /// <summary>
        /// Amount of glow that should be animated towards.
        /// </summary>
        /// <remarks>Its value must be greater than 0 and not greater than 5.</remarks>
        private const float fullGlow = 2;

        /// <summary>
        /// Whether the glow effect is currently (supposed to be) enabled.
        /// </summary>
        private bool glowEnabled;

        private TweenOperation<bool> construction;

        /// <summary>
        /// The <see cref="SEESpline"/> represented by this edge.
        /// </summary>
        private SEESpline spline;

        /// <summary>
        /// The highlight effect of the edge.
        /// </summary>
        private HighlightEffect highlightEffect;

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
        /// Change the color gradient of the edge to a new gradient from <paramref name="newStartColor"/> to
        /// <paramref name="newEndColor"/>.
        /// </summary>
        /// <param name="target">The spline this edge should animate towards</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ChangeColorsTo(Color newStartColor, Color newEndColor, float duration)
        {
            return color.AnimateTo((newStartColor, newEndColor), duration);
        }

        /// <summary>
        /// Fade the alpha property of the edge to the given new <paramref name="alpha"/> value.
        /// Note that this will affect edge highlights as well.
        /// </summary>
        /// <param name="alpha">The new alpha value for the edge. Must be in interval [0; 1]</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <exception cref="ArgumentException">If the given <paramref name="alpha"/> value is outside the
        /// range of [0; 1].</exception>
        public IOperationCallback<Action> FadeTo(float alpha, float duration)
        {
            if (alpha < 0 || alpha > 1)
            {
                throw new ArgumentException("Given alpha value must be greater than zero and not more than one!");
            }

            (Color start, Color end) = color.TargetValue;
            // Edges being faded should also lead to highlights being faded.
            float targetGlow = GetTargetGlow(glowEnabled ? fullGlow : 0, alpha);

            return new AndCombinedOperationCallback<Action>(new[]
            {
                color.AnimateTo((start.WithAlpha(alpha), end.WithAlpha(alpha)), duration),
                glow.AnimateTo(targetGlow, duration)
            });
        }

        /// <summary>
        /// Fade in the glow effect on this edge.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> GlowIn(float duration)
        {
            float targetGlow = GetTargetGlow(fullGlow, color.TargetValue.start.a);
            glowEnabled = true;
            return glow.AnimateTo(targetGlow, duration);
        }

        /// <summary>
        /// Fade out the glow effect on this edge.
        /// </summary>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> GlowOut(float duration)
        {
            glowEnabled = false;
            return glow.AnimateTo(0, duration);
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

        public IOperationCallback<Action> Construct(float duration)
        {
            return construction.AnimateTo(true, duration);
        }

        public IOperationCallback<Action> Destruct(float duration)
        {
            return construction.AnimateTo(false, duration);
        }
        #endregion

        /// <summary>
        /// Calculates a value for the <see cref="glow"/> operation according to the following formula:
        /// min(<paramref name="glowTarget"/> / <see cref="fullGlow"/>, <paramref name="alphaTarget"/>) * fullGlow
        ///
        /// In other words, this ensures the edge doesn't glow brighter than its alpha value.
        /// </summary>
        /// <param name="glowTarget">The desired glow target value</param>
        /// <param name="alphaTarget">The desired alpha target value</param>
        /// <returns>Glow value which doesn't exceed alpha value</returns>
        private float GetTargetGlow(float glowTarget, float alphaTarget)
        {
            // Normalized glow (i.e., glow expressed as value in [0,1]) must not be higher than alpha.
            return Mathf.Min(glowTarget/fullGlow, alphaTarget) * fullGlow;
        }

        private void OnEnable()
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

            color = new TweenOperation<(Color start, Color end)>(AnimateToColorAction, spline.GradientColors);

            Tween[] ConstructAction(bool extending, float duration)
            {
                return new[] { DOTween.To(() => spline.SubsplineEndT,
                                          u => spline.SubsplineEndT = u,
                                          extending ? 1.0f : 0.0f,
                                          duration).SetEase(Ease.InOutExpo).Play() };
            }
            construction = new TweenOperation<bool>(ConstructAction, spline.SubsplineEndT >= 1);
        }

        private void OnDisable()
        {
            morphism.KillAnimator();
            morphism = null;
            color.KillAnimator();
            color = null;
            glow.KillAnimator();
            glow = null;
        }

        /// <summary>
        /// Refreshes the glow effect properties.
        ///
        /// Needs to be called whenever the material changes. Hierarchy changes are handled automatically
        /// </summary>
        public async UniTaskVoid RefreshGlow(bool fullRefresh = false)
        {
            if (highlightEffect != null && glow != null)
            {
                if (fullRefresh)
                {
                    glow.KillAnimator();
                    Destroyer.Destroy(highlightEffect);
                    await UniTask.WaitForEndOfFrame();  // component is only destroyed by the end of the frame.
                    highlightEffect = Highlighter.GetHighlightEffect(gameObject);
                    SetupGlow();
                }
                else
                {
                    highlightEffect.Refresh();
                }
            }
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
