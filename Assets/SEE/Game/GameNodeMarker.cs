using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Author: Hannes Kuss
    ///
    /// Allows to create marking-spheres for code city nodes 
    /// </summary>
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
            return newSphere;
        }
    }
}