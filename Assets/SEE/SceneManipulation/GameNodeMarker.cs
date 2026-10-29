using System;
using SEE.Extensions;
using SEE.Factories;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Handlling spawning and deleting the marker spheres on game nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Names for the child GameObject used as the marker.
        /// </summary>
        public const string MarkerName = "SEE_NodeMarker";

        /// <summary>
        /// Shared yellow material for markers so we don't spam new materials on toggle.
        /// </summary>
        private static Material markerMaterial;

        /// <summary>
        /// Checked if a node currently has a marker attached to it.
        /// </summary>
        /// <param name="node">The game node we want to check.</param>
        /// <returns>True if marked, false otherwise.</returns>
        public static bool IsMarked(GameObject node)
        {
            return node != null && node.transform.Find(MarkerName) != null;
        }

        /// <summary>
        /// Marks or unmarks a node. Won't make duplicate spheres if called multiple times.
        /// </summary>
        /// <param name="node">Node to toggle marking for.</param>
        /// <param name="marked">Should it be marked or not.</param>
        /// <exception cref="ArgumentNullException">If the passed node is null.</exception>
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
                    // Object destruction is delayed during Play mode, so hide and unparent it
                    // right away in case another toggle happens in the same frame.
                    existing.gameObject.SetActive(false);
                    existing.SetParent(null);
                    Destroyer.Destroy(existing.gameObject);
                }
                return;
            }

            node.WorldSpaceSize(out Vector3 size, out Vector3 center);
            float diameter = Mathf.Min(size.x, size.z);
            // Ignored other markers when getting top position so inner node markers don't mess up height calculations.
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

                // Removeed collider so it doesn't mess with raycasts or VR interaction.
                Collider collider = marker.GetComponent<Collider>();
                collider.enabled = false;
                Destroyer.Destroy(collider);

                if (markerMaterial == null)
                {
                    markerMaterial = MaterialsFactory.New(MaterialsFactory.ShaderType.PortalFree, Color.yellow);
                }
                marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            }

            // Seted size in world space before re-parenting so non-uniform parent scales don't distort the sphere.
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one * diameter;
            marker.transform.position = position;
            marker.transform.SetParent(node.transform, worldPositionStays: true);
        }
    }
}