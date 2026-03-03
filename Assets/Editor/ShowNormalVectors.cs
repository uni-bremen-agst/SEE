#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// SEE menu entry to visualize the normal vectors of a mesh.
    /// </summary>
    internal class ShowNormalVectors
    {
        private static Color Color = Color.yellow;
        private static readonly float NormalsLength = 1f;

        [MenuItem("SEE/Show Normals")]
        public static void Show()
        {
            GameObject gameObject = Selection.activeGameObject;

            if (gameObject != null)
            {
                MeshFilter filter = gameObject.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) return;

                Mesh mesh = filter.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;

                for (int i = 0; i < vertices.Length; i++)
                {
                    // Convert local vertex positions and normals to world space
                    Vector3 worldPos = gameObject.transform.TransformPoint(vertices[i]);
                    Vector3 worldNormal = gameObject.transform.TransformDirection(normals[i]);

                    Draw(worldPos, worldNormal * NormalsLength);
                }
            }
        }

        private static void Draw(Vector3 from, Vector3 direction)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = "NormalVector";
            gameObject.transform.position = from;
            gameObject.transform.localScale = 0.01f * Vector3.one;
            // Add a LineRenderer component (or grab one already attached)
            LineRenderer line = gameObject.AddComponent<LineRenderer>();

            // Set line width
            line.startWidth = 0.01f;
            line.endWidth = 0.01f;
            line.startColor = Color;
            line.endColor = Color;

            // A line needs at least 2 points (index 0 is start, index 1 is end)
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, from + direction); // Calculates the destination point
        }
    }
}

#endif
