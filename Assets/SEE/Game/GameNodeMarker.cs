using System;
using UnityEngine;

/// <summary>
/// Creates a marker above a selected node
/// </summary>
namespace SEE.Game
{
    public static class GameNodeMarker
    {
        /// <summary>
        /// Creates a new marker above the selected node
        /// </summary>
        /// <param name="parent">The node upon which to create a marker</param>
        /// <returns>The marker gameObject</returns>
        public static GameObject CreateMarker(GameObject parent)
        {
            Vector3 position = parent.transform.position;
            Vector3 worldSpaceScale = parent.transform.lossyScale;
            // Create sphere gameobject
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Set diameter of sphere to 30% of highlighted block's x size
            float diameter = Math.Min(worldSpaceScale.x, worldSpaceScale.y);
            marker.transform.localScale = new Vector3(diameter, diameter, diameter);
            // Move sphere to position of highlighted block
            float offset = (float)(position.y + worldSpaceScale.y / 2 + diameter / 2 + 0.01 * diameter);
            marker.transform.position = new Vector3(position.x, offset, position.z);
            marker.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            // Set highlighted block as parent gameobject of the sphere
            marker.transform.SetParent(parent.gameObject.transform);
            return marker;
        }
    }
}