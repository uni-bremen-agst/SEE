using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using HighlightPlus;
using SEE.DataModel;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;
using ArgumentException = System.ArgumentException;

namespace SEE.Game.Operator
{
    /// <summary>
    /// A component managing operations done on the graph element (i.e., node or edge) it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// Note that this only contains non-color operations. Color operations are handled by
    /// the generic <see cref="GraphElementOperator{C}"/> class.
    /// </summary>
    public abstract class GraphElementOperator : AbstractOperator
    {
        /// <summary>
        /// Operation handling the blinking of the element.
        /// The parameter specifies the number of blinks.
        /// </summary>
        protected TweenOperation<int> Blinking;

        /// <summary>
        /// Operation handling the glow effect around the element.
        /// </summary>
        protected TweenOperation<float> Glow;

        /// <summary>
        /// Amount of glow that should be animated towards.
        /// </summary>
        /// <remarks>Its value must be greater than 0 and not greater than 5.</remarks>
        protected const float FullGlow = 2;

        /// <summary>
        /// Whether the glow effect is currently (supposed to be) enabled.
        /// </summary>
        protected bool GlowEnabled;

        /// <summary>
        /// The city to which the element belongs.
        /// </summary>
        public AbstractSEECity City
        {
            get;
            protected set;
        }

        protected override float BaseAnimationDuration => City.BaseAnimationDuration;

        /// <summary>
        /// Calculates a value for the <see cref="glow"/> operation according to the following formula:
        /// <c>min(1, colorAlpha) * fullGlow</c>
        /// </summary>
        /// <returns>Glow value which doesn't exceed alpha value</returns>
        protected abstract float GetTargetGlow();

        /// <summary>
        /// Returns the color to use for the spear highlighting the element.
        /// </summary>
        /// <returns>the color to use for the spear highlighting the element</returns>
        protected abstract Color GetHighlightColor();

        /// <summary>
        /// Returns an array of tweens that animate the element to blink <paramref name="blinkCount"/> times
        /// for the given <paramref name="duration"/>.
        /// </summary>
        /// <param name="blinkCount">the number of times the element should blink</param>
        /// <param name="duration">the duration of the blinking animation</param>
        /// <returns>an array of tweens that animate the element to blink <paramref name="blinkCount"/> times
        /// for the given <paramref name="duration"/></returns>
        protected abstract Tween[] BlinkAction(int blinkCount, float duration);

        /// <summary>
        /// Makes the element blink <paramref name="blinkCount"/> times.
        /// </summary>
        /// <param name="blinkCount">The number of times the element should blink.
        /// If set to -1, the element will blink indefinitely.
        /// If set to 0, the element will not blink at all.
        /// </param>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the blinking duration.
        /// If set to 0, will execute directly, that is, the blinking is stopped
        /// before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Blink(int blinkCount, float factor = 1)
        {
            return Blinking.AnimateTo(blinkCount, ToDuration(factor));
        }

        /// <summary>
        /// Fade in the glow effect on this element.
        /// </summary>
        /// <param name="factor">Factor to apply to the <see cref="BaseAnimationDuration"/>
        /// that controls the animation duration.
        /// If set to 0, will execute directly, that is, the value is set before control is returned to the caller.
        /// </param>
        /// <returns>An operation callback for the requested animation</returns>
        public virtual IOperationCallback<Action> GlowIn(float factor = 1)
        {
            float targetGlow = GetTargetGlow();
            GlowEnabled = true;
            return Glow.AnimateTo(targetGlow, ToDuration(factor));
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
            GlowEnabled = false;
            return Glow.AnimateTo(0, ToDuration(factor));
        }

        /// <summary>
        /// Displays a marker above the element and makes it blink and glow for <paramref name="duration"/> seconds.
        /// </summary>
        /// <param name="duration">The amount of time in seconds the element should be highlighted.
        /// If this is set to a negative value, the element will be highlighted indefinitely, with a blink rate
        /// proportional to the absolute value of <paramref name="duration"/>.
        /// </param>
        /// <param name="showNotification">Whether to display a notification.</param>
        /// <returns>An operation callback for the requested animation</returns>
        public IOperationCallback<Action> Highlight(float duration, bool showNotification = true)
        {
            if (showNotification)
            {
                ShowNotification.Info($"Highlighting '{name}'",
                      $"The selected element will be blinking and marked by a spear for {duration} seconds.",
                      log: false);
            }

            // Display marker above the element
            // FIXME: marker is not displayed above edge.
            MarkerFactory marker = new(new MarkerAttributes(height: 1f, width: 0.01f, GetHighlightColor(), default, default));
            marker.MarkBorn(gameObject);
            // The factor of 1.3 causes the element to blink slightly more than once per second,
            // which seems visually fitting.
            int blinkCount = duration >= 0 ? Mathf.RoundToInt(duration * 1.3f) : -1;
            return new AndCombinedOperationCallback<Action>(new[]
            {
                GlowIn(ToFactor(Mathf.Abs(duration / blinkCount))),
                Blink(blinkCount: blinkCount, ToFactor(Mathf.Abs(duration)))
            }).OnComplete(() =>
            {
                GlowOut(ToFactor(0.5f));
                marker.Clear();
            });
        }
    }

    /// <summary>
    /// A component managing operations done on the graph element (i.e., node or edge) it is attached to.
    /// Available operations consist of the public methods exported by this class.
    /// Operations can be animated or executed directly, by setting the duration to 0.
    /// </summary>
    /// <typeparam name="C">The type of the color of the graph element</typeparam>
    public abstract class GraphElementOperator<C> : GraphElementOperator, IObserver<ChangeEvent> where C : struct
    {
        /// <summary>
        /// Operation handling changes to the color of the element.
        /// </summary>
        protected TweenOperation<C> Color { get; private set; }

        /// <summary>
        /// The highlight effect of the element.
        /// </summary>
        private HighlightEffect highlightEffect;

        /// <summary>
        /// The current color of the element.
        /// </summary>
        public C TargetColor => Color.TargetValue;

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
            return Color.AnimateTo(targetColor, ToDuration(factor));
        }

        protected override Color GetHighlightColor()
        {
            List<Color> colors = AsEnumerable(TargetColor).ToList();
            return colors.Aggregate((x, y) => x + y) / colors.Count;
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

            C targetColor = ModifyColor(Color.TargetValue, c => c.WithAlpha(alpha));
            // Elements being faded should also lead to highlights being faded.
            float targetGlow = GetTargetGlow(GlowEnabled ? FullGlow : 0, alpha);

            float duration = ToDuration(factor);
            return new AndCombinedOperationCallback<Action>(new[]
            {
                Color.AnimateTo(targetColor, duration),
                Glow.AnimateTo(targetGlow, duration)
            });
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

            Color targetColor = AsEnumerable(Color.TargetValue).Aggregate((c1, c2) => UnityEngine.Color.Lerp(c1, c2, 0.5f)).Invert();
            highlightEffect.hitFxColor = targetColor;
            highlightEffect.HitFX();
        }

        #endregion

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

            Glow = new TweenOperation<float>(AnimateToGlowAction, highlightEffect.glow);
            return;

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
            return Mathf.Min(glowTarget / FullGlow, alphaTarget) * FullGlow;
        }

        /// <summary>
        /// Calculates a value for the <see cref="glow"/> operation according to the following formula:
        /// min(1, colorAlpha) * fullGlow
        /// </summary>
        /// <returns>Glow value which doesn't exceed alpha value</returns>
        protected override float GetTargetGlow()
        {
            return GetTargetGlow(FullGlow, AsEnumerable(Color.TargetValue).Max(x => x.a));
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
                RefreshGlowAsync().Forget();
            }
        }

        /// <summary>
        /// Refreshes the glow effect properties.
        ///
        /// Needs to be called whenever the material changes. Hierarchy changes are handled automatically.
        /// </summary>
        public async UniTaskVoid RefreshGlowAsync(bool fullRefresh = false)
        {
            if (highlightEffect != null && Glow != null)
            {
                if (fullRefresh)
                {
                    Glow.KillAnimator();
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

        public void OnCompleted()
        {
            // Nothing to be done.
        }

        public void OnError(Exception error)
        {
            throw error;
        }


        protected virtual void OnEnable()
        {
            City = GetCity(gameObject);
            Color = InitializeColorOperation();

            Blinking = new TweenOperation<int>(BlinkAction, 0, equalityComparer: new AlwaysFalseEqualityComparer<int>(),
                                               conflictingOperations: new[] { Color });

            if (TryGetComponent(out highlightEffect))
            {
                // If the component already exists, we need to rebuild it to be sure it fits our material.
                RefreshGlowAsync(true).Forget();
            }
            else
            {
                highlightEffect = Highlighter.GetHighlightEffect(gameObject);
                highlightEffect.Refresh();
            }

            SetupGlow();

            if (gameObject.TryGetComponentOrLog(out GraphElementRef elementRef) && elementRef.Elem != null)
            {
                // When the hierarchy changes, we need to refresh the glow effect properties.
                elementRef.Elem.Subscribe(this);
            }
        }

        protected virtual void OnDisable()
        {
            Color.KillAnimator();
            Color = null;
            Glow.KillAnimator();
            Glow = null;
        }
    }
}
