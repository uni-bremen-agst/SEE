#if UNITY_EDITOR

using SEE.GO;
using UnityEditor;
using UnityEngine;

namespace SEEEditor
{
    /// <summary>
    /// SEE menu entry to visualize the normal vectors of a mesh.
    /// </summary>
    internal class ShowNormalVectors
    {
        /// <summary>
        /// Color of the normal vector.
        /// </summary>
        private static Color color = Color.yellow;
        /// <summary>
        /// Length of the normal vector.
        /// </summary>
        private static readonly float normalsLength = 0.2f;

        /// <summary>
        /// Shows the normal vectors of the selected game object as lines.
        /// </summary>
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

                GameObject parent = new("Normal Vectors of " + gameObject.FullName());
                for (int i = 0; i < vertices.Length; i++)
                {
                    // Convert local vertex positions and normals to world space
                    Vector3 worldPos = gameObject.transform.TransformPoint(vertices[i]);
                    Vector3 worldNormal = gameObject.transform.TransformDirection(normals[i]);

                    Draw(parent, worldPos, worldNormal * normalsLength);
                }
            }
        }

        /// <summary>
        /// Draws a line from <paramref name="from"/> to
        /// <paramref name="from"/> + <paramref name="direction"/> with given
        /// <see cref="color"/> and <see cref="normalsLength"/>.
        /// </summary>
        /// <param name="parent">The parent under which the lines will be added as children.</param>
        /// <param name="from">Origin of the line.</param>
        /// <param name="direction">Direction of the line.</param>
        private static void Draw(GameObject parent, Vector3 from, Vector3 direction)
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
            line.startColor = color;
            line.endColor = color;

            // A line needs at least 2 points (index 0 is start, index 1 is end)
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, from + direction); // Calculates the destination point

            gameObject.transform.SetParent(parent.transform);
        }
    }
}

#endif
