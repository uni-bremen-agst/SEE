using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace SEE.Utils
{
    /// <summary>
    /// What precisely was hit by a ray cast.
    /// </summary>
    public enum HitGraphElement
    {
        None, // Neither a node nor an edge was hit.
        Node, // A node was hit.
        Edge  // An edge was hit.
    }

    /// <summary>
    /// Utilities related to ray casting.
    /// </summary>
    public static class Raycasting
    {
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
        /// attached, <see cref="HitGraphElement.None"/> is returned.</returns>
        public static HitGraphElement RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef)
        {
            return RaycastGraphElement(out raycastHit, out elementRef, Input.mousePosition);
        }
        
        /// <summary>
        /// Raycasts the scene from the camera in the direction the passed position vector is pointing.
        /// The hit will be set, if no GUI element is hit.
        /// </summary>
        /// <param name="raycastHit">will always be the hit object if any was hit, no matter whether
        /// it was a graph element or not.</param>
        /// <param name="elementRef">is null if the result is <see cref="HitGraphElement.None"/></param>
        /// <param name="pointerPosition">The position of the pointer used to determine the raycast origin</param>
        /// <returns>if no GUI element is hit, but a GameObject with either an attached <see cref="NodeRef"/> or
        /// <see cref="EdgeRef"/> is hit, then <see cref="HitGraphElement.Node"/> or <see cref="HitGraphElement.Edge"/>
        /// respectively, is returned. Otherwise if a GUI Element is hit or if the hit game object has neither a
        /// <see cref="NodeRef"/> nor an <see cref="EdgeRef"/> attached, <see cref="HitGraphElement.None"/> is returned
        /// </returns>
        public static HitGraphElement RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef,
            Vector2 pointerPosition)
        {
            Ray ray = MainCamera.Camera.ScreenPointToRay(pointerPosition);
            if (!IsMouseOverGUI() && Physics.Raycast(ray, out RaycastHit hit))
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
        /// <returns>true if the mouse is not over any GUI element and if anything was hit</returns>
        public static bool RaycastAnything(out RaycastHit raycastHit)
        {
            return RaycastAnything(out raycastHit, Input.mousePosition);
        }
        
        //// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// Returns true if the mouse is not over any GUI element and if anything was hit.
        /// <paramref name="raycastHit"/> will be set, if true is returned. 
        /// </summary>
        /// <param name="raycastHit">hit object if true is returned, undefined otherwise</param>
        /// <param name="pointerPosition">The provided pointerposition</param>
        /// <returns>true if the mouse is not over any GUI element and if anything was hit</returns>
        public static bool RaycastAnything(out RaycastHit raycastHit, Vector2 pointerPosition)
        {
            raycastHit = new RaycastHit();
            return !IsMouseOverGUI() && Physics.Raycast(MainCamera.Camera.ScreenPointToRay(pointerPosition), out raycastHit);
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
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
            if (!IsMouseOverGUI() && Physics.Raycast(ray, out RaycastHit hit))
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
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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
            return RaycastPlane(plane, out hit, Input.mousePosition);
        }
        
        /// <summary>
        /// Raycasts against the given plane.
        /// </summary>
        /// <param name="plane">The plane to raycast against.</param>
        /// <param name="hit">The hit point of the plane or <see cref="Vector3.positiveInfinity"/>,
        /// <param name="position">The screen point to construct to ray from.</param>
        /// if ray and plane are parallel.</param>
        /// <returns>Whether the plane was hit.</returns>
        public static bool RaycastPlane(UnityEngine.Plane plane, out Vector3 hit, Vector2 position)
        {
            Ray ray = MainCamera.Camera.ScreenPointToRay(position);
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
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
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
    }
}
