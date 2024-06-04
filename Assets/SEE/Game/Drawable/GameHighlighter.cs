using HighlightPlus;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using SEE.GO;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides highlighters for drawables or <see cref="DrawableType"/> objects.
    /// </summary>
    public static class GameHighlighter
    {
        /// <summary>
        /// Enables the highlight effect for the given <paramref name="obj"/>
        /// </summary>
        /// <param name="obj">The object that should be highlighted.</param>
        /// <returns>The created highlight effect.</returns>
        public static HighlightEffect Enable(GameObject obj)
        {
            HighlightEffect effect = obj.AddOrGetComponent<HighlightEffect>();
            effect.highlighted = true;
            effect.previewInEditor = false;
            effect.outline = 1;
            effect.outlineQuality = HighlightPlus.QualityLevel.Highest;
            effect.outlineColor = Color.yellow;
            effect.glowQuality = HighlightPlus.QualityLevel.Highest;
            effect.glow = 1.0f;
            effect.glowHQColor = Color.yellow;

            return effect;
        }
    }
}