using System;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    public static class GameNodeMarker
    {
        public static GameObject AddMarker(GameObject parent) {
                GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                result.GetComponent<Renderer>().material.color = Color.yellow;

                // minimum of the width (x axis) and depth (z axis) of the marked node for diameter of sphere
                float diameterSphere = Math.Min(parent.transform.lossyScale.x, parent.transform.lossyScale.z);
                result.transform.localScale = new Vector3(diameterSphere, diameterSphere, diameterSphere);
                // FIXME: Position needs some space between GetTop() and the Sphere because, Sphere Position is in the middle of it
                result.transform.position = new Vector3(parent.GetTop().x, parent.GetTop().y, parent.GetTop().z);

                result.transform.SetParent(parent.transform);
                Portal.SetPortal(parent, gameObject: result);
                return result;
        }

        // FIXME: Needs to call the NetAction too, to work for the client
        public static bool DeleteMarker(GameObject markedNode)
        {
            Destroyer.Destroy(markedNode);
            return true;
        }
    }
}