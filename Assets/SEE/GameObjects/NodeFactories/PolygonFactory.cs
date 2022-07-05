using SEE.Game;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.GO.NodeFactories
{
    /// <summary>
    /// A factory for shapes with a irregular closed polygon as a floor space
    /// as visual representations of graph nodes in the scene.
    /// </summary>
    public class PolygonFactory : NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shader">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public PolygonFactory(Materials.ShaderType shaderType, ColorRange colorRange)
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
        /// FIXME: Continue.
        /// </summary>
        /// <param name="gameObject">the game object whose dimensions are to be set</param>
        /// <param name="metrics">the metric values determining the lengths of <paramref name="gameObject"/></param>
        protected override void SetDimensions(GameObject gameObject, float[] metrics)
        {
            // We want to be able to compare the axes across different nodes,
            // hence, we need the same point of reference. In case of cubes, the
            // reference is the width metric. Hence, we use that here, too, for
            // reasons of consistency.
            SetSize(gameObject, new Vector3(metrics[0] / 2, metrics[1], metrics[0] / 2));
        }

        private const float targetRadius = 0.5f;

        protected override Mesh GetMesh(float[] metrics)
        {
            // https://math.stackexchange.com/questions/1930607/maximum-area-enclosure-given-side-lengths

            float[] allButHeight = AllButHeight(metrics).ToArray();

            CheckPreconditions(allButHeight);

            Vector2[] vertices = MetricsToVertices(allButHeight, out float radius);
            Vector2[] vertices2D = new Vector2[vertices.Length];
            Vector3[] vertices3D = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 vertex = vertices[v];
                vertices2D[v] = new Vector2(vertex.x, vertex.y);
                vertices3D[v] = new Vector3(vertex.x, 0, vertex.y);
            }
            // TODO: The mesh is currently only two-dimensional.
            Mesh mesh = new Mesh
            {
                name = "SEESPolygonMesh"
            };
            mesh.vertices = vertices3D;
            // It is recommended to assign a triangle array after assigning the
            // vertex array, in order to avoid out of bounds errors.
            mesh.triangles = Triangulator.Triangulate(vertices2D);

            return mesh;

            // Verify sanity: Note that if one of the line segments would be longer than the sum of
            // the length of the rest of the line segments, they cannot form a polygon.
            static void CheckPreconditions(float[] metrics)
            {
                if (metrics.Length >= 4)
                {
                    double maxLength = metrics[0];
                    double sumLength = metrics[0];

                    for (int i = 1; i < metrics.Length; i++)
                    {
                        sumLength += metrics[i];
                        if (maxLength < metrics[i])
                        {
                            maxLength = metrics[i];
                        }
                    }

                    if (maxLength > sumLength - maxLength)
                    {
                        throw new Exception($"Not a valid polygon; one of the line segments is too long ({maxLength} <= {sumLength} - {maxLength}).");
                    }
                }
                else
                {
                    throw new Exception($"More than three metrics should be given to a three-dimensional polygon. There are only {metrics.Length} metrics.");
                }
            }
        }

        private Vector2[] MetricsToVertices(float[] metrics, out float radius)
        {
            Vector2[] result = new Vector2[metrics.Length];
            radius = FindRadius(metrics, 0.0001f, out uint i);
            Debug.Log($"radius = {radius:F6} using {i} iterations.\n");

            // We start at 12 o'clock.
            float radian = Mathf.PI / 2;
            for (int j = 0; j < metrics.Length; ++j)
            {
                Vector2 vertexOnCircle = new Vector2(targetRadius * Mathf.Cos(radian),
                                                     targetRadius * Mathf.Sin(radian));
                result[j] = vertexOnCircle;
                // Note: Radians rotate counter clockwise with increasing values.
                // We want to traverse the circle clockwise, however.
                radian -= Theta(metrics[j], radius);
            }
            return result;
        }

        /// <summary>
        /// Theta(l, r) is the angle between the r-length edges in the isosceles triangle.
        /// The triangle is formed by three points, P1, P2, P3. P3 is the center of the
        /// circle. P1 and P2 are on the circle. Parameter <paramref name="length"/> is
        /// the distance between P1 and P2. The distances between P3 and P1 is the
        /// radius of the circle and equal to the distance between P3 and P2.
        /// </summary>
        /// <param name="length">the length of the edge from one point on a circle to another point
        /// on that circle</param>
        /// <param name="radius">the radius of the circle of the two points connected by the edge</param>
        /// <returns>angle between the r-length edges in the isosceles triangles</returns>
        private static float Theta(float length, float radius)
        {
            return 2.0f * Mathf.Asin(0.5f * length / radius);
        }

        private static float FindRadius(float[] lengths, float epsilon, out uint numberOfIterations)
        {
            // Each of the n (n = lengths.Count) line segments (lengths) essentially forms an isosceles
            // triangle with the center of the common circle. Two of the sides are of length r, the
            // radius of the common circle, and the third is the line segment itself, length Li.
            // Essentially, we can decompose the polygon into triangular wedges, each being an
            // isosceles triangle. This also means that the order of the line segments does not
            // affect the area, as reordering the triangular parts does not change their areas.
            float minTheta = 1.0f - epsilon;
            float maxTheta = 1.0f + epsilon;

            // Sum over all values in lengths.
            float sumLength = lengths[0];
            // The maximum over all values in lengths.
            float maxLength = lengths[0];

            for (uint i = 1; i < lengths.Length; i++)
            {
                sumLength += lengths[i];
                if (maxLength < lengths[i])
                {
                    maxLength = lengths[i];
                }
            }

            // We will be running a binary search for the suitable radius to fit all
            // lengths. Each iteration will increase minRadius and decrease maxRadius.
            // The final radius will be between the two.
            float minRadius = maxLength / 2;
            float maxRadius = sumLength / 2;

            // Resulting radius of the circle on which the vertices are placed.
            float radius;

            numberOfIterations = 0;
            while (true)
            {
                // The sum over all current theta angles.
                float sumTheta = 0.0f;

                numberOfIterations++;

                radius = (minRadius / 2) + (maxRadius / 2);

                for (uint i = 0; i < lengths.Length; i++)
                {
                    sumTheta += Theta(lengths[i], radius);
                }

                sumTheta /= 2 * Mathf.PI;

                if (sumTheta >= minTheta && sumTheta <= maxTheta)
                {
                    break;
                }
                else if (sumTheta < 1.0)
                {
                    maxRadius = radius;
                }
                else
                {
                    minRadius = radius;
                }
            }

            return radius;
        }
    }
}