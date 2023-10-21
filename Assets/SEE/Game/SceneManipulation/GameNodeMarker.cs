using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Scenemanipulation
{
    /// <summary>
    /// Creates or deletes sphere to mark certain nodes.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Creates a sphere above the selected node or deletes if a sphere exist.
        /// </summary>
        /// <param name="parent">selected node</param>
        /// <param name="scale">scale of the node</param>
        /// <returns></returns>
        public static GameObject CreateOrDeleteSphere(GameObject parent, Vector3 scale)
        {
            GameObject sphere = null;
            //Search for sphere
            foreach(Transform child in parent.transform)
            {
                if(child.name == "Sphere")
                {
                    sphere = child.gameObject;
                    break;
                }
            }
            //Delete sphere if one existed
            if(sphere != null)
            {
                Destroyer.Destroy(sphere);
                return null;
            }
            //Create and scale sphere
            else
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = GameObjectExtensions.GetTop(parent);
                sphere.transform.localScale = scale;
                sphere.transform.SetParent(parent.transform);
                sphere.SetColor(parent.GetColor().Darker());

                return sphere;
            }
        }
    }
}

