using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{

    /// <summary>
    /// This class is responsible for the edit-node-process via network from one client to all others and to the server. 
    /// </summary>
    public class EditNodeNetAction : AbstractAction
    {

        /// <summary>
        /// The new name of the node which has to be edited.
        /// </summary>
        public string scname;

        /// <summary>
        /// The new type of the node which has to be edited
        /// </summary>
        public string type;

        /// <summary>
        /// The id of the GameNode-Object which has to be edited.
        /// It can't be changed after the node-creation.
        /// </summary>
        public string gameObjectID;

        /// <summary>
        /// Constructs an EditNodeNetAction object
        /// </summary>
        /// <param name="SourceName">The new sourcename</param>
        /// <param name="Type">the new type</param>
        /// <param name="GameObjectID">the gameobject id which the node belongs to</param>
        public EditNodeNetAction(string sourceName, string type, string gameObjectID) : base()
        {
            // nodeToEdit = node;
            scname = sourceName;
            this.type = type;
            this.gameObjectID = gameObjectID;

        }
        /// <summary>
        /// Things to Execute on the Server (None for this Class)
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }
        /// <summary>
        /// Things to Execute on the Client Sets finds the GameObject on the Client and sets its parameter
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                Node node = GameObject.Find(gameObjectID).GetNode();
                if (node != null)
                {
                    node.SourceName = scname;
                    node.Type = type;

                }
                else
                {
                    //FIXME: Controll if a Debug Log is the right thing
                    Debug.LogWarning("Found no gameObject: " + gameObjectID);
                }
            }

        }

    }
}
