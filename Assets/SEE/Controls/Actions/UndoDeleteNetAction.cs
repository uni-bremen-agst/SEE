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
    ///  This class processes the undo mechanism of the deleteNet script.
    ///  It reverts the deletion by adding any deleted node or edge from a specific graph and 
    ///  uses the animated undo mechanism of the animationsOfDeletion script used as a coroutine in order to move the deleted nodes to
    ///  their original position. 
    /// </summary>
    public class UndoDeleteNetAction : AbstractAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The unique ID of the gameObject of a node or edge that needs to be deleted.
        /// </summary>
        public string gameObjectID;

        /// <summary>
        /// The unique ID of a given root. Necessary for any client to find the specific graph, a node or an edge is removed from.
        /// </summary>
        public String rootID;

        /// <summary>
        /// The client´s graph a node or an edge had been removed from.
        /// </summary>
        public Graph graph;

        /// <summary>
        /// Creates a new UndoDeleteNetAction instance.
        /// </summary>
        /// <param name="gameObjectID"></param> the unique name of the gameObject of a node or edge 
        /// which had been deleted before
        /// <param name="rootID"></param> the unique name of a graph´s root.
        public UndoDeleteNetAction(string gameObjectID, String rootID) : base()
        {
            this.gameObjectID = gameObjectID;
            this.rootID = rootID;
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
        /// Undoes any deletion of a game object identified by <see cref="gameObjectID"/> on each client.
        /// Furthermore any node or edge which had been removed before is be added again to the clients graph.
        /// The graph is identified by <see cref="rootID"/>
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(gameObjectID);
                GameObject rootNode = GameObject.Find(rootID);
                if (rootNode != null)
                {
                    graph = rootNode.GetGraph();
                    Assert.IsNotNull(graph, "graph shall not be null");
                   
                }
                else
                {
                    throw new System.Exception($"There is no game object with the ID {rootID}");
                }

                if (gameObject != null)
                {
                    if (gameObject.HasEdgeRef())
                    {
                        PlayerSettings.GetPlayerSettings().StartCoroutine(AnimationsOfDeletion.DelayEdges(gameObject));
                        if (gameObject.TryGetComponentOrLog(out EdgeRef edgeReference))
                        {
                            try
                            {
                                graph.AddEdge(edgeReference.Value);
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarningFormat("Edge cannot be added to the graph \n", e);
                            }
                        }
                    }
                    if (gameObject.HasNodeRef())
                    {
                        List<GameObject> removeFromGarbage = new List<GameObject>();
                        removeFromGarbage.Add(gameObject);
                        PlayerSettings.GetPlayerSettings().StartCoroutine(AnimationsOfDeletion.RemoveNodeFromGarbage(removeFromGarbage));
                        Portal.SetInfinitePortal(gameObject);
                        Node node = gameObject.GetNode();
                        graph.AddNode(node);
                        graph.FinalizeNodeHierarchy();
                        node.ItsGraph = graph;
                    }
                }
                else
                {
                    throw new System.Exception($"There is no game object with the ID {gameObjectID}");
                }
            }
        }
    }
}
