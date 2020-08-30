using UnityEngine;

namespace SEE.UI3D
{
    internal static class UI3DProperties
    {
        public const float DefaultAlpha = 0.5f;
        public static readonly Color DefaultColor = new Color(1.0f, 0.25f, 0.0f, DefaultAlpha);
        public static readonly Color DefaultColorSecondary = new Color(1.0f, 0.75f, 0.0f, DefaultAlpha);
        public static readonly Color DefaultColorTertiary = new Color(1.0f, 0.0f, 0.5f, DefaultAlpha);
    }

    internal class Cursor : MonoBehaviour
    {
        private const string OutlineShaderName = "Unlit/CursorOutlineShader";
        private const string PlainColorShaderName = "Unlit/PlainColorShader";

        private Transform focus;

        private GameObject outline;
        private Material outlineMaterial;

        private float axisHalfLength;
        private Material axisMaterial;

        private Cursor()
        {
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
            c.outlineMaterial.SetColor("_Color", UI3DProperties.DefaultColor);
            c.outline.GetComponent<MeshRenderer>().sharedMaterial = c.outlineMaterial;

            c.axisMaterial = new Material(Shader.Find(PlainColorShaderName));
            c.axisMaterial.SetColor("_Color", Color.black);

            c.gameObject.SetActive(false);

            return c;
        }

        private void Update()
        {
            if (focus != null)
            {
                transform.position = focus.position;
                axisHalfLength = 0.01f * (Camera.main.transform.position - transform.position).magnitude;
            }
        }

        private void OnRenderObject()
        {
            if (focus)
            {
                if (axisHalfLength > 0.0f)
                {
                    axisMaterial.SetPass(0);
                    GL.Begin(GL.LINES);
                    {
                        Vector3 c = focus.transform.position;
                        GL.Vertex(new Vector3(c.x - axisHalfLength, c.y, c.z));
                        GL.Vertex(new Vector3(c.x + axisHalfLength, c.y, c.z));
                        GL.Vertex(new Vector3(c.x, c.y - axisHalfLength, c.z));
                        GL.Vertex(new Vector3(c.x, c.y + axisHalfLength, c.z));
                        GL.Vertex(new Vector3(c.x, c.y, c.z - axisHalfLength));
                        GL.Vertex(new Vector3(c.x, c.y, c.z + axisHalfLength));
                    }
                    GL.End();
                }
            }
        }

        public Transform GetFocus()
        {
            return focus;
        }

        public void SetFocus(Transform focus)
        {
            this.focus = focus;
            gameObject.SetActive(focus != null);
        }
    }

    internal class MoveGizmo : MonoBehaviour
    {
        private const string PlainColorShaderName = "Unlit/PlainColorShader";

        private float scale;
        private Vector3 start;
        private Vector3 end;

        private Material planeMaterial;
        private Material axisMaterial;
        private Material lineMaterial;

        internal static MoveGizmo Create(float scale)
        {
            GameObject go = new GameObject("MovePivot");
            MoveGizmo p = go.AddComponent<MoveGizmo>();

            p.scale = scale;
            p.start = Vector3.zero;
            p.end = Vector3.zero;

            Shader shader = Shader.Find(PlainColorShaderName);
            p.planeMaterial = new Material(shader);
            p.axisMaterial = new Material(shader);
            p.lineMaterial = new Material(shader);
            p.planeMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.2f * UI3DProperties.DefaultAlpha));
            p.axisMaterial.SetColor("_Color", new Color(0.0f, 0.0f, 0.0f, 0.5f * UI3DProperties.DefaultAlpha));
            p.lineMaterial.SetColor("_Color", UI3DProperties.DefaultColor);

            go.SetActive(false);

            return p;
        }

        private void OnRenderObject()
        {
            planeMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            {
                GL.Vertex(start);
                GL.Vertex(new Vector3(end.x, end.y, start.z));
                GL.Vertex(end);
                GL.Vertex(new Vector3(start.x, end.y, end.z));
            }
            GL.End();

            axisMaterial.SetPass(0);
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

            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            {
                GL.Vertex(start);
                GL.Vertex(end);
            }
            GL.End();
        }

        internal void SetPositions(Vector3 startPoint, Vector3 endPoint)
        {
            start = startPoint;
            end = endPoint;
        }
    }

    internal class RotateGizmo : MonoBehaviour
    {
        private const string PivotOutlineShaderName = "Unlit/RotationGizmoShader";
        private const float Alpha = 0.5f;

        public Vector3 Center { get => transform.position; set => transform.position = value; }
        public float Radius { get => transform.localScale.x; set => transform.localScale = new Vector3(value, value, value); }
        
        private Material material;
        private float minAngle;
        private float maxAngle;

        internal static RotateGizmo Create(Plane cullingPlane, int textureResolution)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "RotatePivot";
            go.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            go.transform.position = cullingPlane.CenterTop;

            RotateGizmo p = go.AddComponent<RotateGizmo>();

            int outer = textureResolution / 2;
            int inner = Mathf.RoundToInt((float)outer * 0.98f);

            p.material = new Material(Shader.Find(PivotOutlineShaderName));
            p.material.SetTexture("_MainTex", Tools.TextureGenerator.CreateCircleOutlineTextureR8(outer, inner, Alpha, 0.0f));
            p.material.SetFloat("_Alpha", Alpha);

            go.GetComponent<MeshRenderer>().sharedMaterial = p.material;
            go.SetActive(false);

            return p;
        }

        internal float GetMinAngle()
        {
            return minAngle;
        }

        internal float GetMaxAngle()
        {
            return maxAngle;
        }
        
        internal void SetMinAngle(float minAngleRadians)
        {
            minAngle = minAngleRadians;
            material.SetFloat("_MinAngle", minAngleRadians);
        }

        internal void SetMaxAngle(float maxAngleRadians)
        {
            maxAngle = maxAngleRadians;
            material.SetFloat("_MaxAngle", maxAngleRadians);
            material.SetColor("_Color", UI3DProperties.DefaultColor);
        }
    }
}
