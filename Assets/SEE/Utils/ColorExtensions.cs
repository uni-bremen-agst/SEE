using UnityEngine;

namespace SEE.Utils
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns given <paramref name="color"/> lightened by 50%.
        /// </summary>
        /// <param name="color">base color to be lightened</param>
        /// <returns>given <paramref name="color"/> lightened by 50%</returns>
        public static Color Lighter(this Color color)
        {
            return Color.Lerp(color, Color.white, 0.5f); // To lighten by 50 %
        }
    }
}