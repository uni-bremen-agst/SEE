using SEE.Extensions;
using SEE.GraphElementRefs;
using SEE.SceneManipulation;
using SEE.Utils;

using UnityEngine;
using System;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Used to mark and unmark nodes
    /// </summary>
    public static class GameNodeMarker
    {

        /// <summary>
        /// adds or removes mark to/from node
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
        /// checks if node is marked
        /// returns said mark or null if not marked
        /// </summary>
        /// <param name="node">The node to check for a mark.</param>
        public static Transform IsMarked(GameObject node)
        {
            return node.transform.Find("Mark");
        }

        /// <summary>
        /// removes mark from node
        /// </summary>
        /// <param name="node">The node from which to remove the mark.</param>
        public static void RemoveMark(GameObject mark)
        {
            Destroyer.Destroy(mark);
        }

        /// <summary>
        /// paints the actual mark
        /// </summary>
        /// <param name="parent">the parent of the supposed mark</param>
        public static void PaintMark(GameObject parent)
        {
            Renderer renderer = parent.GetComponent<Renderer>();

            if (renderer != null)
            {
                float diameter = Mathf.Min(renderer.bounds.size.x, renderer.bounds.size.z);

                GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                mark.name = "Mark";

                mark.transform.localScale = Vector3.one * diameter;

                mark.transform.position = parent.GetTop() + Vector3.up * (diameter / 2);

                mark.transform.SetParent(parent.transform, true);
            }
        }
    }
}
