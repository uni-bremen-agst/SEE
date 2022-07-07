using System;
using System.Collections.Generic;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

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
        private const uint RAYCAST_BUFFER_SIZE = 500;

        /// <summary>
        /// Layer number for game objects representing UI components.
        /// </summary>
        private const uint UI_LAYER = 5;

        /// <summary>
        /// Names of game objects which are on the <see cref="UI_LAYER"/>,
        /// but won't "count" as UI components, e.g., when checking whether
        /// the user is currently hovering over any UI components.
        /// </summary>
        private static readonly ISet<string> ignoredUINames =
            new HashSet<string>
            {
                // TODO @koschke: Is it alright to let clicks on this "fall through" to below game objects?
                "ChatBox" // ignored because otherwise it'd obscure half the screen.
            };

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// The hit will be set, if no GUI element is hit.
        ///
        /// Note: <paramref name="elementRef"/> is null if the result is <see cref="HitGraphElement.None"/>.
        /// Yet, <paramref name="raycastHit"/> will always be the hit object if any was hit,
        /// no matter whether it was a graph element or not.
        /// </summary>
        ///
        /// <param name="raycastHit">The hit object.</param>
        /// <param name="nodeRef">The hit graph element if <code>None</code> is not returned.</param>
        /// <returns>if no GUI element is hit, but a GameObject with either
        /// an attached <see cref="NodeRef"/> or <see cref="EdgeRef"/> is hit, then
        /// <see cref="HitGraphElement.Node"/> or <see cref="HitGraphElement.Edge"/>,
        /// respectively, is returned. Otherwise if a GUI element is hit or if the
        /// hit game object has neither a <see cref="NodeRef"/> nor an <see cref="EdgeRef"/>
        /// attached, <see cref="HitGraphElement.None"/> is returned.
        public static HitGraphElement RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef)
        {
            if (!IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out RaycastHit hit))
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
            return !IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out raycastHit, maxDistance);
        }

        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing and chooses the
        /// node that is lowest in the node hierarchy, so in the lowest level of the tree
        /// (more precisely, the one with the greatest value of the node attribute Level;
        /// Level counting starts at the root and increases downward into the tree).
        /// </summary>
        /// <param name="raycastHit">hit object of lowest node if true is returned, null otherwise</param>
        /// <param name="hitNode">lowest node if true is returned, null otherwise</param>
        /// <param name="referenceNode">if given, all nodes which are not in the same graph as
        /// <paramref name="referenceNode"/> as well as itself will not be considered when sorting raycast results.
        /// </param>
        /// <returns>true if the mouse was over at least one node</returns>
        public static bool RaycastLowestNode(out RaycastHit? raycastHit, out Node hitNode, NodeRef referenceNode = null)
        {
            RaycastHit[] hits = new RaycastHit[RAYCAST_BUFFER_SIZE];
            int numberOfHits = Physics.RaycastNonAlloc(UserPointsTo(), hits);
            if (numberOfHits == RAYCAST_BUFFER_SIZE)
            {
                Debug.LogWarning("We possibly got more hits than buffer space is available.\n");
            }

            raycastHit = null;
            hitNode = null;
            for (int i = 0; i < numberOfHits; i++)
            {
                RaycastHit hit = hits[i];
                if (referenceNode == null || hit.collider.gameObject != referenceNode.gameObject)
                {
                    NodeRef nodeRef = hit.transform.GetComponent<NodeRef>();
                    // Is it a node at all and if so, are they in the same graph?
                    if (nodeRef != null && nodeRef.Value != null
                        && (referenceNode == null || (referenceNode.Value != null && nodeRef.Value.ItsGraph == referenceNode.Value.ItsGraph)))
                    {
                        // update newParent when we found a node deeper into the tree
                        if (hitNode == null || nodeRef.Value.Level > hitNode.Level)
                        {
                            hitNode = nodeRef.Value;
                            raycastHit = hit;
                        }
                    }
                }
            }

            return numberOfHits > 0;
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

            raycastHit = new RaycastHit();
            obj = null;
            if (!IsMouseOverGUI() && Physics.Raycast(UserPointsTo(), out RaycastHit hit))
            {
                raycastHit = hit;
                if (hit.transform.TryGetComponent(out InteractableObject io))
                {
                    if (io.GraphElemRef.elem is Node)
                    {
                        result = HitGraphElement.Node;
                    }
                    else
                    {
                        Assert.IsTrue(io.GraphElemRef.elem is Edge);
                        result = HitGraphElement.Edge;
                    }
                    obj = io;
                }
            }
            return result;
        }

        /// <summary>
        /// Whether the mouse currently hovers over a GUI element.
        ///
        /// Note: If no <see cref="EventSystem"/> exists in the scene, internal calls will fails
        /// and <c>false</c> will be returned.
        /// </summary>
        /// <returns>Whether the mouse currently hovers over a GUI element.</returns>
        public static bool IsMouseOverGUI()
        {
            InputSystemUIInputModule inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (inputModule == null)
            {
                Debug.LogError("Could not find input system UI module! Falling back to old detection for now.");
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            else
            {
                GameObject lastGameObject = inputModule.GetLastRaycastResult(Mouse.current.deviceId).gameObject;
                return lastGameObject != null && lastGameObject.layer == UI_LAYER
                                              && !ignoredUINames.Contains(lastGameObject.name);
            }
        }

        /// <summary>
        /// Raycasts against the given plane.
        /// </summary>
        /// <param name="plane">The plane to raycast against.</param>
        /// <param name="hit">The hit point of the plane or <see cref="Vector3.positiveInfinity"/>,
        /// if ray and plane are parallel.</param>
        /// <returns>Whether the plane was hit.</returns>
        public static bool RaycastPlane(UnityEngine.Plane plane, out Vector3 hit)
        {
            Ray ray = UserPointsTo();
            bool result = plane.Raycast(ray, out float enter);
            if (result)
            {
                hit = ray.GetPoint(enter);
            }
            else
            {
                hit = Vector3.positiveInfinity;
            }
            return result;
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
            UnityEngine.Plane raycastPlane = new UnityEngine.Plane(Vector3.up, clippingPlane.transform.position);
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
        /// A ray from the user's mouse.
        /// </summary>
        /// <returns>ray from the user's mouse</returns>
        public static Ray UserPointsTo()
        {
            // FIXME: We need to an interaction for VR, too.
            Camera mainCamera = MainCamera.Camera;
            return mainCamera != null
                ? mainCamera.ScreenPointToRay(Input.mousePosition)
                : new Ray(origin: Vector3.zero, direction: Vector3.zero);
        }
    }
}