using SEE.GO;
using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Creates new markers for nodes or deleting them again, respectively.
    /// </summary>
    public static class GameNodeMarker
    {

        private const string PREFIX = "MarkerSphere";

        /// <summary>
        /// Iterates through the given node's children and marks their parent, if no child was marked yet.
        /// Otherwise the marker is destroyed.
        /// </summary>
        /// <returns>new instance</returns>
        public static GameObject ToggleMark(GameObject node)
        {
            GameObject markerObj = null;
            foreach (Transform child in node.transform)
            {
                if (isMarked(child, node))
                {
                    Destroyer.DestroyGameObject(markerObj);
                    return null;
                }
            }
            return createMarkerSphere(node);
        }

        private static Boolean isMarked(Transform child, GameObject node)
        {
            return child.name == PREFIX + node.name;
        }

        /// <summary>
        /// Creates a new marker sphere.
        /// </summary>
        /// <returns>A new marker sphere</returns>
        private static GameObject createMarkerSphere(GameObject node)
        {
            GameObject markerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObj.transform.localScale = CalculateMarkerSize(node);
            markerObj.transform.position = CalculateMarkerPosition(node);
            markerObj.GetComponent<SphereCollider>().radius = 0;
            markerObj.SetColor(Color.blue);
            markerObj.name = PREFIX + node.name;
            markerObj.transform.SetParent(node.transform);
            return markerObj;
        }

        /// <summary>
        /// Returns the scale of the marker sphere based on the area of the <paramref name="targetNode"/>.
        /// </summary>
        /// <param name="targetNode">The marked node relative to which the marker sphere is positioned and scaled.</param>
        /// <returns>The scale of the marker sphere.</returns>
        private static Vector3 CalculateMarkerSize(GameObject targetNode)
        {
            // calculate the maximum ground area
            float verticalLength = Math.Min(targetNode.transform.lossyScale.x,
                                            targetNode.transform.lossyScale.z);

            // return the marker size as a cube
            return Vector3.one * verticalLength;
        }

        /// <summary>
        /// Returns the position of the marker sphere based on the position of the <paramref name="targetNode"/>.
        /// </summary>
        /// <param name="targetNode">The marked node relative to which the marker sphere is positioned and scaled.</param>
        /// <returns>The position of the marker sphere.</returns>
        private static Vector3 CalculateMarkerPosition(GameObject targetNode)
        {
            return new Vector3(targetNode.transform.position.x,
                        targetNode.transform.position.y
                        + targetNode.transform.lossyScale.y / 2
                        + CalculateMarkerSize(targetNode).y,
                        targetNode.transform.position.z);
        }
    }
}
