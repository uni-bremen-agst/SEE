using SEE.Controls.Actions;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// This class is responsible for deleting a node via network from one client to all others and 
    /// to the server. 
    /// </summary>
    public class DeleteNetAction : AbstractAction
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
        public DeleteNetAction(string gameObjectID)
        {
            this.GameObjectID = gameObjectID;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Deletes given GameObject on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {           
                DeleteAction.DeleteSelectedObject(GameObject.Find(GameObjectID));
            }
        }
    }
}
