using SEE.Game;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the (animated) movement of a game node through the network.
    /// </summary>
    internal class MoveNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique name of the gameObject that needs to be moved.
        /// Must be known to <see cref="GraphElementIDMap"/>.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Where the game object should be moved in world space.
        /// </summary>
        public Vector3 TargetPosition;

        /// <summary>
        /// The factor by which the movement animation duration is multiplied.
        /// </summary>
        public float Factor;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">the unique game-object name of the game object to be moved;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="targetPosition">the new position of the game object in world space</param>
        /// <param name="animationDuration">the factor by which the animation duration is multiplied</param>
        public MoveNetAction(string gameObjectID, Vector3 targetPosition, float factor)
        {
            GameObjectID = gameObjectID;
            TargetPosition = targetPosition;
            Factor = factor;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameNodeMover.MoveTo(Find(GameObjectID), TargetPosition, Factor);
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
