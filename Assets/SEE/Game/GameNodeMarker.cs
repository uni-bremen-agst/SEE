using SEE.Controls.Actions;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Author: Hannes Kuss
    ///
    /// Allows to create marking-spheres for code city nodes and also to delete them.
    /// </summary>
    /// <example>
    ///     This Example shows how to create a marker for a node 
    ///     <code>
    ///        GameObject node = ...;
    ///        GameNodeMarker.CreateMarker(node);
    ///     </code>
    ///     To Delete a marker of a node you need to do something like this:
    ///     <code>
    ///         GameObject node = ...;
    ///         GameNodeMarker.RemoveMarker(node);
    ///     </code>
    ///     <code>
    ///         
    ///     </code>
    /// </example>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Creates a marker.
        /// 
        /// The nodePos should be the real position of the code city node, not the desired position of the marker sphere
        /// </summary>
        /// <param name="node">The GameObject of the node</param>
        /// <param name="yOffset">The y-axis offset of the node. Default is .5</param>
        /// <returns>The created sphere</returns>
        public static GameObject CreateMarker(GameObject node, float yOffset = 1.5F)
        {
            GameObject newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            newSphere.transform.parent = node.transform;
            newSphere.transform.localPosition = new Vector3(0, yOffset, 0);
            newSphere.transform.localScale = Vector3.one;
            string sphereName = node.name + MarkAction.MARKER_NAME_SUFFIX;
            newSphere.name = sphereName;
            return newSphere;
        }

        /// <summary>
        /// Deletes a marker sphere if it's exists
        /// </summary>
        /// <param name="node">The node which marker should be deleted</param>
        /// <returns>Returns true if the passed node has a marker sphere. Otherwise false</returns>
        public static bool RemoveMarker(GameObject node)
        {
            var nodeTran = node.transform;
            for (int i = 0; i < nodeTran.childCount; i++)
            {
                GameObject markerCanidate = nodeTran.GetChild(i).gameObject;
                // If the child is a marker sphere
                if (markerCanidate.name.EndsWith(MarkAction.MARKER_NAME_SUFFIX))
                {
                    Destroyer.DestroyGameObject(markerCanidate);
                    return true;
                }
            }

            return false;
        }
    }
}