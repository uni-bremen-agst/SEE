using System;
using SEE.Extensions;
using SEE.Factories;
using SEE.Game;
using SEE.Utils;
using UnityEngine;

namespace SEE.SceneManipulation
{
    /// <summary>
    /// Puts a marker sphere above game nodes or removes it again, respectively.
    /// </summary>
    public static class GameNodeMarker
    {
        /// <summary>
        /// The name of the game object representing the marker sphere.
        /// </summary>
        public const string MarkerName = "MARK_SPHERE";

        /// <summary>
        /// The color of the marker sphere.
        /// </summary>
        public static readonly Color MarkerColor = Color.magenta;

        /// <summary>
        /// Returns true if <paramref name="gameNode"/> has a marker sphere.
        /// </summary>
        /// <param name="gameNode">The game node to check.</param>
        /// <returns>True if <paramref name="gameNode"/> is marked.</returns>
        public static bool IsMarked(GameObject gameNode)
        {
            return FindMarker(gameNode) != null;
        }

        /// <summary>
        /// Returns the marker sphere of <paramref name="gameNode"/>, or null if it has none.
        /// Only immediate children of <paramref name="gameNode"/> are considered.
        /// </summary>
        /// <param name="gameNode">The game node whose marker is requested.</param>
        /// <returns>The marker sphere or null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameNode"/> is null.</exception>
        public static GameObject FindMarker(GameObject gameNode)
        {
            if (gameNode == null)
            {
                throw new ArgumentNullException(nameof(gameNode));
            }
            foreach (Transform child in gameNode.transform)
            {
                if (child.CompareTag(Tags.Decoration) && child.name == MarkerName)
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Puts a marker sphere above <paramref name="gameNode"/>. If <paramref name="gameNode"/>
        /// is already marked, its existing marker is returned.
        /// </summary>
        /// <param name="gameNode">The game node to be marked.</param>
        /// <returns>The marker sphere.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameNode"/> is null.</exception>
        public static GameObject Mark(GameObject gameNode)
        {
            GameObject sphere = FindMarker(gameNode);
            if (sphere != null)
            {
                return sphere;
            }

            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = MarkerName;
            sphere.tag = Tags.Decoration;

            Collider collider = sphere.GetComponent<Collider>();
            // Destroy takes effect only at the end of the frame, so we disable the collider now.
            collider.enabled = false;
            Destroyer.Destroy(collider);

            Material material = MaterialsFactory.New(MaterialsFactory.ShaderType.OpaqueMetallic, MarkerColor);
            sphere.GetComponent<Renderer>().sharedMaterial = material;

            Vector3 nodeSize = gameNode.WorldSpaceSize();
            float diameter = Mathf.Min(nodeSize.x, nodeSize.z);
            Vector3 top = gameNode.GetTop();

            sphere.transform.SetParent(gameNode.transform);
            // The node's scale is not uniform; a local scale would distort the sphere.
            sphere.SetAbsoluteScale(Vector3.one * diameter, animate: false);
            sphere.transform.position = top + Vector3.up * diameter / 2;
            Portal.InheritPortal(from: gameNode, to: sphere);
            return sphere;
        }

        /// <summary>
        /// Removes the marker sphere of <paramref name="gameNode"/>, if there is one.
        /// </summary>
        /// <param name="gameNode">The game node to be unmarked.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="gameNode"/> is null.</exception>
        public static void Unmark(GameObject gameNode)
        {
            GameObject sphere = FindMarker(gameNode);
            if (sphere != null)
            {
                // Detach first, so that the node counts as unmarked before the end of the frame.
                sphere.transform.SetParent(null);
                Destroyer.Destroy(sphere.GetComponent<Renderer>().sharedMaterial);
                Destroyer.Destroy(sphere);
            }
        }
    }
}
