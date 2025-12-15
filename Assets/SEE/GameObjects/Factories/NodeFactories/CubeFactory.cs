using SEE.Game;
using UnityEngine;

namespace SEE.GO.Factories.NodeFactories
{
    /// <summary>
    /// A factory for cubes as visual representations of graph nodes in the scene.
    /// </summary>
    internal class CubeFactory : NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shader">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public CubeFactory(MaterialsFactory.ShaderType shaderType, ColorRange colorRange)
            : base(shaderType, colorRange)
        { }

        /// <summary>
        /// Adds a <see cref="BoxCollider"/> to <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">the game object receiving the collider</param>
        protected override void AddCollider(GameObject gameObject)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        /// <summary>
        /// Model mesh for a game object to be re-used for all instances.
        /// It will be created in <see cref="GetMesh()"/> on demand.
        /// </summary>
        private static Mesh modelMesh;

        /// <summary>
        /// Returns a (cached) mesh for a cube.
        /// Sets <see cref="modelMesh"/> if not yet set to cache the newly generated mesh.
        /// </summary>
        /// <param name="metrics">this parameter will be ignored</param>
        /// <returns>mesh for a cube</returns>
        protected override Mesh GetMesh(float[] metrics)
        {
            if (modelMesh != null)
            {
                return modelMesh;
            }
            modelMesh = new Mesh
            {
                name = "SEECubeMesh"
            };

            // See http://ilkinulas.github.io/development/unity/2016/05/06/uv-mapping.html
            const float extent = 0.5f;

            const float l = -extent; // left
            const float r =  extent; // right
            const float f = -extent; // front
            const float b =  extent; // back
            const float d = -extent; // down
            const float u =  extent; // up

            Vector3[] vertices = {
                // back rectangle
                new(r, d,  b), // 0
                new(l, d,  b), // 1
                new(r, u,  b), // 2
                new(l, u,  b), // 3

                // front rectangle
                new(r, u, f), // 4
                new(l, u, f), // 5
                new(r, d, f), // 6
                new(l, d, f), // 7

                // top rectangle
                new(r, u, b), // 8
                new(l, u, b), // 9
                new(r, u, f), // 10
                new(l, u, f), // 11

                // bottom rectangle
                new(r, d, f), // 12
                new(r, d, b), // 13
                new(l, d, b), // 14
                new(l, d, f), // 15

                // left rectangle
                new(l, d, b), // 16
                new(l, u, b), // 17
                new(l, u, f), // 18
                new(l, d, f), // 19

                // right rectangle
                new(r, d, f), // 20
                new(r, u, f), // 21
                new(r, u, b), // 22
                new(r, d, b), // 23
            };

            Vector3[] normals = {
                new(0.0f, 0.0f, 1.0f), // 0
                new(0.0f, 0.0f, 1.0f), // 1
                new(0.0f, 0.0f, 1.0f), // 2
                new(0.0f, 0.0f, 1.0f), // 3

                new(0.0f, 0.0f, -1.0f), // 4
                new(0.0f, 0.0f, -1.0f), // 5
                new(0.0f, 0.0f, -1.0f), // 6
                new(0.0f, 0.0f, -1.0f), // 7

                new(0.0f, 1.0f, 0.0f), // 8
                new(0.0f, 1.0f, 0.0f), // 9
                new(0.0f, 1.0f, 0.0f), // 10
                new(0.0f, 1.0f, 0.0f), // 11

                new(0.0f, -1.0f, 0.0f), // 12
                new(0.0f, -1.0f, 0.0f), // 13
                new(0.0f, -1.0f, 0.0f), // 14
                new(0.0f, -1.0f, 0.0f), // 15

                new(-1.0f, 0.0f, 0.0f), // 16
                new(-1.0f, 0.0f, 0.0f), // 17
                new(-1.0f, 0.0f, 0.0f), // 18
                new(-1.0f, 0.0f, 0.0f), // 19

                new(1.0f, 0.0f, 0.0f), // 20
                new(1.0f, 0.0f, 0.0f), // 21
                new(1.0f, 0.0f, 0.0f), // 22
                new(1.0f, 0.0f, 0.0f)  // 23
             };

            // The triangles forming the cube.
            // Unity3D uses clockwise winding order for determining front-facing triangles.
            int[] triangles =
            {
                // back rectangle
                /* 0 */ 0, /* 1 */ 2, /* 2 */ 3,
                /* 3 */ 0, /* 4 */ 3, /* 5 */ 1,

                // top rectangle
                /* 6 */ 8, /* 7 */ 10, /* 8 */ 11,
                /* 9 */ 8, /* 10 */ 11, /* 11 */ 9,

                // front rectangle
                /* 6 */ 4, /* 7 */ 6, /* 8 */ 7,
                /* 9 */ 7, /* 10 */ 5, /* 11 */ 4,

                /* 18 */ 12, /* 19 */ 13, /* 20 */ 14,
                /* 21 */ 12, /* 22 */ 14, /* 23 */ 15,

                // left rectangle
                /* 24 */ 16, /* 25 */ 17, /* 26 */ 18,
                /* 27 */ 16, /* 28 */ 18, /* 29 */ 19,

                /* 30 */ 20, /* 31 */ 21, /* 32 */ 22,
                /* 33 */ 20, /* 34 */ 22, /* 35 */ 23
            };

            // A mesh stores the texture mapping data as UVs. These are basically
            // 2D fold-outs of the actual 3D Mesh, as if you peeled back the skin
            // of an object and laid it out flat.
            // UV coordinates (also sometimes called texture coordinates) are references
            // to specific locations on the image. They only use two dimensions (u,v).
            // Texture mapping is the list of 2D UV coordinates mapped to their 3D vertex
            // counterparts on the surface in three dimensions (x,y,z). This mapping tells Unity
            // exactly how and where to project the image on the Mesh.
            // The size of the UV array must be the same as the size of the vertices array.
            // An element of a UV array is a two-dimensional vector with co-ordinates (U, V).
            // The U co-ordinate relates to the width of the texture, the V co-ordinate to its height.
            // Unity stores UVs in 0-1 space. [0,0] represents the bottom-left corner of the texture,
            // and [1,1] represents the top-right.
            Vector2[] uvs =
            {
                // back rectangle
                new(0.0f, 0.0f), // 0
                new(1.0f, 0.0f), // 1
                new(0.0f, 1.0f), // 2
                new(1.0f, 1.0f), // 3

                // front rectangle
                new(1.0f, 1.0f), // 4
                new(0.0f, 1.0f), // 5
                new(1.0f, 0.0f), // 6
                new(0.0f, 0.0f), // 7

                // top rectangle
                new(1.0f, 1.0f), // 8
                new(0.0f, 1.0f), // 9
                new(1.0f, 0.0f), // 10
                new(0.0f, 0.0f), // 11

                // bottom rectangle
                new(0.0f, 0.0f), // 12
                new(0.0f, 1.0f), // 13
                new(1.0f, 1.0f), // 14
                new(1.0f, 0.0f), // 15

                // left rectangle
                new(0.0f, 0.0f), // 16
                new(0.0f, 1.0f), // 17
                new(1.0f, 1.0f), // 18
                new(1.0f, 0.0f), // 19

                // right rectangle
                new(0.0f, 0.0f), // 20
                new(0.0f, 1.0f), // 21
                new(1.0f, 1.0f), // 22
                new(1.0f, 0.0f), // 23
            };

            modelMesh.SetVertices(vertices);
            modelMesh.SetNormals(normals);
            modelMesh.SetIndices(triangles, MeshTopology.Triangles, 0);
            modelMesh.uv = uvs;
            modelMesh.Optimize();

            return modelMesh;
        }
    }
}

