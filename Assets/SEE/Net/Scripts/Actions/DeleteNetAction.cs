using Assets.SEE.Game;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.GO;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// This class is responsible for deleting a node or edge via network from one client to all others and 
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

        public GameObject garbageCan;

        /// <summary>
        /// Creates a new DeleteNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a node or edge 
        /// that has to be deleted</param>
        public DeleteNetAction(string gameObjectID)
        {
            this.GameObjectID = gameObjectID;
            garbageCan = GameObject.Find("garbageCan");
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
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(GameObjectID);
                DeleteAction del = new DeleteAction();
                if (gameObject != null)
                {
                    if (gameObject.HasNodeRef())
                    {
                        GameNodeAdder.Remove(gameObject);
                        PlayerSettings.GetPlayerSettings().StartCoroutine(AnimationsOfDeletion.MoveNodeToGarbage(gameObject.AllAncestors()));
                        del.MarkAsDeleted(gameObject.AllAncestors());
                        Portal.SetInfinitePortal(gameObject);
                    }
                    else if (gameObject.HasEdgeRef())
                    {
                        Debug.Log("hasedgeRef");
                        del.MarkAsDeleted(gameObject.AllAncestors());
                        GameEdgeAdder.Remove(gameObject);
                    }
                }
                else
                {
                    throw new System.Exception($"There is no game object with the ID {GameObjectID}");
                }
            }
        }
    }
}
