using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.StickyNote;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Menu.Drawable.StickyNoteRotation;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.StickyNote
{
    /// <summary>
    /// Handles spawning and positioning a sticky note.
    /// </summary>
    internal sealed class StickyNoteSpawnOperation
    {
        /// <summary>
        /// Handles keyboard and mouse-wheel transformations of the sticky note.
        /// </summary>
        private readonly StickyNoteTransformInteraction transformInteraction = new();

        /// <summary>
        /// Whether spawning is currently in progress.
        /// </summary>
        private bool inProgress;

        /// <summary>
        /// Whether the sticky note placement is finished.
        /// </summary>
        private bool finish;

        /// <summary>
        /// The sticky note currently being spawned.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// Whether a sticky note is currently being spawned.
        /// </summary>
        internal bool IsInProgress => inProgress;

        /// <summary>
        /// Tries to execute the sticky note spawn operation.
        /// </summary>
        /// <param name="config">The resulting sticky note configuration.</param>
        /// <returns>Whether the spawn operation was completed.</returns>
        internal bool TryExecute(out DrawableConfig config)
        {
            config = null;

            SpawnOnPosition();
            SetPositionAndRotation();

            if (!finish)
            {
                return false;
            }

            config = DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawableSurface(stickyNote));
            new StickyNoteSpawnNetAction(config).Execute();
            ResetState();
            return true;
        }

        /// <summary>
        /// Selects the position for the sticky note and spawns it there.
        /// </summary>
        private void SpawnOnPosition()
        {
            if (!SEEInput.LeftMouseDown() || inProgress
                || !Raycasting.RaycastAnything(out RaycastHit raycastHit))
            {
                return;
            }

            inProgress = true;
            stickyNote = GameStickyNoteManager.Spawn(raycastHit);

            if (raycastHit.collider.gameObject.CompareTag(Tags.Drawable)
                || GameFinder.HasDrawableSurface(raycastHit.collider.gameObject)
                || GameFinder.IsPartOfADrawable(raycastHit.collider.gameObject)
                || ValueHolder.IsASuitableObjectForStickyNote(raycastHit.collider.gameObject))
            {
                finish = true;
                return;
            }

            StickyNoteMenu.Instance.Destroy();
            StickyNoteRotationMenu.Enable(stickyNote, raycastHit.collider.gameObject);
            StickyNoteMoveMenu.Instance.Enable(stickyNote.GetRootParent(), true);
        }

        /// <summary>
        /// Handles fine-tuning of the sticky note position and rotation.
        /// </summary>
        private void SetPositionAndRotation()
        {
            if (StickyNoteMoveMenu.Instance.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNote != null && StickyNoteMoveMenu.Instance.IsOpen())
            {
                transformInteraction.MoveByKey(stickyNote, false);
            }

            if (StickyNoteRotationMenu.TryGetFinish(out bool isRotationFinished))
            {
                finish = isRotationFinished;
            }
            else if (stickyNote != null && StickyNoteRotationMenu.IsYActive())
            {
                transformInteraction.RotateByWheel(stickyNote, false);
            }
        }

        /// <summary>
        /// Cancels the current spawn operation and removes the unfinished sticky note.
        /// </summary>
        internal void Cancel()
        {
            if (stickyNote != null)
            {
                Destroyer.Destroy(stickyNote);
            }

            ResetState();
        }

        /// <summary>
        /// Resets the state of this operation.
        /// </summary>
        private void ResetState()
        {
            stickyNote = null;
            inProgress = false;
            finish = false;
        }
    }
}
