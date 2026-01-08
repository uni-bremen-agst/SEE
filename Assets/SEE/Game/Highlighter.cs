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
        /// The default quality level for the glow effect (<see cref="HighlightEffect.glowQuality"/>.
        /// </summary>
        /// <remarks>The value <see cref="HighlightPlus.QualityLevel.Highest"/> does not work
        /// in Highlight Plus version 21.0 anymore.</remarks>
        public const HighlightPlus.QualityLevel DefaultGlowQuality = HighlightPlus.QualityLevel.Highest;

        /// <summary>
        /// Whether or not the <paramref name="gameObject"/> should be highlighted.
        /// </summary>
        /// <param name="gameObject">The game object whose highlighting is to be set.</param>
        /// <param name="highlight">If true, <paramref name="gameObject"/> will be
        /// highlighted; otherwise its highlighting will be turned off.</param>
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
        /// <param name="gameObject">Game objec.</param>
        /// <returns><see cref="HighlightEffect"/> component responsible for adding the highlight effect.</returns>
        public static HighlightEffect GetHighlightEffect(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out HighlightEffect highlight))
            {
                highlight = gameObject.AddComponent<HighlightEffect>();
                highlight.innerGlow = 0;
                Color inverted = gameObject.GetComponent<Renderer>().sharedMaterial.color.Invert();
                highlight.outlineColor = inverted;
                highlight.SetGlowColor(Color.yellow);
                highlight.glow = 2;
                highlight.glowQuality = DefaultGlowQuality;
                highlight.effectGroup = TargetOptions.OnlyThisObject;
                highlight.glowDownsampling = 1;
                highlight.hitFxColor = inverted;
                highlight.hitFxInitialIntensity = 1f;
            }
            return highlight;
        }
    }
}
