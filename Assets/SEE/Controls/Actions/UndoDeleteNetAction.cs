using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    ///  This class implements undo for the <see cref="DeleteNetAction"/>.
    ///  It reverts the deletion by adding any deleted node or edge from a 
    ///  specific graph and uses the animated undo mechanism of the 
    ///  <see cref="DeletionAnimation"/> used as a coroutine in order to move 
    ///  the deleted nodes to their original position. 
    /// </summary>
    public class UndoDeleteNetAction : AbstractAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The unique ID of the gameObject of a node or edge that needs to be deleted.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The unique ID of a given root. Necessary for any client to find the specific graph, 
        /// a node or an edge is removed from.
        /// </summary>
        public String RootID;

        /// <summary>
        /// The client's graph a node or an edge has been removed from.
        /// </summary>
        public Graph Graph;

        /// <summary>
        /// Returns a new <see cref="UndoDeleteNetAction"/> instance.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a node or edge 
        /// which had been deleted before</param>
        /// <param name="rootID">the unique name of a graph's root</param>
        public UndoDeleteNetAction(string gameObjectID, String rootID) : base()
        {
            this.GameObjectID = gameObjectID;
            this.RootID = rootID;
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
        /// Undoes any deletion of a game object identified by <see cref="GameObjectID"/> on each client.
        /// Furthermore any node or edge which has been removed before is added again to the client´s graph.
        /// The graph is identified by <see cref="RootID"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {                
                GameObject rootNode = GameObject.Find(RootID);
                if (rootNode != null)
                {
                    Graph = rootNode.GetGraph();
                    if (Graph == null)
                    {
                        throw new Exception("Graph shall not be null.");
                    }
                }
                else
                {
                    throw new Exception($"There is no game object with the ID {RootID}.");
                }

                GameObject gameObject = GameObject.Find(GameObjectID);
                if (gameObject != null)
                {
                    if (gameObject.HasEdgeRef())
                    {
                        PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.RemoveNodeFromGarbage(new List<GameObject> {gameObject}));
                        if (gameObject.TryGetComponentOrLog(out EdgeRef edgeReference))
                        {
                            try
                            {
                                Graph.AddEdge(edgeReference.Value);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Edge cannot be added to the graph: {e}.\n");
                            }
                        }
                    }
                    else if (gameObject.HasNodeRef())
                    {
                        List<GameObject> removeFromGarbage = new List<GameObject>
                        {
                            gameObject
                        };
                        PlayerSettings.GetPlayerSettings().StartCoroutine(DeletionAnimation.RemoveNodeFromGarbage(removeFromGarbage));
                        Portal.SetInfinitePortal(gameObject);
                        Graph.AddNode(gameObject.GetNode());
                        Graph.FinalizeNodeHierarchy();
                    }
                }
                else
                {
                    throw new Exception($"There is no game object with the ID {GameObjectID}.");
                }
            }
        }
    }
}
