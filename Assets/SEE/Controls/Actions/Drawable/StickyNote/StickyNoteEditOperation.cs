using HighlightPlus;
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
    /// Handles selecting and editing a sticky note.
    /// </summary>
    internal sealed class StickyNoteEditOperation
    {
        /// <summary>
        /// The possible results of an edit update.
        /// </summary>
        internal enum Result
        {
            InProgress,
            Completed,
            Reset
        }

        /// <summary>
        /// Handles mouse-wheel transformations of the sticky note.
        /// </summary>
        private readonly StickyNoteTransformInteraction transformInteraction = new();

        /// <summary>
        /// The sticky note currently being edited.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// The original configuration of the sticky note.
        /// </summary>
        private DrawableConfig originalConfig;

        /// <summary>
        /// The changed configuration of the sticky note.
        /// </summary>
        private DrawableConfig changedConfig;

        /// <summary>
        /// Whether a sticky note is currently being edited.
        /// </summary>
        internal bool IsInProgress => stickyNote != null;

        /// <summary>
        /// Updates the current edit operation.
        /// </summary>
        /// <param name="original">The original sticky note configuration if editing was completed.</param>
        /// <param name="changed">The changed sticky note configuration if editing was completed.</param>
        /// <returns>The current result of the edit operation.</returns>
        internal Result Update(out DrawableConfig original, out DrawableConfig changed)
        {
            original = null;
            changed = null;

            if (SEEInput.LeftMouseDown()
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && StickyNoteSelection.IsSelectableObject(raycastHit.collider.gameObject))
            {
                GameObject surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);

                if (stickyNote != null)
                {
                    UpdateChangedTransform();

                    if (!ConfigsEqual(originalConfig, changedConfig))
                    {
                        return Complete(out original, out changed);
                    }

                    CloseEditingUI();
                }

                return SetChosenStickyNote(surface);
            }

            if (SetRotationAndScale())
            {
                return Complete(out original, out changed);
            }

            return Result.InProgress;
        }

        /// <summary>
        /// Selects the sticky note to edit or resets the edit operation.
        /// </summary>
        /// <param name="surface">The selected drawable surface.</param>
        /// <returns>The current result of the edit operation.</returns>
        private Result SetChosenStickyNote(GameObject surface)
        {
            GameObject selectedStickyNote = surface.transform.parent.gameObject;

            if (stickyNote == null || stickyNote != selectedStickyNote)
            {
                if (GameFinder.GetDrawableSurfaceParentName(surface).Contains(ValueHolder.StickyNotePrefix))
                {
                    stickyNote = selectedStickyNote;
                    stickyNote.EnableGlowOutline();

                    originalConfig = DrawableConfigManager.GetDrawableConfig(surface);
                    changedConfig = DrawableConfigManager.GetDrawableConfig(surface);

                    StickyNoteMenu.Instance.Destroy();
                    StickyNoteEditMenu.Instance.Enable(stickyNote, changedConfig);
                    return Result.InProgress;
                }

                if (stickyNote == null)
                {
                    ShowNotification.Warn("Wrong selection", "You did not select a sticky note.");
                    return Result.InProgress;
                }
            }

            StickyNoteMenu.Instance.Enable();
            ResetState();
            return Result.Reset;
        }

        /// <summary>
        /// Handles rotation and scaling through their menus and the mouse wheel.
        /// </summary>
        /// <returns>Whether editing was finished.</returns>
        private bool SetRotationAndScale()
        {
            bool finish = false;

            if (StickyNoteRotationMenu.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNote != null && StickyNoteRotationMenu.IsYActive())
            {
                transformInteraction.RotateByWheel(stickyNote.GetRootParent(), false);
            }

            if (ScaleMenu.Instance.TryGetFinish(out bool isScaleFinished))
            {
                finish = isScaleFinished;
            }
            else if (ScaleMenu.Instance.IsOpen())
            {
                ScaleByWheel();
            }

            return finish;
        }

        /// <summary>
        /// Scales the sticky note according to the current mouse-wheel interaction.
        /// </summary>
        private void ScaleByWheel()
        {
            if (!transformInteraction.TryGetScaleFactor(out float scaleFactor))
            {
                return;
            }

            changedConfig.Scale = GameScaler.Scale(stickyNote, scaleFactor);
            ScaleMenu.Instance.AssignValue(stickyNote);

            GameObject surface = GameFinder.GetDrawableSurface(stickyNote);
            new ScaleNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface), stickyNote.name,
                changedConfig.Scale).Execute();
        }

        /// <summary>
        /// Completes the current edit operation.
        /// </summary>
        /// <param name="original">The original sticky note configuration.</param>
        /// <param name="changed">The changed sticky note configuration.</param>
        /// <returns><see cref="Result.Completed"/>.</returns>
        private Result Complete(out DrawableConfig original, out DrawableConfig changed)
        {
            UpdateChangedTransform();
            CloseEditingUI();

            original = originalConfig;
            changed = changedConfig;

            ResetState();
            return Result.Completed;
        }

        /// <summary>
        /// Updates the changed configuration with the current transform.
        /// </summary>
        private void UpdateChangedTransform()
        {
            changedConfig.Scale = stickyNote.transform.localScale;
            changedConfig.Rotation = stickyNote.GetRootParent().transform.eulerAngles;
        }

        /// <summary>
        /// Cancels editing and restores the original configuration.
        /// </summary>
        internal void Cancel()
        {
            CloseEditingUI();

            if (stickyNote != null && originalConfig != null)
            {
                GameObject originalStickyNote = GameFinder.FindDrawableSurface(originalConfig.ID, originalConfig.ParentID)
                    .transform.parent.gameObject;

                GameStickyNoteEdit.Change(originalStickyNote, originalConfig);
                new StickyNoteChangeNetAction(originalConfig).Execute();
            }

            ResetState();
        }

        /// <summary>
        /// Closes the menus and removes the editing highlight.
        /// </summary>
        private void CloseEditingUI()
        {
            StickyNoteEditMenu.Instance.Destroy();
            StickyNoteRotationMenu.Destroy();
            ScaleMenu.Instance.Destroy();
            stickyNote?.Destroy<HighlightEffect>();
        }

        /// <summary>
        /// Checks whether both configurations contain the same editable values.
        /// </summary>
        private static bool ConfigsEqual(DrawableConfig original, DrawableConfig changed)
        {
            return original.Scale.Equals(changed.Scale) && original.Color.Equals(changed.Color)
                && original.Rotation.Equals(changed.Rotation) && original.Order.Equals(changed.Order)
                && original.Lighting.Equals(changed.Lighting);
        }

        /// <summary>
        /// Resets the state of this operation.
        /// </summary>
        private void ResetState()
        {
            stickyNote = null;
            originalConfig = null;
            changedConfig = null;
        }
    }
}
