using System;
using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Sets attributes of lines.
    /// </summary>
    class LineFactory
    {
        const float defaultWidth = 0.1f;

        internal static void SetDefaults(LineRenderer line)
        {
            line.sortingLayerName = "OnTop";
            line.sortingOrder = 5;

            // simplify rendering; no shadows
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            SetColors(line);
            SetWidth(line, defaultWidth);
        }

        internal static void SetColors(LineRenderer line)
        {
            SetColors(line, Color.green, Color.red);
        }

        internal static void SetColors(LineRenderer line, Color startColor, Color endColor)
        {
            line.startColor = startColor;
            line.endColor = endColor;
        }

        internal static void SetColor(LineRenderer line, Color color)
        {
            SetColors(line, color, color);
        }

        internal static void SetWidth(LineRenderer line, float width)
        {
            line.startWidth = width;
            line.endWidth = width;
        }

    }
}
