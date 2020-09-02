using UnityEngine;
using System;

namespace SEE.Utils
{
    /// <summary>
    /// Utilities related to ray casting.
    /// </summary>
    public static class Raycasting 
    {
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