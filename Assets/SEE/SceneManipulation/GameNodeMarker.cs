using SEE.Extensions;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Adds and removes marker spheres on game nodes in the code city.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// The name of the marker object that is used to mark game nodes in the scene.
        /// </summary>
        private const string MarkerName = "Marker";

        /// <summary>
        /// Checks if the given game node has a marker attached to it.
        /// </summary>
        /// <param name="gameNode">The game node to check for a marker.</param>
        /// <returns>True if the game node is marked, false otherwise.</returns>
        public static bool IsMarked(GameObject gameNode)
        {
            return gameNode.transform.Find(MarkerName) != null;
        }

        /// <summary>
        /// Toggles the marker on the given game node. If the game node is already marked
        /// it will remove the marker, otherwise it will add a new marker.
        /// </summary>
        /// <param name="gameNode">The game node to toggle the marker on.</param>
        /// <returns>True if the game node is marked after the toggle, false otherwise.</returns>
        public static bool Toggle(GameObject gameNode)
        {
            if (IsMarked(gameNode))
            {
                RemoveMarker(gameNode);
                return false;
            }
            else
            {
                AddMarker(gameNode);
                return true;
            }
        }

        /// <summary>
        /// Adds a marker to the given game node.
        /// </summary>
        /// <param name="gameNode">The game node to add a marker to.</param>
        private static void AddMarker(GameObject gameNode)
        {
            // Create a new sphere primitive to serve as the marker
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            // Setting the name so we can find it later with IsMarked
            sphere.name = MarkerName;

            // Removing the collider so it doesn't interfere with other interactions
            if (sphere.TryGetComponent(out Collider collider))
            {
                Destroyer.Destroy(collider);
            }

            // Getting the renderer and setting the color
            if (sphere.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = Color.green;
            }

            Vector3 size = gameNode.WorldSpaceSize();

            // The diameter of the sphere must be the minimum of the width (x axis) and depth (z axis) of the marked node.
            float diameter = Mathf.Min(size.x, size.z);
            sphere.transform.localScale = Vector3.one * diameter;

            // The sphere is to be placed above the X/Z center of the node to be marked.
            // The sphere's bottom touches the node's roof.
            sphere.transform.position = gameNode.GetTop() + (Vector3.up * diameter / 2);

            // Adding sphere to the game node so it moves with it.
            sphere.transform.SetParent(gameNode.transform, true);
        }

        /// <summary>
        /// Removes the marker from the given game node.
        /// </summary>
        /// <param name="gameNode">The game node to remove the marker from.</param>
        private static void RemoveMarker(GameObject gameNode)
        {
            Transform marker = gameNode.transform.Find(MarkerName);
            if (marker != null)
            {
                Destroyer.Destroy(marker.gameObject);
            }
        }
    }
}
