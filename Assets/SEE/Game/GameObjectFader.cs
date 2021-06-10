using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides fading in and out for game objects.
    /// </summary>
    public static class GameObjectFader
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
        /// How often the colors should change during blinking.
        /// Must be an even number so that the blinking object has its
        /// original color in the end.
        /// </summary>
        public const int NumberOfBlinks = 4;

        /// <summary>
        /// Delegate that will be called when the coroutines <see cref="FadeIn(GameObject, Callback)"/>
        /// and <see cref="FadeOut(GameObject, Callback)"/> have finished.
        /// </summary>
        /// <param name="gameObject">the game object originally passed to those coroutines</param>
        public delegate void Callback(GameObject gameObject);

        /// <summary>
        /// Let's the given <paramref name="gameObject"/> blink a few times (as specified by
        /// <see cref="NumberOfBlinks"/>) and then fade out, i.e., make it fully transparent.
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
            for (int i = 1; i <= NumberOfBlinks; i++)
            {
                InvertColor(gameObject, BlinkTime);
                yield return new WaitForSeconds(BlinkTime);
            }
            GameObjectFader.FadeOut(gameObject, FadeTime);
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
            GameObjectFader.FadeIn(gameObject, FadeTime);
            yield return new WaitForSeconds(FadeTime);

            for (int i = 1; i <= NumberOfBlinks; i++)
            {
                InvertColor(gameObject, BlinkTime);
                yield return new WaitForSeconds(BlinkTime);
            }
            callBack?.Invoke(gameObject);
        }

        /// <summary>
        /// Inverts the color of the given <paramref name="gameObject"/>. The animation
        /// runs as specified by <paramref name="inversionTime"/> in seconds.
        /// </summary>
        /// <param name="gameObject">game object whose color is to be inverted</param>
        /// <param name="inversionTime">duration of the animation in seconds</param>
        public static void InvertColor(GameObject gameObject, float inversionTime = BlinkTime)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                Color color = renderer.material.color;
                Color.RGBToHSV(color, out float H, out float S, out float V);
                float negativeH = (H + 0.5f) % 1f;
                Color negativeColor = Color.HSVToRGB(negativeH, S, V);
                renderer.material.DOColor(negativeColor, inversionTime);
            }
            else
            {
                Debug.LogError($"{gameObject.name} has no renderer.\n");
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
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                // Note: the material must use Rendering Mode "Transparent", otherwise
                // the following call has no effect.
                renderer.material.DOFade(alpha, animationDuration);
            }
            else
            {
                Debug.LogError($"{gameObject.name} has no renderer.\n");
            }
        }
    }
}
