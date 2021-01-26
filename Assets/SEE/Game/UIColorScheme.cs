using UnityEngine;

namespace SEE
{

    public static class UIColorScheme
    {
        private static readonly Color[] lightColorScheme = {
            NewColor(0xffffffff),
            NewColor(0x85b5c7ff),
            NewColor(0xffcd22ff),
            NewColor(0x00bbffff)
        };

        private static readonly Color[] darkColorPalette =
        {
            NewColor(0x21282dff)
        };

        public static Color GetLight(int index)
        {
            Color result = lightColorScheme[index % lightColorScheme.Length];
            return result;
        }

        public static Color GetDark(int index)
        {
            Color result = darkColorPalette[index % darkColorPalette.Length];
            return result;
        }

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

            Color result = new Color(rFloat, gFloat, bFloat, aFloat);
            return result;
        }
    }

}
