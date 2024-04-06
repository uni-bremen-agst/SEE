using SEE.Game.Evolution;

namespace SEE.Net.Actions.Animation
{
    /// <summary>
    /// Applies <see cref="AnimationInteraction.PressFastBackward"/> on the 
    /// <see cref="AnimationInteraction"/> component.
    /// </summary>
    public class PressFastBackwardNetAction : AnimationNetAction
    {
        public PressFastBackwardNetAction(string gameObjectID) : base(gameObjectID)
        {
        }

        protected override void Trigger(AnimationInteraction ai)
        {
            ai.PressFastBackward();
        }
    }
}