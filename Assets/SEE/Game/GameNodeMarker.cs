using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Net;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// This class provides functionality for adding a sphere marker above selected nodes.
    /// </summary>
    public static class GameNodeMarker 
    {
        /// <summary>
        /// Creates and returns a new sphere game object as child of <paramref name="parent"/> at the
        /// given <paramref name="position"/>. The diameter of the sphere is the minimum of the width (x axis)
        /// and depth (y axis) of <paramref name="worldSpaceScale"/>.
        /// </summary>
        /// <param name="parent">The node that should be the parent of new marker</param>
        /// <param name="position">the position in world space for the center point of the new marker </param>
        /// <param name="worldSpaceScale">the scale in world space of the new marker</param>
        /// <returns>new marker game object or null if none could be created or a marker already exists</returns>
        /// <exception cref="Exception"></exception>
        public static GameObject addSphere(GameObject parent, Vector3 position, Vector3 worldSpaceScale)
        {
            GameObject sphere;
            
            if(parent == null)
            {
                throw new Exception("GameObject must not be null.");
            }
            else if (deleteExistingSphere(parent))
            {
                sphere = null;
            }
            else
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                float diameter = Math.Min(worldSpaceScale.x, worldSpaceScale.y);
                sphere.transform.localScale = new Vector3(diameter, diameter, diameter);
                sphere.transform.position = new Vector3(position.x, position.y + 0.1f, position.z);
                sphere.transform.SetParent(parent.gameObject.transform);
                sphere.GetComponent<Renderer>().material.color = Color.red;
            }
            return sphere;
        }
        
        /// <summary>
        /// This function searches all children of <param name="parent"></param> for existing childs with the name
        /// "Sphere". If a sphere marker is already present it will be destroyed.
        /// </summary>
        /// <param name="parent">the game object to be checked for existing sphere marker</param>
        /// <returns>true if marker exists</returns>
        private static bool deleteExistingSphere(GameObject parent)
        {
            for (int i = 0; i <= parent.transform.childCount - 1; i++)
            {
                if (parent.transform.GetChild(i).transform.name == "Sphere" && parent.transform.childCount > 0)
                {
                    GameObject sphere = parent.transform.GetChild(i).gameObject;
                    //FIXME: network operation for deleting an existing marker not working properly
                    //new DeleteNetAction(sphere.name).Execute();
                    Destroyer.DestroyGameObject(parent.transform.GetChild(i).gameObject);
                    return true;
                }
            }
            return false;
        }
    }
}