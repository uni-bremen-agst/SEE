using SEE.Game.Evolution;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Net.Actions.Animation
{
    /// <summary>
    /// Common superclass for all network actions that trigger an <see cref="AnimationInteraction"/> component.
    /// </summary>
    public abstract class AnimationNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique full (hierarchical) name of the gameObject holding an <see cref="AnimationInteraction"/>
        /// component that needs to be triggered.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">The unique full (hierarchical) name of the gameObject holding an
        /// <see cref="AnimationInteraction"/> component that is to be triggered.</param>
        public AnimationNetAction(string gameObjectID) : base()
        {
            GameObjectID = gameObjectID;
        }

        /// <summary>
        /// Returns the <see cref="AnimationInteraction"/> component attached to the gameObject with the
        /// given <paramref name="gameObjectID"/>.
        ///
        /// The result is never null.
        /// </summary>
        /// <param name="gameObjectID">The unique full (hierarchical) name of the gameObject holding an
        /// <see cref="AnimationInteraction"/>component.</param>
        /// <returns>The <see cref="AnimationInteraction"/> component attached to the gameObject with the
        /// given <paramref name="gameObjectID"/>.</returns>
        /// <exception cref="Exception">Thrown if there is no game object with <paramref name="gameObjectID"/>
        /// or if it does not have an <see cref="AnimationInteraction"/>.</exception>
        protected static AnimationInteraction FindAnimationInteraction(string gameObjectID)
        {
            GameObject animationInteractionHolder
                = GameObject.Find(gameObjectID) ?? throw new Exception($"Could not find GameObject with ID {gameObjectID}.");
            if (animationInteractionHolder.TryGetComponentOrLog(out AnimationInteraction ai))
            {
                return ai;
            }
            throw new Exception($"GameObject with ID {gameObjectID} does not have an {nameof(AnimationInteraction)} component.");
        }

        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Runs <see cref="Trigger"/> on the <see cref="AnimationInteraction"/> component attached to the"
        /// game object with <see cref="GameObjectID"/> if this is note the requester.
        /// </summary>
        public override void ExecuteOnClient()
        {
            FindAnimationInteraction(GameObjectID).PressPlay();
        }

        /// <summary>
        /// The method to be called on the <see cref="AnimationInteraction"/> component, Must be implemented
        /// by subclasses.
        /// </summary>
        /// <param name="ai">The <see cref="AnimationInteraction"/> the trigger should be applied to.</param>
        protected abstract void Trigger(AnimationInteraction ai);
    }
}
