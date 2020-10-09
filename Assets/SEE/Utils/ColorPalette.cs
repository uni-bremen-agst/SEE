using UnityEngine;

namespace SEE.Utils
{

    public static class ColorPalette
    {
        // https://cran.r-project.org/web/packages/viridis/vignettes/intro-to-viridis.html
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

        public static Color Viridis(float t)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(t), 0, ViridisColorPalette.Length - 1);
            Color result = ViridisColorPalette[i];
            return result;
        }
    }

}
