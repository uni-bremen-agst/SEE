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
        /// Cached property index for the _PortalMin shader property.
        /// </summary>
        private static readonly int PortalMin = Shader.PropertyToID("_PortalMin");
        
        /// <summary>
        /// Cached property index for the _PortalMax shader property.
        /// </summary>
        private static readonly int PortalMax = Shader.PropertyToID("_PortalMax");

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
            GetDimensions(parent, out Vector2 leftFront, out Vector2 rightBack);
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
            if (gameObject.TryGetComponent(out GO.Plane cullingPlane))
            {
                leftFrontCorner = cullingPlane.LeftFrontCorner;
                rightBackCorner = cullingPlane.RightBackCorner;
            }
            else
            {
                Debug.LogWarningFormat("Game object {0} has no GO.Plane.\n", gameObject.name);
                leftFrontCorner = Vector2.zero;
                rightBackCorner = Vector2.zero;
            }
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
            if (transform.TryGetComponent(out Renderer renderer))
            {
                SetPortal(leftFront, rightBack, renderer.sharedMaterial);
            }
            foreach (Transform child in transform)
            {
                SetPortal(child, leftFront, rightBack);
            }
        }

        /// <summary>
        /// Sets the culling area (portal) of <paramref name="gameObject"/> to the rectangle in
        /// the x/z plane defined by the extents of <paramref name="root"/>.
        /// </summary>
        /// <param name="root">object defining the extent of the culling area</param>
        /// <param name="gameObject">object whose culling area is to be set</param>
        public static void SetPortal(GameObject root, GameObject gameObject)
        {
            GetDimensions(root, out Vector2 leftFront, out Vector2 rightBack);
            SetPortalOfMaterials(gameObject, leftFront, rightBack);
        }

        /// <summary>
        /// Sets the culling area (portal) of <paramref name="go"/> and all its descendants
        /// to an infititely large rectangle.
        /// </summary>
        /// <param name="go">object whose culling area is to be set</param>
        public static void SetInfinitePortal(GameObject go)
        {
            SetPortalOfMaterials(go, Vector2.negativeInfinity, Vector2.positiveInfinity);
            foreach (Transform child in go.transform)
            {
                SetInfinitePortal(child.gameObject);
            }
        }

        private static void SetPortalOfMaterials(GameObject go, Vector2 leftFront, Vector2 rightBack)
        {
            if (go.TryGetComponent(out Renderer renderer))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    SetPortal(leftFront, rightBack, material);
                }
            }
        }

        private static void SetPortal(Vector2 leftFrontCorner, Vector2 rightBackCorner, Material material)
        {
            material.SetVector(PortalMin, new Vector4(leftFrontCorner.x, leftFrontCorner.y));
            material.SetVector(PortalMax, new Vector4(rightBackCorner.x, rightBackCorner.y));
        }
        
    }
}