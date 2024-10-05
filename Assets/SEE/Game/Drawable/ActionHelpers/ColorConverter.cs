using UnityEngine;

namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class provides a method to calculate the complementary color.
    /// </summary>
    public static class ColorConverter
    {
        /// <summary>
        /// Calculates the complementary color of the given <paramref name="color"/>.
        /// </summary>
        /// <param name="color">The color of which the complementary color should be calculated.</param>
        /// <returns>The complementary color.</returns>
        public static Color Complementary(Color color)
        {
            Color.RGBToHSV(color, out float H, out float S, out float V);
            /// Calculate the complementary color.
            float negativH = (H + 0.5f) % 1f;
            Color negativColor = Color.HSVToRGB(negativH, S, V);

            /// If the color does not have a complementary color, take the default.
            if (color == negativColor)
            {
                negativColor = ValueHolder.DefaultComplementary;
            }

            return negativColor;
        }
    }
}
