using System;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Creates new game objects representing graph nodes or deleting these again,
    /// respectively.
    /// </summary>
    public static class GameNodeMarker
    {

        /// <summary>
        /// Check the transfered target node whether it was been marked already, and toggle it to the other state.
        /// <param name="targetNode">the node that was targeted for marking.</param>
        /// </summary>
        /// <returns>new instance</returns>
        public static GameObject TryMarking(GameObject targetNode)
        {

            // save the sphere that was used as a marker
            GameObject markerSphere = null;

            // iterate over all children of the targeted node
            foreach (Transform child in targetNode.transform)
            {
                // exit the loop if a (/the) marker was found
                if (child.name == "MarkerSphere" + targetNode.name)
                {
                    markerSphere = child.gameObject;
                    break;
                }
            }

            if (markerSphere != null)
            {
                // delete existing marker sphere
                Destroyer.DestroyGameObject(markerSphere);
                return null;
            }
            else
            {
                // create a new marker sphere because there is none currently
                GameObject newMarkerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                // the scale of the marker sphere depends on the scale of the marked node
                newMarkerSphere.transform.localScale = CalculateMarkerSize(targetNode);

                // the position of the marker sphere is above the marked node
                newMarkerSphere.transform.position = CalculateMarkerPosition(targetNode);

                // FIXME: check back with lead devs whether this consitutes desired behaviour
                // ensure markers are not blocking us from unmarking or marking other nodes
                newMarkerSphere.GetComponent<SphereCollider>().radius = 0;

                // FIXME: marker color could be a configuration option
                newMarkerSphere.SetColor(Color.green);

                // marker spheres can be recognized by their name-prefix "MarkerSphere"
                newMarkerSphere.name = "MarkerSphere" + targetNode.name;

                // assign the marker sphere to the node it is marking
                newMarkerSphere.transform.SetParent(targetNode.transform);

                return newMarkerSphere;
            }
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
