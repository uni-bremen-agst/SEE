using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    internal class RotateCityNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject of a node or edge that needs to be deleted.
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

        public RotateCityNetAction(string nodeID, Vector3 position, float yAngle) : base()
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
                    CodeCityManipulator.Set(gameObject.transform, position: Position, yAngle: YAngle);
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