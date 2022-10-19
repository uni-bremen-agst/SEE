using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for the edit-node process via network from one client
    /// to all others and to the server.
    /// </summary>
    public class EditNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The new name of the node that has to be edited.
        /// </summary>
        public string SourceName;

        /// <summary>
        /// The new type of the node that has to be edited
        /// </summary>
        public string NodeType;

        /// <summary>
        /// The unique name of the GameNode object that has to be edited.
        /// It cannot be changed after the node creation.
        /// </summary>
        public string NodeID;

        /// <summary>
        /// Constructs an EditNodeNetAction object.
        /// </summary>
        /// <param name="nodeID">the unique name of the gameobject the node belongs to</param>
        /// <param name="sourceName">The new source name</param>
        /// <param name="type">the new node type</param>
        public EditNodeNetAction(string nodeID, string sourceName, string type) : base()
        {
            this.SourceName = sourceName;
            this.NodeType = type;
            this.NodeID = nodeID;
        }

        /// <summary>
        /// Things to execute on the server (none for this class)
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Sets the attributes of the GameObject on the client side.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                Node node = GraphElementIDMap.Find(NodeID)?.GetNode();
                if (node != null)
                {
                    node.SourceName = SourceName;
                    node.Type = NodeType;
                }
                else
                {
                    Debug.LogError($"Found no gameObject: {NodeID}.\n");
                }
            }
        }
    }
}
