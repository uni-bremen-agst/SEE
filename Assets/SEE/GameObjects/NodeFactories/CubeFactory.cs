using SEE.DataModel;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for cubes as visual representations of graph nodes in the scene.
    /// Cubes are used for both leaves (as an alternative to CScape buildings) and
    /// inner nodes (e.g., for the streets in EvoStreets), but because they are
    /// used for inner nodes, too, they must provide SetLineWidth() even though
    /// it does not do anything.
    /// </summary>
    public class CubeFactory : InnerNodeFactory
    {
        private Mesh cubeMesh;

        public CubeFactory()
        {
            cubeMesh = new Mesh();

            // For correct rendering of transparency, the faces are defined in the order:
            // z+, x-, x+, y+, z-
            // The bottom face is never seen and thus excluded.

            Vector3[] vertices = new Vector3[20]
            {
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f)
            };

            Vector3[] normals = new Vector3[20]
            {
                new Vector3( 0.0f,  0.0f,  1.0f),
                new Vector3( 0.0f,  0.0f,  1.0f),
                new Vector3( 0.0f,  0.0f,  1.0f),
                new Vector3( 0.0f,  0.0f,  1.0f),

                new Vector3(-1.0f,  0.0f,  0.0f),
                new Vector3(-1.0f,  0.0f,  0.0f),
                new Vector3(-1.0f,  0.0f,  0.0f),
                new Vector3(-1.0f,  0.0f,  0.0f),
                
                new Vector3( 1.0f,  0.0f,  0.0f),
                new Vector3( 1.0f,  0.0f,  0.0f),
                new Vector3( 1.0f,  0.0f,  0.0f),
                new Vector3( 1.0f,  0.0f,  0.0f),
                
                new Vector3( 0.0f,  1.0f,  0.0f),
                new Vector3( 0.0f,  1.0f,  0.0f),
                new Vector3( 0.0f,  1.0f,  0.0f),
                new Vector3( 0.0f,  1.0f,  0.0f),
                
                new Vector3( 0.0f,  0.0f, -1.0f),
                new Vector3( 0.0f,  0.0f, -1.0f),
                new Vector3( 0.0f,  0.0f, -1.0f),
                new Vector3( 0.0f,  0.0f, -1.0f)
            };

            // Note: Winding order in unity is clockwise
            int[] indices = new int[30]
            {
                 0,  3,  2,  2,  1,  0,
                 4,  7,  6,  6,  5,  4,
                 8, 11, 10, 10,  9,  8,
                12, 15, 14, 14, 13, 12,
                16, 19, 18, 18, 17, 16
            };

            cubeMesh.SetVertices(vertices);
            cubeMesh.SetNormals(normals);
            cubeMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }

        public override GameObject NewBlock(int style)
        {
            GameObject result = new GameObject("Cube");
            result.AddComponent<MeshFilter>().mesh = cubeMesh;
            result.AddComponent<BoxCollider>();
            result.AddComponent<MeshRenderer>();

            SetHeight(result, DefaultHeight);

            result.tag = Tags.Node;
            Renderer renderer = result.GetComponent<Renderer>();
            // Object should not cast shadows: too expensive and may hide information,
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Assigns a material to the object.
            renderer.sharedMaterial = materials.DefaultMaterial(Mathf.Clamp(style, 0, NumberOfStyles()-1));

            // Object should be static so that we save rendering time at run-time.
            result.isStatic = true;
            return result;
        }
    }
}

