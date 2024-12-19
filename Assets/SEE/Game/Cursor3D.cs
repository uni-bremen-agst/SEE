using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using SEE.GO;
using SEE.Utils;
using SEE.Controls;

namespace SEE.UI3D
{
    /// <summary>
    /// The cursor representing the center of the selected elements of a city visually. May be used
    /// as e.g. center of rotation.
    /// </summary>
    internal class Cursor3D : MonoBehaviour
    {
        /// <summary>
        /// The name of the shader used for the circular outline of the cursor.
        /// </summary>
        private const string outlineShaderName = "Unlit/CursorOutlineShader";

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
        /// The cached shader property ID for the cursor's main texture.
        /// </summary>
        private static readonly int mainTexProperty = Shader.PropertyToID("_MainTex");

        /// <summary>
        /// The cached shader property ID for the cursor's color.
        /// </summary>
        private static readonly int colorProperty = Shader.PropertyToID("_Color");

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
                    focusses[i] = focusses[^1];
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
            GameObject go = new("Cursor: " + cityName);
#else
        internal static Cursor3D Create()
        {
            GameObject go = new GameObject();
#endif
            Cursor3D c = go.AddComponent<Cursor3D>();

            c.focusses = new List<InteractableObject>();

            GameObject outlineGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroyer.Destroy(outlineGameObject.GetComponent<MeshCollider>());
            outlineGameObject.transform.parent = go.transform;
            outlineGameObject.transform.localPosition = Vector3.zero;
            outlineGameObject.transform.localScale = Vector3.one;
            Material outlineMaterial = new(Shader.Find(outlineShaderName));
            outlineMaterial.SetTexture(mainTexProperty, TextureGenerator.CreateCircleOutlineTextureR8(32, 31, 1.0f, 0.0f));
            outlineMaterial.SetColor(colorProperty, UI3DProperties.DefaultColor);
            outlineGameObject.GetComponent<MeshRenderer>().sharedMaterial = outlineMaterial;

            c.axisMaterial = new Material(Shader.Find(UI3DProperties.PlainColorShaderName));
            c.axisMaterial.SetColor(colorProperty, Color.black);

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
}
