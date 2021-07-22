using System.Collections.Generic;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Allows one to decorate game objects with circle lines.
    /// </summary>
    internal class CircleDecorator
    {
        /// <summary>
        /// Constructor for creating circle lines around the center of an object.
        /// </summary>
        /// <param name="nodeFactory">the factory that was used to create the objects for which to draw a circle line</param>
        /// <param name="color">the color for the circle line</param>
        public CircleDecorator(NodeFactory nodeFactory, Color color)
        {
            this.nodeFactory = nodeFactory;
            this.color = color;
        }

        /// <summary>
        /// The factory that was used to create the objects for which to draw a circle line.
        /// </summary>
        private readonly NodeFactory nodeFactory;

        /// <summary>
        /// The color for the circle line.
        /// </summary>
        private readonly Color color;

        /// <summary>
        /// The material we use for the circle line. It is the same for all circle lines
        /// to reduce the number of drawing calls.
        /// </summary>
        private readonly Material material = Materials.New(Materials.ShaderType.TransparentLine);

        /// <summary>
        /// Attaches a circle line to all game nodes. 
        /// The center of the circle is the center of the game node and the radius is
        /// the minimum of the game node's width and depth.
        /// The width of the line depends upon the radius. The greater the radius, the 
        /// thicker the circle line.
        /// </summary>
        /// <param name="gameNodes">list of game nodes for which to create Donut chart visualizations</param>
        public void Add(IEnumerable<GameObject> gameNodes)
        {
            foreach (GameObject gameNode in gameNodes)
            {
                Vector3 extent = nodeFactory.GetSize(gameNode) / 2.0f;
                // We want the circle to fit into gameNode, that is why we choose
                // the shorter value of the x and z co-ordinates. If the object
                // is a circle, then both are alike anyway.
                float radius = Mathf.Min(extent.x, extent.z);
                // Set line widths in relation to the radius of the object.
                float lineWidth = radius / 100.0f;
                AttachCircleLine(gameNode, lineWidth);
            }
        }

        /// <summary>
        /// Adds a line renderer that draws the circle line for the game objects.
        /// The thickness of the line is specified by <paramref name="lineWidth"/>.
        /// The center of the circle is the center of the game node and the radius is
        /// the minimum of the game node's width and depth.
        /// </summary>
        /// <param name="gameObject">the game object the circle is to be drawn</param>
        /// <param name="lineWidth">thickness of the circle line</param>
        private void AttachCircleLine(GameObject gameObject, float lineWidth)
        {
            // Note that we draw the circle line relative to the containing gameObject.
            // Hence, the radius is always 0.5 and the center of the circle is always
            // Vector3.zero.
            const float radius = 0.5f;

            // Number of line segments constituting the circle
            const int segments = 360;

            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
            }

            LineFactory.SetDefaults(line);
            LineFactory.SetColor(line, color);
            LineFactory.SetWidth(line, lineWidth);

            // We want to set the points of the circle lines relative to the game object.
            line.useWorldSpace = false;

            line.sharedMaterial = material;

            line.positionCount = segments + 1;
            const int pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
            Vector3[] points = new Vector3[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float rad = Mathf.Deg2Rad * (i * 360f / segments);
                points[i] = new Vector3(Mathf.Sin(rad) * radius, 0.0f, Mathf.Cos(rad) * radius);
            }
            line.SetPositions(points);
        }
    }
}
