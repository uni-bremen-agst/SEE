using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using SEE.GO;
using SEE.Utils;

namespace SEE.Game.UI3D
{
    internal static class UI3DProperties
    {
        public const float DefaultAlpha = 0.5f;
        public static readonly Color DefaultColor = new Color(1.0f, 0.25f, 0.0f, DefaultAlpha);
        public static readonly Color DefaultColorSecondary = new Color(1.0f, 0.75f, 0.0f, DefaultAlpha);
        public static readonly Color DefaultColorTertiary = new Color(1.0f, 0.0f, 0.5f, DefaultAlpha);
    }

    internal class Cursor3D : MonoBehaviour
    {
        private const string OutlineShaderName = "Unlit/CursorOutlineShader";
        private const string PlainColorShaderName = "Unlit/PlainColorShader";

        /// <summary>
        /// The list of Transforms currently in the focus.
        /// </summary>
        private List<Transform> focusses;

        private GameObject outline;
        private Material outlineMaterial;

        private float axisHalfLength;
        private Material axisMaterial;

        private bool hasRunThisFrame;

        private Cursor3D()
        {
        }

        /// <summary>
        /// Removes every Transform from <see cref="focusses"/> that has been
        /// destroyed, i.e., for which == null holds (Unity has redefined operator ==).
        /// </summary>
        private void RemoveDestroyedTransforms()
        {
            for (int i = focusses.Count - 1; i >= 0; i--)
            {
                if (focusses[i] == null)
                {
                    focusses[i] = focusses[focusses.Count - 1];
                    focusses.RemoveAt(focusses.Count - 1);
                }
            }
        }

        internal static Cursor3D Create()
        {
            GameObject go = new GameObject("Cursor");
            Cursor3D c = go.AddComponent<Cursor3D>();

            c.focusses = new List<Transform>();

            c.outline = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(c.outline.GetComponent<MeshCollider>());
            c.outline.transform.parent = go.transform;
            c.outline.transform.localPosition = Vector3.zero;
            c.outline.transform.localScale = Vector3.one;
            c.outlineMaterial = new Material(Shader.Find(OutlineShaderName));
            c.outlineMaterial.SetTexture("_MainTex", TextureGenerator.CreateCircleOutlineTextureR8(32, 31, 1.0f, 0.0f));
            c.outlineMaterial.SetColor("_Color", UI3DProperties.DefaultColor);
            c.outline.GetComponent<MeshRenderer>().sharedMaterial = c.outlineMaterial;

            c.axisMaterial = new Material(Shader.Find(PlainColorShaderName));
            c.axisMaterial.SetColor("_Color", Color.black);

            c.hasRunThisFrame = false;

            c.gameObject.SetActive(false);

            return c;
        }

        private void Update()
        {
            hasRunThisFrame = false;
            RemoveDestroyedTransforms();
            if (focusses.Count != 0)
            {
                transform.position = GetPosition();
                axisHalfLength = 0.01f * (MainCamera.Camera.transform.position - transform.position).magnitude;
            }
        }

        private void OnRenderObject()
        {
            if (!hasRunThisFrame && HasFocus() && axisHalfLength > 0.0f)
            {
                hasRunThisFrame = true;

                axisMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                {
                    Vector3 c = GetPosition();
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

        public bool HasFocus()
        {
            RemoveDestroyedTransforms();
            return focusses.Count != 0;
        }

        public Transform[] GetFocusses()
        {
            RemoveDestroyedTransforms();
            Transform[] result = new Transform[focusses.Count];
            focusses.CopyTo(result);
            return result;
        }

        public void AddFocus(Transform focus)
        {
            if (focus && !focusses.Contains(focus))
            {
                focusses.Add(focus);
                gameObject.SetActive(true);
            }
        }

        public void RemoveFocus(Transform focus)
        {
            if (focus)
            {
                focusses.Remove(focus);
                gameObject.SetActive(focusses.Count != 0);
            }
        }

        public void ReplaceFocusses(List<Transform> focusses)
        {
            if (focusses != null && focusses.Count != 0)
            {
                this.focusses = focusses;
                gameObject.SetActive(true);
            }
            else
            {
                this.focusses.Clear();
                gameObject.SetActive(false);
            }
        }

        public void ReplaceFocus(Transform focus)
        {
            if (focus != null)
            {
                focusses.Clear();
                focusses.Add(focus);
                gameObject.SetActive(true);
            }
            else
            {
                focusses.Clear();
                gameObject.SetActive(false);
            }
        }

        public Vector3 GetPosition()
        {
            Vector3 result = Vector3.zero;

            RemoveDestroyedTransforms();

            if (focusses.Count == 1)
            {
                result = focusses[0].position;
            }
            else
            {
                GetMostDistantFocussesXZ(out Transform _, out Transform _, out float _, out float _, out float _, out result);
            }

            return result;
        }

        public float GetDiameterXZ()
        {
            float result = 0.0f;

            RemoveDestroyedTransforms();

            if (focusses.Count == 1)
            {
                result = focusses[0].lossyScale.x;
            }
            else
            {
                GetMostDistantFocussesXZ(out Transform _, out Transform _, out float _, out float _, out result, out Vector3 _);
            }

            return result;
        }

        private void GetMostDistantFocussesXZ(
            out Transform focus0, out Transform focus1,
            out float radius0, out float radius1,
            out float diameter, out Vector3 center)
        {
            Assert.IsTrue(focusses.Count >= 2);

            focus0 = null;
            focus1 = null;
            radius0 = 0.0f;
            radius1 = 0.0f;
            diameter = 0.0f;
            center = Vector3.zero;

            for (int i = 0; i < focusses.Count - 1; i++)
            {
                for (int j = i + 1; j < focusses.Count; j++)
                {
                    // TODO(torben): it is assumed that the x and z scale are
                    // identical and x or z are the diameter of the circle, which is
                    // obviously not true for rectangular layouts!

                    Transform foc0 = focusses[i];
                    Transform foc1 = focusses[j];
                    float rad0 = 0.5f * foc0.lossyScale.x;
                    float rad1 = 0.5f * foc1.lossyScale.x;
                    float d01 = (foc1.position.XZ() - foc0.position.XZ()).magnitude;

                    if (rad0 >= d01 + rad1)
                    {
                        float dia = 2.0f * rad0;
                        if (dia > diameter)
                        {
                            focus0 = foc0;
                            radius0 = rad0;
                            diameter = dia;
                            center = foc0.position;
                        }
                    }
                    else if (rad1 >= d01 + rad0)
                    {
                        float dia = 2.0f * rad1;
                        if (dia > diameter)
                        {
                            focus0 = foc1;
                            radius0 = rad1;
                            diameter = dia;
                            center = foc1.position;
                        }
                    }
                    else
                    {
                        float dia = rad0 + rad1 + d01;
                        if (dia > diameter)
                        {
                            focus0 = foc0;
                            focus1 = foc1;
                            radius0 = rad0;
                            radius1 = rad1;
                            diameter = dia;
                            Vector3 v01 = foc1.position - foc0.position;
                            Vector3 v01n = v01.normalized;
                            Vector3 p0 = focus0.position - v01n * rad0;
                            Vector3 p1 = focus1.position + v01n * rad1;
                            center = 0.5f * (p0 + p1);
                        }
                    }
                }
            }
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

    public class RotateGizmo : MonoBehaviour
    {
        private const string PivotOutlineShaderName = "Unlit/RotationGizmoShader";
        private const float Alpha = 0.5f;

        public Vector3 Center { get => transform.position; set => transform.position = value; }
        public float Radius { get => transform.localScale.x; set => transform.localScale = new Vector3(value, value, value); }

        private Material material;
        private float minAngle;
        private float maxAngle;

        internal static RotateGizmo Create(GO.Plane cullingPlane, int textureResolution)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<MeshCollider>());
            go.name = "RotatePivot";
            go.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            go.transform.position = cullingPlane.CenterTop;

            RotateGizmo p = go.AddComponent<RotateGizmo>();

            int outer = textureResolution / 2;
            int inner = Mathf.RoundToInt(outer * 0.98f);

            p.material = new Material(Shader.Find(PivotOutlineShaderName));
            p.material.SetTexture("_MainTex", TextureGenerator.CreateCircleOutlineTextureR8(outer, inner, Alpha, 0.0f));
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
