using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Manages the portal information of shaders. A portal is a plane in X/Z
    /// in which game objects can be drawn. If those game objects leave the
    /// portal, they will be culled.
    /// </summary>
    public static class Portal
    {
        /// <summary>
        /// Sets the culling area (portal) of all children of <paramref name="parent"/> to the
        /// complete area of <paramref name="parent"/>.
        /// 
        /// Precondition: <paramref name="parent"/> must have a plane component
        /// attached to it.
        /// </summary>
        /// <param name="parent">game objects whose children should be culled if they leave 
        /// the area of the <paramref name="parent"/></param>
        public static void SetPortal(GameObject parent)
        {
            Vector2 leftFront, rightBack;
            GetDimensions(parent, out leftFront, out rightBack);
            foreach (Transform child in parent.transform)
            {
                SetPortal(child, leftFront, rightBack);
            }
        }

        /// <summary>
        /// Yields the <paramref name="leftFrontCorner"/> and <paramref name="rightBackCorner"/> 
        /// of the plane attached to <paramref name="gameObject"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> must have a plane component
        /// attached to it.
        /// </summary>
        /// <param name="gameObject">the game object whose plane dimensions are to be retrieved</param>
        /// <param name="leftFrontCorner">the left front corner in the X/Z plane of the plane component</param>
        /// <param name="rightBackCorner">the right back corner in the X/Z plane of the plane component</param>
        public static void GetDimensions(GameObject gameObject, out Vector2 leftFrontCorner, out Vector2 rightBackCorner)
        {
            Plane cullingPlane = gameObject.GetComponent<Plane>();
            leftFrontCorner = cullingPlane.LeftFrontCorner;
            rightBackCorner = cullingPlane.RightBackCorner;
        }

        /// <summary>
        /// Recursively sets the culling area (portal) of the <paramref name="transform"/> and all
        /// its children to the rectangle in the x/z plane defined by the given <paramref name="leftFront"/>
        /// and <paramref name="rightBack"/> corner.
        /// </summary>
        /// <param name="transform">object whose culling area is to be set</param>
        /// <param name="leftFront">left front corner of the culling area</param>
        /// <param name="rightBack">right back corner of the culling area</param>
        public static void SetPortal(Transform transform, Vector2 leftFront, Vector2 rightBack)
        {
            Material material = transform.GetComponent<Renderer>().sharedMaterial;
            material.SetVector("portalMin", new Vector4(leftFront.x, leftFront.y));
            material.SetVector("portalMax", new Vector4(rightBack.x, rightBack.y));
            foreach (Transform child in transform)
            {
                SetPortal(child, leftFront, rightBack);
            }
        }
    }
}