using SEE.Game;
using SEE.Game.SceneManipulation;
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
        public float AnimationFactor;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">The unique game-object name of the game object to be moved;
        /// must be known to <see cref="GraphElementIDMap"/>.</param>
        /// <param name="targetPosition">The new position of the game object in world space.</param>
        /// <param name="animationFactor">The factor by which the animation duration is multiplied.</param>
        public MoveNetAction(string gameObjectID, Vector3 targetPosition, float animationFactor)
        {
            GameObjectID = gameObjectID;
            TargetPosition = targetPosition;
            AnimationFactor = animationFactor;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMover.MoveTo(Find(GameObjectID), TargetPosition, AnimationFactor);
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
