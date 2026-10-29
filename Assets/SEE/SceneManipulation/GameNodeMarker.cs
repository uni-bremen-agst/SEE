using SEE.Extensions;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Provides the necessary functions to create and destroy the sphere for the marking of a node.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Toggles the marking of the given <paramref name="node"/>: if the node is not
        /// yet marked, a sphere is created above it whose diameter is the minimum of the
        /// node's width and depth and whose bottom touches the node's roof. If the node
        /// is already marked, the sphere is destroyed.
        /// </summary>
        /// <param name="node">The game node to be marked or unmarked.</param>
        public static void ToggleMark(GameObject node)
        {
            Transform existingSphere = node.transform.Find("MarkingSphere");

            if (existingSphere != null)
            {
                Destroyer.Destroy(existingSphere.gameObject);
            }
            else
            {
                Vector3 nodeSize = node.WorldSpaceSize();
                float diameter = Mathf.Min(nodeSize.x, nodeSize.z);

                Vector3 top = node.GetTop();

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(diameter, diameter, diameter);
                top.y = top.y + diameter / 2;
                sphere.transform.position = top;
                sphere.name = "MarkingSphere";
                sphere.transform.SetParent(node.transform);
            }
        }
    }
}
