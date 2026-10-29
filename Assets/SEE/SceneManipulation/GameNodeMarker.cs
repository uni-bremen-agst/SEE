using SEE.Extensions;
using SEE.GraphElementRefs;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Used to mark and unmark nodes
    /// </summary>
    public static class GameNodeMarker
    {

        /// <summary>
        /// adds mark to node
        /// </summary>
        /// <param name="node">The node to add the mark to.</param>
        public void AddMark(GameObject node)
        {
            GameObject mark = IsMarked(node);
            mark != null ? RemoveMark(node, mark) :
            {
                addedMark = PaintMark(node)
            }
        }

        /// <summary>
        /// checks if node is marked
        /// returns said mark or null if not marked
        /// </summary>
        /// <param name="node">The node to check for a mark.</param>
        public GameObject IsMarked(GameObject node)
        {
            GameObject result = null;
            foreach(GameObject n in node.GetChildren())
            {
                if(n.name == "Mark")
                {
                    result = n;
                }
            }
            return result;
        }

        /// <summary>
        /// removes mark from node
        /// </summary>
        /// <param name="node">The node from which to remove the mark.</param>
        public void RemoveMark(GameObject node, GameObject mark)
        {
            Destroyer.Destroy(mark.NodeID);
        }


        /// <summary>
        /// paints the actual mark and returns it
        /// </summary>
        /// <param name="parent">the parent of the supposed mark</param>
        public GameObject PaintMark(GameObject parent)
        {
            GameObject mark;

            Renderer renderer = parent.GetComponent<Renderer>();

            if (renderer != null)
            {
                float diameter = Mathf.Min(renderer.bounds.size.x, renderer.bounds.size.z);

                GameObject mark = UnityEnginge.GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mark.name = "Mark";

                mark.transform.localScale = Vector3.one * diameter;

                Vector3 top = parent.GetTop();

                mark.transform.position = top + Vector3.up * (diameter / 2f);

                mark.transform.SetParent(parent.transform, true);
            }

        }
    }
}
