using UnityEngine;
using System;
using SEE.GO;
using UnityEngine.EventSystems;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities related to ray casting.
    /// </summary>
    public static class Raycasting 
    {
        public static bool RaycastNodes(out RaycastHit raycastHit)
        {
            bool result = false;

            raycastHit = new RaycastHit();
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!UnityEngine.Object.FindObjectOfType<EventSystem>().IsPointerOverGameObject()
                && Physics.Raycast(ray, out RaycastHit hit)
                && hit.transform.GetComponent<NodeRef>() != null)
            {
                raycastHit = hit;
                result = true;
            }

            return result;
        }
        
        //public static bool RaycastGUI()
        //{
        //
        //}
        // DesktopChartAction.cs

        /// <summary>
        /// Returns a sorted list of hits of a ray starting at the main camera and directing
        /// at towards the current mouse position. The list is sorted by the diancce to the camera;
        /// closer objects are at the front. The list may be empty if no object was hit.
        /// </summary>
        /// <returns>sorted list of hits</returns>
        public static RaycastHit[] SortedHits()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            Array.Sort(hits, (h0, h1) => h0.distance.CompareTo(h1.distance));
            return hits;
        }
    }
}