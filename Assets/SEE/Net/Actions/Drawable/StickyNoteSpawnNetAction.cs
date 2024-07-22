using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for spawn <see cref="StickyNoteAction"/> a sticky note on all clients.
    /// </summary>
    public class StickyNoteSpawnNetAction : SurfaceNetAction
    {
        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteSpawnNetAction(DrawableConfig config) : base(config)
        {
        }

        /// <summary>
        /// Spawn the sticky note on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            if (Surface == null)
            {
                GameStickyNoteManager.Spawn(DrawableConf);
            }
        }
    }
}
