using UnityEngine;

using SEE.Extensions;
using SEE.Factories;
using SEE.Game;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Provides functionality for visually marking game nodes.
    /// </summary>

    internal static class GameNodeMarker
    {
        /// <summary>
        /// The name identifying a marker among a node's immediate children.
        /// </summary>
        private const string MarkerName = "MarkingSphere";

        /// <summary>
        /// Returns whether <paramref name="gameNode"/> has a marker.
        /// </summary>
        /// <param name="gameNode">The existing graph node to check; must not be null.</param>
        /// <returns>True if a marker child exists; otherwise, false.</returns>
        internal static bool IsMarked(GameObject gameNode)
        {
            return FindMarker(gameNode) != null;
        }

        /// <summary>
        /// Applies the requested marking state to <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">The existing graph node to mark or unmark; must not be null.</param>
        /// <param name="marked">True to ensure a marker exists; false to remove it if present.</param>
        /// <exception cref="System.IO.FileNotFoundException">The required marker material resource cannot be loaded.</exception>
        internal static void SetMarked(GameObject gameNode, bool marked)
        {
            Transform marker = FindMarker(gameNode);

            if (marked && marker == null)
            {
                CreateMarker(gameNode);
            }
            else if (!marked && marker != null)
            {
                RemoveMarker(marker);
            }
        }

        /// <summary>
        /// Adds a sphere decoration above the roof of <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">The existing, unmarked graph node to decorate; must not be null.</param>
        /// <exception cref="System.IO.FileNotFoundException">The required marker material resource cannot be loaded.</exception>
        private static void CreateMarker(GameObject gameNode)
        {
            gameNode.WorldSpaceSize(out Vector3 size, out Vector3 center);

            float diameter = Mathf.Min(size.x, size.z);
            float radius = diameter / 2.0f;
            float roof = gameNode.GetRoof();

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            marker.name = MarkerName;
            marker.tag = Tags.Decoration;

            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = MaterialsFactory.New(
                MaterialsFactory.ShaderType.OpaqueMetallic,
                Color.red);

            SphereCollider collider = marker.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            marker.transform.localScale = new(diameter, diameter, diameter);
            marker.transform.position = new(center.x, roof + radius, center.z);

            marker.transform.SetParent(gameNode.transform, true);

            Portal.InheritPortal(gameNode, marker);
        }

        /// <summary>
        /// Deactivates, detaches, and destroys the marker decoration.
        /// </summary>
        /// <param name="marker">The marker transform to remove; must not be null.</param>
        private static void RemoveMarker(Transform marker)
        {
            marker.gameObject.SetActive(false);
            marker.SetParent(null);
            Destroyer.Destroy(marker.gameObject);
        }

        /// <summary>
        /// Finds the immediate child identified by <see cref="MarkerName"/>.
        /// </summary>
        /// <param name="gameNode">The graph node whose children are searched; must not be null.</param>
        /// <returns>The marker transform, or null if no matching child exists.</returns>
        private static Transform FindMarker(GameObject gameNode)
        {
            return gameNode.transform.Find(MarkerName);
        }

    }
}
