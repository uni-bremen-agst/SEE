using UnityEngine;

namespace SEE.Tools
{
    /// <summary>
    /// Creates various textures.
    /// </summary>
    public static class TextureGenerator
    {
        /// <summary>
        /// Creates a texture of a circle outline with width and height of
        /// (<paramref name="outerRadius"/> + 1).
        /// </summary>
        /// <param name="outerRadius">The outer radius of the circle. The texture will
        /// have width and height of this parameter plus one.</param>
        /// <param name="innerRadius">The inner radius of the circle.</param>
        /// <param name="circleColor">The color of the circle outline</param>
        /// <param name="backgroundColor">The color of the background of the texture.
        /// </param>
        /// <returns>The created circle outline texture.</returns>
        public static Texture2D CreateCircleOutlineTexture(int outerRadius, int innerRadius, Color circleColor, Color backgroundColor)
        {
            int size = 2 * outerRadius + 1;
            Texture2D result = new Texture2D(size, size, TextureFormat.R8, false);

            Color[] colors = new Color[size * size];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = backgroundColor;
            }
            result.SetPixels(colors);

            int xc = outerRadius;
            int yc = outerRadius;
            int xo = outerRadius;
            int xi = innerRadius;
            int y = 0;
            int erro = 1 - xo;
            int erri = 1 - xi;

            void XLine(Texture2D _tex, int _x1, int _x2, int _y, Color _color)
            {
                while (_x1 <= _x2)
                {
                    _tex.SetPixel(_x1++, _y, _color);
                }
            }

            void YLine(Texture2D _tex, int _x, int _y1, int _y2, Color _color)
            {
                while (_y1 <= _y2)
                {
                    _tex.SetPixel(_x, _y1++, _color);
                }
            }

            while (xo >= y)
            {
                XLine(result, xc + xi, xc + xo, yc + y, circleColor);
                YLine(result, xc + y, yc + xi, yc + xo, circleColor);
                XLine(result, xc - xo, xc - xi, yc + y, circleColor);
                YLine(result, xc - y, yc + xi, yc + xo, circleColor);
                XLine(result, xc - xo, xc - xi, yc - y, circleColor);
                YLine(result, xc - y, yc - xo, yc - xi, circleColor);
                XLine(result, xc + xi, xc + xo, yc - y, circleColor);
                YLine(result, xc + y, yc - xo, yc - xi, circleColor);

                y++;

                if (erro < 0)
                {
                    erro += 2 * y + 1;
                }
                else
                {
                    xo--;
                    erro += 2 * (y - xo + 1);
                }

                if (y > innerRadius)
                {
                    xi = y;
                }
                else
                {
                    if (erri < 0)
                    {
                        erri += 2 * y + 1;
                    }
                    else
                    {
                        xi--;
                        erri += 2 * (y - xi + 1);
                    }
                }
            }

            result.Apply(true, true);
            return result;
        }
    }
}
