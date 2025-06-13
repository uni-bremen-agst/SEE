using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates changed size and position of a single game node through the network.
    /// <para>
    /// Children are not scaled or moved along with the resized node.
    /// </para>
    /// </summary>
    public class ResizeNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the gameObject that has to be resized.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The new local scale to transfer over the network.
        /// </summary>
        public Vector3 LocalScale;

        /// <summary>
        /// The new world-space position to transfer over the network.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Should the edges be updated along with the targe node?
        /// </summary>
        public bool UpdateEdges;

        /// <summary>
        /// Should the child nodes keep their size?
        /// </summary>
        public bool ReparentChildren;

        /// <summary>
        /// Should the object layers be updated?
        /// </summary>
        public bool UpdateLayers;

        /// <summary>
        /// Constructs a <see cref="ResizeNodeNetAction"/>.
        /// </summary>
        /// <param name="gameObjectID">The unique name of the <see cref="GameObject"/> that should be resized</param>
        /// <param name="localScale">The new local scale of the <see cref="GameObject"/></param>
        /// <param name="position">The new world-space position of the <see cref="GameObject"/></param>
        /// <param name="reparentChildren">if <c>true</c>, the children are not moved and scaled along with their parent</param>
        /// <param name="updateEdges">if true, the connecting edges will be moved along with the node</param>
        /// <param name="updateLayers">if true, layers will be updated via <see cref="InteractableObject.UpdateLayer"/>.</param>
        public ResizeNodeNetAction(
            string gameObjectID,
            Vector3 localScale,
            Vector3 position,
            bool updateEdges = true,
            bool reparentChildren = true,
            bool updateLayers = false)
        {
            GameObjectID = gameObjectID;
            LocalScale = localScale;
            Position = position;
            UpdateEdges = updateEdges;
            ReparentChildren = reparentChildren;
            UpdateLayers = updateLayers;
        }

        /// <summary>
        /// Finds the GameObject on the client and sets its scale.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject go = Find(GameObjectID);
            if (go != null)
            {
                go.NodeOperator().ResizeTo(LocalScale, Position, factor: 1, UpdateEdges, ReparentChildren, UpdateLayers);
            }
            else
            {
                throw new System.Exception($"There is no game object with the ID {GameObjectID}.");
            }
        }
    }
}
