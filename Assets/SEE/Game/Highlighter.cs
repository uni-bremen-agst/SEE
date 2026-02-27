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
        private const HighlightPlus.QualityLevel defaultGlowQuality = HighlightPlus.QualityLevel.Highest;

        /// <summary>
        /// The default glow intensity for the glow highlight effect.
        /// </summary>
        private const float defaultGlowIntensity = 1.0f;

        /// <summary>
        /// Whether or not the <paramref name="gameObject"/> should be highlighted.
        /// </summary>
        /// <param name="gameObject">The game object whose highlighting is to be set.</param>
        /// <param name="highlight">If true, <paramref name="gameObject"/> will be
        /// highlighted; otherwise its highlighting will be turned off.</param>
        public static void SetHighlight(this GameObject gameObject, bool highlight)
        {
            HighlightEffect effect = GetHighlightEffect(gameObject);
            effect.highlighted = highlight;
        }

        /// <summary>
        /// If <paramref name="gameObject"/> does not have a <see cref="HighlightEffect"/>
        /// attached, one will be attached to it with our default values. That <see cref="HighlightEffect"/>
        /// will be returned.
        /// </summary>
        /// <param name="gameObject">Game object where to attach a <see cref="HighlightEffect"/>.</param>
        /// <returns><see cref="HighlightEffect"/> component responsible for adding the highlight effect.</returns>
        public static HighlightEffect GetHighlightEffect(this GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out HighlightEffect effect))
            {
                effect = gameObject.AddComponent<HighlightEffect>();
                effect.innerGlow = 0;
                Color inverted = gameObject.GetComponent<Renderer>().sharedMaterial.color.Invert();
                effect.outlineColor = inverted;
                effect.SetGlowColor(Color.yellow);
                effect.glow = defaultGlowIntensity;
                effect.glowQuality = defaultGlowQuality;
                effect.effectGroup = TargetOptions.OnlyThisObject;
                effect.glowDownsampling = 1;
                effect.hitFxColor = inverted;
                effect.hitFxInitialIntensity = 1f;
                effect.UpdateMaterialProperties();
            }
            return effect;
        }

        /// <summary>
        /// Enables the highlight glow and outline effect for the given <paramref name="gameObject"/>
        /// </summary>
        /// <param name="gameObject">The object that should be highlighted.</param>
        /// <returns>The created highlight effect.</returns>
        public static HighlightEffect EnableGlowOutline(this GameObject gameObject)
        {
            HighlightEffect effect = gameObject.AddOrGetComponent<HighlightEffect>();
            effect.highlighted = true;
            effect.outline = 1;
            effect.outlineQuality = HighlightPlus.QualityLevel.Highest;
            effect.outlineColor = Color.yellow;
            effect.glowQuality = defaultGlowQuality;
            effect.glow = defaultGlowIntensity;
            effect.glowHQColor = Color.yellow;
            effect.UpdateMaterialProperties();
            return effect;
        }

        /// <summary>
        /// Enables the highlight glow and overlay effect for the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The object that should be highlighted.</param>
        /// <returns>The created highlight effect.</returns>
        public static HighlightEffect EnableGlowOverlay(this GameObject gameObject)
        {
            HighlightEffect effect = gameObject.AddOrGetComponent<HighlightEffect>();
            effect.highlighted = true;
            effect.outline = 0;
            effect.glowQuality = Highlighter.defaultGlowQuality;
            effect.glow = defaultGlowIntensity;
            effect.glowHQColor = Color.yellow;
            effect.overlay = 1.0f;
            effect.overlayColor = Color.magenta;
            effect.UpdateMaterialProperties();
            return effect;
        }
    }
}
