using SEE.Game;
using System.Linq;
using UnityEngine;

namespace SEE.GO.NodeFactories
{
    /// <summary>
    /// A factory for bar charts as visual representations of graph nodes
    /// in the scene.
    /// </summary>
    internal class BarsFactory : NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shaderType">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public BarsFactory(Materials.ShaderType shaderType, ColorRange colorRange)
            : base(shaderType, colorRange)
        { }

        /// <summary>
        /// Adds a <see cref="MeshCollider"/> to <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">game object receiving the <see cref="MeshCollider"/></param>
        protected override void AddCollider(GameObject gameObject)
        {
            gameObject.AddComponent<MeshCollider>();
        }

        /// <summary>
        /// Sets the height of <paramref name="gameObject"/> according to the height metric.
        /// No other size aspect is changed.
        /// </summary>
        /// <param name="gameObject">the game object whose dimensions are to be set</param>
        /// <param name="metrics">the metric values determining the lengths of <paramref name="gameObject"/></param>
        protected override void SetDimensions(GameObject gameObject, float[] metrics)
        {
            SetHeight(gameObject, metrics[1]);
        }

        /// <summary>
        /// The width of the bar chart.
        /// </summary>
        private const float TargetWidth = 1.0f;

        protected override Mesh GetMesh(float[] metrics)
        {
            float[] allButHeight = AllButHeight(metrics).ToArray();
            uint numberOfBars = (uint)allButHeight.Length;
            float widthOfBar = TargetWidth / numberOfBars;
            float xOffset = -TargetWidth / 2;
            float zOffset = -allButHeight.Max() / 2;

            // Four vertices per bar.
            Vector3[] vertices = new Vector3[numberOfBars * 4];
            // Two rectangles per bar; three indices per triangle.
            int[] triangles = new int[numberOfBars * 2 * 3];

            float leftCorner = 0;
            int nextTriangleIndex = 0;
            for (int i = 0; i < numberOfBars; i++)
            {
                int v = 4 * i;
                vertices[v] = new Vector3(xOffset + leftCorner, 0, zOffset);
                vertices[v + 1] = new Vector3(xOffset + leftCorner, 0, zOffset + allButHeight[i]);
                leftCorner += widthOfBar;
                vertices[v + 2] = new Vector3(xOffset + leftCorner, 0, zOffset + allButHeight[i]);
                vertices[v + 3] = new Vector3(xOffset + leftCorner, 0, zOffset);

                // Unity uses clockwise winding order for determining front-facing triangles.
                // first triangle
                triangles[nextTriangleIndex++] = v;
                triangles[nextTriangleIndex++] = v + 1;
                triangles[nextTriangleIndex++] = v + 3;

                // second triangle
                triangles[nextTriangleIndex++] = v + 1;
                triangles[nextTriangleIndex++] = v + 2;
                triangles[nextTriangleIndex++] = v + 3;

            }
            // TODO: The mesh is currently only two-dimensional.
            Mesh mesh = new Mesh
            {
                name = "SEEBarMesh"
            };
            mesh.vertices = vertices;
            // It is recommended to assign a triangle array after assigning the
            // vertex array, in order to avoid out of bounds errors.
            mesh.triangles = triangles;

            return mesh;
        }
    }
}

