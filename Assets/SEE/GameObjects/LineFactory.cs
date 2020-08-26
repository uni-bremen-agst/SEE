using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Sets attributes of lines.
    /// </summary>
    class LineFactory
    {
        const float defaultWidth = 0.1f;

        /// <summary>
        /// Path to the material used for edges.
        /// </summary>
        protected const string materialPath = "Hidden/Internal-Colored";

        /// <summary>
        /// The material used for lines.
        /// </summary>
        public readonly static Material DefaultLineMaterial = NewLineMaterial();

        /// <summary>
        /// Returns a new material for lines.
        /// </summary>
        /// <returns>default material for edges</returns>
        public static Material NewLineMaterial()
        {
            Material material = new Material(Shader.Find(materialPath));
            if (material == null)
            {
                Debug.LogErrorFormat("SEE.Go.LineFactory: Could not find material {0} for lines.\n", materialPath);
            }
            return material;
        }

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

        public static void Draw(GameObject edge, Vector3[] linePoints, float width, Material material = null)
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
        }
    }
}
