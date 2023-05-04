using SEE.Game;
using SEE.Game.Operator;
using SEE.GO;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the rotation of a game node through the network.
    /// </summary>
    internal class RotateNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject of a node that needs to be rotated.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Where the game object should be placed in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The rotation of the game object around the y axis in degrees.
        /// </summary>
        public float YAngle;

        public RotateNodeNetAction(string nodeID, Vector3 position, float yAngle)
        {
            GameObjectID = nodeID;
            Position = position;
            YAngle = yAngle;
        }

        /// <summary>
        /// Rotation of node in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GraphElementIDMap.Find(GameObjectID);
                if (gameObject != null)
                {
                    NodeOperator nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                    nodeOperator.RotateTo(Quaternion.Euler(0, YAngle, 0), 0);
                    nodeOperator.MoveTo(Position, 0);
                }
                else
                {
                    throw new System.Exception($"There is no game object with the ID {GameObjectID}.");
                }
            }
        }

        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}