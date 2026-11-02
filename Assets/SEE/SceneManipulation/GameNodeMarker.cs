using SEE.Extensions;
using SEE.Utils;

using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Used to mark and unmark nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// The name of the nodes defined as a mark.
        /// </summary>
        private const string markName = "Mark";

        /// <summary>
        /// Adds or removes mark to/from node.
        /// </summary>
        /// <param name="node">The node to add/remove the mark to.</param>
        public static void AddMark(GameObject node)
        {
            Transform mark = IsMarked(node);
            if (mark != null)
            {
                RemoveMark(mark.gameObject);
            }
            else
            {
                PaintMark(node);
            }
        }

        /// <summary>
        /// Checks if node is marked.
        /// Returns said mark or null if not marked.
        /// </summary>
        /// <param name="node">The node to check for a mark.</param>
        /// <returns>A Transform of the Mark itself.</returns>
        public static Transform IsMarked(GameObject node)
        {
            return node.transform.Find(markName);
        }

        /// <summary>
        /// Removes mark from node.
        /// </summary>
        /// <param name="node">The node from which to remove the mark.</param>
        public static void RemoveMark(GameObject mark)
        {
            Destroyer.Destroy(mark);
        }

        /// <summary>
        /// Paints the actual mark.
        /// </summary>
        /// <param name="parent">the parent of the supposed mark</param>
        public static void PaintMark(GameObject parent)
        {
            Renderer renderer = parent.GetComponent<Renderer>();

            if (renderer != null)
            {
                float diameter = Mathf.Min(renderer.bounds.size.x, renderer.bounds.size.z);
                GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mark.name = markName;
                mark.transform.localScale = Vector3.one * diameter;
                mark.transform.position = parent.GetTop() + Vector3.up * (diameter / 2);
                mark.transform.SetParent(parent.transform, true);
            }
        }
    }
}
