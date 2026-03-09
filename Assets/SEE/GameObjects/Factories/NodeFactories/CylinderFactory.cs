using SEE.Game;
using UnityEngine;

namespace SEE.GO.Factories.NodeFactories
{
    /// <summary>
    /// A factory for cylinder game objects.
    /// </summary>
    internal class CylinderFactory : NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shaderType">Shader type to be used for rendering the materials the created objects consist of.</param>
        /// <param name="colorRange">The color range of the created objects.</param>
        public CylinderFactory(MaterialsFactory.ShaderType shaderType, ColorRange colorRange)
            : base(shaderType, colorRange)
        { }

        /// <summary>
        /// The default number of radial segments of the cylinder mesh.
        /// </summary>
        private const int defaultRadialSegments = 40;
        /// <summary>
        /// The default radius of the cylinder. The diameter should be 1
        /// so that the scale is comparable to standard cubes.
        /// </summary>
        private const float defaultRadius = 0.5f;
        /// <summary>
        /// The default height of the cylinder. Again, it should be 1 so that
        /// the scale is comparable to standard cubes.
        /// </summary>
        private const float defaultHeight = 1.0f;

        /// <summary>
        /// Model mesh for a game object to be re-used for all instances.
        /// It will be created in <see cref="GetMesh()"/> on demand.
        /// </summary>
        private static Mesh modelMesh;

        /// <summary>
        /// Returns a (cached) cylinder mesh.
        /// Sets <see cref="modelMesh"/> if not yet set to cache the newly generated mesh.
        /// </summary>
        /// <param name="metrics">This parameter will be ignored.</param>
        /// <returns>Cylinder mesh (the same for each call).</returns>
        protected override Mesh GetMesh(float[] metrics)
        {
            if (modelMesh != null)
            {
                return modelMesh;
            }
            modelMesh = new Mesh
            {
                name = "SEECylinderMesh"
            };

            int radialSegments = defaultRadialSegments;
            float radius = defaultRadius;
            float height = defaultHeight;

            // To achieve flat shading, EVERY triangle needs its own unique vertices.
            // Top cap = radialSegments * 3 vertices
            // Bottom cap = radialSegments * 3 vertices
            // Sides = radialSegments * 2 triangles * 3 vertices = radialSegments * 6
            int numVertices = (radialSegments * 3) * 2 + (radialSegments * 6);

            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[numVertices];

            int v = 0;
            float angleStep = Mathf.PI * 2f / radialSegments;

            for (int i = 0; i < radialSegments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                // Pre-calculate positions
                float cos1 = Mathf.Cos(angle1); float sin1 = Mathf.Sin(angle1);
                float cos2 = Mathf.Cos(angle2); float sin2 = Mathf.Sin(angle2);

                Vector3 p1Top = new(cos1 * radius, height / 2f, sin1 * radius);
                Vector3 p2Top = new(cos2 * radius, height / 2f, sin2 * radius);
                Vector3 p1Bot = new(cos1 * radius, -height / 2f, sin1 * radius);
                Vector3 p2Bot = new(cos2 * radius, -height / 2f, sin2 * radius);

                // --- 1. TOP CAP ---
                // UVs mapped to a circle in the top-left quadrant
                vertices[v] = new Vector3(0, height / 2f, 0);
                uvs[v] = new Vector2(0.25f, 0.75f);
                triangles[v] = v; v++;

                vertices[v] = p2Top;
                uvs[v] = new Vector2(0.25f + cos2 * 0.25f, 0.75f + sin2 * 0.25f);
                triangles[v] = v; v++;

                vertices[v] = p1Top;
                uvs[v] = new Vector2(0.25f + cos1 * 0.25f, 0.75f + sin1 * 0.25f);
                triangles[v] = v; v++;

                // --- 2. BOTTOM CAP ---
                // UVs mapped to a circle in the top-right quadrant
                vertices[v] = new Vector3(0, -height / 2f, 0);
                uvs[v] = new Vector2(0.75f, 0.75f);
                triangles[v] = v; v++;

                vertices[v] = p1Bot;
                uvs[v] = new Vector2(0.75f + cos1 * 0.25f, 0.75f + sin1 * 0.25f);
                triangles[v] = v; v++;

                vertices[v] = p2Bot;
                uvs[v] = new Vector2(0.75f + cos2 * 0.25f, 0.75f + sin2 * 0.25f);
                triangles[v] = v; v++;

                // --- 3. SIDES ---
                // UVs mapped to the bottom half of the texture (0.0 to 0.5 on the Y axis)
                float u1 = (float)i / radialSegments;
                float u2 = (float)(i + 1) / radialSegments;

                // Side Triangle 1
                vertices[v] = p1Top; uvs[v] = new Vector2(u1, 0.5f); triangles[v] = v; v++;
                vertices[v] = p2Top; uvs[v] = new Vector2(u2, 0.5f); triangles[v] = v; v++;
                vertices[v] = p1Bot; uvs[v] = new Vector2(u1, 0.0f); triangles[v] = v; v++;

                // Side Triangle 2
                vertices[v] = p2Top; uvs[v] = new Vector2(u2, 0.5f); triangles[v] = v; v++;
                vertices[v] = p2Bot; uvs[v] = new Vector2(u2, 0.0f); triangles[v] = v; v++;
                vertices[v] = p1Bot; uvs[v] = new Vector2(u1, 0.0f); triangles[v] = v; v++;
            }

            modelMesh.vertices = vertices;
            modelMesh.uv = uvs;
            modelMesh.triangles = triangles;

            // Recalculates lighting based on our unshared vertices (creates flat shading)
            modelMesh.RecalculateNormals();

            return modelMesh;
        }

        /// <summary>
        /// Adds a <see cref="MeshCollider"/> to <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object receiving the collider.</param>
        protected override void AddCollider(GameObject gameObject)
        {
            gameObject.AddComponent<MeshCollider>();
        }
    }
}
