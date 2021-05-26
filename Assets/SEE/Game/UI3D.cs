using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using SEE.GO;
using SEE.Utils;
using SEE.Controls;

namespace SEE.Game.UI3D
{
    /// <summary>
    /// Properties of 3d ui-elements.
    /// </summary>
    internal static class UI3DProperties
    {
        /// <summary>
        /// A plain color shader used for single-colored objects.
        /// </summary>
        internal const string PlainColorShaderName = "Unlit/PlainColorShader";

        /// <summary>
        /// The default alpha value used for various transparent ui-elements.
        /// </summary>
        internal const float DefaultAlpha = 0.5f;

        /// <summary>
        /// The default color of every 3d ui-element.
        /// </summary>
        internal static readonly Color DefaultColor = new Color(1.0f, 0.25f, 0.0f, DefaultAlpha);

        /// <summary>
        /// The secondary default color of every 3d ui-element.
        /// </summary>
        internal static readonly Color DefaultColorSecondary = new Color(1.0f, 0.75f, 0.0f, DefaultAlpha);

        /// <summary>
        /// The tertiary color of every 3d ui-element.
        /// </summary>
        internal static readonly Color DefaultColorTertiary = new Color(1.0f, 0.0f, 0.5f, DefaultAlpha);
    }

    /// <summary>
    /// The cursor representing the center of the selected elements of a city visually. May be used
    /// as e.g. center of rotation.
    /// </summary>
    internal class Cursor3D : MonoBehaviour
    {
        /// <summary>
        /// The name of the shader used for the circular outline of the cursor.
        /// </summary>
        private const string OutlineShaderName = "Unlit/CursorOutlineShader";

        /// <summary>
        /// The focusses of the cursor.
        /// </summary>
        private List<InteractableObject> focusses;

        /// <summary>
        /// The halved length of one axis line.
        /// </summary>
        private float axisHalfLength;

        /// <summary>
        /// The material of the axis used for rendering the axis as lines.
        /// </summary>
        private Material axisMaterial;

        /// <summary>
        /// Removes every <see cref="Transform"/> <c>t</c> from <see cref="focusses"/> that
        /// has been destroyed, i.e., for which <c>t == null</c> holds (Unity has redefined
        /// operator <c>==</c>).
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

        /// <summary>
        /// Creates a new cursor. The city name is only used in debug build.
        /// </summary>
        /// <param name="cityName">The name of the city, this cursor is used for.</param>
        /// <returns></returns>
#if UNITY_EDITOR
        internal static Cursor3D Create(string cityName)
        {
            GameObject go = new GameObject("Cursor: " + cityName);
#else
        internal static Cursor3D Create()
        {
            GameObject go = new GameObject();
#endif
            Cursor3D c = go.AddComponent<Cursor3D>();

            c.focusses = new List<InteractableObject>();

            GameObject outlineGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(outlineGameObject.GetComponent<MeshCollider>());
            outlineGameObject.transform.parent = go.transform;
            outlineGameObject.transform.localPosition = Vector3.zero;
            outlineGameObject.transform.localScale = Vector3.one;
            Material outlineMaterial = new Material(Shader.Find(OutlineShaderName));
            outlineMaterial.SetTexture("_MainTex", TextureGenerator.CreateCircleOutlineTextureR8(32, 31, 1.0f, 0.0f));
            outlineMaterial.SetColor("_Color", UI3DProperties.DefaultColor);
            outlineGameObject.GetComponent<MeshRenderer>().sharedMaterial = outlineMaterial;

            c.axisMaterial = new Material(Shader.Find(UI3DProperties.PlainColorShaderName));
            c.axisMaterial.SetColor("_Color", Color.black);

            c.gameObject.SetActive(false);

            return c;
        }

        private void Update()
        {
            RemoveDestroyedTransforms();
            gameObject.SetActive(focusses.Count != 0);
            if (focusses.Count != 0)
            {
                transform.position = ComputeCenter();
                axisHalfLength = 0.01f * (MainCamera.Camera.transform.position - transform.position).magnitude;
            }
        }

        private void OnRenderObject()
        {
            if (HasFocus() && axisHalfLength > 0.0f)
            {
                axisMaterial.SetPass(0);
                GL.Begin(GL.LINES);
                {
                    Vector3 c = ComputeCenter();
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

        /// <summary>
        /// Whether the cursor has a focus.
        /// </summary>
        /// <returns></returns>
        public bool HasFocus()
        {
            RemoveDestroyedTransforms();
            return focusses.Count != 0;
        }

        /// <summary>
        /// Returns the current focusses as an array.
        /// </summary>
        /// <returns>The current focusses as an array.</returns>
        public IEnumerable<InteractableObject> GetFocusses()
        {
            RemoveDestroyedTransforms();
            return focusses;
        }

        /// <summary>
        /// Adds a focus to the focussed objects of the cursor.
        /// </summary>
        /// <param name="focus">The new additional focus.</param>
        public void AddFocus(InteractableObject focus)
        {
            if (focus && !focusses.Contains(focus))
            {
                focusses.Add(focus);
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Removed a focus from the focussed objects of the cursor.
        /// </summary>
        /// <param name="focus">The focus to be removed.</param>
        public void RemoveFocus(InteractableObject focus)
        {
            if (focus)
            {
                focusses.Remove(focus);
                gameObject.SetActive(focusses.Count != 0);
            }
        }

        /// <summary>
        /// Replaces the focusses by given focusses.
        /// </summary>
        /// <param name="focusses">The new focusses.</param>
        public void ReplaceFocusses(List<InteractableObject> focusses)
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

        /// <summary>
        /// Replaces the focus by given object.
        /// </summary>
        /// <param name="focus">The new focus.</param>
        public void ReplaceFocus(InteractableObject focus)
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

        /// <summary>
        /// Computes and returns the current center of the cursor.
        /// </summary>
        /// <returns>The center of the cursor.</returns>
        public Vector3 ComputeCenter()
        {
            RemoveDestroyedTransforms();

            Vector3 result;
            if (focusses.Count == 1)
            {
                result = focusses[0].transform.position;
            }
            else
            {
                Assert.IsTrue(focusses.Count >= 2);
                GetFarthestSpacedOutFocussesXZ(out Transform _, out Transform _, out float _, out float _, out float _, out result);
            }

            return result;
        }

        /// <summary>
        /// Computes and returns the distance between the two farthest spaced out focusses' outer
        /// edges on the XZ-plane.
        /// </summary>
        /// <returns>The distance between the two farthest spaced out focusses' outer edges on the
        /// XZ-plane.</returns>
        public float ComputeDiameterXZ()
        {
            RemoveDestroyedTransforms();

            float result;
            if (focusses.Count == 1)
            {
                result = focusses[0].transform.lossyScale.x;
            }
            else
            {
                GetFarthestSpacedOutFocussesXZ(out _, out _, out _, out _, out result, out _);
            }

            return result;
        }

        /// <summary>
        /// Computes the farthest spaced out focusses and outputs some properties.
        /// </summary>
        /// <param name="focus0">The 1st found focus.</param>
        /// <param name="focus1">The 2nd found focus.</param>
        /// <param name="radius0">The radius of the 1st found focus.</param>
        /// <param name="radius1">The radius of the 2nd found focus.</param>
        /// <param name="diameter">The distance between the two focusses' outer edges.</param>
        /// <param name="center">The center between the two focusses.</param>
        private void GetFarthestSpacedOutFocussesXZ(
            out Transform focus0,
            out Transform focus1,
            out float radius0,
            out float radius1,
            out float diameter,
            out Vector3 center)
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
                    // FIXME it is assumed that the x and z scale are identical and x or z are the
                    // diameter of the circle, which is obviously not true for rectangular
                    // layouts!

                    Transform foc0 = focusses[i].transform;
                    Transform foc1 = focusses[j].transform;
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

        /// <summary>
        /// Creates a new move-gizmo.
        /// </summary>
        /// <returns>The gizmo.</returns>
        internal static MoveGizmo Create()
        {
            GameObject go = new GameObject("MovePivot");
            MoveGizmo p = go.AddComponent<MoveGizmo>();

            p.start = Vector3.zero;
            p.end = Vector3.zero;

            Shader shader = Shader.Find(UI3DProperties.PlainColorShaderName);
            p.fillRectangleMaterial = new Material(shader);
            p.outlineRectangleMaterial = new Material(shader);
            p.directLineMaterial = new Material(shader);
            p.fillRectangleMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.2f * UI3DProperties.DefaultAlpha));
            p.outlineRectangleMaterial.SetColor("_Color", new Color(0.0f, 0.0f, 0.0f, 0.5f * UI3DProperties.DefaultAlpha));
            p.directLineMaterial.SetColor("_Color", UI3DProperties.DefaultColor);

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

    /// <summary>
    /// This gizmo visually represents a rotation of an object.
    /// </summary>
    public class RotateGizmo : MonoBehaviour
    {
        /// <summary>
        /// The name of the material responsible for rendering the circular rotation visualization.
        /// </summary>
        private const string RotateGizmoShaderName = "Unlit/RotateGizmoShader";

        /// <summary>
        /// The center of the rotation.
        /// </summary>
        public Vector3 Center { get => transform.position; set => transform.position = value; }

        /// <summary>
        /// The radius of the gizmo.
        /// </summary>
        public float Radius { get => transform.localScale.x; set => transform.localScale = new Vector3(value, value, value); }

        /// <summary>
        /// The material responsible for rendering the circular rotation visualization.
        /// </summary>
        private Material material;

        /// <summary>
        /// <see cref="StartAngle"/>
        /// </summary>
        private float startAngle;

        /// <summary>
        /// The start angle of the rotation.
        /// </summary>
        internal float StartAngle
        {
            get => startAngle;
            set
            {
                startAngle = value;
                material.SetFloat("_MinAngle", value);
            }
        }

        /// <summary>
        /// <see cref="TargetAngle"/>
        /// </summary>
        private float targetAngle;

        /// <summary>
        /// The target angle of the rotation.
        /// </summary>
        internal float TargetAngle
        {
            get => targetAngle;
            set
            {
                targetAngle = value;
                material.SetFloat("_MaxAngle", value);
            }
        }

        /// <summary>
        /// Creates a rotation gizmo.
        /// </summary>
        /// <param name="textureResolution">The resolution of the outline texture.</param>
        /// <returns>The gizmo.</returns>
        internal static RotateGizmo Create(int textureResolution)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<MeshCollider>());
            go.name = "RotatePivot";
            go.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            go.transform.position = Vector3.zero;

            RotateGizmo p = go.AddComponent<RotateGizmo>();

            int outer = textureResolution / 2;
            int inner = Mathf.RoundToInt(outer * 0.98f);

            p.material = new Material(Shader.Find(RotateGizmoShaderName));
            p.material.SetTexture("_MainTex", TextureGenerator.CreateCircleOutlineTextureR8(outer, inner, UI3DProperties.DefaultAlpha, 0.0f));
            p.material.SetColor("_Color", UI3DProperties.DefaultColor);
            p.material.SetFloat("_Alpha", UI3DProperties.DefaultAlpha);

            go.GetComponent<MeshRenderer>().sharedMaterial = p.material;
            go.SetActive(false);

            return p;
        }
    }
}
