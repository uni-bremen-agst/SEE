using SEE.Extensions;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    public static class GameNodeMarker
    {
        private const string MarkerName = "SEE Node Marker";

        /// <summary>
        /// Returns whether <paramref name="node"/> currently has a marker.
        /// </summary>
        public static bool IsMarked(GameObject node)
        {
            return FindMarker(node) != null;
        }

        /// <summary>
        /// Creates a marker for <paramref name="node"/> if it does not already have one.
        /// </summary>
        public static GameObject MarkNode(GameObject node)
        {
            GameObject existingMarker = FindMarker(node);
            if (existingMarker != null)
            {
                return existingMarker;
            }

            GameObject marker = NewMarker();
            marker.name = MarkerName;
            Vector3 nodeSize = node.WorldSpaceSize();
            float diameter = Mathf.Min(nodeSize.x, nodeSize.z);
            marker.transform.position = node.GetTop() + diameter / 2 * Vector3.up;
            marker.transform.localScale = Vector3.one * diameter;
            marker.GetComponent<Renderer>().material.color = Color.yellow;
            marker.transform.SetParent(node.transform, true);
            return marker;
        }

        /// <summary>
        /// Destroys the marker belonging to <paramref name="node"/>, if one exists.
        /// </summary>
        public static void UnmarkNode(GameObject node)
        {
            GameObject marker = FindMarker(node);
            if (marker != null)
            {
                Destroyer.Destroy(marker);
            }
        }

        /// <summary>
        /// Finds the marker stored as a direct child of <paramref name="node"/>.
        /// </summary>
        private static GameObject FindMarker(GameObject node)
        {
            return node.transform.Find(MarkerName)?.gameObject;
        }

        private static GameObject NewMarker()
        {
            return GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
    }
}
