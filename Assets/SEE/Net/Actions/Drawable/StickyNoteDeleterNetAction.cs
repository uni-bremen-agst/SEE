using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Utils;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for delete <see cref="StickyNoteAction"/> a sticky note on all clients.
    /// </summary>
    public class StickyNoteDeleterNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteDeleterNetAction(DrawableConfig conf) : base(conf)
        {
        }

        /// <summary>
        /// Deletes the sticky note on each client.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="StickyNoteID"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            Destroyer.Destroy(Surface.GetRootParent());
        }
    }
}
