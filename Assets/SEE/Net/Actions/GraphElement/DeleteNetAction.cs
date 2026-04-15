using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// This class propagates a <see cref="DeleteAction"/> to all clients in the network.
    /// </summary>
    /// <remarks>This class works for both game nodes and game edges.</remarks>
    public class DeleteNetAction : GraphElementNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// Indicates whether unused node types should be removed.
        /// Only applicable for the clear variant.
        /// The clear variant refers to cleaning an architecture or implementation root node.
        /// Architecture and implementation subroot nodes must not be deleted, as they
        /// cannot be added again at runtime. If the deletion action is applied to one of
        /// these subroot nodes, only their child nodes are removed.
        /// The subroot node itself remains intact.
        /// </summary>
        public bool RemoveNodeTypes;

        /// <summary>
        /// Creates a new DeleteNetAction.
        /// </summary>
        /// <param name="gameObjectID">The unique name of the gameObject of a node or edge
        /// that has to be deleted.</param>
        /// <param name="removeNodeTypes">Indicates whether the node types should be removed.
        /// Only applicable for the clear variant.</param>
        public DeleteNetAction(string gameObjectID, bool removeNodeTypes = false) : base(gameObjectID)
        {
            RemoveNodeTypes = removeNodeTypes;
        }

        /// <summary>
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject objToDelete = Find(SourceGameNodeId);
            if (objToDelete.TryGetNode(out Node node) && node.IsRoot())
            {
                GameElementDeleter.DeleteRoot(objToDelete);
            }
            else
            {
#pragma warning disable VSTHRD110
                GameElementDeleter.Delete(objToDelete, RemoveNodeTypes);
#pragma warning restore VSTHRD110
            }
        }
    }
}
