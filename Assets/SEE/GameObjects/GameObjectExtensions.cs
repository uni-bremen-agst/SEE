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

        /// <summary>
        /// Sets the color for this <paramref name="gameObject"/> to given <paramref name="color"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> has a renderer whose material has attribute _Color.
        /// </summary>
        /// <param name="gameObject">objects whose color is to be set</param>
        /// <param name="color">the new color to be set</param>
        public static void SetColor(this GameObject gameObject, Color color)
        {
            if (gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                Material material = renderer.sharedMaterial;
                material.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// Sets the start and end line color of <paramref name="gameObject"/>.
        /// 
        /// Precondition: <paramref name="gameObject"/> must have a line renderer.
        /// </summary>
        /// <param name="gameObject">object holding a line renderer whose start and end color is to be set</param>
        /// <param name="startColor">start color of the line</param>
        /// <param name="endColor">end color of the line</param>
        public static void SetLineColor(this GameObject gameObject, Color startColor, Color endColor)
        {
            if (gameObject.TryGetComponent<LineRenderer>(out LineRenderer renderer))
            {
                renderer.startColor = startColor;
                renderer.endColor = endColor;
            }
        }

        /// <summary>
        /// Sets the visibility of this <paramref name="gameObject"/> to <paramref name="show"/>.
        /// If <paramref name="show"/> is false, the object becomes invisible. If it is true
        /// instead, it becomes visible. Only the renderer of <paramref name="gameObject"/> 
        /// is turned on/off, which will not affect whether the <paramref name="gameObject"/>
        /// is active or inactive. If <paramref name="gameObject"/> has children, their
        /// renderers will not be changed. However, if <paramref name="gameObject"/>'s
        /// renderer is turned off, the children will neither be visible.
        /// 
        /// Precondition: <paramref name="gameObject"/> must have a Renderer.
        /// </summary>
        /// <param name="gameObject">object whose visibility is to be changed</param>
        /// <param name="show">whether or not to make the object visible</param>
        public static void SetVisibility(this GameObject gameObject, bool show)
        {
            if (gameObject.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.enabled = show;
            }
        }
    }
}