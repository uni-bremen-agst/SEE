using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;
using SEE.Utils;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Creates new game objects representing a marker.
    /// The position is over the marked Node.
    /// The scale is proportional to the marked Node.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Creates and returns a new sphere.
        /// </summary>
        /// <param name="parent">the parent node of the sphere</param>
        /// <param name="scale">the scale of the sphere</param>
        /// <returns>new sphere</returns>

        private static GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //[SerializeField] private static List<GameObject> markedNodes;
        public static GameObject NewSphere(GameObject parent)
        {
            if (parent == null)
            {
                throw new Exception("Parent must not be null.");
            }
            else if (Existing(parent) && !sphere.activeSelf)
            {
                sphere.SetActive(false);
                return null;
            }

            sphere.GetComponent<Renderer>().material.color = Color.red;
            float realScale = Math.Min(parent.transform.lossyScale.x, parent.transform.lossyScale.y);
            Vector3 position = parent.GetTop();
            sphere.transform.position = position;
            sphere.transform.localScale = new Vector3(realScale, realScale, realScale);
            sphere.transform.SetParent(parent.transform);
            sphere.SetActive(true);
            return sphere;
        }

        /// <summary>
        /// Checks if the parent already has a sphere.
        /// </summary>
        /// <param name="parent">the parent node of the sphere</param>
        /// <returns>true if the parent already has a sphere</returns>
        public static bool Existing(GameObject parent)
        {
            if (parent.transform.Find("Sphere"))
            {
                return true;
            }
            return false;
        }

    }
}
