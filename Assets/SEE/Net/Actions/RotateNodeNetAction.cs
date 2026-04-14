using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the rotation of a game node through the network.
    /// </summary>
    internal class RotateNodeNetAction : GraphElementNetAction
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
        /// <param name="id">The unique ID of the game object to be rotated.</param>
        /// <param name="rotation">The rotation by which to rotate the game object.</param>
        public RotateNodeNetAction(string id, Quaternion rotation) : base(id)
        {
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
    }
}
