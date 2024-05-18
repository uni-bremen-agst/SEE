using HighlightPlus;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;

namespace SEE.Controls.Actions.Drawable
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
        /// True if the operation is in progress.
        /// </summary>
        private bool inProgress = false;

        /// <summary>
        /// Sticky note object.
        /// </summary>
        private GameObject stickyNote;

        /// <summary>
        /// Sticky note holder, can also be the sticky note itself.
        /// </summary>
        private GameObject stickyNoteHolder;

        /// <summary>
        /// True if the mouse was released. It will be needed for the moving operation.
        /// </summary>
        private bool mouseWasReleased = false;

        /// <summary>
        /// True if the move menu is open.
        /// </summary>
        private bool moveMenuOpened = false;

        /// <summary>
        /// An euler angles backup of the selected object.
        /// Needed for move operation.
        /// </summary>
        private Vector3 eulerAnglesBackup;

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
            public readonly DrawableConfig OriginalConfig;

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
            /// <param name="originalConfig">the original configuration of the sticky note</param>
            /// <param name="action">the executed operation.</param>
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
            StickyNoteMenu.Enable();
        }

        /// <summary>
        /// Destroys the menu and resets the action if it's not yet finished.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            StickyNoteMenu.Disable();
            StickyNoteRotationMenu.Disable();
            StickyNoteEditMenu.Disable();
            StickyNoteMoveMenu.Disable();
            ScaleMenu.Disable();
            stickyNote?.Destroy<HighlightEffect>();

            if (selectedAction == Operation.Move && stickyNote != null)
            {
                foreach (Collider collider in
                    GameFinder.GetHighestParent(stickyNote).GetComponentsInChildren<Collider>())
                {
                    collider.enabled = true;
                }
            }
            if (!finish)
            {
                switch(memento.Action)
                {
                    case Operation.Spawn:
                        if (stickyNote != null)
                        {
                            Destroyer.Destroy(stickyNote);
                        }
                        break;
                    case Operation.Move:
                        GameObject stickyHolder = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.OriginalConfig.ID, memento.OriginalConfig.ParentID));
                        GameStickyNoteManager.Move(stickyHolder, memento.OriginalConfig.Position,
                            memento.OriginalConfig.Rotation);
                        GameObject drawable = GameFinder.GetDrawable(stickyHolder);
                        string drawableParentID = GameFinder.GetDrawableParentName(drawable);
                        new StickyNoteMoveNetAction(drawable.name, drawableParentID, memento.OriginalConfig.Position,
                            memento.OriginalConfig.Rotation).Execute();
                        break;
                    case Operation.Edit:
                        GameObject sticky = GameFinder.FindDrawable(memento.OriginalConfig.ID,
                            memento.OriginalConfig.ParentID)
                            .transform.parent.gameObject;
                        GameStickyNoteManager.Change(sticky, memento.OriginalConfig);
                        new StickyNoteChangeNetAction(memento.OriginalConfig).Execute();
                        break;
                }
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.StickyNote"/>.
        /// After the user selects an operation, the corresponding method will be called.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
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
            if ((stickyNote != null || stickyNoteHolder != null)
                && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                StickyNoteRotationMenu.Disable();
                StickyNoteEditMenu.Disable();
                StickyNoteMoveMenu.Disable();
                ScaleMenu.Disable();
                stickyNote?.Destroy<HighlightEffect>();

                if (selectedAction == Operation.Move
                    && stickyNote != null)
                {
                    foreach (Collider collider in
                        GameFinder.GetHighestParent(stickyNote).GetComponentsInChildren<Collider>())
                    {
                        collider.enabled = true;
                    }
                }
                if (!finish)
                {
                    switch (selectedAction)
                    {
                        case Operation.Spawn:
                            if (stickyNote != null)
                            {
                                Destroyer.Destroy(stickyNote);
                            }
                            break;
                        case Operation.Move:
                            GameObject stickyHolder = GameFinder.GetHighestParent(
                            GameFinder.FindDrawable(memento.OriginalConfig.ID, memento.OriginalConfig.ParentID));
                            GameStickyNoteManager.Move(stickyHolder, memento.OriginalConfig.Position,
                                memento.OriginalConfig.Rotation);
                            GameObject d = GameFinder.GetDrawable(stickyHolder);
                            string drawableParentID = GameFinder.GetDrawableParentName(d);
                            new StickyNoteMoveNetAction(d.name, drawableParentID, memento.OriginalConfig.Position,
                                memento.OriginalConfig.Rotation).Execute();
                            break;
                        case Operation.Edit:
                            GameObject sticky = GameFinder.FindDrawable(memento.OriginalConfig.ID,
                                memento.OriginalConfig.ParentID)
                                .transform.parent.gameObject;
                            GameStickyNoteManager.Change(sticky, memento.OriginalConfig);
                            new StickyNoteChangeNetAction(memento.OriginalConfig).Execute();
                            break;
                    }
                }
                StickyNoteMenu.Enable();
                stickyNote = null;
                stickyNoteHolder = null;
                inProgress = false;
                mouseWasReleased = false;
                moveMenuOpened = false;
                selectedAction = Operation.None;
            }
        }

        /// <summary>
        /// Through Spawn(), it is possible to create a new sticky note.
        /// It will be positioned at the detected mouse click location.
        /// For this, a collider is necessary at the target point.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        private bool Spawn()
        {
            /// Invocation to place the sticky note at the desired location.
            SpawnOnPosition();

            /// Waits for the correct position and rotation of the sticky note placed from an unsuitable object.
            SetPositionAndRotation(true);

            /// When the spawning is finished,   create the sticky note on all clients and complete the current state.
            if (finish)
            {
                DrawableConfig config = DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawable(stickyNote));
                new StickyNoteSpawnNetAction(config).Execute();
                memento = new(config, selectedAction);
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Selects the position for the sticky note and spawns it there.
        /// If the detected object is a drawable or has one or is part of a sticky note,
        /// or is listed in the suitable objects list by name or tag,
        /// it is not necessary to choose a rotation, as it can be inherited from the detected object.
        /// Otherwise, a desired rotation must be selected for the sticky note.
        /// </summary>
        private void SpawnOnPosition()
        {
            if (Input.GetMouseButtonDown(0) && !inProgress
                && Raycasting.RaycastAnything(out RaycastHit raycastHit))
            {
                inProgress = true;
                stickyNote = GameStickyNoteManager.Spawn(raycastHit);

                /// Block for the rotation of the suitable object.
                if (raycastHit.collider.gameObject.CompareTag(Tags.Drawable)
                    || GameFinder.HasDrawable(raycastHit.collider.gameObject)
                    || GameFinder.IsPartOfADrawable(raycastHit.collider.gameObject)
                    || ValueHolder.IsASuitableObjectForStickyNote(raycastHit.collider.gameObject))
                {
                    finish = true;
                }
                else
                {
                    /// Block for selecting the rotation and the right position.
                    StickyNoteMenu.Disable();
                    StickyNoteRotationMenu.Enable(stickyNote, raycastHit.collider.gameObject);
                    StickyNoteMoveMenu.Enable(GameFinder.GetHighestParent(stickyNote), true);
                }
            }
        }

        /// <summary>
        /// It waits for either the Move Menu or the Rotation Menu to finish.
        /// Additionally, as long as no finish is received from the menus,
        /// moving via key and rotating via mouse wheel are provided.
        /// </summary>
        private void SetPositionAndRotation(bool spawnMode)
        {
            if (StickyNoteMoveMenu.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNote != null && StickyNoteMoveMenu.IsActive())
            {
                MoveByKey(stickyNote, spawnMode);
            }

            if (StickyNoteRotationMenu.TryGetFinish(out bool isRotationFinished))
            {
                finish = isRotationFinished;
            }
            else if (stickyNote != null && StickyNoteRotationMenu.IsYActive())
            {
                RotateByWheel(stickyNote, spawnMode);
            }
        }

        /// <summary>
        /// Enables the movement of a sticky note.
        /// Initially, the movement is based on the mouse position.
        /// With another left-click of the mouse, this is terminated,
        /// and a Rotate and Move menu is opened to make final adjustments.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        private bool Move()
        {
            /// With this block a sticky note to move will be selected.
            if (!MoveSelection())
            {
                return false;
            }

            /// Finish the sticky note selection and starts the moving.
            if (inProgress && !mouseWasReleased)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    mouseWasReleased = true;
                }
            }

            /// If a sticky note was selected change the position.
            /// With a mouse left-click again, the mouse movement is disabled,
            /// and a rotate and move menu is opened to make final adjustments.
            MoveByMouse();

            /// This block is executed after move by mouse and allows fine-tuning for the position and rotation.
            SetPositionAndRotation(false);

            /// When the moving is finish completed the current state.
            /// And save the position and rotation in memento, because they could be changed with the menu's.
            if (finish)
            {
                StickyNoteMoveMenu.Disable();
                StickyNoteRotationMenu.Disable();
                memento.ChangedConfig.Position = stickyNoteHolder.transform.position;
                memento.ChangedConfig.Rotation = stickyNoteHolder.transform.eulerAngles;
                stickyNote.transform.Find("Back").GetComponent<Collider>().enabled = true;
                foreach (Collider collider in
                    GameFinder.GetHighestParent(stickyNote).GetComponentsInChildren<Collider>())
                {
                    collider.enabled = true;
                }
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Provides the selection of an object for moving.
        /// If an attempt is made to select a non-sticky note, appropriate information is provided.
        /// Otherwise, it creates the Memento and disables the collider of the sticky note.
        /// The reason for this is that move by mouse would not work as intended with an active collider.
        /// Finally, important data for editing the move is collected.
        /// </summary>
        /// <returns>true, if a sticky not was selected, Otherwise false</returns>
        private bool MoveSelection()
        {
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) && !inProgress
                && (raycastHit.collider.gameObject.CompareTag(Tags.Drawable)
                    || GameFinder.HasDrawable(raycastHit.collider.gameObject)
                    || CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);

                /// Only accept to move a sticky note.
                if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    inProgress = true;
                    memento = new(DrawableConfigManager.GetDrawableConfig(drawable), selectedAction);
                    memento.ChangedConfig = DrawableConfigManager.GetDrawableConfig(drawable);
                    StickyNoteMenu.Disable();
                    drawable.GetComponent<Collider>().enabled = false;
                    stickyNote = drawable.transform.parent.gameObject;
                    stickyNote.transform.Find("Back").GetComponent<Collider>().enabled = false;
                    stickyNoteHolder = GameFinder.GetHighestParent(drawable);
                    foreach(Collider collider in stickyNoteHolder.GetComponentsInChildren<Collider>())
                    {
                        collider.enabled = false;
                    }
                    eulerAnglesBackup = stickyNoteHolder.transform.eulerAngles;
                }
                else
                {
                    ShowNotification.Info("Wrong selection", "You don't selected a sticky note.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Enables move by mouse.
        /// The sticky note follows the mouse position.
        /// This is ended by a mouse click.
        /// Subsequently, the Move and Rotation Menu are opened to do the fine-tuning.
        /// </summary>
        private void MoveByMouse()
        {
            if (inProgress && mouseWasReleased && !moveMenuOpened
                && Raycasting.RaycastAnything(out RaycastHit hit))
            {
                Vector3 eulerAngles = eulerAnglesBackup;
                /// If the new target object is a suitable object, the rotation is adopted.
                if (hit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameFinder.HasDrawable(hit.collider.gameObject) ||
                    GameFinder.IsPartOfADrawable(hit.collider.gameObject) ||
                    ValueHolder.IsASuitableObjectForStickyNote(hit.collider.gameObject))
                {
                    /// Adopts the euler angles of the hit object,
                    /// unless it is a <see cref="DrawableType"/> object.
                    /// In that case, take the euler angles of the drawable.
                    if (DrawableType.Get(hit.collider.gameObject) == null)
                    {
                        eulerAngles = hit.collider.gameObject.transform.eulerAngles;
                    }
                    else
                    {
                        GameObject drawable = GameFinder.GetDrawable(hit.collider.gameObject);
                        eulerAngles = drawable.transform.eulerAngles;
                    }
                }

                Vector3 oldPos = stickyNoteHolder.transform.position;
                /// If the new target object has a drawable or is part of a sticky note,
                /// the Z-axis of the drawable is used for the hit point.
                /// This is done to address overlap display errors.
                if (GameFinder.HasDrawable(hit.collider.gameObject) ||
                    CheckIsPartOfStickyNote(hit.collider.gameObject))
                {
                    GameObject drawable = GameFinder.GetDrawable(hit.collider.gameObject);
                    hit.point = new Vector3(hit.point.x, hit.point.y, drawable.transform.position.z);
                }

                GameStickyNoteManager.Move(stickyNoteHolder, hit.point, eulerAngles);
                Vector3 newPos = stickyNoteHolder.transform.position;
                /// This block ensures the minimum distance from the object
                if (oldPos != newPos)
                {
                    newPos = GameStickyNoteManager.FinishMoving(stickyNoteHolder);
                }

                new StickyNoteMoveNetAction(GameFinder.GetDrawable(stickyNote).name, stickyNote.name,
                    newPos, eulerAngles).Execute();

                /// Opens the move and rotation menu for fine-tuning.
                if (Input.GetMouseButton(0))
                {
                    GameFinder.GetDrawable(stickyNoteHolder).GetComponent<Collider>().enabled = true;
                    StickyNoteRotationMenu.Enable(stickyNoteHolder);
                    StickyNoteMoveMenu.Enable(stickyNoteHolder);
                    moveMenuOpened = true;
                }
            }
        }

        /// <summary>
        /// Enables moving via the arrow keys, as well as in the <see cref="MoveRotateAction"/>.
        /// In addition there are the keys page up for forward and page down for back moving.
        /// </summary>
        /// <param name="stickyNote">The object that should be moved.</param>
        /// <param name="spawnMode">If this method will be called of the spawn method..</param>
        private void MoveByKey(GameObject stickyNote, bool spawnMode)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
                || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)
                || Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.PageDown))
            {
                ValueHolder.MoveDirection direction = GetDirection();
                GameObject holder = GameFinder.GetHighestParent(stickyNote);
                Vector3 newPos = GameStickyNoteManager.MoveByMenu(holder, direction, StickyNoteMoveMenu.GetSpeed());
                if (!spawnMode)
                {
                    GameObject drawable = GameFinder.GetDrawable(stickyNote);
                    new StickyNoteMoveNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable),
                    newPos, holder.transform.eulerAngles).Execute();
                }
            }
        }

        /// <summary>
        /// Get the <see cref="ValueHolder.MoveDirection"/> of the pressed key.
        /// </summary>
        /// <returns>The <see cref="ValueHolder.MoveDirection"/> of the pressed key.</returns>
        private ValueHolder.MoveDirection GetDirection()
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                return ValueHolder.MoveDirection.Left;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                return ValueHolder.MoveDirection.Right;
            }
            else if (Input.GetKey(KeyCode.PageUp))
            {
                return ValueHolder.MoveDirection.Forward;
            }
            else if (Input.GetKey(KeyCode.PageDown))
            {
                return ValueHolder.MoveDirection.Back;
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                return ValueHolder.MoveDirection.Up;
            }
            else
            {
                return ValueHolder.MoveDirection.Down;
            }
        }

        /// <summary>
        /// With this operation the sticky note can be edited.
        /// It provides options to change the color, order in layer, scale and rotation.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
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
                memento.ChangedConfig.Rotation = GameFinder.GetHighestParent(stickyNote).transform.eulerAngles;
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
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (raycastHit.collider.gameObject.CompareTag(Tags.Drawable)
                    || GameFinder.HasDrawable(raycastHit.collider.gameObject)
                    || CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);

                /// Checks if changes have been made when a sticky note was already selected.
                if (CheckChanges() == EditReturnState.True)
                {
                    return EditReturnState.True;
                }

                if (SetChosenStickyNote(drawable) != EditReturnState.False)
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
                StickyNoteEditMenu.Disable();
                StickyNoteRotationMenu.Disable();
                ScaleMenu.Disable();
                stickyNote.Destroy<HighlightEffect>();
                memento.ChangedConfig.Scale = stickyNote.transform.localScale;
                memento.ChangedConfig.Rotation = GameFinder.GetHighestParent(stickyNote).transform.eulerAngles;

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
        /// <param name="drawable">The drawable of the selected sticky note.</param>
        /// <returns>The current edit return state:
        /// None, if nothing should be returned.
        /// True, if the update method should return true.
        /// False, if the update method should retrun false.</returns>
        private EditReturnState SetChosenStickyNote(GameObject drawable)
        {
            /// This block is executed when no sticky note has been selected yet
            /// or when the newly selected sticky note is different from the previous one.
            if (stickyNote == null || stickyNote != drawable.transform.parent.gameObject)
            {
                /// To edit the sticky note, it is required that a sticky note was selected.
                if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    stickyNote = drawable.transform.parent.gameObject;
                    GameHighlighter.Enable(stickyNote);

                    memento = new(DrawableConfigManager.GetDrawableConfig(drawable), selectedAction)
                    {
                        ChangedConfig = DrawableConfigManager.GetDrawableConfig(drawable)
                    };
                    StickyNoteMenu.Disable();
                    StickyNoteEditMenu.Enable(drawable.transform.parent.gameObject, memento.ChangedConfig);
                }
                else
                {
                    if (stickyNote == null)
                    {
                        ShowNotification.Warn("Wrong selection", "You don't selected a sticky note.");
                        return EditReturnState.False;
                    }
                    else
                    {
                        stickyNote = null;
                        selectedAction = Operation.None;
                        StickyNoteMenu.Enable();
                    }
                }
            }
            else /// Will be executed if the newly selected sticky note is the same as the previous one.
            {
                memento.ChangedConfig.Scale = stickyNote.transform.localScale;
                memento.ChangedConfig.Rotation = GameFinder.GetHighestParent(stickyNote).transform.eulerAngles;

                if (!CheckEquals(memento.OriginalConfig, memento.ChangedConfig))
                {
                    finish = true;
                    return EditReturnState.False;
                }
                else
                {
                    stickyNote = null;
                    selectedAction = Operation.None;
                    StickyNoteMenu.Enable();
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
                stickyNoteHolder = GameFinder.GetHighestParent(stickyNote);
                RotateByWheel(stickyNoteHolder, true);
            }

            if (ScaleMenu.TryGetFinish(out bool isScaleFinished))
            {
                finish = isScaleFinished;
            }
            else if (ScaleMenu.IsActive())
            {
                ScaleByWheel();
            }
        }

        /// <summary>
        /// Enables scaling via the mouse wheel, as well as in the <see cref="MoveRotateAction"/>.
        /// </summary>
        private void RotateByWheel(GameObject stickyNote, bool spawnMode)
        {
            GameObject drawable = GameFinder.GetDrawable(stickyNote);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);
            bool rotate = false;
            float degree = 0;

            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                degree = ValueHolder.Rotate;
                rotate = true;
            }
            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
            {
                degree = ValueHolder.FotateFast;
                rotate = true;
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                degree = -ValueHolder.Rotate;
                rotate = true;

            }
            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
            {
                degree = -ValueHolder.FotateFast;
                rotate = true;
            }

            if (rotate)
            {
                stickyNoteHolder = GameFinder.GetHighestParent(stickyNote);
                float newDegree = stickyNoteHolder.transform.localEulerAngles.y + degree;
                GameStickyNoteManager.SetRotateY(stickyNoteHolder, newDegree, stickyNoteHolder.transform.position);
                StickyNoteRotationMenu.AssignValueToYSlider(newDegree);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, newDegree,
                        stickyNoteHolder.transform.position).Execute();
                }
            }
        }

        /// <summary>
        /// Enables scaling via the mouse wheel, as well as in the <see cref="ScaleAction"/>.
        /// </summary>
        private void ScaleByWheel()
        {
            float scaleFactor = 0f;
            bool isScaled = false;

            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleUp;
                isScaled = true;
            }
            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleUpFast;
                isScaled = true;
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleDown;
                isScaled = true;

            }
            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleDownFast;
                isScaled = true;
            }
            if (isScaled)
            {
                memento.ChangedConfig.Scale = GameScaler.Scale(stickyNote, scaleFactor);
                ScaleMenu.AssignValue(stickyNote);
                GameObject drawable = GameFinder.GetDrawable(stickyNote);
                new ScaleNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), stickyNote.name,
                    memento.ChangedConfig.Scale).Execute();
            }
        }

        /// <summary>
        /// Deletes the chosen sticky note.
        /// If the chosen object is not a sticky note, an information will be shown.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        private bool Delete()
        {
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                GameFinder.HasDrawable(raycastHit.collider.gameObject) ||
                CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                                raycastHit.collider.gameObject :
                                GameFinder.GetDrawable(raycastHit.collider.gameObject);
                if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    memento = new(DrawableConfigManager.GetDrawableConfig(drawable), selectedAction);
                    new StickyNoteDeleterNetAction(GameFinder.GetHighestParent(drawable).name).Execute();
                    Destroyer.Destroy(GameFinder.GetHighestParent(drawable));
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
                else
                {
                    ShowNotification.Warn("Wrong selection", "You don't selected a sticky note.");
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if the values of the two given configurations are the same.
        /// </summary>
        /// <param name="original">the original configuration</param>
        /// <param name="changed">the changed configuration</param>
        /// <returns></returns>
        private bool CheckEquals(DrawableConfig original, DrawableConfig changed)
        {
            return original.Scale.Equals(changed.Scale) && original.Color.Equals(changed.Color) &&
                original.Rotation.Equals(changed.Rotation) && original.Order.Equals(changed.Order);
        }

        /// <summary>
        /// Checks if the selected object is part of a sticky note.
        /// </summary>
        /// <param name="selectedObject">"The object to be checked.</param>
        /// <returns>true, if the object parent is the sticky note; otherwise false.</returns>
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
                    GameObject toDelete = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.OriginalConfig.ID, memento.OriginalConfig.ParentID));
                    new StickyNoteDeleterNetAction(toDelete.name).Execute();
                    Destroyer.Destroy(toDelete);
                    break;
                case Operation.Move:
                    GameObject stickyHolder = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.OriginalConfig.ID, memento.OriginalConfig.ParentID));
                    GameStickyNoteManager.Move(stickyHolder, memento.OriginalConfig.Position,
                        memento.OriginalConfig.Rotation);
                    GameObject d = GameFinder.GetDrawable(stickyHolder);
                    string drawableParentID = GameFinder.GetDrawableParentName(d);
                    new StickyNoteMoveNetAction(d.name, drawableParentID, memento.OriginalConfig.Position,
                        memento.OriginalConfig.Rotation).Execute();
                    break;
                case Operation.Edit:
                    GameObject sticky = GameFinder.FindDrawable(memento.OriginalConfig.ID,
                        memento.OriginalConfig.ParentID)
                        .transform.parent.gameObject;
                    GameStickyNoteManager.Change(sticky, memento.OriginalConfig);
                    new StickyNoteChangeNetAction(memento.OriginalConfig).Execute();
                    break;
                case Operation.Delete:
                    GameObject stickyNote = GameStickyNoteManager.Spawn(memento.OriginalConfig);
                    new StickyNoteSpawnNetAction(memento.OriginalConfig).Execute();
                    GameObject drawable = GameFinder.GetDrawable(stickyNote);
                    foreach (DrawableType type in memento.OriginalConfig.GetAllDrawableTypes())
                    {
                        DrawableType.Restore(type, drawable);
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
                    GameObject stickyHolder = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.ChangedConfig.ID, memento.ChangedConfig.ParentID));
                    GameStickyNoteManager.Move(stickyHolder, memento.ChangedConfig.Position,
                        memento.ChangedConfig.Rotation);
                    GameObject d = GameFinder.GetDrawable(stickyHolder);
                    string drawableParentID = GameFinder.GetDrawableParentName(d);
                    new StickyNoteMoveNetAction(d.name, drawableParentID, memento.ChangedConfig.Position,
                        memento.ChangedConfig.Rotation).Execute();
                    break;
                case Operation.Edit:
                    GameObject sticky = GameFinder.FindDrawable(memento.ChangedConfig.ID,
                        memento.ChangedConfig.ParentID)
                        .transform.parent.gameObject;
                    GameStickyNoteManager.Change(sticky, memento.ChangedConfig);
                    new StickyNoteChangeNetAction(memento.ChangedConfig).Execute();
                    break;
                case Operation.Delete:
                    GameObject toDelete = GameFinder.GetHighestParent(GameFinder.FindDrawable(memento.OriginalConfig.ID,
                        memento.OriginalConfig.ParentID));
                    new StickyNoteDeleterNetAction(toDelete.name).Execute();
                    Destroyer.Destroy(toDelete);
                    break;
            }
        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="StickyNoteAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new StickyNoteAction();
        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="StickyNoteAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.StickyNote"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.StickyNote;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>the sticky note id</returns>
        public override HashSet<string> GetChangedObjects()
        {
            GameObject stickyNoteDrawable = GameFinder.FindDrawable(memento.OriginalConfig.ID,
                memento.OriginalConfig.ParentID);
            if (stickyNoteDrawable != null)
            {
                return new()
                {
                    stickyNoteDrawable.transform.parent.name
                };
            }
            else
            {
                return new();
            }
        }
    }
}