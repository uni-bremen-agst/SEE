using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;




namespace SEE.Game
{

    public static class GameNodeMarker 
    {


        
        public static GameObject addSphere(GameObject parent, Vector3 position, Vector3 worldSpaceScale)
        {

            GameObject sphere;
            
            if(parent == null)
            {
                throw new Exception("GameObject must not be null.");
            }
            else if (deleteExistingSphere(parent))
            {
                return null;
            }
            else
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = new Vector3(0.2f * worldSpaceScale.x, 0.2f * worldSpaceScale.y, 0.2f * worldSpaceScale.z);
                sphere.transform.position = new Vector3(position.x, position.y + 0.1f, position.z);
                sphere.transform.SetParent(parent.gameObject.transform);
                sphere.GetComponent<Renderer>().material.color = Color.red;
            }

            return sphere;

        }



        private static bool deleteExistingSphere(GameObject parent)
        {
            for (int i = 0; i <= parent.transform.childCount - 1; i++)
            {
                if (parent.transform.GetChild(i).transform.name == "Sphere")
                {
                    Destroyer.DestroyGameObject(parent.transform.GetChild(i).gameObject);
                    return true;
                }
            }

            return false;
        }
    }












}