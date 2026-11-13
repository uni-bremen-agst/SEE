using System;
using UnityEngine;

using SEE.Extensions;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates and removes sphere markers for game nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name of the sphere used to mark a node.
        /// </summary>
        private const string markerName = "SEE_NodeMarker";

        /// <summary>
        /// Returns whether the given <paramref name="node"/> is marked.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>True if the node is marked.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="node"/> is null.
        /// </exception>
        public static bool IsMarked(GameObject node)
        {
            return GetMarker(node) != null;
        }

        /// <summary>
        /// Marks or unmarks the given <paramref name="node"/> based on its current
        /// marking state.
        /// </summary>
        /// <param name="node">The node whose marking is to be toggled.</param>
        /// <returns>True if the node is marked after the operation.</returns>
        public static bool ToggleMark(GameObject node)
        {
            if (IsMarked(node))
            {
                Unmark(node);
                return false;
            }

            Mark(node);
            return true;
        }

        /// <summary>
        /// Creates a sphere marker on top of the given <paramref name="node"/>.
        /// The sphere's diameter is the minimum of the node's width and depth,
        /// determined by its renderer's world-space bounds.
        /// If the node is already marked, nothing happens.
        /// </summary>
        /// <param name="node">The node to mark.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="node"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="node"/> has no renderer.
        /// </exception>
        public static void Mark(GameObject node)
        {
            if (IsMarked(node))
            {
                return;
            }

            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"Cannot mark node '{node.name}' because it has no Renderer.");
            }

            float diameter = Mathf.Min(renderer.bounds.size.x, renderer.bounds.size.z);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = markerName;

            sphere.transform.localScale = Vector3.one * diameter;

            // Offset by the sphere's radius so its bottom rests on top of the node.
            sphere.transform.position = node.GetTop() + Vector3.up * (diameter / 2f);
            sphere.transform.SetParent(node.transform, true);
        }

        /// <summary>
        /// Removes the sphere marker of the given <paramref name="node"/>.
        /// If the node is not marked, nothing happens.
        /// </summary>
        /// <param name="node">The node to unmark.</param>
        public static void Unmark(GameObject node)
        {
            Transform marker = GetMarker(node);

            if (marker != null)
            {
                Destroyer.Destroy(marker.gameObject);
            }
        }

        /// <summary>
        /// Returns the marker of the given <paramref name="node"/>, if one exists.
        /// </summary>
        /// <param name="node">The node whose marker is retrieved.</param>
        /// <returns>The marker or null if none exists.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="node"/> is null.
        /// </exception>
        private static Transform GetMarker(GameObject node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return node.transform.Find(markerName);
        }
    }
}
