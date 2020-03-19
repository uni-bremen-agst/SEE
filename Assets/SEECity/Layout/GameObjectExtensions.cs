using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Provides extensions for GameObjects.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// An extension of GameObjects to retrieve their linknames. If <paramref name="gameObject"/>
        /// has a NodeRef attached to it, the corresponding node's linkname is returned.
        /// If <paramref name="gameObject"/> has an EdgeRef attached to it, the corresponding 
        /// edge's linkname is returned. Otherwise the name of <paramref name="gameObject"/> is
        /// returned.
        /// </summary>
        /// <returns>linkname for <paramref name="gameObject"/></returns>
        public static string LinkName(this GameObject gameObject)
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
                    return "edge '" + edgeRef.edge.LinkName + "'";
                }
            }
            else
            {
                return "node '" + nodeRef.node.LinkName + "'"; 
            }
        }
    }
}