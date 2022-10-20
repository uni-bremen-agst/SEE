using System;
using System.Collections.Generic;
using Crosstales;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game
{
    public class GameNodeMarker
    {
        private static readonly Dictionary<string, GameObject> markedNodes = new Dictionary<string, GameObject>();

        /// <summary>
        /// Marks the node if not yet marked, and unmarks the node when already marked.
        /// Creates a sphere above the selected node, or destroys that sphere.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="markID">The </param>
        /// <returns>ID of the marking sphere.</returns>
        public static string MarkNode(GameObject node, string markID = null)
        {
            Debug.Log($"Is node marked: {markedNodes.ContainsKey(node.name)}");
            if (markedNodes.ContainsKey(node.name))
            {
                // unmarks the node
                GameObject sphere = markedNodes[node.name];
                markID = sphere.name;
                Destroyer.DestroyGameObject(gameObject: sphere);
                markedNodes.Remove(key: node.name);
            }
            else
            {
                // creates a new random unique ID if not specified
                markID = string.IsNullOrEmpty(markID) ? Guid.NewGuid().ToString() : markID;
                // adds the marking sphere
                CreateMarkingSphere(node: node, markID: markID);
            }

            return markID;
        }

        /// <summary>
        /// Creates a sphere above the selected node.
        /// The diameter of the sphere is the minimum of the width and depth of the marked node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="markID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static void CreateMarkingSphere(GameObject node, string markID)
        {
            SEECity city = node.ContainingCity() as SEECity;
            if (node == null)
            {
                throw new Exception("Node must not be null.");
            }
            else if (markID == null)
            {
                throw new Exception("markID must not be null.");
            }
            else if (city == null)
            {
                throw new Exception($"Node {node.name} ist not contained in a code city.");
            }

            // creates the marking sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = markID;

            // parents the sphere to the node
            sphere.transform.SetParent(node.transform);
            // FIXME: sphere position
            Vector3 lossyScale = node.transform.lossyScale / 2;
            sphere.transform.position = node.transform.position + new Vector3(lossyScale.x, lossyScale.y, lossyScale.z);
            // FIXME: sphere size
            Vector3 localScale = node.transform.localScale;
            float radius = Mathf.Min(localScale.x, localScale.z);
            sphere.transform.localScale = new Vector3(radius, radius, radius);

            sphere.SetColor(node.GetColor().Darker());

            // stores that marked node
            markedNodes[node.name] = sphere;
        }
    }
}