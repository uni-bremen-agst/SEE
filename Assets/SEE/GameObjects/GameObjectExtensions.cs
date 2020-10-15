using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Provides extensions for GameObjects.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// An extension of GameObjects to retrieve their IDs. If <paramref name="gameObject"/>
        /// has a NodeRef attached to it, the corresponding node's ID is returned.
        /// If <paramref name="gameObject"/> has an EdgeRef attached to it, the corresponding 
        /// edge's ID is returned. Otherwise the name of <paramref name="gameObject"/> is
        /// returned.
        /// </summary>
        /// <returns>ID for <paramref name="gameObject"/></returns>
        public static string ID(this GameObject gameObject)
        {
            NodeRef nodeRef = gameObject.GetComponent<NodeRef>();
            if (nodeRef == null)
            {
                EdgeRef edgeRef = gameObject.GetComponent<EdgeRef>();
                if (edgeRef == null)
                {
                    return gameObject.name;
                }
                else
                {
                    return edgeRef.edge.ID;
                }
            }
            else
            {
                return nodeRef.node.ID;
            }
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>size of given <paramref name="gameObject"/></returns>
        public static Vector3 Size(this GameObject gameObject)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            return renderer.bounds.size;
        }
    }
}