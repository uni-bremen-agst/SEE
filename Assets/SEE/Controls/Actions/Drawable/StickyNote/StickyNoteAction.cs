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
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.StickyNote
{
    /// <summary>
    /// This class provides all operations for sticky notes.
    /// </summary>
    public class StickyNoteAction : DrawableAction
    {
        /// <summary>
        /// The selected operation for sticky notes.
        /// </summary>
        private Operation selectedAction = Operation.None;

        /// <summary>
        /// The different operations for sticky notes.
        /// </summary>
        public enum Operation
        {
            None,
            Spawn,
            Move,
            Edit,
            Delete
        }

        /// <summary>
        /// True if the operation is finished.
        /// </summary>
        private bool finish = false;

        /// <summary>
        /// Sticky note object.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// Sticky note holder, can also be the sticky note itself.
        /// </summary>
        private GameObject stickyNoteHolder;

        /// <summary>
        /// Handles deleting sticky notes.
        /// </summary>
        private readonly StickyNoteDeleteOperation deleteOperation = new();

        /// <summary>
        /// Handles keyboard and mouse-wheel transformations of sticky notes.
        /// </summary>
        private readonly StickyNoteTransformInteraction transformInteraction = new();

        /// <summary>
        /// Handles spawning sticky notes.
        /// </summary>
        private readonly StickyNoteSpawnOperation spawnOperation = new();

        /// <summary>
        /// Handles moving sticky notes.
        /// </summary>
        private readonly StickyNoteMoveOperation moveOperation = new();

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="StickyNoteAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The original configuration of the sticky note.
            /// </summary>
            public DrawableConfig OriginalConfig;

            /// <summary>
            /// The changed configuration of the sticky note.
            /// </summary>
            public DrawableConfig ChangedConfig;

            /// <summary>
            /// The operation that was executed.
            /// </summary>
            public readonly Operation Action;
            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="originalConfig">The original configuration of the sticky note.</param>
            /// <param name="action">The executed operation.</param>
            public Memento(DrawableConfig originalConfig, Operation action)
            {
                OriginalConfig = originalConfig;
                Action = action;
                ChangedConfig = null;
            }
        }

        /// <summary>
        /// Enables the sticky note menu with which an operation can be selected.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            StickyNoteMenu.Instance.Enable();
        }

        /// <summary>
        /// Destroys the menu and resets the action if it's not yet finished.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            StickyNoteMenu.Instance.Destroy();
            StickyNoteRotationMenu.Destroy();
            StickyNoteEditMenu.Instance.Destroy();
            StickyNoteMoveMenu.Instance.Destroy();
            ScaleMenu.Instance.Destroy();
            stickyNote?.Destroy<HighlightEffect>();

            if (!finish)
            {
                if (spawnOperation.IsInProgress)
                {
                    spawnOperation.Cancel();
                }

                if (moveOperation.IsInProgress)
                {
                    moveOperation.Cancel();
                }

                switch (memento.Action)
                {
                    case Operation.Edit:
                        GameObject sticky = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                            memento.OriginalConfig.ParentID)
                            .transform.parent.gameObject;
                        GameStickyNoteEdit.Change(sticky, memento.OriginalConfig);
                        new StickyNoteChangeNetAction(memento.OriginalConfig).Execute();
                        break;
                }
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.StickyNote"/>.
        /// After the user selects an operation, the corresponding method will be called.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            Cancel();
            if (!Raycasting.IsMouseOverGUI())
            {
                if (StickyNoteMenu.TryGetOperation(out Operation op))
                {
                    selectedAction = op;
                }
                switch (selectedAction)
                {
                    case Operation.None:
                        if (Input.GetMouseButtonDown(0))
                        {
                            ShowNotification.Info("Select an operation",
                                "First you need to select an operation from the menu.");
                        }
                        break;
                    case Operation.Spawn:
                        return Spawn();
                    case Operation.Move:
                        return Move();
                    case Operation.Edit:
                        return Edit();
                    case Operation.Delete:
                        return Delete();
                }
            }
            return false;
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if ((stickyNote != null || stickyNoteHolder != null
                    || spawnOperation.IsInProgress || moveOperation.IsInProgress)
                && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                StickyNoteRotationMenu.Destroy();
                StickyNoteEditMenu.Instance.Destroy();
                StickyNoteMoveMenu.Instance.Destroy();
                ScaleMenu.Instance.Destroy();
                stickyNote?.Destroy<HighlightEffect>();

                if (!finish)
                {
                    switch (selectedAction)
                    {
                        case Operation.Spawn:
                            spawnOperation.Cancel();
                            break;
                        case Operation.Move:
                            moveOperation.Cancel();
                            break;
                        case Operation.Edit:
                            GameObject sticky = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                                memento.OriginalConfig.ParentID)
                                .transform.parent.gameObject;
                            GameStickyNoteEdit.Change(sticky, memento.OriginalConfig);
                            new StickyNoteChangeNetAction(memento.OriginalConfig).Execute();
                            break;
                    }
                }
                StickyNoteMenu.Instance.Enable();
                stickyNote = null;
                stickyNoteHolder = null;
                selectedAction = Operation.None;
            }
        }

        /// <summary>
        /// Spawns a sticky note.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        private bool Spawn()
        {
            if (!spawnOperation.TryExecute(out DrawableConfig config))
            {
                return false;
            }

            memento = new Memento(config, selectedAction);
            finish = true;
            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Moves a sticky note.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        private bool Move()
        {
            if (!moveOperation.TryExecute(out DrawableConfig originalConfig, out DrawableConfig changedConfig))
            {
                return false;
            }

            memento = new Memento(originalConfig, selectedAction)
            {
                ChangedConfig = changedConfig
            };

            finish = true;
            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// With this operation the sticky note can be edited.
        /// It provides options to change the color, order in layer, scale and rotation.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        private bool Edit()
        {
            /// This block provides the selection for editing and handles the opening of the needed menus.
            switch (EditSelection())
            {
                case EditReturnState.False:
                    return false;
                case EditReturnState.True:
                    return true;
                case EditReturnState.None:
                    break;
            }

            /// Block for editing the rotation or scale.
            SetRotationAndScale();

            /// When the editing is finished, complete the current state
            /// and save the scale and rotation in memento, because they could be
            /// changed with the menu.
            if (finish)
            {
                memento.ChangedConfig.Scale = stickyNote.transform.localScale;
                memento.ChangedConfig.Rotation = stickyNote.GetRootParent().transform.eulerAngles;
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The return states of the edit selection.
        /// </summary>
        private enum EditReturnState
        {
            False,
            True,
            None
        }

        /// <summary>
        /// Selects a sticky note to be edited.
        /// </summary>
        /// <returns>The edit return state.
        /// None, if nothing should be returned.
        /// True, if the update method should return true.
        /// False, if the update method should return false.</returns>
        private EditReturnState EditSelection()
        {
            if (SEEInput.LeftMouseDown()
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (raycastHit.collider.gameObject.CompareTag(Tags.Drawable)
                    || GameFinder.HasDrawableSurface(raycastHit.collider.gameObject)
                    || CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);

                /// Checks if changes have been made when a sticky note was already selected.
                if (CheckChanges() == EditReturnState.True)
                {
                    return EditReturnState.True;
                }

                if (SetChosenStickyNote(surface) != EditReturnState.False)
                {
                    return EditReturnState.False;
                }
            }
            return EditReturnState.None;
        }

        /// <summary>
        /// Checks if changes have been made when a sticky note was already selected.
        /// </summary>
        /// <returns>True, if changes were made, otherwise None.</returns>
        private EditReturnState CheckChanges()
        {
            if (stickyNote != null)
            {
                StickyNoteEditMenu.Instance.Destroy();
                StickyNoteRotationMenu.Destroy();
                ScaleMenu.Instance.Destroy();
                stickyNote.Destroy<HighlightEffect>();
                memento.ChangedConfig.Scale = stickyNote.transform.localScale;
                memento.ChangedConfig.Rotation = stickyNote.GetRootParent().transform.eulerAngles;

                /// If there are changed in the configuration finish this operation.
                if (!CheckEquals(memento.OriginalConfig, memento.ChangedConfig))
                {
                    finish = true;
                    CurrentState = IReversibleAction.Progress.Completed;
                    return EditReturnState.True;
                }
            }
            return EditReturnState.None;
        }

        /// <summary>
        /// Checks if a sticky note has already been set.
        /// If not or if a different one than previously chosen is selected, the sticky note is highlighted,
        /// and the edit menu is opened.
        /// If an attempt is made to select a non-sticky note, an appropriate message is displayed.
        /// If a sticky note was already selected and then a non-sticky note is chosen,
        /// the action is canceled and reset.
        /// This case can only occur if no changes have been made because if changes were present,
        /// the action would have already been completed in the <see cref="CheckChanges"/> section.
        /// If the same sticky note as the previous time is chosen, it is checked if changes are present.
        /// If yes, finish is initiated; otherwise, the action is reset.
        /// </summary>
        /// <param name="surface">The drawable surface of the selected sticky note.</param>
        /// <returns>The current edit return state:
        /// None, if nothing should be returned.
        /// True, if the update method should return true.
        /// False, if the update method should retrun false.</returns>
        private EditReturnState SetChosenStickyNote(GameObject surface)
        {
            /// This block is executed when no sticky note has been selected yet
            /// or when the newly selected sticky note is different from the previous one.
            if (stickyNote == null || stickyNote != surface.transform.parent.gameObject)
            {
                /// To edit the sticky note, it is required that a sticky note was selected.
                if (GameFinder.GetDrawableSurfaceParentName(surface).Contains(ValueHolder.StickyNotePrefix))
                {
                    stickyNote = surface.transform.parent.gameObject;
                    stickyNote.EnableGlowOutline();

                    memento = new(DrawableConfigManager.GetDrawableConfig(surface), selectedAction)
                    {
                        ChangedConfig = DrawableConfigManager.GetDrawableConfig(surface)
                    };
                    StickyNoteMenu.Instance.Destroy();
                    StickyNoteEditMenu.Instance.Enable(surface.transform.parent.gameObject, memento.ChangedConfig);
                }
                else
                {
                    if (stickyNote == null)
                    {
                        ShowNotification.Warn("Wrong selection", "You don't select a sticky note.");
                        return EditReturnState.False;
                    }
                    else
                    {
                        stickyNote = null;
                        selectedAction = Operation.None;
                        StickyNoteMenu.Instance.Enable();
                    }
                }
            }
            else /// Will be executed if the newly selected sticky note is the same as the previous one.
            {
                memento.ChangedConfig.Scale = stickyNote.transform.localScale;
                memento.ChangedConfig.Rotation = stickyNote.GetRootParent().transform.eulerAngles;

                if (!CheckEquals(memento.OriginalConfig, memento.ChangedConfig))
                {
                    finish = true;
                    return EditReturnState.False;
                }
                else
                {
                    stickyNote = null;
                    selectedAction = Operation.None;
                    StickyNoteMenu.Instance.Enable();
                }
            }
            return EditReturnState.None;
        }

        /// <summary>
        /// It waits for either the rotation menu or the scale menu to provide a finish.
        /// Additionally, as long as no finish is received from the menus,
        /// rotating and scale via mouse wheel are provided.
        /// Only one menu can be open at a time, so the mouse wheel can be used for both menus.
        /// </summary>
        private void SetRotationAndScale()
        {
            if (StickyNoteRotationMenu.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNote != null && StickyNoteRotationMenu.IsYActive())
            {
                stickyNoteHolder = stickyNote.GetRootParent();
                transformInteraction.RotateByWheel(stickyNoteHolder, false);
            }

            if (ScaleMenu.Instance.TryGetFinish(out bool isScaleFinished))
            {
                finish = isScaleFinished;
            }
            else if (ScaleMenu.Instance.IsOpen())
            {
                ScaleByWheel();
            }
        }

        /// <summary>
        /// Enables scaling via the mouse wheel, as well as in the <see cref="ScaleAction"/>.
        /// </summary>
        private void ScaleByWheel()
        {
            if (transformInteraction.TryGetScaleFactor(out float scaleFactor))
            {
                memento.ChangedConfig.Scale = GameScaler.Scale(stickyNote, scaleFactor);
                ScaleMenu.Instance.AssignValue(stickyNote);
                GameObject surface = GameFinder.GetDrawableSurface(stickyNote);
                new ScaleNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface), stickyNote.name,
                    memento.ChangedConfig.Scale).Execute();
            }
        }

        /// <summary>
        /// Deletes the selected sticky note.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        private bool Delete()
        {
            if (!deleteOperation.TryExecute(out DrawableConfig deletedConfig))
            {
                return false;
            }

            memento = new Memento(deletedConfig, selectedAction);
            CurrentState = IReversibleAction.Progress.Completed;

            return true;
        }

        /// <summary>
        /// Checks if the values of the two given configurations are the same.
        /// </summary>
        /// <param name="original">The original configuration.</param>
        /// <param name="changed">The changed configuration.</param>
        /// <returns>.</returns>
        private bool CheckEquals(DrawableConfig original, DrawableConfig changed)
        {
            return original.Scale.Equals(changed.Scale) && original.Color.Equals(changed.Color)
                && original.Rotation.Equals(changed.Rotation) && original.Order.Equals(changed.Order)
                && original.Lighting.Equals(changed.Lighting);
        }

        /// <summary>
        /// Checks if the selected object is part of a sticky note.
        /// </summary>
        /// <param name="selectedObject">"The object to be checked.</param>
        /// <returns>True, if the object parent is the sticky note; otherwise false.</returns>
        private bool CheckIsPartOfStickyNote(GameObject selectedObject)
        {
            if (selectedObject.transform.parent != null)
            {
                return selectedObject.transform.parent.name.StartsWith(ValueHolder.StickyNotePrefix);
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., it spawn/delete the sticky note or change to old position / values.
        /// </summary>
        public override void Undo()
        {
            switch (memento.Action)
            {
                case Operation.Spawn:
                    GameObject toDeleteSurface = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                        memento.OriginalConfig.ParentID);
                    new StickyNoteDeleterNetAction(DrawableConfigManager.GetDrawableConfig(toDeleteSurface)).Execute();
                    Destroyer.Destroy(toDeleteSurface.GetRootParent());
                    break;
                case Operation.Move:
                    GameObject stickyHolder = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                        memento.OriginalConfig.ParentID).GetRootParent();
                    GameStickyNoteTransform.Move(stickyHolder, memento.OriginalConfig.Position,
                        memento.OriginalConfig.Rotation);
                    GameObject surface = GameFinder.GetDrawableSurface(stickyHolder);
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName, memento.OriginalConfig.Position,
                        memento.OriginalConfig.Rotation).Execute();
                    break;
                case Operation.Edit:
                    GameObject sticky = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                        memento.OriginalConfig.ParentID)
                        .transform.parent.gameObject;
                    GameStickyNoteEdit.Change(sticky, memento.OriginalConfig);
                    new StickyNoteChangeNetAction(memento.OriginalConfig).Execute();
                    break;
                case Operation.Delete:
                    GameObject stickyNote = GameStickyNoteManager.Spawn(memento.OriginalConfig);
                    new StickyNoteSpawnNetAction(memento.OriginalConfig).Execute();
                    GameObject sf = GameFinder.GetDrawableSurface(stickyNote);
                    foreach (DrawableType type in memento.OriginalConfig.GetAllDrawableTypes())
                    {
                        DrawableType.Restore(type, sf);
                    }
                    break;
            }
        }

        /// <summary>
        /// Repeats this action, i.e., it spawn/move/edit or deletes the sticky note.
        /// </summary>
        public override void Redo()
        {
            switch (memento.Action)
            {
                case Operation.Spawn:
                    GameStickyNoteManager.Spawn(memento.OriginalConfig);
                    new StickyNoteSpawnNetAction(memento.OriginalConfig).Execute();
                    break;
                case Operation.Move:
                    GameObject stickyHolder = GameFinder.FindDrawableSurface(memento.ChangedConfig.ID,
                        memento.ChangedConfig.ParentID).GetRootParent();
                    GameStickyNoteTransform.Move(stickyHolder, memento.ChangedConfig.Position,
                        memento.ChangedConfig.Rotation);
                    GameObject surface = GameFinder.GetDrawableSurface(stickyHolder);
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    new StickyNoteMoveNetAction(surface.name, surfaceParentName, memento.ChangedConfig.Position,
                        memento.ChangedConfig.Rotation).Execute();
                    break;
                case Operation.Edit:
                    GameObject sticky = GameFinder.FindDrawableSurface(memento.ChangedConfig.ID,
                        memento.ChangedConfig.ParentID)
                        .transform.parent.gameObject;
                    GameStickyNoteEdit.Change(sticky, memento.ChangedConfig);
                    new StickyNoteChangeNetAction(memento.ChangedConfig).Execute();
                    break;
                case Operation.Delete:
                    GameObject toDeleteSurface = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                        memento.OriginalConfig.ParentID);
                    /// Save the current state of the Sticky Note; another user may have made changes.
                    memento.OriginalConfig = DrawableConfigManager.GetDrawableConfig(toDeleteSurface);
                    new StickyNoteDeleterNetAction(DrawableConfigManager.GetDrawableConfig(toDeleteSurface)).Execute();
                    Destroyer.Destroy(toDeleteSurface.GetRootParent());
                    break;
            }
        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="StickyNoteAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new StickyNoteAction();
        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="StickyNoteAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.StickyNote"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.StickyNote;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The sticky note id.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            GameObject stickyNoteSurface = GameFinder.FindDrawableSurface(memento.OriginalConfig.ID,
                memento.OriginalConfig.ParentID);
            if (stickyNoteSurface != null)
            {
                return new()
                {
                    stickyNoteSurface.transform.parent.name
                };
            }
            else
            {
                return new();
            }
        }
    }
}
