using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for spawn <see cref="StickyNoteAction"/> a sticky note on all clients.
    /// </summary>
    public class StickyNoteSpawnNetAction : AbstractNetAction
    {
        /// <summary>
        /// The sticky note that should be spawn.
        /// </summary>
        public DrawableConfig DrawableConf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteSpawnNetAction(DrawableConfig config)
        {
            DrawableConf = config;
        }

        /// <summary>
        /// Spawn the sticky note on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            if (GameFinder.FindDrawableSurface(DrawableConf.ID, DrawableConf.ParentID) == null)
            {
                GameStickyNoteManager.Spawn(DrawableConf);
            }
        }
    }
}
