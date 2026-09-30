using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.StickyNote
{
    /// <summary>
    /// Handles selecting and deleting a sticky note.
    /// </summary>
    internal sealed class StickyNoteDeleteOperation
    {
        /// <summary>
        /// Tries to delete the sticky note selected by the user.
        /// </summary>
        /// <param name="deletedConfig">
        /// The configuration of the deleted sticky note if the operation completed.
        /// </param>
        /// <returns>Whether a sticky note was deleted.</returns>
        internal bool TryExecute(out DrawableConfig deletedConfig)
        {
            deletedConfig = null;

            if (!Input.GetMouseButtonDown(0)
                || !Raycasting.RaycastAnything(out RaycastHit raycastHit)
                || !StickyNoteSelection.IsSelectableObject(raycastHit.collider.gameObject))
            {
                return false;
            }

            GameObject surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);

            if (!GameFinder.GetDrawableSurfaceParentName(surface).Contains(ValueHolder.StickyNotePrefix))
            {
                ShowNotification.Warn("Wrong selection", "You did not select a sticky note.");

                return false;
            }

            deletedConfig = DrawableConfigManager.GetDrawableConfig(surface);

            new StickyNoteDeleterNetAction(deletedConfig).Execute();
            Destroyer.Destroy(surface.GetRootParent());

            return true;
        }
    }
}
