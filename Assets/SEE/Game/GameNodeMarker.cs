using System;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
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

                // FIXME: (?) this is totally not working...
                // the position of the marker sphere is above the marked node
                newMarkerSphere.transform.position = new Vector3(0, 0, 0);

                // FIXME: (?) this isn't working either...
                // the position of the marker sphere is above the marked node
                newMarkerSphere.transform.position = new Vector3(targetNode.transform.position.x,
                                                                 targetNode.transform.position.y,
                                                                 // + targetNode.transform.localScale.y
                                                                 // + newMarkerSphere.GetComponent<SphereCollider>().radius,
                                                                 // + 0.1f,
                                                                 targetNode.transform.position.z);

                // FIXME: (?) ensure markers are not blocking us from unmarking or marking other nodes
                newMarkerSphere.GetComponent<SphereCollider>().radius = 0;

                newMarkerSphere.SetColor(Color.magenta);

                // marker spheres can be recognized by their name-prefix "MarkerSphere"
                newMarkerSphere.name = "MarkerSphere" + targetNode.name;

                // assign the marker sphere to the node it marks
                newMarkerSphere.transform.SetParent(targetNode.transform);

                return newMarkerSphere;
            }
        }
    }
}
