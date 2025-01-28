using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates a <see cref="DeleteAction"/> to all clients in the network.
    /// </summary>
    public class DeleteNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The unique name of the gameObject of a node or edge that needs to be deleted.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Creates a new DeleteNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a node or edge
        /// that has to be deleted</param>
        public DeleteNetAction(string gameObjectID) : base()
        {
            GameObjectID = gameObjectID;
        }

        /// <summary>
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject objToDelete = Find(GameObjectID);
            if (objToDelete.TryGetNode(out Node node) && node.IsRoot())
            {
                GameElementDeleter.DeleteRoot(objToDelete);
            }
            else
            {
#pragma warning disable VSTHRD110
                GameElementDeleter.Delete(objToDelete);
#pragma warning restore VSTHRD110
            }
        }
    }
}
