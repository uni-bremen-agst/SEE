using UnityEngine;
using HighlightPlus;
using SEE.Utils;

namespace SEE.Game
{
    /// <summary>
    /// A helper class to highlight game objects.
    /// </summary>
    internal static class Highlighter
    {
        /// <summary>
        /// Whether or not the <paramref name="gameObject"/> should be highlighted.
        /// </summary>
        /// <param name="gameObject">the game object whose highlighting is to be set</param>
        /// <param name="highlight">if <c>true</c>, <paramref name="gameObject"/> will be
        /// highlighted; otherwise its highlighting will be turned off</param>
        public static void SetHighlight(GameObject gameObject, bool highlight)
        {
            HighlightEffect highlightEffect = GetHighlightEffect(gameObject);
            highlightEffect.highlighted = highlight;
        }

        /// <summary>
        /// If <paramref name="gameObject"/> does not have a <see cref="HighlightEffect"/>
        /// attached, one will be attached to it with our default values. That <see cref="HighlightEffect"/>
        /// will be returned.
        /// </summary>
        /// <param name="gameObject">game objec</param>
        /// <returns><see cref="HighlightEffect"/> component responsible for adding the highlight effect</returns>
        public static HighlightEffect GetHighlightEffect(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out HighlightEffect highlight))
            {
                highlight = gameObject.AddComponent<HighlightEffect>();
                highlight.innerGlow = 0;
                highlight.outlineColor = gameObject.GetComponent<Renderer>().material.color.Invert();
                highlight.SetGlowColor(Color.yellow);
                highlight.glow = 2;
                highlight.glowQuality = HighlightPlus.QualityLevel.Highest;
                highlight.effectGroup = TargetOptions.OnlyThisObject;
            }
            return highlight;
        }
    }
}
