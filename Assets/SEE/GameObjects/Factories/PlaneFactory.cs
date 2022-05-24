using System;
using SEE.DataModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace SEE.GO
{
    /// <summary>
    /// A factory for planes where blocks can be put on.
    /// </summary>
    internal static class PlaneFactory
    {
        private static readonly int SpecularHighlights = Shader.PropertyToID("_SpecularHighlights");
        private const float planeMeshFactor = 10.0f;

        /// <summary>
        /// Returns a newly created plane at centerPosition with given color, width, depth, and height.
        /// </summary>
        /// <param name="shader">the shader to draw the plane</param>
        /// <param name="centerPosition">center position of the plane</param>
        /// <param name="color">color of the plane</param>
        /// <param name="width">width of the plane (x axis)</param>
        /// <param name="depth">depth of the plane (z axis)</param>
        /// <param name="height">height of the plane (y axis)</param>
        /// <returns>new plane</returns>
        public static GameObject NewPlane(Materials.ShaderType shaderType, Vector3 centerPosition, Color color,
                                          float width, float depth, float height = 1.0f)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.tag = Tags.Decoration;
            plane.transform.position = centerPosition;

            Renderer planeRenderer = plane.GetComponent<Renderer>();
            planeRenderer.sharedMaterial = Materials.New(shaderType, color, renderQueueOffset: Materials.RenderEarlierOffset(shaderType));

            // Neither casting nor receiving shadows.
            planeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            planeRenderer.receiveShadows = false;

            // Turn off reflection of plane
            planeRenderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            planeRenderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            planeRenderer.sharedMaterial.SetFloat(SpecularHighlights, 0.0f);
            // To turn reflection on again, use (_SPECULARHIGHLIGHTS_OFF and _GLOSSYREFLECTIONS_OFF
            // work as toggle, there is no _SPECULARHIGHLIGHTS_ON and _GLOSSYREFLECTIONS_ON):
            //planeRenderer.sharedMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            //planeRenderer.sharedMaterial.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            //planeRenderer.sharedMaterial.SetFloat("_SpecularHighlights", 1.0f);

            // A plane is a flat square with edges ten units long oriented in the XZ plane of the local
            // coordinate space. Thus, the mesh of a plane is 10 times larger than its scale factors for X and Y.
            // When we want a plane to have width 12 units, we need to divide the scale for the width
            // by 1.2.
            Vector3 planeScale = new Vector3(width, height * planeMeshFactor, depth) / planeMeshFactor;
            plane.transform.localScale = planeScale;
            return plane;
        }

        /// <summary>
        /// Returns a newly created plane with the two corners leftFrontCorner = (x0, z0)
        /// and rightBackCorner = (x1, z1).
        ///
        /// with given color, width, depth, and height.
        /// Draws a plane in rectangle with  at ground level.
        ///
        /// Preconditions: x0 < x1 and z0 < z1 (Exception is thrown otherwise)
        /// </summary>
        /// <param name="shader">the shader to draw the plane</param>
        /// <param name="leftFrontCorner">2D co-ordinate of the left front corner</param>
        /// <param name="rightBackCorner">2D co-ordinate of the right back corner</param>
        /// <param name="groundLevel">y co-ordinate for ground level of the plane;
        ///    defines the lower end along the y axis</param>
        /// <param name="color">color of the plane</param>
        /// <param name="height">height (thickness) of the plane</param>
        public static GameObject NewPlane(Materials.ShaderType shaderType, Vector2 leftFrontCorner,
                                          Vector2 rightBackCorner, float groundLevel, Color color,
                                          float height = 2 * float.Epsilon)
        {
            float width = Distance(leftFrontCorner.x, rightBackCorner.x);
            float depth = Distance(leftFrontCorner.y, rightBackCorner.y);

            Vector3 centerPosition = new Vector3(leftFrontCorner.x + width / 2.0f, groundLevel + height / 2.0f, leftFrontCorner.y + depth / 2.0f);
            return NewPlane(shaderType, centerPosition, color, width, depth, height);
        }

        /// <summary>
        /// Returns the distance from v0 to v1.
        ///
        /// Precondition: v1 >= v0 (otherwise an Exception is thrown)
        /// </summary>
        /// <param name="v0">start position</param>
        /// <param name="v1">end position</param>
        /// <returns>distance from v0 to v1</returns>
        private static float Distance(float v0, float v1)
        {
            if (v1 < v0)
            {
                Debug.AssertFormat(v1 >= v0, "v1 >= v0 expected. Actual v0 = {0}, v1 = {1}.\n", v0, v1);
                throw new Exception("v1 >= v0 expected");
            }
            else
            {
                if (v0 < 0.0f)
                {
                    return v1 + Math.Abs(v0);
                }
                else
                {
                    return v1 - v0;
                }
            }
        }

        /// <summary>
        /// Adjusts the given <paramref name="plane"/> in X-Z space where <paramref name="leftFrontCorner"/>
        /// defines the left front corner and <paramref name="rightBackCorner"/> is the right back corner.
        /// The height and y co-ordinate of <paramref name="plane"/> will be maintained.
        ///
        /// Precondition: <paramref name="plane"/> is a plane game object.
        /// </summary>
        /// <param name="plane">a plane game object to be adjusted</param>
        /// <param name="leftFrontCorner">new left front corner of the plane</param>
        /// <param name="rightBackCorner">new right back corner of the plane</param>
        internal static void AdjustXZ(GameObject plane, Vector2 leftFrontCorner, Vector2 rightBackCorner)
        {
            GetTransform(plane, leftFrontCorner, rightBackCorner, out Vector3 centerPosition, out Vector3 planeScale);
            plane.transform.position = centerPosition;
            plane.transform.localScale = planeScale;
        }

        /// <summary>
        /// Determines the new <paramref name="centerPosition"/> and <paramref name="scale"/> for the given
        /// <paramref name="plane"/> so that <paramref name="leftFrontCorner"/> would be the left front
        /// corner and <paramref name="rightBackCorner"/> would be the right back corner in the X-Z plane
        /// and the y co-ordinate and the height of <paramref name="plane"/> would remain the same.
        ///
        /// Precondition: <paramref name="plane"/> is a plane game object.
        /// </summary>
        /// <param name="plane">a plane game object to be adjusted</param>
        /// <param name="leftFrontCorner">new left front corner of the plane</param>
        /// <param name="rightBackCorner">new right back corner of the plane</param>
        /// <param name="centerPosition">the new center of the plane</param>
        /// <param name="scale">the new scale of the plane</param>
        internal static void GetTransform(GameObject plane, Vector2 leftFrontCorner, Vector2 rightBackCorner, out Vector3 centerPosition, out Vector3 scale)
        {
            float width = Distance(leftFrontCorner.x, rightBackCorner.x);
            float depth = Distance(leftFrontCorner.y, rightBackCorner.y);
            // We will maintain the height and ground level of the plane.
            float height = plane.transform.localScale.y;
            float groundLevel = plane.transform.position.y;

            centerPosition = new Vector3(leftFrontCorner.x + width / 2.0f, groundLevel, leftFrontCorner.y + depth / 2.0f);

            // A plane is a flat square with edges ten units long oriented in the XZ plane of the local
            // coordinate space. Thus, the mesh of a plane is 10 times larger than its scale factors for X and Y.
            // When we want a plane to have width 12 units, we need to divide the scale for the width
            // by 1.2.
            scale = new Vector3(width, height * planeMeshFactor, depth) / planeMeshFactor;
        }
    }
}
