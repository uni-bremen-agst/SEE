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
        /// Handles deleting sticky notes.
        /// </summary>
        private readonly StickyNoteDeleteOperation deleteOperation = new();

        /// <summary>
        /// Handles spawning sticky notes.
        /// </summary>
        private readonly StickyNoteSpawnOperation spawnOperation = new();

        /// <summary>
        /// Handles moving sticky notes.
        /// </summary>
        private readonly StickyNoteMoveOperation moveOperation = new();

        /// <summary>
        /// Handles editing sticky notes.
        /// </summary>
        private readonly StickyNoteEditOperation editOperation = new();

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

            if (spawnOperation.IsInProgress)
            {
                spawnOperation.Cancel();
            }

            if (moveOperation.IsInProgress)
            {
                moveOperation.Cancel();
            }

            if (editOperation.IsInProgress)
            {
                editOperation.Cancel();
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
            if ((spawnOperation.IsInProgress || moveOperation.IsInProgress || editOperation.IsInProgress)
                && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");

                StickyNoteRotationMenu.Destroy();
                StickyNoteEditMenu.Instance.Destroy();
                StickyNoteMoveMenu.Instance.Destroy();
                ScaleMenu.Instance.Destroy();

                if (spawnOperation.IsInProgress)
                {
                    spawnOperation.Cancel();
                }

                if (moveOperation.IsInProgress)
                {
                    moveOperation.Cancel();
                }

                if (editOperation.IsInProgress)
                {
                    editOperation.Cancel();
                }

                StickyNoteMenu.Instance.Enable();
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

            CurrentState = IReversibleAction.Progress.Completed;
            return true;
        }

        /// <summary>
        /// Edits a sticky note.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        private bool Edit()
        {
            StickyNoteEditOperation.Result result =
                editOperation.Update(out DrawableConfig originalConfig, out DrawableConfig changedConfig);

            switch (result)
            {
                case StickyNoteEditOperation.Result.Completed:
                    memento = new Memento(originalConfig, selectedAction)
                    {
                        ChangedConfig = changedConfig
                    };
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;

                case StickyNoteEditOperation.Result.Reset:
                    selectedAction = Operation.None;
                    return false;

                default:
                    return false;
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
