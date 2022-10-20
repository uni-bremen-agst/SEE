using UnityEngine;

namespace SEE.Game
{
    public static class GameNodeMarker
    {
        public static GameObject CreateMarker(GameObject parent, Vector3 position, Vector3 worldSpaceScale)
        {
            // Create sphere gameobject
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Set diameter of sphere to 30% of highlighted block's x size
            float diameter = 0.3f * worldSpaceScale.x;
            gameObject.transform.localScale = new Vector3(diameter, diameter, diameter);
            // Move sphere to position of highlighted block
            float offset = (float)(position.y + worldSpaceScale.y / 2 + diameter / 2 + 0.01 * diameter);
            gameObject.transform.position = new Vector3(position.x, offset, position.z);
            gameObject.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            // Set highlighted block as parent gameobject of the sphere
            gameObject.transform.SetParent(parent.gameObject.transform);
            return gameObject;
        }
    }
}