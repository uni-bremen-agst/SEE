using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Provides a consistent color scheme for UI elements.
    /// </summary>
    public static class UIColorScheme
    {
        /// <summary>
        /// Palette of light colors.
        /// </summary>
        private static readonly Color[] lightColorPalette =
        {
            NewColor(0xffffffff),
            NewColor(0x85b5c7ff),
            NewColor(0xffcd22ff),
            NewColor(0x00bbffff)
        };

        /// <summary>
        /// Palette of dark colors.
        /// </summary>
        private static readonly Color[] darkColorPalette =
        {
            NewColor(0x21282dff)
        };

        /// <summary>
        /// Returns the <paramref name="index"/>'th color of the palette of
        /// light colors.
        ///
        /// Note: <paramref name="index"/> will be clamped into the index
        /// range of the palette of light colors.
        /// </summary>
        /// <param name="index">index of the requested color</param>
        /// <returns><paramref name="index"/>'th color of the palette of
        /// light colors</returns>
        public static Color GetLight(int index)
        {
            return lightColorPalette[Mathf.Clamp(index, 0, lightColorPalette.Length)];
        }

        /// <summary>
        /// Returns the <paramref name="index"/>'th color of the palette of
        /// dark colors.
        ///
        /// Note: <paramref name="index"/> will be clamped into the index
        /// range of the palette of dark colors.
        /// </summary>
        /// <param name="index">index of the requested color</param>
        /// <returns><paramref name="index"/>'th color of the palette of
        /// dark colors</returns>
        public static Color GetDark(int index)
        {
            return darkColorPalette[Mathf.Clamp(index, 0, lightColorPalette.Length)];
        }

        /// <summary>
        /// Returns a new color corresponding to the given RGB code <paramref name="hex"/>.
        /// Note: <paramref name="hex"/> is assumed to contain the alpha value.
        /// </summary>
        /// <param name="hex">hexidecimal encoding of RFB color code</param>
        /// <returns>new color corresponding to the given RGB code <paramref name="hex"/></returns>
        private static Color NewColor(uint hex)
        {
            byte rSByte = (byte)((hex >> 24) & 0xff);
            byte gSByte = (byte)((hex >> 16) & 0xff);
            byte bSByte = (byte)((hex >> 8) & 0xff);
            byte aSByte = (byte)(hex & 0xff);

            float rFloat = (float)rSByte / 255.0f;
            float gFloat = (float)gSByte / 255.0f;
            float bFloat = (float)bSByte / 255.0f;
            float aFloat = (float)aSByte / 255.0f;

            return new Color(rFloat, gFloat, bFloat, aFloat);
        }
    }
}
