using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for the scaling nodes via network from one
    /// client to all others and to the server.
    /// </summary>
    public class ScaleNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the gameObject that has to be scaled.
        /// </summary>
        public string UniqueGameObjectName;

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
        /// <param name="uniqueGameObjectName">The unique name of the GameObject that should be scaled through the network</param>
        /// <param name="scale">The new scale of the GameObject</param>
        /// <param name="position">The new position of the GameObject</param>
        public ScaleNodeNetAction(string uniqueGameObjectName, Vector3 scale, Vector3 position) : base()
        {
            this.UniqueGameObjectName = uniqueGameObjectName;
            this.Scale = scale;
            Position = position;
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
                GameObject scaleObj = GameObject.Find(UniqueGameObjectName);
                if (scaleObj != null)
                {
                    scaleObj.SetScale(Scale);
                    scaleObj.transform.position = Position;
                }
                else
                {
                    Debug.LogError($"Found no game object: {UniqueGameObjectName}.\n");
                }
            }
        }
    }
}