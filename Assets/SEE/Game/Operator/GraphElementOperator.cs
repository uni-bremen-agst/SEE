using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using HighlightPlus;
using SEE.DataModel;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using ArgumentException = System.ArgumentException;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the graph element (i.e., node or edge) it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    /// <typeparam name="C">The type of the color of the graph element</typeparam>
    public abstract class GraphElementOperator<C> : AbstractOperator, IObserver<ChangeEvent> where C : struct
    {
        /// <summary>
        /// Operation handling changes to the color of the element.
        /// </summary>
        protected TweenOperation<C> color { get; private set; }

        /// <summary>
        /// Operation handling the glow effect around the element.
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
        /// The highlight effect of the element.
        /// </summary>
        private HighlightEffect highlightEffect;

        /// <summary>
        /// The city to which the element belongs.
        /// </summary>
        public AbstractSEECity City
        {
            get;
            private set;
        }

        protected override float BaseAnimationDuration => City.BaseAnimationDuration;

        #region Abstract Methods

        /// <summary>
        /// Creates a new <see cref="TweenOperation"/> that animates the color of the element.
        /// </summary>
        /// <returns>a new <see cref="TweenOperation"/> that animates the color of the element</returns>
        protected abstract TweenOperation<C> InitializeColorOperation();

        /// <summary>
        /// Applies the given <paramref name="modifier"/> to the given <paramref name="color"/> and
        /// returns the result. If <typeparamref name="C"/> contains multiple colors, the modifier
        /// will be applied to each of them.
        /// </summary>
        /// <param name="color">the color to modify</param>
        /// <param name="modifier">the modifier to apply</param>
        /// <returns>the modified color</returns>
        protected abstract C ModifyColor(C color, Func<Color, Color> modifier);

        /// <summary>
        /// Returns an enumerable of all colors contained in the given <paramref name="color"/>.
        /// </summary>
        /// <param name="color">the color to enumerate</param>
        /// <returns>an enumerable of all colors contained in the given <paramref name="color"/></returns>
        protected abstract IEnumerable<Color> AsEnumerable(C color);

        #endregion

        #region Public API

        /// <summary>
        /// Change the color of the element to the given <paramref name="targetColor"/>.
        /// </summary>
        /// <param name="targetColor">The target color this element should animate towards.</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <param name="useAlpha">Whether to incorporate the alpha values from the given colors.
        /// If set to false, the alpha values of the current color will be used instead.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> ChangeColorsTo(C targetColor, float factor = 1, bool useAlpha = true)
        {
            if (!useAlpha)
            {
                using IEnumerator<float> alphas = AsEnumerable(targetColor).Select(c => c.a).GetEnumerator();
                targetColor = ModifyColor(targetColor, c =>
                {
                    // Use alpha values from the current color.
                    alphas.MoveNext();
                    return c.WithAlpha(alphas.Current);
                });
            }
            return color.AnimateTo(targetColor, ToDuration(factor));
        }

        /// <summary>
        /// Fade the alpha property of the element to the given new <paramref name="alpha"/> value.
        /// Note that this will affect highlights as well.
        /// </summary>
        /// <param name="alpha">The new alpha value for the element. Must be in interval [0; 1]</param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        /// <exception cref="ArgumentException">If the given <paramref name="alpha"/> value is outside the
        /// range of [0; 1].</exception>
        public IOperationCallback<Action> FadeTo(float alpha, float factor = 1)
        {
            if (alpha is < 0 or > 1)
            {
                throw new ArgumentException("Given alpha value must be greater than zero and not more than one!");
            }

            C targetColor = ModifyColor(color.TargetValue, c => c.WithAlpha(alpha));
            // Elements being faded should also lead to highlights being faded.
            float targetGlow = GetTargetGlow(glowEnabled ? fullGlow : 0, alpha);

            float duration = ToDuration(factor);
            return new AndCombinedOperationCallback<Action>(new[]
            {
                color.AnimateTo(targetColor, duration),
                glow.AnimateTo(targetGlow, duration)
            });
        }

        /// <summary>
        /// Fade in the glow effect on this element.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> GlowIn(float factor = 1)
        {
            float targetGlow = GetTargetGlow(fullGlow, AsEnumerable(color.TargetValue).Max(x => x.a));
            glowEnabled = true;
            return glow.AnimateTo(targetGlow, ToDuration(factor));
        }

        /// <summary>
        /// Fade out the glow effect on this element.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> GlowOut(float factor = 1)
        {
            glowEnabled = false;
            return glow.AnimateTo(0, ToDuration(factor));
        }

        /// <summary>
        /// Flashes an element in its inverted color for a short time based on the given <paramref name="factor"/>.
        /// Note that this animation is not controlled by an operation and thus not necessarily synchronized.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        public void HitEffect(float factor = 0.5f)
        {
            // NOTE: This is not controlled by an operation. HighlightEffect itself controls the animation.
            //       Should be alright because overlapping animations aren't a big problem here.
            highlightEffect.hitFxFadeOutDuration = ToDuration(factor);

            Color targetColor = AsEnumerable(color.TargetValue).Aggregate((c1, c2) => Color.Lerp(c1, c2, 0.5f)).Invert();
            highlightEffect.hitFxColor = targetColor;
            highlightEffect.HitFX();
        }

        #endregion

        /// <summary>
        /// Refreshes the glow effect properties.
        ///
        /// Needs to be called whenever the material changes. Hierarchy changes are handled automatically.
        /// </summary>
        public async UniTaskVoid RefreshGlow(bool fullRefresh = false)
        {
            if (highlightEffect != null && glow != null)
            {
                if (fullRefresh)
                {
                    glow.KillAnimator();
                    Destroyer.Destroy(highlightEffect);
                    await UniTask.WaitForEndOfFrame(); // component is only destroyed by the end of the frame.
                    highlightEffect = Highlighter.GetHighlightEffect(gameObject);
                    SetupGlow();
                }
                else
                {
                    highlightEffect.Refresh();
                }
            }
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
        /// Calculates a value for the <see cref="glow"/> operation according to the following formula:
        /// min(<paramref name="glowTarget"/> / <see cref="fullGlow"/>, <paramref name="alphaTarget"/>) * fullGlow
        ///
        /// In other words, this ensures the element doesn't glow brighter than its alpha value.
        /// </summary>
        /// <param name="glowTarget">The desired glow target value</param>
        /// <param name="alphaTarget">The desired alpha target value</param>
        /// <returns>Glow value which doesn't exceed alpha value</returns>
        private static float GetTargetGlow(float glowTarget, float alphaTarget)
        {
            // Normalized glow (i.e., glow expressed as value in [0,1]) must not be higher than alpha.
            return Mathf.Min(glowTarget / fullGlow, alphaTarget) * fullGlow;
        }

        /// <summary>
        /// Determines the <see cref="AbstractSEECity"/> this <paramref name="gameObject"/> belongs to and returns it.
        /// </summary>
        /// <param name="gameObject">The object to get the city for</param>
        /// <returns>The city this object belongs to</returns>
        /// <exception cref="InvalidOperationException">If the object doesn't belong to a city</exception>
        private static AbstractSEECity GetCity(GameObject gameObject)
        {
            GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            if (codeCityObject == null || !codeCityObject.TryGetComponent(out AbstractSEECity city))
            {
                throw new InvalidOperationException($"GraphElementOperator-operated object {gameObject.FullName()}"
                                                    + $" in code city {CodeCityName(codeCityObject)}"
                                                    + $" must have an {nameof(AbstractSEECity)} component!");
            }

            return city;

            static string CodeCityName(GameObject codeCityObject)
            {
                return codeCityObject ? codeCityObject.FullName() : "<null>";
            }
        }

        protected virtual void OnEnable()
        {
            City = GetCity(gameObject);
            color = InitializeColorOperation();

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

            if (gameObject.TryGetComponentOrLog(out GraphElementRef elementRef) && elementRef.elem != null)
            {
                // When the hierarchy changes, we need to refresh the glow effect properties.
                elementRef.elem.Subscribe(this);
            }
        }

        protected virtual void OnDisable()
        {
            color.KillAnimator();
            color = null;
            glow.KillAnimator();
            glow = null;
        }

        /// <summary>
        /// Handles hierarchy changes by refreshing the glow effect.
        /// </summary>
        /// <param name="value">The event that was triggered</param>
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