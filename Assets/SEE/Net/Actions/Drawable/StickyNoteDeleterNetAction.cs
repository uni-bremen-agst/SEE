using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is reponsible for delete <see cref="StickyNoteAction"/> a sticky note on all clients.
    /// </summary>
    public class StickyNoteDeleterNetAction : DrawableNetAction
    {
        /// <summary>
        /// The sticky note that should be delete.
        /// </summary>
        public DrawableConfig Conf;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        public StickyNoteDeleterNetAction(DrawableConfig conf) : base(conf.ID, conf.ParentID)
        {
            this.Conf = conf;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
        /// <summary>
        /// Deletes the sticky note on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="StickyNoteID"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                base.ExecuteOnClient();
                Destroyer.Destroy(GameFinder.GetHighestParent(Surface));
            }
        }
    }
}