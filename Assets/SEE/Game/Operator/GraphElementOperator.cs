using Cysharp.Threading.Tasks;
using DG.Tweening;
using HighlightPlus;
using SEE.Game.City;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the graph elements (nodes or edges) it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    public abstract class GraphElementOperator : AbstractOperator
    {
        /// <summary>
        /// Operation handling changes to the color gradient of the edge.
        /// </summary>
        protected TweenOperation<(Color start, Color end)> color;

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


        /// <summary>
        /// The highlight effect of the edge.
        /// </summary>
        protected HighlightEffect highlightEffect;

        /// <summary>
        /// Show the element, revealing it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to reveal the edge.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Show(GraphElementAnimationKind animationKind, float duration)
        {
            return ShowOrHide(true, animationKind, duration);
        }

        /// <summary>
        /// Hide the element, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="animationKind">In which way to hide the edge.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Hide(GraphElementAnimationKind animationKind, float duration)
        {
            return ShowOrHide(false, animationKind, duration);
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
            if (alpha is < 0 or > 1)
            {
                throw new ArgumentException("Given alpha value must be greater than zero and not more than one!");
            }

            (Color start, Color end) = color.TargetValue;
            // Elements being faded should also lead to highlights being faded.
            float targetGlow = GetTargetGlow(glowEnabled ? fullGlow : 0, alpha);

            return new AndCombinedOperationCallback<Action>(new[]
            {
                color.AnimateTo((start.WithAlpha(alpha), end.WithAlpha(alpha)), duration),
                glow.AnimateTo(targetGlow, duration)
            });
        }

        /// <summary>
        /// Calculates a value for the <see cref="glow"/> operation according to the following formula:
        /// min(<paramref name="glowTarget"/> / <see cref="fullGlow"/>, <paramref name="alphaTarget"/>) * fullGlow
        ///
        /// In other words, this ensures the edge doesn't glow brighter than its alpha value.
        /// </summary>
        /// <param name="glowTarget">The desired glow target value</param>
        /// <param name="alphaTarget">The desired alpha target value</param>
        /// <returns>Glow value which doesn't exceed alpha value</returns>
        protected float GetTargetGlow(float glowTarget, float alphaTarget)
        {
            // Normalized glow (i.e., glow expressed as value in [0,1]) must not be higher than alpha.
            return Mathf.Min(glowTarget / fullGlow, alphaTarget) * fullGlow;
        }

        /// <summary>
        /// Show or hide the element, animating it as specified in the <see cref="animationKind"/>.
        /// </summary>
        /// <param name="show">Whether to show or hide the element.</param>
        /// <param name="animationKind">In which way to animate the element.</param>
        /// <param name="duration">Time in seconds the animation should take. If set to 0, will execute directly,
        /// that is, the value is set before control is returned to the caller.</param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the given <paramref name="animationKind"/>
        /// is unknown.</exception>
        protected virtual IOperationCallback<Action> ShowOrHide(bool show, GraphElementAnimationKind animationKind, float duration)
        {
            return animationKind switch
            {
                GraphElementAnimationKind.None => new DummyOperationCallback<Action>(),
                GraphElementAnimationKind.Fading => FadeTo(show ? 1.0f : 0.0f, duration),
                GraphElementAnimationKind.Buildup => show ? Construct(duration) : Destruct(duration),
                _ => throw new ArgumentOutOfRangeException(nameof(animationKind), "Unknown edge animation kind supplied.")
            };
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

        protected abstract IOperationCallback<Action> Construct(float duration);
        protected abstract IOperationCallback<Action> Destruct(float duration);

        protected virtual void OnEnable()
        {
            if (TryGetComponent(out highlightEffect))
            {
                // If the component already exists, we need to rebuild it to be sure it fits our material.
                RefreshGlow(true).Forget();
            }
            else
            {
                highlightEffect = Highlighter.GetHighlightEffect(gameObject);
                highlightEffect.Refresh();
            }

            SetupGlow();
        }

        protected virtual void OnDisable()
        {
            color.KillAnimator();
            color = null;
            glow.KillAnimator();
            glow = null;
        }

        /// <summary>
        /// Sets up the <see cref="highlightEffect"/>, assuming it has been assigned
        /// a new instance of <see cref="HighlightEffect"/>.
        /// </summary>
        private void SetupGlow()
        {
            if (!highlightEffect.highlighted)
            {
                // We control highlighting not by the `highlighted` toggle, but by the amount of `glow`.
                highlightEffect.glow = 0;
                highlightEffect.highlighted = true;
            }

            highlightEffect.outline = 0;
            Tween[] AnimateToGlowAction(float endGlow, float duration) => new Tween[]
            {
                DOTween.To(() => highlightEffect.glow, g =>
                {
                    highlightEffect.glow = g;
                    highlightEffect.UpdateMaterialProperties();
                }, endGlow, duration).OnPlay(() =>
                {
                    highlightEffect.Refresh();
                }).Play()
            };

            glow = new TweenOperation<float>(AnimateToGlowAction, highlightEffect.glow);
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
    }
}