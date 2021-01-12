using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Sets attributes of lines.
    /// </summary>
    internal static class LineFactory
    {
        private const float defaultWidth = 0.1f;

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

        public static LineRenderer Draw(GameObject edge, Vector3[] linePoints, float width, Material material = null)
        {
            LineRenderer line = edge.GetComponent<LineRenderer>();
            if (line == null)
            {
                // edge does not yet have a renderer; we add a new one
                line = edge.AddComponent<LineRenderer>();
            }

            line.useWorldSpace = true;
            if (material != null)
            {
                // use sharedMaterial if changes to the original material should affect all
                // objects using this material; renderer.material instead will create a copy
                // of the material and will not be affected by changes of the original material
                line.sharedMaterial = material;
            }
            line.positionCount = linePoints.Length; // number of vertices       
            line.SetPositions(linePoints);
            SetDefaults(line);
            SetWidth(line, width);
            SetColors(line);            
            return line;
        }
    }
}
