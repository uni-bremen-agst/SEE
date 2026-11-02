using UnityEngine;
using SEE.Extensions;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates/removes the visual marker for graph nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name used for the marker child object.
        /// </summary>
        private const string markerName = "Marker";
        /// <summary>
        /// Checks whether the node currently has a marker.
        /// </summary>
        /// <param name="node">The graph node to check.</param>
        /// <returns>True if the node has a marker, if not false.</returns>
        public static bool IsMarked(GameObject node)
        {
            return node.transform.Find(markerName) != null;
        }
        /// <summary>
        /// Sets the marking state of the node by creating/removing its marker.
        /// </summary>
        /// <param name="node">The graph node whose marking is to be changed.</param>
        /// <param name="marked">True to mark, false to unmark.</param>
        public static void SetMarked(GameObject node, bool marked)
        {
            Transform existingMarker = node.transform.Find(markerName);
            if (!marked)
            {
                if (existingMarker != null)
                {
                    Destroyer.Destroy(existingMarker.gameObject);
                }
                return;
            }

            if (existingMarker != null)
            {
                return;
            }

            // Determine the marker diameter from the smaller X/Z dimension of the node.
            Bounds bounds = node.GetComponent<Renderer>().bounds;
            Vector3 size = bounds.size;
            float diameter = Mathf.Min(size.x, size.z);
            Vector3 roof = node.GetTop();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = markerName;

            // Position the sphere so that its bottom touches the center of the nodes roof.
            sphere.transform.position = roof + Vector3.up * (diameter / 2f);
            sphere.transform.localScale = Vector3.one * diameter;

            // Make the marker a child of the node so it moves together with the node.
            sphere.transform.SetParent(node.transform);
        }
    }
}
