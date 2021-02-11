using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for the edit-node process via network from one client to all others and to the server. 
    /// </summary>
    public class EditNodeNetAction : AbstractAction
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
        /// The id of the GameNode object that has to be edited.
        /// It can't be changed after the node creation.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Constructs an EditNodeNetAction object.
        /// </summary>
        /// <param name="sourceName">The new source name</param>
        /// <param name="type">the new node type</param>
        /// <param name="gameObjectID">the gameobject id the node belongs to</param>
        public EditNodeNetAction(string sourceName, string type, string gameObjectID) : base()
        {
            SourceName = sourceName;
            this.NodeType = type;
            this.GameObjectID = gameObjectID;
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
                // FIXME: Are the game-object ids in Unity really synchronized across
                // the server and all clients?
                Node node = GameObject.Find(GameObjectID)?.GetNode();
                if (node != null)
                {
                    node.SourceName = SourceName;
                    node.Type = NodeType;
                }
                else
                {
                    Debug.LogError($"Found no gameObject: {GameObjectID}.\n");
                }
            }
        }
    }
}
