using SEE.Extensions;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates and removes markers (spheres) above game nodes.
    /// This logic is shared by <see cref="SEE.Controls.ReversibleActions.MarkAction"/>
    /// (local execution) and <see cref="SEE.Net.Actions.GraphElement.MarkNetAction"/>
    /// (execution on all other clients).
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name of the game object representing the marker. It is used to find
        /// the marker among the children of a game node.
        /// </summary>
        private const string markerName = "Marker";

        /// <summary>
        /// Returns the marker of <paramref name="gameNode"/> or null if it is not marked.
        /// </summary>
        /// <param name="gameNode">The game node whose marker is requested.</param>
        /// <returns>The marker or null.</returns>
        private static GameObject GetMarker(GameObject gameNode)
        {
            Transform marker = gameNode.transform.Find(markerName);
            return marker != null ? marker.gameObject : null;
        }

        /// <summary>
        /// True if <paramref name="gameNode"/> currently has a marker.
        /// </summary>
        /// <param name="gameNode">The game node to check.</param>
        /// <returns>True if marked.</returns>
        public static bool IsMarked(GameObject gameNode)
        {
            return GetMarker(gameNode) != null;
        }

        /// <summary>
        /// Puts a sphere with the given <paramref name="color"/> above <paramref name="gameNode"/>.
        /// The diameter of the sphere is the minimum of the width (x) and depth (z) of the node.
        /// If the node is already marked, nothing happens.
        /// </summary>
        /// <param name="gameNode">The game node to be marked.</param>
        /// <param name="color">The color of the sphere.</param>
        public static void Mark(GameObject gameNode, Color color)
        {
            if (IsMarked(gameNode))
            {
                return;
            }

            Vector3 size = gameNode.WorldSpaceSize();
            float diameter = Mathf.Min(size.x, size.z);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = markerName;

            // The marker must not be hit by raycasts; otherwise, clicking on it
            // would not select the node underneath.
            if (sphere.TryGetComponent(out Collider collider))
            {
                Destroyer.Destroy(collider);
            }

            sphere.GetComponent<Renderer>().material.color = color;

            // Set the world-space size and position first, then attach it to the node,
            // keeping the world-space values. As a child of the node, the marker
            // follows the node when the node is moved.
            sphere.transform.localScale = new Vector3(diameter, diameter, diameter);
            // GetTop considers the node and all its active descendants, so the
            // sphere is placed above the highest part of the node.
            sphere.transform.position = gameNode.GetTop() + 0.5f * diameter * Vector3.up;
            sphere.transform.SetParent(gameNode.transform, worldPositionStays: true);
        }

        /// <summary>
        /// Removes the marker from <paramref name="gameNode"/>.
        /// If the node is not marked, nothing happens.
        /// </summary>
        /// <param name="gameNode">The game node whose marker is to be removed.</param>
        public static void Unmark(GameObject gameNode)
        {
            GameObject marker = GetMarker(gameNode);
            if (marker != null)
            {
                Destroyer.Destroy(marker);
            }
        }

        /// <summary>
        /// Marks <paramref name="gameNode"/> if <paramref name="mark"/> is true,
        /// otherwise removes its marker.
        /// </summary>
        /// <param name="gameNode">The affected game node.</param>
        /// <param name="mark">Whether the node should be marked afterwards.</param>
        /// <param name="color">The color of the sphere (only used for marking).</param>
        public static void SetMarked(GameObject gameNode, bool mark, Color color)
        {
            if (mark)
            {
                Mark(gameNode, color);
            }
            else
            {
                Unmark(gameNode);
            }
        }
    }
}
