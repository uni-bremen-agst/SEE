using SEE.Game;
using SEE.Game.SceneManipulation;
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
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject gameObject = GraphElementIDMap.Find(GameObjectID, mustFindElement: true);
#pragma warning disable VSTHRD110
            GameElementDeleter.Delete(gameObject);
#pragma warning restore VSTHRD110
        }
    }
}
