﻿using System;
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
        /// Cached property index for the _PortalMin shader property (Portal Left Front Corner).
        /// </summary>
        private static readonly int portalMin = Shader.PropertyToID("_PortalMin");

        /// <summary>
        /// Cached property index for the _PortalMax shader property (Portal Right Back Corner).
        /// </summary>
        private static readonly int portalMax = Shader.PropertyToID("_PortalMax");

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
        /// Sets the portal of <paramref name="to"/> to the portal of <paramref name="from"/>.
        ///
        /// Postcondition: The portals of <paramref name="from"/> and <paramref name="to"/>
        /// are equal and <paramref name="from"/> has its original portal.
        /// </summary>
        /// <param name="from">the game object from which to retrieve the portal</param>
        /// <param name="to">the game object receiving the portal of <paramref name="from"/></param>
        public static void InheritPortal(GameObject from, GameObject to)
        {
            GetPortal(from, out Vector2 leftFront, out Vector2 rightBack);
            SetPortal(to, leftFront, rightBack);
        }

        /// <summary>
        /// Returns the portal of <paramref name="gameObject"/> a rectangle in the x/z plane
        /// with <paramref name="leftFront"/> corner and <paramref name="rightBack"/> corner.
        ///
        /// Precondition: <paramref name="gameObject"/> must have a <see cref="Renderer"/>.
        /// If there is no renderer, <paramref name="leftFront"/> and <paramref name="rightBack"/>
        /// are both <see cref="Vector2.zero"/>.
        /// </summary>
        /// <param name="gameObject">game objects whose portal is requested</param>
        /// <param name="leftFront">the left front corner of the rectangular portal</param>
        /// <param name="rightBack">the right back corner of the rectangular portal</param>
        public static void GetPortal(GameObject gameObject, out Vector2 leftFront, out Vector2 rightBack)
        {
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                GetPortal(renderer.sharedMaterial, out leftFront, out rightBack);
            }
            else
            {
                Debug.LogError($"Game object {gameObject.name} does not have a renderer.\n");
                leftFront = Vector2.zero;
                rightBack = Vector2.zero;
            }
        }

        /// <summary>
        /// Returns the portal of <paramref name="material"/> a rectangle in the x/z plane
        /// with <paramref name="leftFront"/> corner and <paramref name="rightBack"/> corner.
        ///
        /// Precondition: <paramref name="material"/> must have a the attributes
        /// <see cref="portalMin"/> and <see cref="portalMax"/>. If they do not
        /// exist, the result is undefined.
        /// </summary>
        /// <param name="gameObject">game objects whose portal is requested</param>
        /// <param name="leftFront">the left front corner of the rectangular portal</param>
        /// <param name="rightBack">the right back corner of the rectangular portal</param>
        private static void GetPortal(Material material, out Vector2 leftFront, out Vector2 rightBack)
        {
            // Although PortalMin and PortalMax are Vector4, only their x and y component
            // is set. The x component is the x axis in Unity space, but the y component
            // is the z axis in Unity space.
            Vector4 portalLeftFrontCorner = material.GetVector(portalMin);
            Vector4 portalRightBackCorner = material.GetVector(portalMax);
            leftFront.x = portalLeftFrontCorner.x;
            leftFront.y = portalLeftFrontCorner.y;
            rightBack.x = portalRightBackCorner.x;
            rightBack.y = portalRightBackCorner.y;
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
                Debug.LogWarning($"Game object {gameObject.name} has no {nameof(GO.Plane)}.\n");
                leftFrontCorner = Vector2.zero;
                rightBackCorner = Vector2.zero;
            }
        }

        /// <summary>
        /// Recursively sets the culling area (portal) of the <paramref name="gameObject"/> and all
        /// its children to the rectangle in the x/z plane defined by the given <paramref name="leftFront"/>
        /// and <paramref name="rightBack"/> corner.
        ///
        /// N.B. equivalent to SetPortal(gameObject.transform, leftFront, rightBack).
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
        /// Sets <see cref="portalMin"/> of <paramref name="material"/> to <paramref name="leftFrontCorner"/>
        /// and <see cref="portalMax"/> of <paramref name="material"/> to <paramref name="rightBackCorner"/>.
        /// </summary>
        /// <param name="material">the material whose portal is to be set</param>
        /// <param name="leftFrontCorner">left front corner of the portal</param>
        /// <param name="rightBackCorner">right back corner of the portal</param>
        private static void SetPortal(Material material, Vector2 leftFrontCorner, Vector2 rightBackCorner)
        {
            material.SetVector(portalMin, new Vector4(leftFrontCorner.x, leftFrontCorner.y));
            material.SetVector(portalMax, new Vector4(rightBackCorner.x, rightBackCorner.y));
        }
    }
}