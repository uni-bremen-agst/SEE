using SEE.Game.Evolution;

namespace SEE.Net.Actions.Animation
{
    /// <summary>
    /// Applies <see cref="AnimationInteraction.PressFastForward"/> on the 
    /// <see cref="AnimationInteraction"/> component.
    /// </summary>
    public class PressFastForwardNetAction : AnimationNetAction
    {
        public PressFastForwardNetAction(string gameObjectID) : base(gameObjectID)
        {
        }

        protected override void Trigger(AnimationInteraction ai)
        {
            ai.PressFastForward();
        }
    }
}
