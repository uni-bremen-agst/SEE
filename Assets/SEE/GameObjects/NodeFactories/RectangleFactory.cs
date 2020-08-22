using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for rectangle inner game objects.
    /// </summary>
    public class RectangleFactory : LineInnerNodeFactory
    {
        /// <summary>
        /// Constructor allowing to set the initial unit for the width of the lines that render this inner node.
        /// Every line width passed as a parameter to methods of this class will be multiplied by this factor
        /// for the actual rendering.
        /// </summary>
        /// <param name="unit">initial unit for the width of all lines</param>
        public RectangleFactory(float unit)
            : base(unit)
        {
            materials = new Materials(1, DefaultColor, DefaultColor);
            material = new Material(materials.DefaultMaterial(0, 0));
            material.color = DefaultColor;
        }

        /// <summary>
        /// The material we use for the line drawing the rectangle.
        /// </summary>
        private Material material;

        /// <summary>
        /// The default line width of the rectangle line.
        /// </summary>
        private const float defaultLength = 1.0f;

        // The default color for rectangle lines.
        public static Color DefaultColor = Color.blue;

        public override GameObject NewBlock(int index = 0, int level = 0)
        {
            GameObject result = new GameObject();
            result.isStatic = true;
            AttachLine(result, defaultLength, defaultLineWidth * Unit, DefaultColor);
            return result;
        }

        /// <summary>
        /// Draws the rectangle line and attaches it to given rectangle game object.
        /// </summary>
        /// <param name="rectangle">game object to which the rectangle line should be added</param>
        /// <param name="length">length of the rectangle to be drawn (width and depth)</param>
        /// <param name="lineWidth">width of the line to be drawn</param>
        /// <param name="color">color of the line to be drawn</param>
        private void AttachLine(GameObject rectangle, float length, float lineWidth, Color color)
        {
            LineRenderer line = rectangle.AddComponent<LineRenderer>();

            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, color);
            LineFactory.SetWidth(line, lineWidth);

            // We want to set the points of the circle lines relative to the game object.
            // If the containing object moves, the line renderer should move along with it.
            line.useWorldSpace = false;

            // All circles lines have the same material to reduce the number of drawing calls.
            line.sharedMaterial = material;

            float halfLength = length / 2.0f;

            // Set some positions
            Vector3[] positions = new Vector3[4];
            positions[0] = new Vector3(-halfLength, 0.0f,  halfLength);  // left back corner
            positions[1] = new Vector3( halfLength, 0.0f,  halfLength);  // right back corner
            positions[2] = new Vector3( halfLength, 0.0f, -halfLength);  // right front corner
            positions[3] = new Vector3(-halfLength, 0.0f, -halfLength);  // left front corner
            line.positionCount = positions.Length;
            line.SetPositions(positions);

            // Connect the start and end positions of the line together to form a continuous loop.
            line.loop = true;
        }
    }
}
