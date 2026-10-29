using System;
using UnityEngine;
using SEE.Extensions;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Provides shared operations to create and remove sphere markings above nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Reserved name of the immediate child representing a node's marking.
        /// </summary>
        private const string markerName = "SEE.NodeMarker";

        /// <summary>
        /// Returns whether the given node has a sphere marking.
        /// </summary>
        /// <param name="node">The game object representing the node. Must not be null.</param>
        /// <returns>True if the node has a sphere marking; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">The node is null or destroyed.</exception>
        public static bool IsMarked(GameObject node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            return node.transform.Find(markerName) != null;
        }

        /// <summary>
        /// Creates or removes the node's sphere marking to match the desired state.
        /// Does nothing if the node already has the desired marking state.
        /// </summary>
        /// <param name="node">The game object representing the node. Must not be null.</param>
        /// <param name="marked">True to mark the node; false to unmark it.</param>
        /// <exception cref="ArgumentNullException">The node is null or destroyed.</exception>
        public static void SetMarked(GameObject node, bool marked)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Transform existingMarker = node.transform.Find(markerName);
            if (existingMarker != null)
            {
                if (!marked)
                {
                    // Make removal observable immediately, even before deferred destruction.
                    existingMarker.gameObject.SetActive(false);
                    existingMarker.SetParent(null, true);
                    Destroyer.Destroy(existingMarker.gameObject);
                }
                return;
            }
            if (!marked)
            {
                return;
            }

            node.WorldSpaceSize(out Vector3 size, out Vector3 center);
            float diameter = Mathf.Min(size.x, size.z);
            // Other markings must not increase the roof height of an inner node.
            Vector3 top = node.GetTop(child => child.name != markerName);
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = markerName;
            sphere.transform.position = new Vector3(center.x, top.y + diameter / 2f, center.z);
            sphere.transform.localScale = Vector3.one * diameter;
            sphere.transform.SetParent(node.transform, true);

            // The decoration must not intercept selection rays or collide with players.
            Collider collider = sphere.GetComponent<Collider>();
            collider.enabled = false;
            Destroyer.Destroy(collider);

            // Color this renderer without allocating a separate material for every marking.
            MaterialPropertyBlock properties = new();
            properties.SetColor("_Color", Color.cyan);
            properties.SetColor("_BaseColor", Color.cyan);
            sphere.GetComponent<Renderer>().SetPropertyBlock(properties);
        }
    }
}
