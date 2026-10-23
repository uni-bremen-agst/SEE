using System;
using SEE.Extensions;
using SEE.Factories;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates and removes the spheres used to mark game nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name of the immediate child representing a node's marking.
        /// </summary>
        public const string MarkerName = "SEE_NodeMarker";

        /// <summary>
        /// All markings share one yellow material, avoiding a new material for every toggle.
        /// </summary>
        private static Material markerMaterial;

        /// <summary>
        /// Returns whether <paramref name="node"/> currently has a marking.
        /// </summary>
        /// <param name="node">The game node to inspect.</param>
        /// <returns>True if the node has a marking.</returns>
        public static bool IsMarked(GameObject node)
        {
            return node != null && node.transform.Find(MarkerName) != null;
        }

        /// <summary>
        /// Sets the marking of <paramref name="node"/> to the requested state.
        /// Repeating the same request does not create duplicate spheres or toggle the state.
        /// </summary>
        /// <param name="node">The game node to mark or unmark.</param>
        /// <param name="marked">Whether the node should have a marking.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        public static void SetMarked(GameObject node, bool marked)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Transform existing = node.transform.Find(MarkerName);
            if (!marked)
            {
                if (existing != null)
                {
                    // Destruction is deferred in Play mode. Hide and detach the sphere now so
                    // another toggle in this frame can create a new marking at the correct height.
                    existing.gameObject.SetActive(false);
                    existing.SetParent(null);
                    Destroyer.Destroy(existing.gameObject);
                }
                return;
            }

            node.WorldSpaceSize(out Vector3 size, out Vector3 center);
            float diameter = Mathf.Min(size.x, size.z);
            // Other markings must not raise the roof when an inner node is marked.
            Vector3 top = node.GetTop(child => child.name != MarkerName);
            Vector3 position = new Vector3(center.x, top.y + diameter / 2.0f, center.z);

            GameObject marker;
            if (existing != null)
            {
                marker = existing.gameObject;
                marker.transform.SetParent(null);
            }
            else
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = MarkerName;
                marker.layer = LayerMask.NameToLayer("Ignore Raycast");

                // The marker is decoration. It must not intercept clicks or VR rays.
                Collider collider = marker.GetComponent<Collider>();
                collider.enabled = false;
                Destroyer.Destroy(collider);

                if (markerMaterial == null)
                {
                    markerMaterial = MaterialsFactory.New(MaterialsFactory.ShaderType.PortalFree, Color.yellow);
                }
                marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            }

            // Set world-space dimensions before parenting. This keeps the primitive spherical
            // even when its node or the enclosing city has a non-uniform scale.
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one * diameter;
            marker.transform.position = position;
            marker.transform.SetParent(node.transform, worldPositionStays: true);
        }
    }
}
