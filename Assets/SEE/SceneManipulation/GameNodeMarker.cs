
using UnityEngine;

using SEE.Extensions;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates new markers or deletes existing ones for game nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Creates a marker above the given node if it is not already marked.
        /// </summary>
        /// <param name="node">The node to mark.</param>
        private static void MarkNode(GameObject node)
        {
            if (IsMarked(node))
            {
                return;
            }

            Renderer renderer = node.GetComponent<Renderer>();

            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;

                float diameter = Mathf.Min(bounds.size.x, bounds.size.z);

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "MarkSphere";

                sphere.transform.localScale = Vector3.one * diameter;

                Vector3 top = node.GetTop();
                sphere.transform.position = top + Vector3.up * (diameter / 2f);
                sphere.transform.SetParent(node.transform, true);
            }
        }

        /// <summary>
        /// Checks whether the given node has a marker.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>True if the node is marked, otherwise false.</returns>
        private static bool IsMarked(GameObject node)
        {
            Transform sphere = node.transform.Find("MarkSphere");

            return sphere != null;
        }

        /// <summary>
        /// Removes the marker from the given node.
        /// </summary>
        /// <param name="node">The node to unmark.</param>
        private static void UnmarkNode(GameObject node)
        {
            Transform marker = node.transform.Find("MarkSphere");

            if (marker != null)
            {
                Destroyer.Destroy(marker.gameObject);
            }
        }

        /// <summary>
        /// Toggles the marking state of the given node.
        /// </summary>
        /// <param name="node">The node whose marking state is toggled.</param>
        public static void ToggleMark(GameObject node)
        {
            if (IsMarked(node))
            {
                UnmarkNode(node);
            }
            else
            {
                MarkNode(node);
            }
        }
    }
}
