using System;
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

        /// <summary>
        /// Calculates and returns the optimal text color for the given <paramref name="backgroundColor"/>.
        /// </summary>
        /// <remarks>The code was adapted from
        /// <a href="https://www.codeproject.com/Articles/16565/Determining-Ideal-Text-Color-Based-on-Specified-Ba">
        /// this W3C compliant formula</a>. <a href="https://archive.vn/wip/V4bka">(Archive Link)</a></remarks>
        /// <param name="backgroundColor">The background color on which the text will be put</param>
        /// <returns>the optimal text color for the given <paramref name="backgroundColor"/>.</returns>
        public static Color IdealTextColor(this Color backgroundColor)
        {
            const int nThreshold = 105;
            int bgDelta = Convert.ToInt32((backgroundColor.r * 0.299f) + (backgroundColor.g * 0.587) 
                                                                       + (backgroundColor.b * 0.114));
            return (255 - bgDelta < nThreshold) ? Color.black : Color.white;
        }
    }
}