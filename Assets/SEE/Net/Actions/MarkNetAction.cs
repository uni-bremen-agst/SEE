using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for marking a node via network from one client to all others and to the server.
    /// </summary>
    public class MarkNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The ID of the node gameobject that should be marked.
        /// </summary>
        public string nodeID;

        /// <summary>
        /// The ID of the sphere.
        /// </summary>
        public string markID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeID">unqiue ID of the node that should be marked</param>
        /// <param name="markID">unique ID of the sphere</param>
        public MarkNetAction(string nodeID, string markID)
        {
            this.nodeID = nodeID;
            this.markID = markID;
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
        /// Marks a node on each client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject node = GraphElementIDMap.Find(nodeID);
                if (node == null)
                {
                    throw new System.Exception($"There is no node with the ID {nodeID}");
                }

                GameNodeMarker.MarkNode(node, markID);
            }
        }
    }
}