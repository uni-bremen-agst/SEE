using SEE.Game.Evolution;

namespace SEE.Net.Actions.Animation
{
    /// <summary>
    /// Applies <see cref="AnimationInteraction.PressPlay"/> on the 
    /// <see cref="AnimationInteraction"/> component.
    /// </summary>
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
