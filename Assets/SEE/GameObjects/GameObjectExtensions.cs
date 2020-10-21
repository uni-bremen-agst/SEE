using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Provides extensions for GameObjects retrieving their ID.
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
        /// Returns the render-queue offset of given <paramref name="gameObject"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> must have a renderer attached
        /// to it; otherwise 0 will be returned.
        /// </summary>
        /// <param name="gameObject">objects whose render-queue is requested</param>
        /// <returns>render-queue offset</returns>
        public static int GetRenderQueue(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                return renderer.sharedMaterial.renderQueue;
            }
            else
            {
                Debug.LogWarningFormat("GetRenderQueue: Game object {0} has no renderer.\n", gameObject.name);
                return 0;
            }
        }
    }
}