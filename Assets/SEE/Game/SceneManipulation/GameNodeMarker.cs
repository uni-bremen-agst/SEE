using System;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    public static class GameNodeMarker
    {
        public static GameObject AddMarker(GameObject parent) {

            // When there is no marker yet, one will be created
            if(parent.transform.Find("Sphere")==null)
            {
                Debug.Log("There's no marker yet!");
                GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                result.GetComponent<Renderer>().material.color = Color.yellow;

                // minimum of the width (x axis) and depth (z axis) of the marked node for diameter of sphere
                float diameterSphere = Math.Min(parent.transform.lossyScale.x, parent.transform.lossyScale.z);
                result.transform.localScale = new Vector3(diameterSphere, diameterSphere, diameterSphere);
                result.transform.position = new Vector3(parent.GetTop().x, parent.GetTop().y + (diameterSphere/2), parent.GetTop().z);

                result.transform.SetParent(parent.transform);
                Portal.SetPortal(parent, gameObject: result);
                return result;
            }
            // There's a marker yet, which will be destroyed
            else
            {
                Debug.Log("There's already a marker which will be destroyed");
                Destroyer.Destroy(parent.transform.Find("Sphere").gameObject);
                return null;
            }
        }
    }
}
