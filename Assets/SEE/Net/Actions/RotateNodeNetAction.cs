using SEE.Game;
using UnityEngine;

namespace SEE.Net
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

        public RotateNodeNetAction(string nodeID, Vector3 position, float yAngle) : base()
        {
            GameObjectID = nodeID;
            Position = position;
            YAngle = yAngle;
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(GameObjectID);
                if (gameObject != null)
                {
                    Debug.Log($"[Net] Rotating/moving {gameObject.name} to {Position} and rotation {YAngle}.\n");
                    Positioner.Set(gameObject.transform, position: Position, yAngle: YAngle);
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