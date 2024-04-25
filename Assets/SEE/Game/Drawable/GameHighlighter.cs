using HighlightPlus;
using SEE.Utils;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides highlighter for drawables or <see cref="DrawableType"/> objects
    /// </summary>
    public static class GameHighlighter
    {
        /// <summary>
        /// Enables the highlight effect for the given obj.
        /// </summary>
        /// <param name="obj">The object that should be highlighted.</param>
        /// <returns>The created highlight effect.</returns>
        public static HighlightEffect Enable(GameObject obj)
        {
            HighlightEffect effect = obj.AddComponent<HighlightEffect>();
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

        /// <summary>
        /// Disable/Destroys the highlight effect.
        /// </summary>
        /// <param name="obj">The object in which the highlight effect should be disabled.</param>
        public static void Disable(GameObject obj)
        {
            if (obj.GetComponent<HighlightEffect>() != null)
            {
                Destroyer.Destroy(obj.GetComponent<HighlightEffect>());
            }
        }
    }
}