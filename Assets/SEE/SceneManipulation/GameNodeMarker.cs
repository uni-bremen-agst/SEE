using SEE.Extensions;
using SEE.Factories;
using SEE.Game;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Creates and removes the visual marker of a game node.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// Name  for the marker object.
        /// finding marker later.
        /// </summary>
        private const string MarkerName = "NodeMark";

        /// <summary>
        /// if given node is currently marked.
        /// </summary>
        /// <param name="gameNode">The game node to check.</param>
        /// <returns>True if the node has a marker.</returns>
        public static bool IsMarked(GameObject gameNode)
        {
            return FindMarker(gameNode) != null;
        }

        /// <summary>
        /// Sets the marking state of the given game node.
        /// </summary>
        /// <param name="gameNode">The node whose state should be changed.</param>
        /// <param name="shouldBeMarked">
        /// True -> create a marker, false -> remove it.
        /// </param>
        public static void ApplyMark(GameObject gameNode, bool shouldBeMarked)
        {
            GameObject marker = FindMarker(gameNode);

            if (shouldBeMarked)
            {
                // Do nothing if the node is already marked.
                if (marker == null)
                {
                    CreateMarker(gameNode);
                }
            }
            else
            {
                // Do nothing if there is no marker.
                if (marker != null)
                {
                    RemoveMarker(marker);
                }
            }
        }

        /// <summary>
        /// Creates the sphere above the game node.
        /// </summary>
        /// <param name="gameNode">node to mark.</param>
        private static void CreateMarker(GameObject gameNode)
        {
            // Create the sphere 
            GameObject marker =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            marker.name = MarkerName;
            marker.tag = Tags.Decoration;

            // Determine the size of the node in world space.
            Vector3 nodeSize = gameNode.WorldSpaceSize();

     
            float diameter = Mathf.Min(nodeSize.x, nodeSize.z);
            float radius = diameter / 2.0f;

          
            marker.transform.localScale =
                new Vector3(diameter, diameter, diameter);

            // GetTop() returns the center of the top of the node
            // in world coordinates.
            //
            // The sphere position represents its center.
           
            Vector3 top = gameNode.GetTop();
            marker.transform.position =
                top + Vector3.up * radius;

            
            Renderer markerRenderer = marker.GetComponent<Renderer>();

            markerRenderer.sharedMaterial = MaterialsFactory.New(
                MaterialsFactory.ShaderType.OpaqueMetallic,
                Color.yellow);

            // The marker is only visual and not interfering with selecting graph elements.
            Collider markerCollider = marker.GetComponent<Collider>();

            if (markerCollider != null)
            {
                markerCollider.enabled = false;
            }

            // Attach the marker to the node while keeping
            // its current world-space position.
            marker.transform.SetParent(gameNode.transform, true);

            // Make the marker inherit the portal information
            // of its game node.
            Portal.InheritPortal(gameNode, marker);
        }

        /// <summary>
        /// Removes an existing marker.
        /// </summary>
        /// <param name="marker">The marker to remove.</param>
        private static void RemoveMarker(GameObject marker)
        {
            // Detach it before destroying it.
            marker.transform.SetParent(null);

            Destroyer.Destroy(marker);
        }

        /// <summary>
        /// Finds the marker that belongs to the given node.
        /// </summary>
        /// <param name="gameNode">The node whose marker is searched.</param>
        /// <returns>The marker or null if the node is not marked.</returns>
        private static GameObject FindMarker(GameObject gameNode)
        {
            Transform markerTransform =
                gameNode.transform.Find(MarkerName);

            if (markerTransform == null)
            {
                return null;
            }

            return markerTransform.gameObject;
        }
    }
}