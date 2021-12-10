using SEE.Game;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Propagates the movement of a game node through the network.
    /// </summary>
    internal class MoveNodeNetAction : AbstractNetAction
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
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">the unique game-object name of the game object to be moved</param>
        /// <param name="position">the new position of the game object</param>
        public MoveNodeNetAction(string gameObjectID, Vector3 position)
        {
            GameObjectID = gameObjectID;
            Position = position;
        }
        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject gameObject = GameObject.Find(GameObjectID);
                if (gameObject != null)
                {
                    Debug.Log($"[Net] Moving {gameObject.name} to {Position}.\n");
                    Positioner.Set(gameObject.transform, Position);
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
