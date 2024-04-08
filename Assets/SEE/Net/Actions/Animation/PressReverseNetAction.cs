using SEE.Game.Evolution;

namespace SEE.Net.Actions.Animation
{
    /// <summary>
    /// Applies <see cref="AnimationInteraction.PressReverse"/> on the 
    /// <see cref="AnimationInteraction"/> component.
    /// </summary>
    public class PressReverseNetAction : AnimationNetAction
    {
        public PressReverseNetAction(string gameObjectID) : base(gameObjectID)
        {
        }

        protected override void Trigger(AnimationInteraction ai)
        {
            ai.PressReverse();
        }
    }
}
