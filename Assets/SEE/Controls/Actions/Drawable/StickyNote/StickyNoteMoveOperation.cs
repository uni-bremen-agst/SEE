using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.StickyNote;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Menu.Drawable.StickyNoteRotation;
using SEE.UI.Notification;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.StickyNote
{
    /// <summary>
    /// Handles selecting, moving, and fine-tuning the position and rotation of a sticky note.
    /// </summary>
    internal sealed class StickyNoteMoveOperation
    {
        /// <summary>
        /// Handles keyboard and mouse-wheel transformations of the sticky note.
        /// </summary>
        private readonly StickyNoteTransformInteraction transformInteraction = new();

        /// <summary>
        /// Whether moving is currently in progress.
        /// </summary>
        private bool inProgress;

        /// <summary>
        /// The sticky note currently being moved.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// The holder of the sticky note currently being moved.
        /// </summary>
        private GameObject stickyNoteHolder;

        /// <summary>
        /// Whether the mouse button was released after selecting the sticky note.
        /// </summary>
        private bool mouseWasReleased;

        /// <summary>
        /// Whether the move menu was opened for fine-tuning.
        /// </summary>
        private bool moveMenuOpened;

        /// <summary>
        /// The rotation before moving the sticky note.
        /// </summary>
        private Vector3 eulerAnglesBackup;

        /// <summary>
        /// The original configuration of the sticky note.
        /// </summary>
        private DrawableConfig originalConfig;

        /// <summary>
        /// The changed configuration of the sticky note.
        /// </summary>
        private DrawableConfig changedConfig;

        /// <summary>
        /// Whether a sticky note is currently being moved.
        /// </summary>
        internal bool IsInProgress => inProgress;

        /// <summary>
        /// Tries to execute the sticky note move operation.
        /// </summary>
        /// <param name="original">The original sticky note configuration.</param>
        /// <param name="changed">The changed sticky note configuration.</param>
        /// <returns>Whether the move operation was completed.</returns>
        internal bool TryExecute(out DrawableConfig original, out DrawableConfig changed)
        {
            original = null;
            changed = null;

            if (!MoveSelection())
            {
                return false;
            }

            if (inProgress && !mouseWasReleased && Input.GetMouseButtonUp(0))
            {
                mouseWasReleased = true;
            }

            MoveByMouse();

            if (!SetPositionAndRotation())
            {
                return false;
            }

            StickyNoteMoveMenu.Instance.Destroy();
            StickyNoteRotationMenu.Destroy();

            changedConfig.Position = stickyNoteHolder.transform.position;
            changedConfig.Rotation = stickyNoteHolder.transform.eulerAngles;

            EnableColliders();

            original = originalConfig;
            changed = changedConfig;

            ResetState();
            return true;
        }

        /// <summary>
        /// Selects a sticky note for moving and prepares it for mouse movement.
        /// </summary>
        /// <returns>Whether processing should continue.</returns>
        private bool MoveSelection()
        {
            if (SEEInput.LeftMouseDown()
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) && !inProgress
                && (raycastHit.collider.gameObject.CompareTag(Tags.Drawable)
                    || GameFinder.HasDrawableSurface(raycastHit.collider.gameObject)
                    || IsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);

                if (GameFinder.GetDrawableSurfaceParentName(surface).Contains(ValueHolder.StickyNotePrefix))
                {
                    inProgress = true;
                    originalConfig = DrawableConfigManager.GetDrawableConfig(surface);
                    changedConfig = DrawableConfigManager.GetDrawableConfig(surface);

                    StickyNoteMenu.Instance.Destroy();

                    surface.GetComponent<Collider>().enabled = false;
                    stickyNote = surface.transform.parent.gameObject;
                    stickyNote.transform.Find("Back").GetComponent<Collider>().enabled = false;
                    stickyNoteHolder = surface.GetRootParent();

                    foreach (Collider collider in stickyNoteHolder.GetComponentsInChildren<Collider>())
                    {
                        collider.enabled = false;
                    }

                    eulerAnglesBackup = stickyNoteHolder.transform.eulerAngles;
                }
                else
                {
                    ShowNotification.Warn("Wrong selection", "You did not select a sticky note.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Moves the sticky note according to the current mouse position.
        /// </summary>
        private void MoveByMouse()
        {
            if (!inProgress || !mouseWasReleased || moveMenuOpened || !Raycasting.RaycastAnything(out RaycastHit hit))
            {
                return;
            }

            Vector3 eulerAngles = eulerAnglesBackup;

            if (hit.collider.gameObject.CompareTag(Tags.Drawable)
                || GameFinder.HasDrawableSurface(hit.collider.gameObject)
                || GameFinder.IsPartOfADrawable(hit.collider.gameObject)
                || ValueHolder.IsASuitableObjectForStickyNote(hit.collider.gameObject))
            {
                if (DrawableType.Get(hit.collider.gameObject) == null)
                {
                    eulerAngles = hit.collider.gameObject.transform.eulerAngles;
                }
                else
                {
                    GameObject surface = GameFinder.GetDrawableSurface(hit.collider.gameObject);
                    eulerAngles = surface.transform.eulerAngles;
                }
            }

            Vector3 oldPosition = stickyNoteHolder.transform.position;

            if (GameFinder.HasDrawableSurface(hit.collider.gameObject) || IsPartOfStickyNote(hit.collider.gameObject))
            {
                GameObject surface = GameFinder.GetDrawableSurface(hit.collider.gameObject);
                hit.point = new Vector3(hit.point.x, hit.point.y, surface.transform.position.z);
            }

            GameStickyNoteTransform.Move(stickyNoteHolder, hit.point, eulerAngles);
            Vector3 newPosition = stickyNoteHolder.transform.position;

            if (oldPosition != newPosition)
            {
                newPosition = GameStickyNoteTransform.FinishMoving(stickyNoteHolder);
            }

            new StickyNoteMoveNetAction(GameFinder.GetDrawableSurface(stickyNote).name, stickyNote.name, newPosition, eulerAngles).Execute();

            if (SEEInput.MouseHold(MouseButton.Left))
            {
                GameFinder.GetDrawableSurface(stickyNoteHolder).GetComponent<Collider>().enabled = true;
                StickyNoteRotationMenu.Enable(stickyNoteHolder);
                StickyNoteMoveMenu.Instance.Enable(stickyNoteHolder);
                moveMenuOpened = true;
            }
        }

        /// <summary>
        /// Handles fine-tuning of the sticky note position and rotation.
        /// </summary>
        /// <returns>Whether fine-tuning is finished.</returns>
        private bool SetPositionAndRotation()
        {
            bool finish = false;

            if (StickyNoteMoveMenu.Instance.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNote != null && StickyNoteMoveMenu.Instance.IsOpen())
            {
                transformInteraction.MoveByKey(stickyNote, true);
            }

            if (StickyNoteRotationMenu.TryGetFinish(out bool isRotationFinished))
            {
                finish = isRotationFinished;
            }
            else if (stickyNote != null && StickyNoteRotationMenu.IsYActive())
            {
                transformInteraction.RotateByWheel(stickyNote, true);
            }

            return finish;
        }

        /// <summary>
        /// Cancels the current move operation and restores the original transform.
        /// </summary>
        internal void Cancel()
        {
            if (!inProgress)
            {
                ResetState();
                return;
            }

            EnableColliders();

            if (originalConfig != null)
            {
                GameObject stickyHolder = GameFinder.FindDrawableSurface(originalConfig.ID, originalConfig.ParentID).GetRootParent();
                GameStickyNoteTransform.Move(stickyHolder, originalConfig.Position, originalConfig.Rotation);

                GameObject surface = GameFinder.GetDrawableSurface(stickyHolder);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                new StickyNoteMoveNetAction(surface.name, surfaceParentName, originalConfig.Position, originalConfig.Rotation).Execute();
            }

            ResetState();
        }

        /// <summary>
        /// Enables the colliders of the sticky note after moving.
        /// </summary>
        private void EnableColliders()
        {
            if (stickyNote == null)
            {
                return;
            }

            stickyNote.transform.Find("Back").GetComponent<Collider>().enabled = true;

            foreach (Collider collider in stickyNote.GetRootParent().GetComponentsInChildren<Collider>())
            {
                collider.enabled = true;
            }
        }

        /// <summary>
        /// Checks whether the selected object is part of a sticky note.
        /// </summary>
        /// <param name="selectedObject">The object to check.</param>
        /// <returns>Whether the object is part of a sticky note.</returns>
        private static bool IsPartOfStickyNote(GameObject selectedObject)
        {
            return selectedObject.transform.parent != null
                && selectedObject.transform.parent.name.StartsWith(ValueHolder.StickyNotePrefix);
        }

        /// <summary>
        /// Resets the state of this operation.
        /// </summary>
        private void ResetState()
        {
            inProgress = false;
            stickyNote = null;
            stickyNoteHolder = null;
            mouseWasReleased = false;
            moveMenuOpened = false;
            eulerAnglesBackup = default;
            originalConfig = null;
            changedConfig = null;
        }
    }
}
