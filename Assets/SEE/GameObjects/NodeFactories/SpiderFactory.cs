using SEE.Game;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.GO.NodeFactories
{
    /// <summary>
    /// A factory for shapes with a spider-chart floor space as visual representations
    /// of graph nodes in the scene.
    /// </summary>
    internal class SpiderFactory : NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shader">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public SpiderFactory(Materials.ShaderType shaderType, ColorRange colorRange)
            : base(shaderType, colorRange)
        { }

        /// <summary>
        /// Adds a <see cref="BoxCollider"/> to <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">the game object receiving the collider</param>
        protected override void AddCollider(GameObject gameObject)
        {
            gameObject.AddComponent<MeshCollider>();
        }

        /// <summary>
        /// Sets the height of <paramref name="gameObject"/> according to the height metric.
        /// Let M be the maximum value of <paramref name="metrics"/> excluding the height
        /// metric; then this method sets the width and depth to M/2 such that the length
        /// of the axes showing M is M long.
        /// </summary>
        /// <param name="gameObject">the game object whose dimensions are to be set</param>
        /// <param name="metrics">the metric values determining the lengths of <paramref name="gameObject"/></param>
        protected override void SetDimensions(GameObject gameObject, float[] metrics)
        {
            // The height metric is not put on one of the axes and will be set here.
            // The spider-chart's circle is a unit circle, that is, has a diameter of one.
            // The width and depth will be such that they are double the length of
            // the axes showing the maximal value. As a consequence, the length of the axis
            // with the maximal metric value is this maximal value.
            float max = Enumerable.Max(AllButHeight(metrics));
            SetSize(gameObject, new Vector3(max / 2, metrics[1], max / 2));
        }

        /// <summary>
        /// The radius of the unit circle on which the spider axes will be
        /// placed. We want the spider chart to be one Unity unit. That is, its circle
        /// should have diameter 1 or radius 0.5, respectively.
        /// </summary>
        private const float radius = 0.5f;

        protected override Mesh GetMesh(float[] metrics)
        {
            if (metrics.Length <= 3)
            {
                Debug.LogWarning($"More than three metrics should be given to a spider chart. There are only {metrics.Length} metrics.\n");
            }
            Vector2[] vertices = MetricsToVertices(metrics);
            Vector2[] vertices2D = new Vector2[vertices.Length];
            Vector3[] vertices3D = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                vertices2D[i] = new Vector2(vertex.x, vertex.y);
                vertices3D[i] = new Vector3(vertex.x, 0, vertex.y);
            }
            // TODO: The mesh is currently only two-dimensional.
            Mesh mesh = new Mesh
            {
                name = "SEESpiderMesh"
            };
            mesh.vertices = vertices3D;
            mesh.triangles = Triangulator.Triangulate(vertices2D);

            return mesh;
        }

        /// <summary>
        /// Returns the end points of the axes of the spider chart for the given
        /// <paramref name="metrics"/> (excluding the height metric at index 1).
        /// </summary>
        /// <param name="metrics">the metric values determining the lengths of the
        /// axes of the spider chart</param>
        /// <returns>end points of the axes of the spider chart</returns>
        private static Vector2[] MetricsToVertices(float[] metrics)
        {
            // All metrics except the one used for the height.
            float[] allButHeight = AllButHeight(metrics).ToArray();
            Normalize(allButHeight);

            // How many axes we have on the spider chart.
            int radialSegments = allButHeight.Length;

            // The center position of the circle for the spider chart.
            Vector2 center = Vector2.zero;

            // A step on the circle unit (expressed as radian) so that all axes
            // of the spider charts fit.
            // Axes are equidistant in the spider chart.
            // We want to traverse the circle counter clockwise that is why
            // the radian step should be negative.
            float radianStep = -2 * Mathf.PI / radialSegments;

            Vector2[] result = new Vector2[radialSegments];

            for (int i = 0; i < radialSegments; ++i)
            {
                // The radian for that vertex on the unit circle.
                // Note: radian 0 corresponds to 3 o'clock and radians
                // rotate counter clockwise with increasing values.
                float radian = i * radianStep + Mathf.PI;
                // position of current vertex on the unit circle
                Vector2 vertexOnCircle = new Vector2(radius * Mathf.Cos(radian),
                                                     radius * Mathf.Sin(radian));
                // Vector from the center towards the vertexOnCircle with length radius.
                Vector2 direction = vertexOnCircle - center;
                result[i] = direction * allButHeight[i] / radius;
            }

            return result;

            /// <summary>
            /// Let M be the maximum of <paramref name="values"/>.
            /// Normalizes all <paramref name="values"/> by dividing them by M
            /// and multiplying them by <see cref="radius"/>. As a consequence,
            /// the normalized value of M will be <see cref="radius"/>.
            /// If M = 0, all <paramref name="values"/> will be <see cref="radius"/>.
            /// </summary>
            /// <param name="values">the values to be normalized</param>
            static void Normalize(float[] values)
            {
                float max = Enumerable.Max(values);
                if (max == 0)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = radius;
                    }
                }
                else
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = (values[i] / max) * radius;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all <paramref name="metrics"/> except for the height metric (index 1).
        /// </summary>
        /// <param name="metrics">the metric values to be put onto the spider axes</param>
        /// <returns>all <paramref name="metrics"/> but the height metric at index 1</returns>
        private static IEnumerable<float> AllButHeight(float[] metrics)
        {
            return metrics.Where((value, index) => index != 1);
        }
    }
}
