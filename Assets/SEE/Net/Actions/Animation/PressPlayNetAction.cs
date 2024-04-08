using Assets.SEE.Net.Actions.Animation;
using SEE.Game.Evolution;
using SEE.GO;
using System;
using UnityEngine;

namespace SEE.Net.Actions.Animation
{
    public class PressPlayNetAction : AnimationNetAction
    {
        public PressPlayNetAction(string gameObjectID) : base(gameObjectID)
        {
        }

        protected override void Trigger(AnimationInteraction ai)
        {
            ai.PressPlay();
        }
    }
}
