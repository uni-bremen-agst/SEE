﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

        private List<Transform> focusses;

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

            c.focusses = new List<Transform>();

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
            if (focusses.Count != 0)
            {
                transform.position = GetPosition();
                axisHalfLength = 0.01f * (Camera.main.transform.position - transform.position).magnitude;
            }
        }

        private void OnRenderObject()
        {
            if (focusses.Count != 0)
            {
                if (axisHalfLength > 0.0f)
                {
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
        }

        public bool HasFocus()
        {
            return focusses.Count != 0;
        }

        public Transform[] GetFocusses()
        {
            Transform[] result = new Transform[focusses.Count];
            focusses.CopyTo(result);
            return result;
        }
        
        public void AddFocus(Transform focus)
        {
            if (focus)
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

            if (focusses.Count == 1)
            {
                result = focusses[0].position;
            }
            else
            {
                GetMostDistantFocussesXZ(out Transform focus0, out Transform focus1, out float radius0, out float radius1, out float diameter);
                Vector3 p0 = focus0.position;
                Vector3 p1 = focus1.position;
                Vector3 v01 = focus1.position - focus0.position;
                if (v01.sqrMagnitude != 0.0f)
                {
                    v01.Normalize();
                }
                p0 -= v01 * radius0;
                p1 += v01 * radius1;
                result = 0.5f * (p0 + p1);
            }

            return result;
        }

        public float GetDiameterXZ()
        {
            float result = 0.0f;

            if (focusses.Count == 1)
            {
                result = focusses[0].lossyScale.x;
            }
            else
            {
                GetMostDistantFocussesXZ(out Transform _, out Transform _, out float _, out float _, out result);
            }

            return result;
        }

        private void GetMostDistantFocussesXZ(out Transform focus0, out Transform focus1, out float radius0, out float radius1, out float diameter)
        {
            Assert.IsTrue(focusses.Count >= 2);

            focus0 = null;
            focus1 = null;
            radius0 = 0.0f;
            radius1 = 0.0f;
            diameter = 0.0f;

            for (int i = 0; i < focusses.Count; i++)
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
                    float dia = rad0 + rad1 + (foc1.position.XZ() - foc0.position.XZ()).magnitude;

                    if (dia > diameter)
                    {
                        focus0 = foc0;
                        focus1 = foc1;
                        radius0 = rad0;
                        radius1 = rad1;
                        diameter = dia;
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
