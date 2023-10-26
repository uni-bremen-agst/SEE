﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Provides various extensions methods for the Color class.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns given <paramref name="color"/> lightened by the given <paramref name="factor"/>.
        /// </summary>
        /// <param name="color">base color to be lightened</param>
        /// <param name="factor">lightening factor</param>
        /// <returns>given <paramref name="color"/> lightened by <paramref name="factor"/></returns>
        public static Color Lighter(this Color color, float factor = 0.5f) => Color.Lerp(color, Color.white, factor);

        /// <summary>
        /// Returns given <paramref name="color"/> darkened by the given <paramref name="factor"/>.
        /// </summary>
        /// <param name="color">base color to be darkened</param>
        /// <param name="factor">darkening factor</param>
        /// <returns>given <paramref name="color"/> darkened by <paramref name="factor"/></returns>
        public static Color Darker(this Color color, float factor = 0.5f) => Color.Lerp(color, Color.black, factor);

        /// <summary>
        /// Returns this color with the given <paramref name="alpha"/> value.
        /// </summary>
        /// <param name="color">The color whose alpha value to modify</param>
        /// <param name="alpha">The new alpha value</param>
        /// <returns><paramref name="color"/> with the given <paramref name="alpha"/></returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Calculates and returns the optimal text color for the given <paramref name="backgroundColor"/>.
        /// </summary>
        /// <remarks>The code was adapted from
        /// <a href="https://www.codeproject.com/Articles/16565/Determining-Ideal-Text-Color-Based-on-Specified-Ba">
        /// this W3C compliant formula</a> <a href="https://archive.vn/V4bka">(Archive Link)</a>.
        /// <p>
        /// The original code uses .NET colors, where values are integers ranging from 0 to 255
        /// (<a href="https://docs.microsoft.com/en-us/dotnet/api/system.drawing.color.b?view=net-5.0#remarks">
        /// .NET Color</a>) while Unity colors values are floats ranging from 0 to 1
        /// (<a href="https://docs.unity3d.com/ScriptReference/Color-b.html">Unity Color</a>).
        /// This is why the color components are multiplied by 255 here.</p></remarks>
        /// <param name="backgroundColor">The background color on which the text will be put</param>
        /// <returns>the optimal text color for the given <paramref name="backgroundColor"/>.</returns>
        public static Color IdealTextColor(this Color backgroundColor)
        {
            const int nThreshold = 130;
            int bgDelta = Convert.ToInt32((backgroundColor.r * 255 * 0.299) + (backgroundColor.g * 255 * 0.587)
                                                                            + (backgroundColor.b * 255 * 0.114));
            return (255 - bgDelta < nThreshold) ? Color.black : Color.white;
        }

        /// <summary>
        /// Inverts the given color.
        /// </summary>
        /// <param name="color">The color to invert</param>
        /// <returns>The inverted color.</returns>
        public static Color Invert(this Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b);

            // An alternative approach to invert the color is as follows:
            // Color.RGBToHSV(color, out float H, out float S, out float V);
            // return Color.HSVToRGB((H + 0.5f) % 1f, S, V);
        }

        /// <summary>
        /// Converts the given <paramref name="colors"/> to linearly distributed gradient color keys.
        /// </summary>
        /// <param name="colors">The colors to convert</param>
        /// <returns>The converted colors as gradient color keys.</returns>
        public static IEnumerable<GradientColorKey> ToGradientColorKeys(this ICollection<Color> colors)
        {
            return colors.Select((c, i) => new GradientColorKey(c, (float)i / colors.Count));
        }

        /// <summary>
        /// Converts the given <paramref name="keys"/> to a list of colors.
        /// This simply extracts the colors from the keys.
        /// </summary>
        /// <param name="keys">The keys to convert</param>
        /// <returns>The converted keys as colors.</returns>
        public static IEnumerable<Color> ToColors(this IEnumerable<GradientColorKey> keys)
        {
            return keys.Select(c => c.color);
        }
    }
}
