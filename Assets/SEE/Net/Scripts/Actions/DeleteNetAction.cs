using Assets.SEE.Game;
using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// This class contains the logic of the deleteAction script which is necessary for any client in order to delete a given
    /// gameObject.
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
        public DeleteNetAction(string gameObjectID) : base()
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
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {   
                GameObject gameObject = GameObject.Find(GameObjectID);
                if (gameObject != null)
                {
                    if (gameObject.HasNodeRef())
                    {
                        GameNodeAdder.Remove(gameObject);
                        Portal.SetInfinitePortal(gameObject);
                        PlayerSettings.GetPlayerSettings().StartCoroutine(AnimationsOfDeletion.MoveNodeToGarbage(gameObject.AllAncestors()));
                        Portal.SetInfinitePortal(gameObject);
                    }
                    else if (gameObject.HasEdgeRef())
                    {
                        if (gameObject.TryGetEdge(out Edge edge))
                        {
                            Graph graph = edge.ItsGraph;
                        }
                        AnimationsOfDeletion.HideEdges(gameObject);
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
