﻿using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using SEE.XR;

namespace SEE.Utils
{
    /// <summary>
    /// What precisely was hit by a ray cast.
    /// </summary>
    public enum HitGraphElement
    {
        None, // Neither a node nor an edge was hit.
        Node, // A node was hit.
        Edge // An edge was hit.
    }

    /// <summary>
    /// Utilities related to ray casting.
    /// </summary>
    public static class Raycasting
    {
        /// <summary>
        /// Number of raycast hits we can store in the buffer for <see cref="RaycastLowestNode"/>.
        /// </summary>
        private const uint raycastBufferSize = 500;

        /// <summary>
        /// Layer number for game objects representing UI components.
        /// </summary>
        private const uint uiLayer = 5;

        /// <summary>
        /// Names of game objects which are on the <see cref="uiLayer"/>,
        /// but won't "count" as UI components, e.g., when checking whether
        /// the user is currently hovering over any UI components.
        /// </summary>
        private static readonly ISet<string> ignoredUINames =
            new HashSet<string>
            {
                "ChatBox" // ignored because otherwise it'd obscure half the screen.
            };

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// The hit will be set, if no GUI element is hit.
        /// <para>
        /// Note: <paramref name="elementRef"/> is null if the result is <see cref="HitGraphElement.None"/>.
        /// Yet, <paramref name="raycastHit"/> will always be the hit object if any was hit,
        /// no matter whether it was a graph element or not.
        /// </para>
        /// </summary>
        /// <param name="raycastHit">The hit object.</param>
        /// <param name="nodeRef">The hit graph element if <code>None</code> is not returned.</param>
        /// <returns>if no GUI element is hit, but a GameObject with either
        /// an attached <see cref="NodeRef"/> or <see cref="EdgeRef"/> is hit, then
        /// <see cref="HitGraphElement.Node"/> or <see cref="HitGraphElement.Edge"/>,
        /// respectively, is returned. Otherwise if a GUI element is hit or if the
        /// hit game object has neither a <see cref="NodeRef"/> nor an <see cref="EdgeRef"/>
        /// attached, <see cref="HitGraphElement.None"/> is returned.
        /// </returns>
        public static HitGraphElement RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef)
        {
            if (((SceneSettings.InputType == PlayerInputType.DesktopPlayer && !IsMouseOverGUI()) || SceneSettings.InputType == PlayerInputType.VRPlayer) && Physics.Raycast(UserPointsTo(), out RaycastHit hit))
            {
                raycastHit = hit;
                if (hit.transform.TryGetComponent(out NodeRef nodeRef))
                {
                    elementRef = nodeRef;
                    return HitGraphElement.Node;
                }
                else if (hit.transform.TryGetComponent(out EdgeRef edgeRef))
                {
                    elementRef = edgeRef;
                    return HitGraphElement.Edge;
                }
                else
                {
                    elementRef = null;
                    return HitGraphElement.None;
                }
            }
            else
            {
                raycastHit = new RaycastHit();
                elementRef = null;
                return HitGraphElement.None;
            }
        }

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// Returns true if the mouse is not over any GUI element and if anything was hit.
        /// <paramref name="raycastHit"/> will be set, if true is returned.
        /// </summary>
        /// <param name="raycastHit">hit object if true is returned, undefined otherwise</param>
        /// <param name="maxDistance">how far the ray cast may reach; anything farther away
        /// cannot be hit</param>
        /// <returns>true if the mouse is not over any GUI element and if anything was hit</returns>
        public static bool RaycastAnything(out RaycastHit raycastHit, float maxDistance = float.PositiveInfinity)
        {
            raycastHit = new RaycastHit();
            Physics.queriesHitBackfaces = true;
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
            {
                return !IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out raycastHit, maxDistance);
            }
            else
            {
                return Physics.Raycast(UserPointsTo(), out raycastHit, maxDistance);
            }
        }

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing and chooses the
        /// node that is the lowest one in the node hierarchy, if considered relevant for a hit.
        /// More precisely, the lowest one is that one with the greatest value of the node attribute
        /// <c>Level</c>, where <c>Level</c> counting starts at the root and increases downward into the tree.
        ///
        /// <para>
        /// If <paramref name="referenceNode"/> equals <c>null</c>, all nodes are considered relevant.
        /// Otherwise a node is considered relevant if it is in the same graph as
        /// <paramref name="referenceNode"/> and neither <paramref name="referenceNode"/> itself nor
        /// any of its descendants in the node hierarchy in the underlying graph.
        /// </para>
        /// </summary>
        /// <param name="raycastHit">hit object of lowest node if true is returned, null otherwise</param>
        /// <param name="hitNode">lowest node if true is returned, null otherwise</param>
        /// <param name="referenceNode">if given, all nodes which are not in the same graph as
        /// <paramref name="referenceNode"/> as well as itself will not be considered when sorting raycast results.
        /// </param>
        /// <returns>true if the mouse was over at least one node fulfilling the criteria; only then
        /// <paramref name="hitNode"/> and <paramref name="raycastHit"/> are defined</returns>
        public static bool RaycastLowestNode(out RaycastHit? raycastHit, out Node hitNode, NodeRef referenceNode = null)
        {
            RaycastHit[] hits = new RaycastHit[raycastBufferSize];
            int numberOfHits = Physics.RaycastNonAlloc(UserPointsTo(), hits);
            if (numberOfHits == raycastBufferSize)
            {
                Debug.LogWarning("We possibly got more hits than buffer space is available.\n");
            }

            raycastHit = null;
            hitNode = null;
            // We are using a loop iteration bounded by numberOfHits. A foreach loop would traverse
            // all elements in hits, where most of them would be null.
            for (int i = 0; i < numberOfHits; i++)
            {
                RaycastHit hit = hits[i];
                // referenceNode will be ignored if set
                if (hit.collider.gameObject == referenceNode.gameObject)
                {
                    continue;
                }

                // Is it a node at all?
                NodeRef hitNodeRef = hit.transform.GetComponent<NodeRef>();
                if (hitNodeRef == null || hitNodeRef.Value == null)
                {
                    continue;
                }

                // Is it in the same graph as the reference node, if present?
                if (referenceNode == null || referenceNode.Value == null || hitNodeRef.Value.ItsGraph != referenceNode.Value.ItsGraph)
                {
                    continue;
                }

                // Have we found a node deeper into the tree than the current hitNode?
                if (hitNode != null && hitNode.Level >= hitNodeRef.Value.Level)
                {
                    continue;
                }

                // Check whether descendants are to be ignored and if so, whether the hit node is a descendant
                if (referenceNode != null && hitNodeRef.Value.IsDescendantOf(referenceNode.Value))
                {
                    continue;
                }

                hitNode = hitNodeRef.Value;
                raycastHit = hit;
            }
            return hitNode != null;
        }

        /// <summary>
        /// Raycasts against <see cref="InteractableObject"/>s and outputs either the closest hit
        /// or <c>null</c>, if no such hit exists.
        /// </summary>
        /// <param name="raycastHit">The raycast hit for the hit interactable object or the default value.</param>
        /// <param name="obj">The hit object or <c>null</c>.</param>
        /// <returns>The corresponding enum value for the hit.</returns>
        public static HitGraphElement RaycastInteractableObject(out RaycastHit raycastHit, out InteractableObject obj)
        {
            HitGraphElement result = HitGraphElement.None;

            static HitGraphElement DetermineHit(InteractableObject element)
            {
                HitGraphElement result = element.GraphElemRef.Elem switch
                {
                    null => HitGraphElement.None,
                    Node => HitGraphElement.Node,
                    Edge => HitGraphElement.Edge,
                    _ => throw new System.ArgumentOutOfRangeException()
                };
                return result;
            }

            raycastHit = new RaycastHit();
            obj = null;
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                if (XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit ray))
                {
                    raycastHit = ray;
                    if (ray.transform.TryGetComponent(out InteractableObject io))
                    {
                        result = DetermineHit(io);
                        obj = io;
                    }
                }
            }
            else
            {
                if (!IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out RaycastHit hit))
                {
                    raycastHit = hit;
                    if (hit.transform.TryGetComponent(out InteractableObject io))
                    {
                        result = DetermineHit(io);
                        obj = io;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Whether the mouse currently hovers over a GUI element.
        ///
        /// Note: If no <see cref="EventSystem"/> exists in the scene, internal calls will fail
        /// and <c>false</c> will be returned.
        /// </summary>
        /// <returns>Whether the mouse currently hovers over a GUI element.</returns>
        public static bool IsMouseOverGUI()
        {
            InputSystemUIInputModule inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (inputModule == null)
            {
                Debug.LogError("Could not find input system UI module! Falling back to old detection for now.\n");
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            else
            {
                Assert.IsNotNull(Mouse.current);
                GameObject lastGameObject = inputModule.GetLastRaycastResult(Mouse.current.deviceId).gameObject;
                return lastGameObject != null && lastGameObject.layer == uiLayer
                                              && !ignoredUINames.Contains(lastGameObject.name);
            }
        }

        /// <summary>
        /// Raycasts against the clipping plane.
        /// </summary>
        /// <param name="clippingPlane">The plane, which defines the clipping area.</param>
        /// <param name="hit">Whether the clipping plane was hit.</param>
        /// <param name="hitInsideClippingArea">Whether the clipping plane was hit inside of its clipping area.</param>
        /// <param name="hitPointOnPlane">The hit position on the plane or <see cref="Vector3.zero"/>, if the plane was not hit.</param>
        public static void RaycastClippingPlane(GO.Plane clippingPlane, out bool hit, out bool hitInsideClippingArea, out Vector3 hitPointOnPlane)
        {
            Ray ray = UserPointsTo();
            UnityEngine.Plane raycastPlane = new(Vector3.up, clippingPlane.transform.position);
            hit = raycastPlane.Raycast(ray, out float enter);
            if (hit)
            {
                hitPointOnPlane = ray.GetPoint(enter);
                MathExtensions.TestPointAABB(
                    hitPointOnPlane.XZ(),
                    clippingPlane.LeftFrontCorner,
                    clippingPlane.RightBackCorner,
                    out float distanceFromPoint,
                    out _
                );
                hitInsideClippingArea = distanceFromPoint < 0.0f;
            }
            else
            {
                hitInsideClippingArea = false;
                hitPointOnPlane = Vector3.zero;
            }
        }

        /// <summary>
        /// Raycasts against the given <paramref name="plane"/>. If the plane is hit, <paramref name="hit"/>
        /// contains the co-ordinates of the location where the raycast hit the <paramref name="plane"/>
        /// and <c>true</c> is returned. If the plane is not hit, <c>false</c> is returned and <paramref name="hit"/>
        /// will be <see cref="Vector3.positiveInfinity"/>.
        /// </summary>
        /// <param name="plane">The plane to raycast against.</param>
        /// <param name="hit">The hit point on the plane or <see cref="Vector3.positiveInfinity"/>,
        /// if ray and plane are parallel.</param>
        /// <returns>Whether the plane was hit.</returns>
        public static bool RaycastPlane(UnityEngine.Plane plane, out Vector3 hit)
        {
            Ray ray = UserPointsTo();
            bool result = plane.Raycast(ray, out float enter);
            hit = result ? ray.GetPoint(enter) : Vector3.positiveInfinity;
            return result;
        }

        /// <summary>
        /// A ray from the user's pointing device (mouse in a desktop environment,
        /// controller in VR).
        /// </summary>
        /// <returns>ray from the user's mouse</returns>
        public static Ray UserPointsTo()
        {
            // FIXME: We need to an interaction for VR, too.
            Camera mainCamera = MainCamera.Camera;
            Vector3 screenPoint;
            if (SceneSettings.InputType == PlayerInputType.VRPlayer)
            {
                XRSEEActions.RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit);
                screenPoint = mainCamera.WorldToScreenPoint(hit.point);
            }
            else
            {
                screenPoint = Input.mousePosition;
            }

            return mainCamera != null
                ? mainCamera.ScreenPointToRay(screenPoint)
                : new Ray(origin: Vector3.zero, direction: Vector3.zero);
        }
    }
}
