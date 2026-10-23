using UnityEngine;
using SEE.Extensions;
using SEE.Utils;
namespace SEE.SceneManipulation
{
    public static class GameNodeMarker
    {
        private const string markerName = "Marker";

        public static bool IsMarked(GameObject node)
        {
            return node.transform.Find(markerName) != null;
        }

        public static void SetMarked(GameObject node, bool marked)
        {
            Transform existingMarker = node.transform.Find(markerName);
            if (!marked)
            {
                if (existingMarker != null)
                {
                    Destroyer.Destroy(existingMarker.gameObject);
                }
                return;
            }

            if (existingMarker != null) { return; }

            Bounds bounds = node.GetComponent<Renderer>().bounds;
            Vector3 size = bounds.size;
            Vector3 center = bounds.center;
            float diameter = Mathf.Min(size.x, size.z);

            Vector3 roof = node.GetTop();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = markerName;

            sphere.transform.position = new Vector3(center.x, roof.y + diameter / 2f, center.z);
            sphere.transform.localScale = Vector3.one * diameter;

            sphere.transform.SetParent(node.transform);

        }


    }

}
