using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Provides a colors with high contrast that be distinguished
    /// by most people suffering from different types of color blindness.
    /// </summary>
    public static class ColorPalette
    {
        /// <summary>
        /// The Viridis color palette as computed by an R script.
        /// See: https://cran.r-project.org/web/packages/viridis/vignettes/intro-to-viridis.html
        /// </summary>
        private static readonly Color[] ViridisColorPalette = new Color[] {
            new Color(0.267f, 0.004f, 0.333f, 1.0f),
            new Color(0.275f, 0.125f, 0.024f, 1.0f),
            new Color(0.259f, 0.235f, 0.506f, 1.0f),
            new Color(0.161f, 0.337f, 0.545f, 1.0f),
            new Color(0.176f, 0.431f, 0.557f, 1.0f),
            new Color(0.145f, 0.522f, 0.553f, 1.0f),
            new Color(0.137f, 0.604f, 0.537f, 1.0f),
            new Color(0.161f, 0.682f, 0.502f, 1.0f),
            new Color(0.325f, 0.769f, 0.408f, 1.0f),
            new Color(0.522f, 0.827f, 0.286f, 1.0f),
            new Color(0.741f, 0.875f, 0.184f, 1.0f),
            new Color(0.992f, 0.906f, 0.145f, 1.0f)
        };

        /// <summary>
        /// Yields a color of the Viridis color palette for given <paramref name="colorIndex"/>.
        /// The Viridis color palette offers twelve different colors of high contrast
        /// that be distinguished by most people suffering from different types of color blindness.
        /// The parameter <paramref name="colorIndex"/> will be rounded to an integer and 
        /// clamped into the value range of the Viridis color palette.
        /// </summary>
        /// <param name="colorIndex">the index of the requested color</param>
        /// <returns>the color with the given <paramref name="colorIndex"/> in the Viridis color palette</returns>
        public static Color Viridis(float colorIndex)
        {
            return ViridisColorPalette[Mathf.Clamp(Mathf.RoundToInt(colorIndex * (ViridisColorPalette.Length - 1)), 0, ViridisColorPalette.Length - 1)];
        }
    }
}
