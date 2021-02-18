using SEE.GO;
using UnityEngine;
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
        /// The hit will be set, if no GUI element is hit AND and a GameObject with either an
        /// attached <see cref="NodeRef"/> or <see cref="EdgeRef"/> is hit.
        /// 
        /// Note: <paramref name="nodeRef"/> and <paramref name="raycastHit"/> are undefined
        /// if the result is <see cref="HitGraphElement.None"/>.
        /// </summary>
        /// 
        /// <param name="raycastHit">The resulting hit if <code>None</code> is not returned.</param>
        /// <param name="nodeRef">The hit graph element if <code>None</code> is not returned.</param>
        /// <returns>if no GUI element is hit, but a GameObject with either
        /// an attached <see cref="NodeRef"/> or <see cref="EdgeRef"/> is hit, then 
        /// <see cref="HitGraphElement.Node"/> or <see cref="HitGraphElement.Edge"/>,
        /// respectively, is returned. Otherwise if a GUI element is hit or if the
        /// hit game object has neither a <see cref="NodeRef"/> nor an <see cref="EdgeRef"/>
        /// attached, <see cref="HitGraphElement.None"/> is returned.
        public static HitGraphElement RaycastGraphElement(out RaycastHit raycastHit, out GraphElementRef elementRef)
        {
            HitGraphElement result = HitGraphElement.None;

            raycastHit = new RaycastHit();
            elementRef = null;
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
            if (!IsMouseOverGUI() && Physics.Raycast(ray, out RaycastHit hit))
            {
                raycastHit = hit;
                if (hit.transform.TryGetComponent(out NodeRef nodeRef))
                {
                    result = HitGraphElement.Node;
                    elementRef = nodeRef;
                }
                else if (hit.transform.TryGetComponent(out EdgeRef edgeRef))
                {
                    result = HitGraphElement.Edge;
                    elementRef = edgeRef;
                   // Debug.Log($"RaycastGraphElement: hit edge {edgeRef.name}.\n");
                }
            }
            return result;
        }

        /// <summary>
        /// The cached event system. It is cached because it needs to be queried in
        /// each Update cycle.
        /// </summary>
        private static EventSystem eventSystem = null;

        /// <summary>
        /// Whether the mouse currently hovers over a GUI element.
        /// </summary>
        /// <returns>Whether the mouse currently hovers over a GUI element.</returns>
        public static bool IsMouseOverGUI()
        {
            if (eventSystem == null)
            {
                eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
                if (eventSystem == null)
                {
                    throw new System.Exception("No EventSystem found.");
                }
            }
            return eventSystem.IsPointerOverGameObject();
        }
    }
}