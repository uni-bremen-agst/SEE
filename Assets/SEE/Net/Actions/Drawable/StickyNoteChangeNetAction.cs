using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.StickyNote;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for change the sticky note values on all clients.
    /// </summary>
    public class StickyNoteChangeNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteChangeNetAction(DrawableConfig config) : base(config)
        {
        }

        /// <summary>
        /// Changes the values of the sticky note on each client.
        /// </summary>
        /// /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableConf"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (DrawableConf != null && Surface != null)
            {
                GameStickyNoteEdit.Change(Surface, DrawableConf);
            }
            else
            {
                throw new System.Exception($"There is no drawable with the ID {DrawableConf.ID}.");
            }
        }
    }
}
