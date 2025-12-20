using HighlightPlus;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

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
        /// <param name="highlight">If <c>true</c>, <paramref name="gameObject"/> will be
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
                highlight.UpdateMaterialProperties();
            }
            return highlight;
        }

        /// <summary>
        /// Enables the highlight glow and outline effect for the given <paramref name="obj"/>
        /// </summary>
        /// <param name="obj">The object that should be highlighted.</param>
        /// <returns>The created highlight effect.</returns>
        public static HighlightEffect EnableGlowOutline(GameObject obj)
        {
            HighlightEffect effect = obj.AddOrGetComponent<HighlightEffect>();
            effect.highlighted = true;
            effect.previewInEditor = false;
            effect.outline = 1;
            effect.outlineQuality = HighlightPlus.QualityLevel.Highest;
            effect.outlineColor = Color.yellow;
            effect.glowQuality = Highlighter.DefaultGlowQuality;
            effect.glow = 1.0f;
            effect.glowHQColor = Color.yellow;
            return effect;
        }

        /// <summary>
        /// Enables the highlight glow and overlay effect for the given <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object that should be highlighted.</param>
        /// <returns>The created highlight effect.</returns>
        public static HighlightEffect EnableGlowOverlay(GameObject obj)
        {
            HighlightEffect effect = obj.AddOrGetComponent<HighlightEffect>();
            effect.highlighted = true;
            effect.previewInEditor = false;
            effect.outline = 0;
            effect.glowQuality = Highlighter.DefaultGlowQuality;
            effect.glow = 1.0f;
            effect.glowHQColor = Color.yellow;
            effect.overlay = 1.0f;
            effect.overlayColor = Color.magenta;
            return effect;
        }
    }
}
