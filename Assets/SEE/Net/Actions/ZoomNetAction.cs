using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Propagates the zooming of a game node through the network.
    /// </summary>
    internal class ZoomNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject of a node that needs to be moved.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Where the game object should be placed in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The local scale of the game object after the zooming.
        /// </summary>
        public Vector3 LocalScale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">the unique game-object name of the game object to be moved</param>
        /// <param name="position">the new position of the game object</param>
        /// <param name="localScale">the new local scale of the game object</param>
        public ZoomNetAction(string gameObjectID, Vector3 position, Vector3 localScale)
        {
            GameObjectID = gameObjectID;
            Position = position;
            LocalScale = localScale;
        }

        /// <summary>
        /// Zooming in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GraphElementIDMap.Find(GameObjectID);
                if (gameObject != null)
                {
                    Positioner.Set(gameObject.transform, Position, LocalScale);
                }
                else
                {
                    throw new System.Exception($"There is no game object with the ID {GameObjectID}.");
                }
            }
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
