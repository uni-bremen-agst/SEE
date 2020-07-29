using UnityEngine;

namespace SEE.Controls
{
    internal class Cursor : MonoBehaviour
    {
        private const string OutlineShaderName = "Unlit/CursorOutlineShader";
        private const string AxisShaderName = "Unlit/CursorAxisShader";

        private Transform focus;
        public Transform Focus { get => focus; set { if (value != null) { focus = value; } } }

        private GameObject outline;
        private Material outlineMaterial;

        private float axisHalfLength;
        private Material axisMaterial;

        private Cursor()
        {
        }

        private void Update()
        {
            if (Focus == null)
            {
                Focus = Table.Instance.transform;
            }
            transform.position = Focus.position;
            axisHalfLength = 0.01f * (Camera.main.transform.position - transform.position).magnitude;
        }

        private void OnRenderObject()
        {
            if (axisHalfLength > 0.0f)
            {
                Vector3 c = focus.transform.position;
                Vector3 x0 = new Vector3(c.x - axisHalfLength, c.y, c.z);
                Vector3 x1 = new Vector3(c.x + axisHalfLength, c.y, c.z);
                Vector3 y0 = new Vector3(c.x, c.y - axisHalfLength, c.z);
                Vector3 y1 = new Vector3(c.x, c.y + axisHalfLength, c.z);
                Vector3 z0 = new Vector3(c.x, c.y, c.z - axisHalfLength);
                Vector3 z1 = new Vector3(c.x, c.y, c.z + axisHalfLength);

                axisMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                GL.Vertex(x0);
                GL.Vertex(x1);
                GL.Vertex(y0);
                GL.Vertex(y1);
                GL.Vertex(z0);
                GL.Vertex(z1);
                GL.End();
            }
        }

        internal static Cursor Create()
        {
            GameObject go = new GameObject("Cursor");
            Cursor c = go.AddComponent<Cursor>();

            c.outline = GameObject.CreatePrimitive(PrimitiveType.Quad);
            c.outline.transform.parent = go.transform;
            c.outline.transform.localPosition = Vector3.zero;
            c.outline.transform.localScale = Vector3.one;
            c.outlineMaterial = new Material(Shader.Find(OutlineShaderName));
            c.outlineMaterial.SetTexture("_MainTex", Tools.TextureGenerator.CreateCircleOutlineTextureR8(32, 31, 1.0f, 0.0f));
            c.outline.GetComponent<MeshRenderer>().sharedMaterial = c.outlineMaterial;

            c.axisMaterial = new Material(Shader.Find(AxisShaderName));

            return c;
        }
    }
}
