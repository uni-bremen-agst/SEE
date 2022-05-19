using SEE.DataModel;
using SEE.Game;
using UnityEngine;

namespace SEE.GO
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

        public override GameObject NewBlock(int style, int renderQueueOffset = 0)
        {
            GameObject result = CreateBlock();
            result.AddComponent<BoxCollider>();
            MeshRenderer renderer = result.AddComponent<MeshRenderer>();
            Materials.SetSharedMaterial(renderer, renderQueueOffset: renderQueueOffset, index: style);
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return result;
        }

        private static GameObject CreateBlock()
        {
            GameObject gameObject = new GameObject("Cube") { tag = Tags.Node };
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetCubeMesh();
            return gameObject;
        }

        private static Mesh modelMesh;

        private static Mesh GetCubeMesh()
        {
            if (modelMesh != null)
            {
                return modelMesh;
            }
            //create the mesh
            modelMesh = new Mesh
            {
                name = "SEECubeMesh"
            };

            // See http://ilkinulas.github.io/development/unity/2016/05/06/uv-mapping.html
            const float extent = 0.5f;
            Vector3[] vertices = {
               /* -extent */ new Vector3(-extent, extent, -extent),        // front left upper corner
               /* 1 */ new Vector3(-extent, -extent, -extent),           // front left lower corner
               /* 2 */ new Vector3(extent, extent, -extent),     // front right upper corner
               /* 3 */ new Vector3(extent, -extent, -extent),        // front right lower corner

               /* 4 */ new Vector3(-extent, -extent, extent),        // back left lower corner
               /* 5 */ new Vector3(extent, -extent, extent),     // back right lower corner
               /* 6 */ new Vector3(-extent, extent, extent),     // back left upper corner
               /* 7 */ new Vector3(extent, extent, extent),  // back right upper corner

               /* 8 */ new Vector3(-extent, extent, -extent),        // front left upper corner = vertex 0
               /* 9 */ new Vector3(extent, extent, -extent),     // front right upper corner = vertex 2

               /* 1-extent */ new Vector3(-extent, extent, -extent),       // front left upper corner = vertex 0
               /* 11 */ new Vector3(-extent, extent, extent),    // back left upper corner = vertex 6

               /* 12 */ new Vector3(extent, extent, -extent),    // front right upper corner = vertex 2
               /* 13 */ new Vector3(extent, extent, extent), // back right upper corner = vertex 7
             };

            // The triangles forming the cube.
            // Unity3D uses clockwise winding order for determining front-facing triangles.
            int[] triangles = {
                // front rectangle
                0, 2, 1,
			    1, 2, 3,
                // back rectangle
                4, 5, 6,
			    5, 7, 6,
                // top rectangle
                6, 7, 8,
			    7, 9 ,8,
                // bottom rectangle
                1, 3, 4,
			    3, 5, 4,
                // left rectangle
                1, 11, 10,
			    1, 4, 11,
                // right rectangle
                3, 12, 5,
			    5, 12, 13
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
            Vector2[] uvs = {
               new Vector2(0, 0.66f),
               new Vector2(0.25f, 0.66f),
               new Vector2(0, 0.33f),
               new Vector2(0.25f, 0.33f),

               new Vector2(0.5f, 0.66f),
               new Vector2(0.5f, 0.33f),
               new Vector2(0.75f, 0.66f),
               new Vector2(0.75f, 0.33f),

               new Vector2(1, 0.66f),
               new Vector2(1, 0.33f),

               new Vector2(0.25f, 1),
               new Vector2(0.5f, 1),

               new Vector2(0.25f, 0),
               new Vector2(0.5f, 0),
             };

            modelMesh.vertices = vertices;
            modelMesh.triangles = triangles;
            modelMesh.uv = uvs;
            modelMesh.Optimize();
            modelMesh.RecalculateNormals();

            return modelMesh;
        }
    }
}

