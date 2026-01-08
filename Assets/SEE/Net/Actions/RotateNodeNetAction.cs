using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the rotation of a game node through the network.
    /// </summary>
    internal class RotateNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the game object that needs to be rotated.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The rotation of the game object.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Constructor.
        /// Assumption: The <paramref name="nodes"/> have their final rotation.
        /// These rotations will be broadcasted.
        /// </summary>
        /// <param name="id">The unique ID of the game object to be rotated.</param>
        /// <param name="rotation">The rotation by which to rotate the game object.</param>
        public RotateNodeNetAction(string id, Quaternion rotation)
        {
            GameObjectID = id;
            Rotation = rotation;
        }

        /// <summary>
        /// Rotation of node in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject gameObject = Find(GameObjectID);
            NodeOperator nodeOperator = gameObject.NodeOperator ();
            nodeOperator.RotateTo(Rotation, 0);
        }

        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
