using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Abstract super class of all actions dealing with a code-city object.
    /// </summary>
    public abstract class CityAction : MonoBehaviour // TODO(torben): is this very similar to SceneQueries.cs?
    {
        /// <summary>
        /// Returns first child of <paramref name="codeCity"/> tagged by Tags.Node. 
        /// If <paramref name="codeCity"/> is a node representing a code city,
        /// the result is considered the root of the graph.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>game object representing the root of the graph or null if there is none</returns>
        protected static Transform GetCityRootNode(GameObject codeCity)
        {
            foreach (Transform child in codeCity.transform)
            {
                if (child.tag == Tags.Node)
                {
                    return child.transform;
                }
            }
            return null;
        }

        /// <summary>
        /// Equivalent to: GetCityRootNode(gameObject).GetComponent<NodeRef>.node.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>the root node of the graph or null if there is none</returns>
        protected static Node GetCityRootGraphNode(GameObject codeCity)
        {
            Transform transform = GetCityRootNode(codeCity);
            if (transform == null)
            {
                return null;
            }
            else
            {
                NodeRef nodeRef = transform.GetComponent<NodeRef>();
                if (nodeRef == null)
                {
                    return null;
                }
                else
                {
                    return nodeRef.node;
                }
            }
        }

        /// <summary>
        /// Returns the graph of the root node of <paramref name="codeCity"/> assumed
        /// to represent a code city. Equivalent to: GetCityRootGraphNode(gameObject).ItsGraph.
        /// </summary>
        /// <param name="codeCity">object representing a code city</param>
        /// <returns>the graph represented by <paramref name="codeCity"/> or null</returns>
        protected static Graph GetGraph(GameObject codeCity)
        {
            Node root = GetCityRootGraphNode(codeCity);
            if (root == null)
            {
                return null;
            }
            else
            {
                return root.ItsGraph;
            }
        }

        /// <summary>
        /// Returns the first transform towards the root of the game-object hierarchy
        /// that is tagged by Tags.CodeCity. If none can be found, null is returned.
        /// 
        /// Precondition: The given <paramref name="transform"/> is part of a
        /// game-object tree and either this <paramref name="transform"/> (in which
        /// case <paramref name="transform"/> itself is returned) or any
        /// of its ascendants is tagged by Tags.CodeCity.
        /// </summary>
        /// <param name="transform">transform at which to start the search</param>
        /// <returns>first ascending transform tagged by Tags.CodeCity or null</returns>
        protected static Transform GetHitCity(Transform transform)
        {
            Transform cursor = transform;
            while (cursor != null)
            {
                if (cursor.tag == Tags.CodeCity)
                {
                    return cursor;
                }
                cursor = cursor.parent;
            }
            return cursor;
        }
    }
}