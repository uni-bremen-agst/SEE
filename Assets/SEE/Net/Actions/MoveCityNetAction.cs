using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    internal class MoveCityNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject of a node or edge that needs to be deleted.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Where the game object should be placed in world space.
        /// </summary>
        public Vector3 Position;

        public MoveCityNetAction(string gameObjectID, Vector3 position)
        {
            GameObjectID = gameObjectID;
            Position = position;
        }
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(GameObjectID);
                if (gameObject != null)
                {
                    CodeCityManipulator.Set(gameObject.transform, Position);
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
