using SEE.Extensions;
using UnityEngine;

namespace SEE.Net.Actions.GraphElement
{
    /// <summary>
    /// Propagates the movement/shuffling of a game node through the network.
    /// </summary>
    internal class ShuffleNetAction : NodeNetAction
    {
        /// <summary>
        /// Where the game object should be placed in world space.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameNodeID">The unique game-object name of the game object to be moved.</param>
        /// <param name="position">The new position of the game object.</param>
        public ShuffleNetAction(string gameNodeID, Vector3 position) : base(gameNodeID)
        {
            Position = position;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Find(GraphElementID).NodeOperator().MoveTo(Position, 0);
        }
    }
}
