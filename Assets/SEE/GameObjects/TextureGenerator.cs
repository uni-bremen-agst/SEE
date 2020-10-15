using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Creates various textures.
    /// </summary>
    public static class TextureGenerator
    {
        /// <summary>
        /// Creates a texture of given color with a single 8-bit color channel.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="color">The color of the 8-bit channel of the texture.</param>
        /// <returns>The created texture.</returns>
        public static Texture2D CreateColoredTextureR8(int width, int height, float color)
        {
            Texture2D result = new Texture2D(width, height, TextureFormat.R8, false);
            Color c = new Color(color, 0.0f, 0.0f, 0.0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result.SetPixel(x, y, c);
                }
            }
            result.Apply();
            return result;
        }

        /// <summary>
        /// Creates a texture of given dimensions with a single 8-bit color channel. A
        /// line from <paramref name="p0"/> to <paramref name="p1"/> with
        /// <paramref name="thickness"/> thickness is drawn into the texture.
        /// 
        /// The algorithm is based on 'Murphy's Modified Bresenham Line Algorithm' and
        /// the implementation is based on:
        /// <see cref="https://github.com/danbar/murphy_line_draw/blob/master/murphy_line_draw.m"/>
        /// </summary>
        /// <param name="textureWidth">The width of the texture.</param>
        /// <param name="textureHeight">The height of the texture.</param>
        /// <param name="p0">The start point of the line.</param>
        /// <param name="p1">The end point of the line.</param>
        /// <param name="thickness">The thickness of the line.</param>
        /// <param name="lineColor">The color of the line.</param>
        /// <param name="backgroundColor">The color of the background.</param>
        /// <returns></returns>
        public static Texture2D CreateLineTextureR8(int textureWidth, int textureHeight, Vector2Int p0, Vector2Int p1, float thickness, float lineColor, float backgroundColor)
        {
            Color c0 = new Color(lineColor, 0.0f, 0.0f, 0.0f);
            Color c1 = new Color(backgroundColor, 0.0f, 0.0f, 0.0f);

            Texture2D result = CreateColoredTextureR8(textureWidth, textureHeight, backgroundColor);

            int dx = p1.x - p0.x;
            int dy = p1.y - p0.y;

            int inc_x = (int)Mathf.Sign(Mathf.Sign(dx) + 0.5f);
            int inc_y = (int)Mathf.Sign(Mathf.Sign(dy) + 0.5f);
            if (dx < 0)
            {
                dx = -dx;
            }

            if (dy < 0)
            {
                dy = -dy;
            }

            int len, sd0x, sd0y, dd0x, dd0y, sd1x, sd1y, dd1x, dd1y, ku, kv, kd, kt;
            if (dx > dy)
            {
                len = dx;

                sd0x = 0; sd0y = inc_y;
                dd0x = -inc_x; dd0y = inc_y;

                sd1x = inc_x; sd1y = 0;
                dd1x = inc_x; dd1y = inc_y;

                ku = 2 * dx;
                kv = 2 * dy;
                kd = kv - ku;

                kt = dx - kv;
            }
            else
            {
                len = dy;

                sd0x = inc_x; sd0y = 0;
                dd0x = inc_x; dd0y = -inc_y;

                sd1x = 0; sd1y = inc_y;
                dd1x = inc_x; dd1y = inc_y;

                ku = 2 * dy;
                kv = 2 * dx;
                kd = kv - ku;

                kt = dy - kv;
            }

            float tk = 2.0f * thickness * Mathf.Sqrt(dx * dx + dy * dy);

            int d0 = 0;
            int d1 = 0;
            int dd = 0;

            while (dd < tk)
            {
                BresenhamLineDraw(p0, d1);

                if (d0 < kt)
                {
                    p0.x = p0.x + sd0x;
                    p0.y = p0.y + sd0y;
                }
                else
                {
                    dd = dd + kv;
                    d0 = d0 - ku;

                    if (d1 < kt)
                    {
                        p0.x = p0.x + dd0x;
                        p0.y = p0.y + dd0y;

                        d1 = d1 - kv;
                    }
                    else
                    {
                        if (dx > dy)
                        {
                            p0.x = p0.x + dd0x;
                        }
                        else
                        {
                            p0.y = p0.y + dd0y;
                        }

                        d1 = d1 - kd;
                        if (dd > tk)
                        {
                            return result;
                        }

                        BresenhamLineDraw(p0, d1);

                        if (dx > dy)
                        {
                            p0.y = p0.y + dd0y;
                        }
                        else
                        {
                            p0.x = p0.x + dd0x;
                        }
                    }
                }

                dd = dd + ku;
                d0 = d0 + kv;

            }

            void BresenhamLineDraw(Vector2Int _p0, int _d1)
            {
                for (int p = 0; p < len; p++)
                {
                    result.SetPixel(_p0.x, _p0.y, c0);

                    if (_d1 <= kt)
                    {
                        _p0.x = _p0.x + sd1x;
                        _p0.y = _p0.y + sd1y;

                        _d1 = _d1 + kv;
                    }
                    else
                    {
                        _p0.x = _p0.x + dd1x;
                        _p0.y = _p0.y + dd1y;

                        _d1 = _d1 + kd;
                    }
                }
            }

            result.Apply();
            return result;
        }

        /// <summary>
        /// Creates a texture with a single 8-bit color channel and width and height of
        /// (<paramref name="outerRadius"/> + 1). A circle is drawn into the texture.
        /// </summary>
        /// <param name="outerRadius">The outer radius of the circle. The texture will
        /// have width and height of this parameter plus one.</param>
        /// <param name="innerRadius">The inner radius of the circle.</param>
        /// <param name="circleColor">The color of the circle outline.</param>
        /// <param name="backgroundColor">The color of the background of the texture.
        /// </param>
        /// <returns>The created circle outline texture.</returns>
        public static Texture2D CreateCircleOutlineTextureR8(int outerRadius, int innerRadius, float circleColor, float backgroundColor)
        {
            Color c0 = new Color(circleColor, 0.0f, 0.0f, 0.0f);
            Color c1 = new Color(backgroundColor, 0.0f, 0.0f, 0.0f);

            int size = 2 * outerRadius + 1;
            Texture2D result = CreateColoredTextureR8(size + 1, size + 1, backgroundColor);

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
                XLine(result, xc + xi, xc + xo, yc + y, c0);
                YLine(result, xc + y, yc + xi, yc + xo, c0);
                XLine(result, xc - xo, xc - xi, yc + y, c0);
                YLine(result, xc - y, yc + xi, yc + xo, c0);
                XLine(result, xc - xo, xc - xi, yc - y, c0);
                YLine(result, xc - y, yc - xo, yc - xi, c0);
                XLine(result, xc + xi, xc + xo, yc - y, c0);
                YLine(result, xc + y, yc - xo, yc - xi, c0);

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
