using SEE.GO;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for the scale-Node-process via network from one client to all others and to the server. 
    /// </summary>
    public class ScaleNodeNetAction : AbstractAction
    {
        /// <summary>
        /// The id of the gameObject which has to be scaled
        /// </summary>
        public string gameObjectID;

        /// <summary>
        /// The new Scale to bring over the network 
        /// </summary>
        public Vector3 scale;

        /// <summary>
        /// The new position to bring over the network
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Constructs a ScaleNodeNetAction
        /// </summary>
        /// <param name="GameObjectID">The id from the GameObject which should be scaled through the Network</param>
        /// <param name="Scale">The new scale of the GameObject</param>
        /// <param name="Positon">The new Postition of the GameObject</param>
        public ScaleNodeNetAction(string GameObjectID, Vector3 Scale, Vector3 Positon) : base()
        {
            gameObjectID = GameObjectID;
            scale = Scale;
            position = Positon;
        }

        /// <summary>
        /// Things to Execute on the Server (None for this Class)
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Things to Execute on the Client Sets finds the GameObject on the Client and sets its scale and position
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject scaleObj = GameObject.Find(gameObjectID);
                if (scaleObj != null)
                {
                    scaleObj.SetScale(scale);
                    scaleObj.transform.position = position;
                }
                else
                {
                    //FIXME: Control whether a Debug Log is the right thing
                    Debug.LogWarning("Found no gameObject: " + gameObjectID);
                }
            }

        }

    }
}