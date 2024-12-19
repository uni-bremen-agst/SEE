using UnityEngine;

namespace SEE.UI3D
{
    /// <summary>
    /// This gizmo represents the movement of elements of a city visually.
    /// </summary>
    internal class MoveGizmo : MonoBehaviour
    {
        /// <summary>
        /// The start position of the movement visualization.
        /// </summary>
        private Vector3 start;

        /// <summary>
        /// The end position of the movement visualization.
        /// </summary>
        private Vector3 end;

        /// <summary>
        /// The material of the filled rectangle.
        /// </summary>
        private Material fillRectangleMaterial;

        /// <summary>
        /// The material for the outlined rectangle.
        /// </summary>
        private Material outlineRectangleMaterial;

        /// <summary>
        /// The material of the line between the start- and end-position.
        /// </summary>
        private Material directLineMaterial;

        private static readonly int colorProperty = Shader.PropertyToID("_Color");

        /// <summary>
        /// Creates a new move-gizmo.
        /// </summary>
        /// <returns>The gizmo.</returns>
        internal static MoveGizmo Create()
        {
            GameObject go = new("MovePivot");
            MoveGizmo p = go.AddComponent<MoveGizmo>();

            p.start = Vector3.zero;
            p.end = Vector3.zero;

            Shader shader = Shader.Find(UI3DProperties.PlainColorShaderName);
            p.fillRectangleMaterial = new Material(shader);
            p.outlineRectangleMaterial = new Material(shader);
            p.directLineMaterial = new Material(shader);
            p.fillRectangleMaterial.SetColor(colorProperty, new Color(0.5f, 0.5f, 0.5f, 0.2f * UI3DProperties.DefaultAlpha));
            p.outlineRectangleMaterial.SetColor(colorProperty, new Color(0.0f, 0.0f, 0.0f, 0.5f * UI3DProperties.DefaultAlpha));
            p.directLineMaterial.SetColor(colorProperty, UI3DProperties.DefaultColor);

            go.SetActive(false);

            return p;
        }

        private void OnRenderObject()
        {
            fillRectangleMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            {
                GL.Vertex(start);
                GL.Vertex(new Vector3(end.x, end.y, start.z));
                GL.Vertex(end);
                GL.Vertex(new Vector3(start.x, end.y, end.z));
            }
            GL.End();

            outlineRectangleMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            {
                GL.Vertex(start);
                GL.Vertex(new Vector3(start.x, end.y, end.z));
                GL.Vertex(new Vector3(start.x, end.y, end.z));
                GL.Vertex(end);

                GL.Vertex(start);
                GL.Vertex(new Vector3(end.x, end.y, start.z));
                GL.Vertex(new Vector3(end.x, end.y, start.z));
                GL.Vertex(end);
            }
            GL.End();

            directLineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            {
                GL.Vertex(start);
                GL.Vertex(end);
            }
            GL.End();
        }

        /// <summary>
        /// Sets the start- and end-position of the movement visualization.
        /// </summary>
        /// <param name="startPoint">The new start point.</param>
        /// <param name="endPoint">The new end point.</param>
        internal void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            start = startPoint;
            end = endPoint;
        }
    }
}