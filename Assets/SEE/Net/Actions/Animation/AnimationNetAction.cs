using SEE.Game.Evolution;
using SEE.GO;
using SEE.Net.Actions;
using System;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Animation
{
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
        /// <param name="gameObjectID">the unique full (hierarchical) name of the gameObject holding an
        /// <see cref="AnimationInteraction"/> component that is to be triggered</param>
        public AnimationNetAction(string gameObjectID) : base()
        {
            GameObjectID = gameObjectID;
        }

        protected AnimationInteraction FindAnimationInteraction(string gameObjectID)
        {
            GameObject animationInteractionHolder 
                = GameObject.Find(gameObjectID) ?? throw new Exception($"Could not find GameObject with ID {gameObjectID}.");
            if (animationInteractionHolder.TryGetComponentOrLog(out AnimationInteraction ai))
            {
                return ai;
            }
            throw new Exception($"GameObject with ID {gameObjectID} does not have an {nameof(AnimationInteraction)} component.");
        }

        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                FindAnimationInteraction(GameObjectID).PressPlay();
            }
        }

        protected abstract void Trigger(AnimationInteraction ai);
    }
}