using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameNodeMarker
{
    /// <summary>
    /// Creates and returns a sphere as a child of <paramref name="parent"/> with the given <paramref name="worldSpaceScale"/>.
    /// Precondition: <paramref name="parent"/> must have a valid node reference.
    /// </summary>
    /// <returns>new sphere or null if none could be created</returns>
    public static GameObject NodeMarker(GameObject parent)
    {
        // Initiate new GameObject
        GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Vector3 parentposition = parent.transform.position;
        Vector3 scale = parent.transform.lossyScale;

        // Get size and position for sphere
        float minxyparent = Math.Min(scale.x, scale.y);
        Vector3 size = new Vector3(minxyparent, minxyparent, minxyparent);
        Vector3 position = new Vector3(parentposition.x, parentposition.y + scale.y, parentposition.z);

        // Set size, position and parentobject for sphere
        result.transform.localScale = size;
        result.transform.position = position;
        result.transform.SetParent(parent.gameObject.transform);
        result.GetComponent<Renderer>().material.color = Color.white;
        return result;
    }
}