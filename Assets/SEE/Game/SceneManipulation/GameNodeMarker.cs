using System;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{

    public static class GameNodeMarker
    {
        public static GameObject NodeMarker(GameObject parent, Vector3 position, Vector3 worldSpaceScale)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Vector3 position = parent.transofrm.position;
            Vector3 worldSpaceScale = parent.transform.lossyScale;

            // Getter size and position of sphere.
            float coords = Math.Min(scale.x, scale.y);
            Vector3 size = new Vector3(coords, coords, coords);
            Vector3 position = new Vector3(position.x, position.y + scale.y, position.z);

            // Setter size and position of sphere.
            result.transform.localScale = size;
            result.transform.position = position;
            result.transform.SetParent(parent.gameObject.transform);
            result.GetComponent<Renderer>().material.color = Color.red;
            return result;
        }
    }
}
