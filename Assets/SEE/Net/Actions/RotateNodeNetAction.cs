using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the rotation of a game node through the network.
    /// </summary>
    internal class RotateNodeNetAction : ConcurrentNetAction
    {

        /// <summary>
        /// The rotation of the game object.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Constructor.
        /// Assumption: The <paramref name="nodes"/> have their final rotation.
        /// These rotations will be broadcasted.
        /// </summary>
        /// <param name="id">the unique ID of the game object to be rotated</param>
        /// <param name="rotation">the rotation by which to rotate the game object</param>
        public RotateNodeNetAction(string gameObjectID, Quaternion rotation) : base(gameObjectID)
        {
            Rotation = rotation;
            UseObjectVersion(gameObjectID);
        }

        /// <summary>
        /// Rotation of node in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject gameObject = Find(GameObjectID);
            NodeOperator nodeOperator = gameObject.NodeOperator();
            nodeOperator.RotateTo(Rotation, 0);
            SetVersion();
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Undos the RotateNodeAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            GameObject gameObject = Find(GameObjectID);
            NodeOperator nodeOperator = gameObject.NodeOperator();
            nodeOperator.RotateTo(Quaternion.Inverse(Rotation), 0);
            RollbackNotification();
        }
    }
}
