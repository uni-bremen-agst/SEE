/*
MIT License 
Copyright(c) 2017 MarekMarchlewicz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;

namespace SEE.GO.Whiteboard
{
    [System.Obsolete("Experimental code. Do not use it. May be removed soon.")]
    public enum PaintMode : byte
    {
        Draw,
        Erase
    }

    [System.Obsolete("Experimental code. Do not use it. May be removed soon.")]
    public class Stamp
    {
        private readonly float[] Pixels;
        public float[] CurrentPixels;

        public int Width;
        public int Height;

        public PaintMode mode = PaintMode.Draw;

        private float currentAngle = 0f;

        public Stamp(Texture2D stampTexture)
        {
            Width = stampTexture.width;
            Height = stampTexture.height;

            Pixels = new float[Width * Height];
            CurrentPixels = new float[Width * Height];

            float alphaValue;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    alphaValue = stampTexture.GetPixel(x, y).a;
                    Pixels[x + y * Width] = alphaValue;
                    CurrentPixels[x + y * Width] = alphaValue;
                }
            }
        }

        public void SetRotation(float targetAngle)
        {
            if (targetAngle != currentAngle)
            {
                float sin = Mathf.Sin(Mathf.Deg2Rad * targetAngle);
                float cos = Mathf.Cos(Mathf.Deg2Rad * targetAngle);

                float x0 = Width / 2f;
                float y0 = Height / 2f;

                float deltaX, deltaY;

                int xp, yp;

                float rotatedPixelValue;

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        deltaX = x - x0;
                        deltaY = y - y0;

                        xp = (int)(deltaX * cos - deltaY * sin + x0);
                        yp = (int)(deltaX * sin + deltaY * cos + y0);

                        if (xp >= 0 && xp < Width && yp >= 0 && yp < Height)
                        {
                            rotatedPixelValue = Pixels[xp + Width * yp];
                        }
                        else
                        {
                            rotatedPixelValue = 0f;
                        }

                        CurrentPixels[x + Width * y] = rotatedPixelValue;
                    }
                }

                currentAngle = targetAngle;
            }
        }
    }
}