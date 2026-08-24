using SEE.DataModel.DG;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Updates the hierarchy of game nodes in a code city so that it is isomorphic
    /// to the node hierarchy.
    /// </summary>
    internal class GameNodeHierarchy
    {
        /// <summary>
        /// Updates the hierarchy of game nodes under <paramref name="codeCity"/> so that it is
        /// isomorphic to the node hierarchy of the underlying graph.
        /// </summary>
        /// <param name="codeCity">The game object representing the code city.</param>
        public static void Update(GameObject codeCity)
        {
            Dictionary<Node, GameObject> nodeMap = new();
            CollectNodes(codeCity, nodeMap);
            GraphRenderer.CreateGameNodeHierarchy(nodeMap, codeCity);

            /// <summary>
            /// Collects all graph nodes and their corresponding game nodes that are (transitive)
            /// descendants of <paramref name="root"/>. The result is added to <paramref name="nodeMap"/>,
            /// where the <paramref name="root"/> itself will not be added.
            /// </summary>
            /// <param name="root">Root of the game-node hierarchy whose hierarchy members are to be collected.</param>
            /// <param name="nodeMap">The mapping of graph nodes onto their corresponding game nodes.</param>
            /// <exception cref="Exception">Thrown if a game node has no valid node reference.</exception>
            static void CollectNodes(GameObject root, IDictionary<Node, GameObject> nodeMap)
            {
                if (root != null)
                {
                    foreach (Transform childTransform in root.transform)
                    {
                        GameObject child = childTransform.gameObject;
                        /// If a game node was deleted, it may have been marked inactive, but
                        /// not yet destroyed. We need to ignore such game nodes.
                        if (child.activeInHierarchy && child.CompareTag(Tags.Node))
                        {
                            if (child.TryGetNodeRef(out NodeRef nodeRef))
                            {
                                nodeMap[nodeRef.Value] = child;
                                CollectNodes(child, nodeMap);
                            }
                            else
                            {
                                throw new Exception($"Game node {child.name} without valid node reference.");
                            }
                        }
                    }
                }
            }
        }
    }
}
