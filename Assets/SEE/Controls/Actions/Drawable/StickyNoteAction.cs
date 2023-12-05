using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using HighlightPlus;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides all operations for sticky notes.
    /// </summary>
    public class StickyNoteAction : AbstractPlayerAction
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
        /// The attribut which represents that the operation is finish.
        /// It is public and static becaus it will be changed from the menus
        /// with the finish/done button.
        /// </summary>
        //public static bool finish = false;
        private bool finish = false;

        /// <summary>
        /// Attribute that represents that the operation is in progress.
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
        /// Attribut that represents that the mouse was released, will be
        /// needed for moving operation.
        /// </summary>
        private bool mouseWasReleased = false;

        /// <summary>
        /// Represents that the move menu's are open.
        /// </summary>
        private bool moveMenuOpened = false;

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
            public readonly DrawableConfig originalConfig;

            /// <summary>
            /// The changed configuration of the sticky note
            /// </summary>
            public DrawableConfig changedConfig;

            /// <summary>
            /// The operation that was executed.
            /// </summary>
            public readonly Operation action;
            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="originalConfig">the original configuration of the sticky note</param>
            /// <param name="action">The executed operation.</param>
            public Memento(DrawableConfig originalConfig, Operation action)
            {
                this.originalConfig = originalConfig;
                this.action = action;
                this.changedConfig = null;
            }
        }

        /// <summary>
        /// Enables the sticky note menu with which an operation can be selected.
        /// </summary>
        public override void Awake()
        {
            StickyNoteMenu.Enable();
        }

        /// <summary>
        /// Destroys the menu's and resets finish attribut.
        /// </summary>
        public override void Stop()
        {
            StickyNoteMenu.Disable();
            StickyNoteRotationMenu.Disable();
            StickyNoteEditMenu.Disable();
            StickyNoteMoveMenu.Disable();
            if (stickyNote != null && stickyNote.GetComponent<HighlightEffect>() != null)
            {
                Destroyer.Destroy(stickyNote.GetComponent<HighlightEffect>());
            }
            finish = false;
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.StickyNote"/>.
        /// After the user select an operation the corresponding method will be called.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
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
                            ShowNotification.Info("Select an operation", "First you need to select an operation from the menu.");
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
        /// Through Spawn(), it is possible to create a new sticky note. 
        /// It will be positioned at the detected mouse click location. 
        /// For this, a collider is necessary at the target point.
        /// </summary>
        /// <returns>Whether this Operation is finished</returns>
        private bool Spawn()
        {
            if (Input.GetMouseButtonDown(0) && !inProgress
            && Raycasting.RaycastAnything(out RaycastHit raycastHit))
            {
                inProgress = true;
                stickyNote = GameStickyNoteManager.Spawn(raycastHit);

                /// If the detected object is not drawable, it is necessary to choose the rotation for the sticky note.
                /// Otherwise, it is not necessary, as the provided drawables already have the correct rotation.
                if (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameFinder.hasDrawable(raycastHit.collider.gameObject)
                    || ValueHolder.SuitableObjectsForStickyNotes.Contains(raycastHit.collider.gameObject.name))
                {
                    finish = true;
                    
                }
                else
                {
                    StickyNoteMenu.Disable();
                    StickyNoteRotationMenu.Enable(stickyNote, raycastHit.collider.gameObject);
                    StickyNoteMoveMenu.Enable(GameFinder.GetHighestParent(stickyNote), true);
                }
            }

            if (StickyNoteMoveMenu.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNote != null && StickyNoteMoveMenu.IsActive())
            {
                MoveByKey(stickyNote, true);
            }

            if (StickyNoteRotationMenu.TryGetFinish(out bool isRotationFinished))
            {
                finish = isRotationFinished;
            } else if (stickyNote != null && StickyNoteRotationMenu.IsYActive())
            {
                RotateByWheel(stickyNote, true);
            }

            /// When the spawning is finish create the sticky note on all clients and complete the current state.
            if (finish)
            {
                DrawableConfig config = DrawableConfigManager.GetDrawableConfig(GameFinder.GetDrawable(stickyNote));
                new StickyNoteSpawnNetAction(config).Execute();
                memento = new(config, selectedAction);
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        Vector3 eulerAnglesBackup;

        /// <summary>
        /// "Enables the movement of a sticky note. 
        /// Initially, the movement is based on the mouse position. 
        /// With another left-click of the mouse, this is terminated, 
        /// and a Rotate and Move menu is opened to make final adjustments.
        /// </summary>
        /// <returns>Whether this Operation is finished</returns>
        private bool Move()
        {
            /// With this block a sticky note to move will be selected.
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) && !inProgress &&
                (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                GameFinder.hasDrawable(raycastHit.collider.gameObject) || 
                CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);
                if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    inProgress = true;
                    memento = new(DrawableConfigManager.GetDrawableConfig(drawable), selectedAction);
                    memento.changedConfig = DrawableConfigManager.GetDrawableConfig(drawable);
                    StickyNoteMenu.Disable();
                    drawable.GetComponent<Collider>().enabled = false;
                    stickyNote = drawable.transform.parent.gameObject;
                    stickyNote.transform.Find("Back").GetComponent<Collider>().enabled = false;
                    stickyNoteHolder = GameFinder.GetHighestParent(drawable);
                    eulerAnglesBackup = stickyNoteHolder.transform.eulerAngles;
                }
                else
                {
                    ShowNotification.Info("Wrong selection", "You don't selected a sticky note.");
                    return false;
                }
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
            if (inProgress && mouseWasReleased && !moveMenuOpened &&
                Raycasting.RaycastAnything(out RaycastHit hit))
            {
                Vector3 eulerAngles = eulerAnglesBackup;
                if (hit.collider.gameObject.CompareTag(Tags.Drawable) || 
                    ValueHolder.SuitableObjectsForStickyNotes.Contains(hit.collider.gameObject.name))
                {
                    eulerAngles = hit.collider.gameObject.transform.eulerAngles;
                }
                Vector3 oldPos = stickyNoteHolder.transform.position;
                GameStickyNoteManager.Move(stickyNoteHolder, hit.point, eulerAngles);
                Vector3 newPos = stickyNoteHolder.transform.position;
                if (oldPos != newPos)
                {
                    newPos = GameStickyNoteManager.FinishMoving(stickyNoteHolder);
                }
                new StickyNoteMoveNetAction(GameFinder.GetDrawable(stickyNote).name, stickyNote.name,
                    newPos, eulerAngles).Execute();

                if (Input.GetMouseButton(0))
                {
                    GameFinder.GetDrawable(stickyNoteHolder).GetComponent<Collider>().enabled = true;
                    StickyNoteRotationMenu.Enable(stickyNoteHolder);
                    StickyNoteMoveMenu.Enable(stickyNoteHolder);
                    moveMenuOpened = true;
                }
            }

            if (StickyNoteMoveMenu.TryGetFinish(out bool isFinished))
            {
                finish = isFinished;
            }
            else if (stickyNoteHolder != null && StickyNoteMoveMenu.IsActive())
            {
                MoveByKey(stickyNoteHolder, false);
            }

            if (StickyNoteRotationMenu.TryGetFinish(out bool isRotationFinished))
            {
                finish = isRotationFinished;
            }
            else if (stickyNoteHolder != null && StickyNoteRotationMenu.IsYActive())
            {
                RotateByWheel(stickyNoteHolder, true);
            }

            /// When the moving is finish completed the current state. 
            /// And save the position and rotation in memento, because they could be changed with the menu's.
            if (finish)
            {
                StickyNoteMoveMenu.Disable();
                StickyNoteRotationMenu.Disable();
                memento.changedConfig.Position = stickyNoteHolder.transform.position;
                memento.changedConfig.Rotation = stickyNoteHolder.transform.eulerAngles;
                stickyNote.transform.Find("Back").GetComponent<Collider>().enabled = true;
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enables moving via the arrow keys, as well as in the <see cref="MoveRotatorAction"/>.
        /// In addition there are the keys page up for forward and page down for back moving.
        /// </summary>
        /// <param name="stickyNote">The object that should be moved.</param>
        /// <param name="spawnMode">If this method will be called of the spawn method..</param>
        private void MoveByKey(GameObject stickyNote, bool spawnMode)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) 
                || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.PageUp) ||Input.GetKey(KeyCode.PageDown))
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
        /// <returns>Whether this Operation is finished</returns>
        private bool Edit()
        {
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                GameFinder.hasDrawable(raycastHit.collider.gameObject) ||
                CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                    raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);

                /// This block will executed if there was selected another sticky note before in this action run.
                if (stickyNote != null)
                {
                    StickyNoteEditMenu.Disable();
                    StickyNoteRotationMenu.Disable();
                    ScaleMenu.Disable();
                    if (stickyNote.GetComponent<HighlightEffect>() != null)
                    {
                        Destroyer.Destroy(stickyNote.GetComponent<HighlightEffect>());
                    }
                    memento.changedConfig.Scale = stickyNote.transform.localScale;
                    memento.changedConfig.Rotation = GameFinder.GetHighestParent(stickyNote).transform.eulerAngles;

                    /// If there are changed in the configuration finish this operation.
                    if (!CheckEquals(memento.originalConfig, memento.changedConfig))
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                }

                /// This block is executed when no sticky note has been selected yet 
                /// or when the newly selected sticky note is different from the previous one.
                if (stickyNote == null || stickyNote != drawable.transform.parent.gameObject)
                {
                    /// To edit it is required that a sticky note was selected.
                    if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                    {
                        stickyNote = drawable.transform.parent.gameObject;
                        GameHighlighter.Enable(stickyNote);

                        memento = new(DrawableConfigManager.GetDrawableConfig(drawable), selectedAction);
                        memento.changedConfig = DrawableConfigManager.GetDrawableConfig(drawable);
                        StickyNoteMenu.Disable();
                        StickyNoteEditMenu.Enable(drawable.transform.parent.gameObject, memento.changedConfig);
                    }
                    else
                    {
                        if (stickyNote == null)
                        {
                            ShowNotification.Info("Wrong selection", "You don't selected a sticky note.");
                            return false;
                        } else
                        {
                            stickyNote = null;
                            selectedAction = Operation.None;
                            StickyNoteMenu.Enable();
                        }
                    }
                }
                else /// Will be executed if the new selected sticky note is the same as the previous one.
                {
                    memento.changedConfig.Scale = stickyNote.transform.localScale;
                    memento.changedConfig.Rotation = GameFinder.GetHighestParent(stickyNote).transform.eulerAngles;

                    if (!CheckEquals(memento.originalConfig, memento.changedConfig))
                    {
                        finish = true;
                        return false;
                    }
                    else
                    {
                        stickyNote = null;
                        selectedAction = Operation.None;
                        StickyNoteMenu.Enable();
                    }
                }
            }

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
            } else if (ScaleMenu.IsActive())
            {
                ScaleByWheel();
            }
            

            /// When the editing is finish completed the current state. 
            /// And save the scale and rotation in memento, because they could be changed with the menu's.
            if (finish)
            {
                memento.changedConfig.Scale = stickyNote.transform.localScale;
                memento.changedConfig.Rotation = GameFinder.GetHighestParent(stickyNote).transform.eulerAngles;
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enables scaling via the mouse wheel, as well as in the <see cref="MoveRotatorAction"/>.
        /// </summary>
        private void RotateByWheel(GameObject stickyNote, bool spawnMode)
        {
            GameObject drawable = GameFinder.GetDrawable(stickyNote);
            string drawableParentID = GameFinder.GetDrawableParentName(drawable);
            bool rotate = false;
            float degree = 0;

            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                degree = ValueHolder.rotate;
                rotate = true;
            }
            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
            {
                degree = ValueHolder.rotateFast;
                rotate = true;
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                degree = -ValueHolder.rotate;
                rotate = true;

            }
            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
            {
                degree = -ValueHolder.rotateFast;
                rotate = true;
            }

            if (rotate)
            {
                float newDegree = stickyNote.transform.localEulerAngles.y + degree;
                GameStickyNoteManager.SetRotateY(stickyNote, newDegree, stickyNote.transform.position);
                StickyNoteRotationMenu.AssignValueToYSlider(newDegree);
                if (!spawnMode)
                {
                    new StickyNoteRoateYNetAction(drawable.name, drawableParentID, newDegree, stickyNote.transform.position).Execute();
                }
            }
        }

        /// <summary>
        /// "Enables scaling via the mouse wheel, as well as in the <see cref="ScaleAction"/>.
        /// </summary>
        private void ScaleByWheel()
        {
            float scaleFactor = 0f;
            bool isScaled = false;

            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.scaleUp;
                isScaled = true;
            }
            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.scaleUpFast;
                isScaled = true;
            }

            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.scaleDown;
                isScaled = true;

            }
            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.scaleDownFast;
                isScaled = true;
            }
            if (isScaled)
            {
                memento.changedConfig.Scale = GameScaler.Scale(stickyNote, scaleFactor);
                ScaleMenu.AssignValue(stickyNote);
                GameObject drawable = GameFinder.GetDrawable(stickyNote);
                new ScaleNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), stickyNote.name, memento.changedConfig.Scale).Execute();
            }
        }

        /// <summary>
        /// Deletes the chosen sticky note.
        /// If the chosen object is not a sticky note an information will be shown.
        /// </summary>
        /// <returns>Whether this Operation is finished</returns>
        private bool Delete()
        {
            if (Input.GetMouseButtonDown(0)
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                GameFinder.hasDrawable(raycastHit.collider.gameObject) ||
                CheckIsPartOfStickyNote(raycastHit.collider.gameObject)))
            {
                GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                                raycastHit.collider.gameObject : GameFinder.GetDrawable(raycastHit.collider.gameObject);
                if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    memento = new(DrawableConfigManager.GetDrawableConfig(drawable), selectedAction);
                    new StickyNoteDeleterNetAction(GameFinder.GetHighestParent(drawable).name).Execute();
                    Destroyer.Destroy(GameFinder.GetHighestParent(drawable));
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                }
                else
                {
                    ShowNotification.Info("Wrong selection", "You don't selected a sticky note.");
                    return false;
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if the values of the both configurations are the same.
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
        /// Checks if the selected object is part of the sticky notes.
        /// </summary>
        /// <param name="selectedObject">"The object to be checked.</param>
        /// <returns>true, if the object parent is the sticky note. Otherwise false.</returns>
        private bool CheckIsPartOfStickyNote(GameObject selectedObject)
        {
            return selectedObject.transform.parent.name.StartsWith(ValueHolder.StickyNotePrefix);
        }

        /// <summary>
        /// Reverts this action, i.e., it spawn/delete the sticky note or change to old position / values.
        /// </summary>
        public override void Undo()
        {
            switch (memento.action)
            {
                case Operation.Spawn:
                    GameObject toDelete = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.originalConfig.ID, memento.originalConfig.ParentID));
                    new StickyNoteDeleterNetAction(toDelete.name).Execute();
                    Destroyer.Destroy(toDelete);
                    break;
                case Operation.Move:
                    GameObject stickyHolder = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.originalConfig.ID, memento.originalConfig.ParentID));
                    GameStickyNoteManager.Move(stickyHolder, memento.originalConfig.Position, memento.originalConfig.Rotation);
                    GameObject d = GameFinder.GetDrawable(stickyHolder);
                    string drawableParentID = GameFinder.GetDrawableParentName(d);
                    new StickyNoteMoveNetAction(d.name, drawableParentID, memento.originalConfig.Position,
                        memento.originalConfig.Rotation).Execute();
                    break;
                case Operation.Edit:
                    GameObject sticky = GameFinder.FindDrawable(memento.originalConfig.ID, memento.originalConfig.ParentID)
                        .transform.parent.gameObject;
                    GameStickyNoteManager.Change(sticky, memento.originalConfig);
                    new StickyNoteChangeNetAction(memento.originalConfig).Execute();
                    break;
                case Operation.Delete:
                    GameObject stickyNote = GameStickyNoteManager.Spawn(memento.originalConfig);
                    new StickyNoteSpawnNetAction(memento.originalConfig).Execute();
                    GameObject drawable = GameFinder.GetDrawable(stickyNote);
                    foreach(DrawableType type in memento.originalConfig.GetAllDrawableTypes())
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
            switch (memento.action)
            {
                case Operation.Spawn:
                    GameStickyNoteManager.Spawn(memento.originalConfig);
                    new StickyNoteSpawnNetAction(memento.originalConfig).Execute();
                    break;
                case Operation.Move:
                    GameObject stickyHolder = GameFinder.GetHighestParent(
                        GameFinder.FindDrawable(memento.changedConfig.ID, memento.changedConfig.ParentID));
                    GameStickyNoteManager.Move(stickyHolder, memento.changedConfig.Position, memento.changedConfig.Rotation);
                    GameObject d = GameFinder.GetDrawable(stickyHolder);
                    string drawableParentID = GameFinder.GetDrawableParentName(d);
                    new StickyNoteMoveNetAction(d.name, drawableParentID, memento.changedConfig.Position,
                        memento.changedConfig.Rotation).Execute();
                    break;
                case Operation.Edit:
                    GameObject sticky = GameFinder.FindDrawable(memento.changedConfig.ID, memento.changedConfig.ParentID)
                        .transform.parent.gameObject;
                    GameStickyNoteManager.Change(sticky, memento.changedConfig);
                    new StickyNoteChangeNetAction(memento.changedConfig).Execute();
                    break;
                case Operation.Delete:
                    GameObject toDelete = GameFinder.GetHighestParent(GameFinder.FindDrawable(memento.originalConfig.ID,
                        memento.originalConfig.ParentID));
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
        public static ReversibleAction CreateReversibleAction()
        {
            return new StickyNoteAction();
        }

        /// <summary>
        /// A new instance of <see cref="StickyNoteAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="StickyNoteAction"/></returns>
        public override ReversibleAction NewInstance()
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
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>the sticky note id</returns>
        public override HashSet<string> GetChangedObjects()
        {
            GameObject stickyNoteDrawable = GameFinder.FindDrawable(memento.originalConfig.ID,
                memento.originalConfig.ParentID);
            if (stickyNoteDrawable != null)
            {
                return new HashSet<string>
                {
                    stickyNoteDrawable.transform.parent.name
                };
            }
            else
            {
                return new HashSet<string>();
            }
        }
    }
}