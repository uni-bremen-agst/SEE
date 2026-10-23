using SEE.Extensions;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates or deletes a marker for a game node.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name of the child game object representing the marking.
        /// </summary>
        private const string MarkerName = "NodeMarker";

        /// <summary>
        /// Marks the <paramref name="node"/> if it is not marked yet.
        /// Otherwise it removes the mark.
        /// </summary>
        /// <param name="node">Node to mark or unmark.</param>
        public static void ToggleMark(GameObject node)
        {
            if (node == null)
            {
                return;
            }

            GameObject marker = GetMarker(node);

            if (marker == null)
            {
                Mark(node);
            }
            else
            {
                Unmark(node);
            }
        }

        /// <summary>
        /// Marks the <paramref name="node"/> with a sphere.
        /// </summary>
        /// <param name="node">Node to mark.</param>
        public static void Mark(GameObject node)
        {
            if (node == null || GetMarker(node) != null)
            {
                return;
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = MarkerName;

            Vector3 nodeScale = node.transform.lossyScale;
            float diameter = Mathf.Min(nodeScale.x, nodeScale.z);

            marker.transform.SetParent(null);
            marker.transform.localScale = new Vector3(diameter, diameter, diameter);

            Vector3 roofCenter = node.GetTop();
            marker.transform.position = roofCenter + Vector3.up * (diameter / 2f);

            marker.transform.SetParent(node.transform, worldPositionStays: true);

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.material.color = Color.white;
            }
        }

        /// <summary>
        /// Removes the mark of the <paramref name="node"/>, if it exists.
        /// </summary>
        /// <param name="node">Node whose marking should be removed.</param>
        public static void Unmark(GameObject node)
        {
            GameObject marker = GetMarker(node);

            if (marker != null)
            {
                Destroyer.Destroy(marker);
            }
        }

        /// <summary>
        /// Returns true if the <paramref name="node"/> is currently marked.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <returns>True if the node is marked.</returns>
        public static bool IsMarked(GameObject node)
        {
            return GetMarker(node) != null;
        }

        /// <summary>
        /// Returns the marking sphere of the <paramref name="node"/>
        /// or null if it is not marked.
        /// </summary>
        /// <param name="node">Node whose marking should be returned.</param>
        /// <returns>Marking sphere or null.</returns>
        private static GameObject GetMarker(GameObject node)
        {
            if (node == null)
            {
                return null;
            }

            Transform markerTransform = node.transform.Find(MarkerName);

            if (markerTransform != null)
            {
                return markerTransform.gameObject;
            }
            else
            {
                return null;
            }
        }
    }
}
