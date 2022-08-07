using SEE.Game;
using UnityEngine;

namespace SEE.GO.NodeFactories
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
        public CubeFactory(Materials.ShaderType shaderType, ColorRange colorRange)
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
        /// It will be ceated in <see cref="GetMesh()"/> on demand.
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

            Vector3[] vertices = new Vector3[24]
            {
                // back rectangle
                /*  0 */ new Vector3(r, d,  b),
                /*  1 */ new Vector3(l, d,  b),
                /*  2 */ new Vector3(r, u,  b),
                /*  3 */ new Vector3(l, u,  b),

                // front rectangle
                /*  4 */ new Vector3(r, u, f),
                /*  5 */ new Vector3(l, u, f),
                /*  6 */ new Vector3(r, d, f),
                /*  7 */ new Vector3(l, d, f),

                // top rectangle
                /*  8 */ new Vector3(r, u, b),
                /*  9 */ new Vector3(l, u, b),
                /* 10 */ new Vector3(r, u, f),
                /* 11 */ new Vector3(l, u, f),

                // bottom rectangle
                /* 12 */ new Vector3(r, d, f),
                /* 13 */ new Vector3(r, d, b),
                /* 14 */ new Vector3(l, d, b),
                /* 15 */ new Vector3(l, d, f),

                // left rectangle
                /* 16 */ new Vector3(l, d, b),
                /* 17 */ new Vector3(l, u, b),
                /* 18 */ new Vector3(l, u, f),
                /* 19 */ new Vector3(l, d, f),

                // right rectangle
                /* 20 */ new Vector3(r, d, f),
                /* 21 */ new Vector3(r, u, f),
                /* 22 */ new Vector3(r, u, b),
                /* 23 */ new Vector3(r, d, b),
            };

            Vector3[] normals = new Vector3[24]
            {
                /*  0 */ new Vector3(0.0f, 0.0f, 1.0f),
                /*  1 */ new Vector3(0.0f, 0.0f, 1.0f),
                /*  2 */ new Vector3(0.0f, 0.0f, 1.0f),
                /*  3 */ new Vector3(0.0f, 0.0f, 1.0f),

                /*  4 */ new Vector3(0.0f, 0.0f, -1.0f),
                /*  5 */ new Vector3(0.0f, 0.0f, -1.0f),
                /*  6 */ new Vector3(0.0f, 0.0f, -1.0f),
                /*  7 */ new Vector3(0.0f, 0.0f, -1.0f),

                /*  8 */ new Vector3(0.0f, 1.0f, 0.0f),
                /*  9 */ new Vector3(0.0f, 1.0f, 0.0f),
                /* 10 */ new Vector3(0.0f, 1.0f, 0.0f),
                /* 11 */ new Vector3(0.0f, 1.0f, 0.0f),

                /* 12 */ new Vector3(0.0f, -1.0f, 0.0f),
                /* 13 */ new Vector3(0.0f, -1.0f, 0.0f),
                /* 14 */ new Vector3(0.0f, -1.0f, 0.0f),
                /* 15 */ new Vector3(0.0f, -1.0f, 0.0f),

                /* 16 */ new Vector3(-1.0f, 0.0f, 0.0f),
                /* 17 */ new Vector3(-1.0f, 0.0f, 0.0f),
                /* 18 */ new Vector3(-1.0f, 0.0f, 0.0f),
                /* 19 */ new Vector3(-1.0f, 0.0f, 0.0f),

                /* 20 */ new Vector3(1.0f, 0.0f, 0.0f),
                /* 21 */ new Vector3(1.0f, 0.0f, 0.0f),
                /* 22 */ new Vector3(1.0f, 0.0f, 0.0f),
                /* 23 */ new Vector3(1.0f, 0.0f, 0.0f)
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
                /* 0 */ new Vector2(0.0f, 0.0f),
                /* 1 */ new Vector2(1.0f, 0.0f),
                /* 2 */ new Vector2(0.0f, 1.0f),
                /* 3 */ new Vector2(1.0f, 1.0f),

                // front rectangle
                /* 4 */ new Vector2(1.0f, 1.0f),
                /* 5 */ new Vector2(0.0f, 1.0f),
                /* 6 */ new Vector2(1.0f, 0.0f),
                /* 7 */ new Vector2(0.0f, 0.0f),

                // top rectangle
                /*  8 */ new Vector2(1.0f, 1.0f),
                /*  9 */ new Vector2(0.0f, 1.0f),
                /* 10 */ new Vector2(1.0f, 0.0f),
                /* 11 */ new Vector2(0.0f, 0.0f),

                // bottom rectangle
                /* 12 */ new Vector2(0.0f, 0.0f),
                /* 13 */ new Vector2(0.0f, 1.0f),
                /* 14 */ new Vector2(1.0f, 1.0f),
                /* 15 */ new Vector2(1.0f, 0.0f),

                // left rectangle
                /* 16 */ new Vector2(0.0f, 0.0f),
                /* 17 */ new Vector2(0.0f, 1.0f),
                /* 18 */ new Vector2(1.0f, 1.0f),
                /* 19 */ new Vector2(1.0f, 0.0f),

                // right rectangle
                /* 20 */ new Vector2(0.0f, 0.0f),
                /* 21 */ new Vector2(0.0f, 1.0f),
                /* 22 */ new Vector2(1.0f, 1.0f),
                /* 23 */ new Vector2(1.0f, 0.0f),
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

