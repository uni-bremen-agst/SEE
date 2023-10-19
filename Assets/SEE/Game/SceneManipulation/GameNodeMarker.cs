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
    /// Creates new game objects representing marker.
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
        public static GameObject NewSphere(GameObject parent, Vector3 scale)
        {
            if (parent == null)
            {
                throw new Exception("Parent must not be null.");
            }
            else if (existing(parent) && (sphere.activeSelf != false))
            {
                sphere.SetActive(false);
                return null;
            }

            sphere.GetComponent<Renderer>().material.color = Color.red;
            float realScale = Math.Min(scale.x, scale.y);
            Vector3 position = GameObjectExtensions.GetTop(parent);
            sphere.transform.position = position; //parent.transform.position;
            sphere.transform.localScale = new Vector3(realScale, realScale, realScale); //parent.transform.lossyScale;
            //bool hm = parent.transform.GetChild(parent.transform.childCount) == sphere;
            sphere.transform.SetParent(parent.transform);
            sphere.SetActive(true);
            return sphere;
        }

        /// <summary>
        /// Checks if the parent already has a sphere.
        /// </summary>
        /// <param name="parent">the parent node of the sphere</param>
        /// <returns>true if the parent already has a sphere</returns>
        public static bool existing(GameObject parent)
        {
            for (int i = 0; i <= parent.transform.childCount - 1; i++)
            {
                if (parent.transform.GetChild(i).transform.name == "Sphere")
                {
                    return true;
                }
            }
            return false;
        }

    }
}
