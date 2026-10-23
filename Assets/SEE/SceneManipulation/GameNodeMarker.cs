using UnityEngine;

using SEE.Extensions;
using SEE.Factories;
using SEE.Game;
using SEE.Utils;

namespace SEE.SceneManipulation
{
/// <summary>
/// Provides functionality for visually marking game nodes.
/// </summary>

internal static class GameNodeMarker
{
    private const string MarkerName = "MarkingSphere";

    internal static bool IsMarked(GameObject gameNode)
    {
        return FindMarker(gameNode) != null;
    }

    internal static void SetMarked(GameObject gameNode, bool marked)
    {
        bool isMarked = IsMarked(gameNode);

        if (marked && !isMarked)
        {
            CreateMarker(gameNode);
        }
        else if (!marked && isMarked)
        {
            RemoveMarker(gameNode);
        }
    }

    private static void CreateMarker(GameObject gameNode)
    {
        gameNode.WorldSpaceSize(out Vector3 size, out Vector3 center);

        float diameter = Mathf.Min(size.x, size.z);
        float radius = diameter / 2.0f;
        float roof = gameNode.GetRoof();

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        marker.name = MarkerName;
        marker.tag = Tags.Decoration;

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.sharedMaterial = MaterialsFactory.New(
            MaterialsFactory.ShaderType.OpaqueMetallic,
            Color.red);

        SphereCollider collider = marker.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        marker.transform.localScale = new(diameter, diameter, diameter);
        marker.transform.position = new(center.x, roof + radius, center.z);

        marker.transform.SetParent(gameNode.transform, true);

        Portal.InheritPortal(gameNode, marker);
    }

    private static void RemoveMarker(GameObject gameNode)
    {
        Transform marker = FindMarker(gameNode);

        if (marker != null)
        {
            marker.gameObject.SetActive(false);
            marker.SetParent(null);
            Destroyer.Destroy(marker.gameObject);
        }
    }

    private static Transform FindMarker(GameObject gameNode)
    {
        return gameNode.transform.Find(MarkerName);
    }

}
}