using SEE.Game;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the movement/shuffling of a game node through the network.
    /// </summary>
    internal class ShuffleNetAction : AbstractNetAction
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
        public ShuffleNetAction(string gameObjectID, Vector3 position)
        {
            GameObjectID = gameObjectID;
            Position = position;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Find(GameObjectID).NodeOperator().MoveTo(Position, 0);
        }
    }
}
