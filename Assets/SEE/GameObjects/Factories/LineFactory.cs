using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Sets attributes of lines.
    /// </summary>
    internal static class LineFactory
    {
        /// <summary>
        /// The default width of a line.
        /// </summary>
        private const float defaultWidth = 0.1f;

        /// <summary>
        /// Sets the defaults for <paramref name="line"/>, namely, the sortingLayerName,
        /// sortingOrder, receiveShadows, shadowCastingMode, color, and width.
        /// </summary>
        /// <param name="line">line to be set</param>
        internal static void SetDefaults(LineRenderer line)
        {
            line.sortingLayerName = "OnTop";
            line.sortingOrder = 0;

            // simplify rendering; no shadows
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            SetColors(line);
            SetWidth(line, defaultWidth);
        }

        /// <summary>
        /// Sets the color of the <paramref name="line"/> from <see cref="Color.green"/> to
        /// <see cref="Color.red"/>.
        /// </summary>
        /// <param name="line">line to be set</param>
        internal static void SetColors(LineRenderer line)
        {
            SetColors(line, Color.green, Color.red);
        }

        /// <summary>
        /// Sets the color of the <paramref name="line"/> from <paramref name="startColor"/> to
        /// <paramref name="endColor"/>.
        /// </summary>
        /// <param name="line">line to be set</param>
        /// <param name="startColor">starting color</param>
        /// <param name="endColor">ending color</param>
        internal static void SetColors(LineRenderer line, Color startColor, Color endColor)
        {
            line.startColor = startColor;
            line.endColor = endColor;
        }

        /// <summary>
        /// Sets the color of the <paramref name="line"/> to <paramref name="color"/>.
        /// </summary>
        /// <param name="line">line to be set</param>
        /// <param name="color">starting and ending color</param>
        internal static void SetColor(LineRenderer line, Color color)
        {
            SetColors(line, color, color);
        }

        /// <summary>
        /// Sets the width of the <paramref name="line"/> to <paramref name="width"/>.
        /// </summary>
        /// <param name="line">line to be set</param>
        /// <param name="width">starting and ending width</param>
        internal static void SetWidth(LineRenderer line, float width)
        {
            line.startWidth = width;
            line.endWidth = width;
        }

        /// <summary>
        /// If <paramref name="gameObject"/> does not have a <see cref="LineRenderer"/> yet, one
        /// will be added. The line of it is defined by the given other parameters.
        /// </summary>
        /// <param name="gameObject">game object holding the <see cref="LineRenderer"/></param>
        /// <param name="linePoints">the points of a polyline</param>
        /// <param name="width">the width of the line</param>
        /// <param name="material">the material to be used (will considered a shared material)</param>
        /// <returns>the existing or newly added <see cref="LineRenderer"/></returns>
        internal static LineRenderer Draw(GameObject gameObject, Vector3[] linePoints, float width, Material material = null)
        {
            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            if (line == null)
            {
                // edge does not yet have a renderer; we add a new one
                line = gameObject.AddComponent<LineRenderer>();
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

        /// <summary>
        /// If <paramref name="gameObject"/> does not have a <see cref="LineRenderer"/> yet, one
        /// will be added. The line of it is defined by the given other parameters.
        /// </summary>
        /// <param name="gameObject">game object holding the <see cref="LineRenderer"/></param>
        /// <param name="from">the start of the line</param>
        /// <param name="to">the end of the line</param>
        /// <param name="width">the width of the line</param>
        /// <param name="material">the material to be used (will be considered a shared material)</param>
        /// <returns>the existing or newly added <see cref="LineRenderer"/></returns>
        internal static LineRenderer Draw(GameObject gameObject, Vector3 from, Vector3 to, float width, Material material = null)
        {
            return Draw(gameObject, new Vector3[] { from, to }, width, material);
        }

        /// <summary>
        /// Sets the vertices of the <paramref name="polyLine"/> to <paramref name="linePoints"/>.
        /// The number of points in <paramref name="polyLine"/> and <paramref name="linePoints"/>
        /// must match.
        /// </summary>
        /// <param name="polyLine">line to be redrawn</param>
        /// <param name="linePoints">new vertices defining the line</param>
        /// <exception cref="System.Exception">thrown if the number of points in <paramref name="polyLine"/> and
        /// <paramref name="linePoints"/> do not match</exception>
        internal static void ReDraw(LineRenderer polyLine, Vector3[] linePoints)
        {
            if (polyLine.positionCount != linePoints.Length)
            {
                throw new System.Exception("Numbers of line points do not match.");
            }
            polyLine.SetPositions(linePoints);
        }

        /// <summary>
        /// Sets the vertices of the <paramref name="line"/> to <paramref name="from"/>
        /// and <paramref name="to"/>.
        /// Precondition: <paramref name="line"/> must be a single line consisting of
        /// two points. We do not want to change a true polyline by a single line.
        /// </summary>
        /// <param name="line">line to be redrawn</param>
        /// <param name="from">the start of the line</param>
        /// <param name="to">the end of the line</param>
        /// <exception cref="System.Exception">thrown if the number of points in <paramref name="line"/> is
        /// different from two</exception>
        internal static void ReDraw(LineRenderer line, Vector3 from, Vector3 to)
        {
            if (line.positionCount != 2)
            {
                throw new System.Exception("Numbers of line points do not match.");
            }
            line.SetPositions(new Vector3[] { from, to });
        }
    }
}
