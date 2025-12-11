using System;
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
        /// The name of the shader property that holds the portal information.
        /// </summary>
        private const string portalPropertyName = "_Portal";

        /// <summary>
        /// Cached property index for the <c>_Portal</c> shader property.
        /// </summary>
        private static readonly int portalProp = Shader.PropertyToID(portalPropertyName);

        /// <summary>
        /// Sets the culling area (portal) of all children of <paramref name="parent"/> to the
        /// complete area of <paramref name="parent"/>.
        /// <para>
        /// Precondition: <paramref name="parent"/> must have a plane component
        /// attached to it.
        /// </para>
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
        /// Sets the portal of <paramref name="to"/> to the portal of <paramref name="from"/>.
        /// <para>
        /// Postcondition: The portals of <paramref name="from"/> and <paramref name="to"/>
        /// are equal and <paramref name="from"/> has its original portal.
        /// </para>
        /// </summary>
        /// <param name="from">the game object from which to retrieve the portal</param>
        /// <param name="to">the game object receiving the portal of <paramref name="from"/></param>
        public static void InheritPortal(GameObject from, GameObject to)
        {
            GetPortal(from, out Vector2 leftFront, out Vector2 rightBack);
            SetPortal(to, leftFront, rightBack);
        }

        /// <summary>
        /// Returns the portal of <paramref name="gameObject"/>, a rectangle in the x/z plane
        /// with <paramref name="leftFront"/> corner and <paramref name="rightBack"/> corner.
        /// <para>
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="Renderer"/>.
        /// If there is no renderer, <paramref name="leftFront"/> and <paramref name="rightBack"/>
        /// are both <see cref="Vector2.zero"/>.
        /// </para>
        /// </summary>
        /// <param name="gameObject">game objects whose portal is requested</param>
        /// <param name="leftFront">the left front corner of the rectangular portal</param>
        /// <param name="rightBack">the right back corner of the rectangular portal</param>
        /// <returns><c>true</c> iff a portal is found.</returns>
        public static bool GetPortal(GameObject gameObject, out Vector2 leftFront, out Vector2 rightBack)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return GetPortal(renderer.sharedMaterial, out leftFront, out rightBack);
            }

            leftFront = Vector2.zero;
            rightBack = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Returns the portal of <paramref name="material"/>, a rectangle on the x/z plane,
        /// defined by its <paramref name="leftFront"/> and <paramref name="rightBack"/> corners.
        /// <para>
        /// The values will be zero if the material does not have a portal.
        /// </para>
        /// </summary>
        /// <param name="material">the material from which the portal coordinates should be extracted</param>
        /// <param name="leftFront">the left front corner of the rectangular portal</param>
        /// <param name="rightBack">the right back corner of the rectangular portal</param>
        /// <returns><c>true</c> iff the material has a portal.</returns>
        private static bool GetPortal(Material material, out Vector2 leftFront, out Vector2 rightBack)
        {
            if (HasPortal(material))
            {
                // The _Portal property contains both the min and the max position of the portal plane
                // that spans over Unity's XZ plane: (x_min, z_min, x_max, z_max)
                Vector4 portal = material.GetVector(portalProp);
                leftFront.x = portal.x;
                leftFront.y = portal.y;
                rightBack.x = portal.z;
                rightBack.y = portal.w;
                return true;
            }
            leftFront = Vector2.zero;
            rightBack = Vector2.zero;
            return false;
        }

        /// <summary>
        /// True iff the shader of <paramref name="material"/> has a property named <see cref="portalPropertyName"/>.
        /// </summary>
        /// <param name="material">Material whose shader should be checked.</param>
        /// <returns>True iff the shader of <paramref name="material"/> has a property named <see cref="portalPropertyName"/>.</returns>
        /// <remarks>A material itself may have this property but that is not relevant
        /// if its shader does not actually use it.</remarks>
        private static bool HasPortal(Material material)
        {
            Shader shader = material.shader;
            if (shader == null)
            {
                Debug.LogError($"Material {material.name} has no shader.\n");
                return false;
            }
            return shader.FindPropertyIndex(portalPropertyName) != -1;
        }

        /// <summary>
        /// Yields the <paramref name="leftFrontCorner"/> and <paramref name="rightBackCorner"/>
        /// of the plane attached to <paramref name="gameObject"/>.
        /// <para>
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="GO.Plane"/> component
        /// attached to it.
        /// </para>
        /// </summary>
        /// <param name="gameObject">the game object whose plane dimensions are to be retrieved</param>
        /// <param name="leftFrontCorner">the left front corner in the X/Z plane of the plane component</param>
        /// <param name="rightBackCorner">the right back corner in the X/Z plane of the plane component</param>
        public static void GetDimensions(GameObject gameObject, out Vector2 leftFrontCorner, out Vector2 rightBackCorner)
        {
            if (gameObject.TryGetComponent(out GO.Plane cullingPlane))
            {
                // Apply a minimal offset to slightly expand the bounds.
                // Without this, floating-point precision issues can cause objects
                // that lie exactly on the portal border to be incorrectly classified
                // as being outside the portal.
                float offset = 0.00001f;
                Vector2 offsetVector = new(offset, offset);
                leftFrontCorner = cullingPlane.LeftFrontCorner - offsetVector;
                rightBackCorner = cullingPlane.RightBackCorner + offsetVector;
            }
            else
            {
                Debug.LogWarning($"Game object {gameObject.name} has no {nameof(GO.Plane)}.\n");
                leftFrontCorner = Vector2.zero;
                rightBackCorner = Vector2.zero;
            }
        }

        /// <summary>
        /// Recursively sets the culling area (portal) of the <paramref name="gameObject"/> and all
        /// its children to the rectangle in the x/z plane defined by the given <paramref name="leftFront"/>
        /// and <paramref name="rightBack"/> corner.
        /// </ara>
        /// N.B. equivalent to SetPortal(gameObject.transform, leftFront, rightBack).
        /// </para>
        /// </summary>
        /// <param name="gameObject">object whose culling area is to be set</param>
        /// <param name="leftFront">left front corner of the culling area</param>
        /// <param name="rightBack">right back corner of the culling area</param>
        public static void SetPortal(GameObject gameObject, Vector2 leftFront, Vector2 rightBack)
        {
            SetPortal(gameObject.transform, leftFront, rightBack);
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
                SetPortal(renderer.sharedMaterial, leftFront, rightBack);
            }
            foreach (Transform child in transform)
            {
                SetPortal(child, leftFront, rightBack);
            }
        }

        /// <summary>
        /// Sets the culling area (portal) of <paramref name="gameObject"/> to the rectangle in
        /// the x/z plane defined by the extents of <paramref name="root"/>.
        /// Depending on <paramref name="includeDescendants"/>, this will also be done for any
        /// descendants of <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="root">object defining the extent of the culling area</param>
        /// <param name="gameObject">object whose culling area is to be set</param>
        /// <param name="includeDescendants">
        /// Whether to also set the portal for descendants of <paramref name="gameObject"/> too
        /// </param>
        public static void SetPortal(GameObject root, GameObject gameObject,
                                     IncludeDescendants includeDescendants = IncludeDescendants.OnlySelf)
        {
            GetDimensions(root, out Vector2 leftFront, out Vector2 rightBack);

            switch (includeDescendants)
            {
                case IncludeDescendants.DirectDescendants:
                    foreach (Transform child in gameObject.transform)
                    {
                        SetPortalOfMaterials(child.gameObject, leftFront, rightBack);
                    }

                    // We also need to set the portal of the gameObject itself
                    goto case IncludeDescendants.OnlySelf;

                case IncludeDescendants.AllDescendants:
                    // We will go through the children of gameObject using pre-order-traversal.
                    static void SetPortalOfMaterialsRecursive(GameObject go, Vector2 leftFront, Vector2 rightBack)
                    {
                        SetPortalOfMaterials(go, leftFront, rightBack);
                        foreach (Transform child in go.transform)
                        {
                            SetPortalOfMaterialsRecursive(child.gameObject, leftFront, rightBack);
                        }
                    }

                    SetPortalOfMaterialsRecursive(gameObject, leftFront, rightBack);
                    break;

                case IncludeDescendants.OnlySelf:
                    SetPortalOfMaterials(gameObject, leftFront, rightBack);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(includeDescendants), includeDescendants,
                                                          "Invalid IncludeDescendants setting in SetPortal.");
            }
        }

        /// <summary>
        /// Option for <see cref="SetPortal"/> which controls whether children of a
        /// game object shall have their portal boundaries set, too.
        /// </summary>
        public enum IncludeDescendants
        {
            /// <summary>
            /// Will only set the portal of this game object.
            /// </summary>
            OnlySelf,

            /// <summary>
            /// Will set the portal of the direct descendants of this game object.
            /// </summary>
            DirectDescendants,

            /// <summary>
            /// Will set the portal of all descendants (recursively) of this game object.
            /// </summary>
            AllDescendants
        }

        /// <summary>
        /// Sets the culling area (portal) of <paramref name="go"/> and all its descendants
        /// to an infinitely large rectangle.
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

        /// <summary>
        /// If <paramref name="go"/> has no <see cref="Renderer"/>, nothing happens.
        /// Otherwise the portal of the shared material of each renderer of <paramref name="go"/>
        /// is set to the rectangle defined by <paramref name="leftFrontCorner"/> and <paramref name="rightBackCorner"/>.
        /// </summary>
        /// <param name="go">the game objects whose renderers should receive the new portal information</param>
        /// <param name="leftFrontCorner">left front corner of the portal</param>
        /// <param name="rightBackCorner">right back corner of the portal</param>
        private static void SetPortalOfMaterials(GameObject go, Vector2 leftFrontCorner, Vector2 rightBackCorner)
        {
            Renderer[] renderers = go.GetComponents<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    SetPortal(material, leftFrontCorner, rightBackCorner);
                }
            }
        }

        /// <summary>
        /// Sets <c>_Portal</c> property of <paramref name="material"/> to the XZ rectangle defined by
        /// <paramref name="leftFrontCorner"/> and <paramref name="rightBackCorner"/>.
        /// </summary>
        /// <param name="material">the material whose portal is to be set; must not be null</param>
        /// <param name="leftFrontCorner">left front corner of the portal</param>
        /// <param name="rightBackCorner">right back corner of the portal</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="material"/> is null.</exception>
        private static void SetPortal(Material material, Vector2 leftFrontCorner, Vector2 rightBackCorner)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }
            material.SetVector(portalProp, new Vector4(leftFrontCorner.x, leftFrontCorner.y, rightBackCorner.x, rightBackCorner.y));
        }
    }
}
