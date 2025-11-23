using SEE.Game;
using SEE.Game.SceneManipulation;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the (animated) movement of a game node through the network.
    /// </summary>
    internal class MoveNetAction : ConcurrentNetAction
    {

        /// <summary>
        /// Stores the old position for reversing MoveAction.
        /// </summary>
        public Vector3 OriginalPosition;

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
        /// <param name="gameObjectID">the unique game-object name of the game object to be moved;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="targetPosition">the new position of the game object in world space</param>
        /// <param name="animationFactor">the factor by which the animation duration is multiplied</param>
        public MoveNetAction(string gameObjectID, Vector3 originalPosition, Vector3 targetPosition, float animationFactor) : base(gameObjectID)
        {
            OriginalPosition = originalPosition;
            TargetPosition = targetPosition;
            AnimationFactor = animationFactor;
            UseObjectVersion(gameObjectID);
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameNodeMover.MoveTo(Find(GameObjectID), TargetPosition, AnimationFactor);
            SetVersion();
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Undos the MoveAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            GameNodeMover.MoveTo(Find(GameObjectID), OriginalPosition, AnimationFactor);
            RollbackNotification();
        }
    }
}
