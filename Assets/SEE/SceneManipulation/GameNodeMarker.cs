using System;
using SEE.Extensions;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates and destroys a sphere that visually marks a node.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name of the marking sphere.
        /// </summary>
        private const string MarkerName = "NodeMarker";

        /// <summary>
        /// Returns the marking sphere of <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node whose marker is requested.</param>
        /// <returns>The marking sphere, or null if <paramref name="node"/> is not marked.</returns>
        private static GameObject GetMarker(GameObject node)
        {
            Transform marker = node.transform.Find(MarkerName);
            return marker != null ? marker.gameObject : null;
        }

        /// <summary>
        /// Checks whether <paramref name="node"/> currently has a marking sphere.
        /// </summary>
        /// <param name="node">The node to be checked.</param>
        /// <returns>True if <paramref name="node"/> is marked.</returns>
        private static bool IsMarked(GameObject node)
        {
            return GetMarker(node) != null;
        }

        /// <summary>
        /// Toggles the marking of <paramref name="node"/>.
        /// Creates the sphere above the node if it isn't marked yet,
        /// otherwise destroys the existing sphere.
        /// </summary>
        /// <param name="node">The node whose marking is to be toggled.</param>
        /// <returns>True if <paramref name="node"/> is marked after this call,
        /// false if it is unmarked after this call.</returns>
        public static bool ToggleMarker(GameObject node)
        {
            if (IsMarked(node))
            {
                UnmarkNode(node);
                return false;
            }
            MarkNode(node);
            return true;
        }

        /// <summary>
        /// Creates a marking sphere above the roof of the <paramref name="node"/> if it doesn't exist yet.
        /// </summary>
        /// <param name="node">The node to be marked.</param>
        /// <returns>The marking sphere, or the existing one if the node was already marked.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        private static GameObject MarkNode(GameObject node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            GameObject exists = GetMarker(node);
            if (exists != null)
            {
                return exists;
            }

            float diameter = Mathf.Min(node.transform.lossyScale.x, node.transform.lossyScale.z);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = MarkerName;
            sphere.GetComponent<Renderer>().material.color = Color.magenta.Darker();
            sphere.transform.localScale = Vector3.one * diameter;
            sphere.transform.position = node.GetTop() + Vector3.up * (diameter / 2f);
            sphere.transform.SetParent(node.transform);
            return sphere;
        }

        /// <summary>
        /// Destroys the marking sphere of <paramref name="node"/>, if there is one.
        /// </summary>
        /// <param name="node">The node whose marking is to be removed.</param>
        private static void UnmarkNode(GameObject node)
        {
            GameObject marker = GetMarker(node);
            if (marker != null)
            {
                Destroyer.Destroy(marker);
            }
        }
    }
}
