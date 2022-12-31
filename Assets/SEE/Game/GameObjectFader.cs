using System;
using System.Collections;
using DG.Tweening;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides fading in and out for game objects. There are static as well as instance methods
    /// for the same purpose. If one wants to run the fading animations as a coroutine,
    /// the caller must be a MonoBehaviour. If clients of this class are MonoBehaviours
    /// they can run their <see cref="StartCoroutine"/> with the static methods as parameters.
    /// For situations in which a client of this class is not a MonoBehaviour, we derive
    /// from that MonoBehaviour here. This way we can offer convenient static methods that
    /// create an instance of this class and attach this as a component to the game object to
    /// be animated. When the animation has finished that instance self destructs.
    /// </summary>
    public class GameObjectFader : MonoBehaviour  // TODO: Class should be converted to operations (Operator classes).
    {
        /// <summary>
        /// The amount of time a color should be kept until the next one is
        /// shown. Basically that is the time in seconds for an individual blinking.
        /// </summary>
        public const float BlinkTime = 0.5f;

        /// <summary>
        /// The amount of time until a game object is to be completely faded in or out
        /// (in seconds).
        /// </summary>
        public const float FadeTime = 1.0f;

        /// <summary>
        /// How often the colors should cycle during blinking. A cycle is
        /// a sequence of the original color to the inverted color and then back
        /// again to the original. After a complete cycle, the object has its original
        /// color.
        /// </summary>
        public const int NumberOfColorCycles = 2;

        /// <summary>
        /// Delegate that will be called when the coroutines <see cref="FadeIn(GameObject, Callback)"/>
        /// and <see cref="FadeOut(GameObject, Callback)"/> have finished.
        /// </summary>
        /// <param name="gameObject">the game object originally passed to those coroutines</param>
        public delegate void Callback(GameObject gameObject);

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> blink a few times (as specified by
        /// <see cref="NumberOfColorCycles"/>) and then fade out, i.e., make it fully transparent.
        /// Intended to be used as a coroutine.
        ///
        /// If <paramref name="callBack"/> is not null and the coroutine has finished.
        /// <paramref name="callBack"/>(<paramref name="gameObject"/>) will be called.
        /// </summary>
        /// <param name="gameObject">the game object to be faded out</param>
        /// <param name="callBack">callback to be called when the coroutine has finished</param>
        /// <returns>when to continue the execution of this coroutine</returns>
        public static IEnumerator FadeOut(GameObject gameObject, Callback callBack = null)
        {
            for (int i = 1; i <= 2 * NumberOfColorCycles; i++)
            {
                InvertColor(gameObject);
                yield return new WaitForSeconds(BlinkTime);
            }
            FadeOut(gameObject, FadeTime);
            yield return new WaitForSeconds(FadeTime);
            callBack?.Invoke(gameObject);
        }

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> fade in,
        /// i.e., turn it from fully transparent to fully visible,
        /// and then blink a few times.
        /// Intended to be used as a coroutine.
        ///
        /// If <paramref name="callBack"/> is not null and the coroutine has finished.
        /// <paramref name="callBack"/>(<paramref name="gameObject"/>) will be called.
        /// </summary>
        /// <param name="gameObject">the game object to be faded in</param>
        /// <param name="callBack">callback to be called when the coroutine has finished</param>
        /// <returns>when to continue the execution of this coroutine</returns>
        public static IEnumerator FadeIn(GameObject gameObject, Callback callBack = null)
        {
            FadeIn(gameObject, FadeTime);
            yield return new WaitForSeconds(FadeTime);

            for (int i = 1; i <= 2 * NumberOfColorCycles; i++)
            {
                InvertColor(gameObject);
                yield return new WaitForSeconds(BlinkTime);
            }
            callBack?.Invoke(gameObject);
        }

        /// <summary>
        /// The direction of the fading.
        /// </summary>
        private enum Fading
        {
            /// <summary>
            /// Object should be fading in.
            /// </summary>
            FadingIn,
            /// <summary>
            /// Object should be fading out.
            /// </summary>
            FadingOut
        }

        /// <summary>
        /// The direction of the fading.
        /// </summary>
        private Fading fading;

        /// <summary>
        /// The delegate to be called when the animation is finished
        /// just before this component self destructs.
        /// </summary>
        private Callback callBack;

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> fade in,
        /// i.e., turn it from fully transparent to fully visible,
        /// and then blink a few times.
        ///
        /// If <paramref name="callBack"/> is not null and the animation has finished.
        /// <paramref name="callBack"/>(<paramref name="gameObject"/>) will be called.
        /// </summary>
        /// <param name="gameObject">the game object to be faded in</param>
        /// <param name="callBack">callback to be called when the animation has finished</param>
        public static void FadingIn(GameObject gameObject, Callback callBack = null)
        {
            GameObjectFader fader = gameObject.AddComponent<GameObjectFader>();
            fader.callBack = callBack;
            fader.fading = Fading.FadingIn;
        }

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> blink a few times (as specified by
        /// <see cref="NumberOfColorCycles"/>) and then fade out, i.e., make it fully transparent.
        ///
        /// If <paramref name="callBack"/> is not null and the animation has finished.
        /// <paramref name="callBack"/>(<paramref name="gameObject"/>) will be called.
        /// </summary>
        /// <param name="gameObject">the game object to be faded out</param>
        /// <param name="callBack">callback to be called when the animation has finished</param>
        public static void FadingOut(GameObject gameObject, Callback callBack = null)
        {
            GameObjectFader fader = gameObject.AddComponent<GameObjectFader>();
            fader.callBack = callBack;
            fader.fading = Fading.FadingOut;
        }

        /// <summary>
        /// Calls <see cref="callBack"/> if set and then self destructs this component.
        /// This method is used as a callback itself, set in <see cref="Start"/>,
        /// and called when the animation has finished.
        /// </summary>
        /// <param name="_">ignored</param>
        private void SelfDestruct(GameObject _)
        {
            callBack?.Invoke(gameObject);
            Destroy(this);
        }

        /// <summary>
        /// Starts the coroutine for fading the component in or out, depending
        /// upon the set direction of <see cref="fading"/>. This method
        /// requests <see cref="SelfDestruct(GameObject)"/> to be called when
        /// the animation has finished.
        /// </summary>
        private void Start()
        {
            switch (fading)
            {
                case Fading.FadingIn:
                    StartCoroutine(FadeIn(gameObject, SelfDestruct));
                    break;
                case Fading.FadingOut:
                    StartCoroutine(FadeOut(gameObject, SelfDestruct));
                    break;
                default:
                    throw new NotImplementedException($"Unexpected case {fading} for type {nameof(Fading)}.");
            }
        }

        /// <summary>
        /// Inverts the color of the given <paramref name="gameObject"/>. The animation
        /// runs as specified by <paramref name="inversionTime"/> in seconds.
        /// </summary>
        /// <param name="gameObject">game object whose color is to be inverted</param>
        /// <param name="inversionTime">duration of the animation in seconds</param>
        public static void InvertColor(GameObject gameObject, float inversionTime = BlinkTime)
        {
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                // Colors of lines must be handeled differently from other game objects.
                lineRenderer.startColor = lineRenderer.startColor.Invert();
                lineRenderer.endColor = lineRenderer.endColor.Invert();
            }
            else if (gameObject.TryGetComponentOrLog(out Renderer renderer))
            {
                renderer.material.DOColor(renderer.material.color.Invert(), inversionTime);
            }
        }

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> fade in,
        /// i.e., turn it from fully transparent to fully visible.
        /// </summary>
        /// <param name="gameObject">the game object to be faded in</param>
        /// <param name="fadeTime">the duration of the fading in seconds</param>
        public static void FadeIn(GameObject gameObject, float fadeTime = FadeTime)
        {
            Fade(gameObject, fadeTime, 1.0f);
        }

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> fade out,
        /// i.e., turn it from visible to fully transparent.
        /// </summary>
        /// <param name="gameObject">the game object to be faded out</param>
        /// <param name="fadeTime">the duration of the fading in seconds</param>
        public static void FadeOut(GameObject gameObject, float fadeTime = FadeTime)
        {
            Fade(gameObject, fadeTime, 0.0f);
        }

        /// <summary>
        /// Changes the alpha value of the color of the given <paramref name="gameObject"/>
        /// to the given <paramref name="alpha"/> over the given <paramref name="animationDuration"/>
        /// in seconds.
        /// </summary>
        /// <param name="gameObject">the game object whose alpha color value is to be changed</param>
        /// <param name="animationDuration">the duration of alpha change in seconds</param>
        /// <param name="alpha">the color's alpha value (transparency) the <paramref name="gameObject"/>
        /// should have after <paramref name="animationDuration"/> seconds</param>
        private static void Fade(GameObject gameObject, float animationDuration, float alpha)
        {
            if (gameObject.TryGetComponentOrLog(out Renderer renderer))
            {
                // Note: the material must use Rendering Mode "Transparent", otherwise
                // the following call has no effect.
                renderer.material.DOFade(alpha, animationDuration);
            }
        }
    }
}
