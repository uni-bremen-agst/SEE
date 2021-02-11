using SEE.GO;
using SEE.Net;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// This class is responsible for the scaling nodes via network from one 
    /// client to all others and to the server. 
    /// </summary>
    public class ScaleNodeNetAction : AbstractAction
    {
        /// <summary>
        /// The id of the gameObject that has to be scaled.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The new scale to bring over the network.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The new position to transfer over the network.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Constructs a ScaleNodeNetAction
        /// </summary>
        /// <param name="GameObjectID">The id from the GameObject which should be scaled through the network</param>
        /// <param name="Scale">The new scale of the GameObject</param>
        /// <param name="Positon">The new position of the GameObject</param>
        public ScaleNodeNetAction(string GameObjectID, Vector3 Scale, Vector3 Positon) : base()
        {
            this.GameObjectID = GameObjectID;
            this.Scale = Scale;
            Position = Positon;
        }

        /// <summary>
        /// Things to execute on the server (none for this class).
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank
        }

        /// <summary>
        /// Finds the GameObject on the Client and sets its scale and position
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject scaleObj = GameObject.Find(GameObjectID);
                if (scaleObj != null)
                {
                    scaleObj.SetScale(Scale);
                    scaleObj.transform.position = Position;
                }
                else
                {
                    Debug.LogError($"Found no game object: {GameObjectID}.\n");
                }
            }
        }
    }
}