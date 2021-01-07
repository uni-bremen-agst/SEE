using SEE.GO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities related to ray casting.
    /// </summary>
    public static class Raycasting
    {
        /// <summary>
        /// Raycasts the scene from the camera in the direction the mouse is pointing.
        /// The hit will be set, if no GUI element is hit AND and a GameObject with an
        /// attached <see cref="NodeRef"/> is hit.
        /// </summary>
        /// 
        /// <param name="raycastHit">The resulting hit if <code>true</code> is returned.
        /// </param>
        /// <returns><code>true</code> if no GUI element is hit AND and a GameObject with
        /// an attached <see cref="NodeRef"/> is hit, <code>false</code> otherwise.</returns>
        public static bool RaycastNodes(out RaycastHit raycastHit)
        {
            bool result = false;

            raycastHit = new RaycastHit();
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
            if (!IsMouseOverGUI()
                && Physics.Raycast(ray, out RaycastHit hit)
                && hit.transform.GetComponent<NodeRef>() != null)
            {
                raycastHit = hit;
                result = true;
            }
            return result;
        }

        private static EventSystem eventSystem = null;

        /// <summary>
        /// Whether the mouse currently hovers over a GUI element.
        /// </summary>
        /// <returns>Whether the mouse currently hovers over a GUI element.</returns>
        public static bool IsMouseOverGUI()
        {
            if (eventSystem == null)
            {
                eventSystem = Object.FindObjectOfType<EventSystem>();
            }
            return eventSystem.IsPointerOverGameObject();
        }
    }
}