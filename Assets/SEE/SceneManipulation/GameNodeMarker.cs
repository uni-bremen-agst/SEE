using UnityEngine;
using SEE.Extensions;
using SEE.Utils;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates and destroys visual markers for graph nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        private const string MarkerName = "NodeMarker";

        /// <summary>
        /// Returns true if the given node already has a marker.
        /// </summary>
        public static bool IsMarked(GameObject node)
        {
            return node != null
                   && node.transform.Find(MarkerName) != null;
        }

        /// <summary>
        /// Creates a spherical marker above the given node.
        /// </summary>
        public static void Mark(GameObject node, Color color)
        {
            if (node == null || IsMarked(node))
            {
                return;
            }

            GameObject marker =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            marker.name = MarkerName;

            Vector3 size = node.transform.lossyScale;
            float diameter = Mathf.Min(size.x, size.z);

            Vector3 top = node.GetTop();

            marker.transform.position =
                top + Vector3.up * (diameter / 2f);

            marker.transform.localScale =
                Vector3.one * diameter;

            marker.transform.SetParent(node.transform, true);

            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.material.color = color;
        }

        /// <summary>
        /// Destroys the marker of the given node.
        /// </summary>
        public static void Unmark(GameObject node)
        {
            if (node == null)
            {
                return;
            }

            Transform marker = node.transform.Find(MarkerName);

            if (marker != null)
            {
                Destroyer.Destroy(marker.gameObject);
            }
        }
    }
}