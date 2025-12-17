using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Menu.Drawable;
using SEE.UI.Notification;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;
using SEE.Utils.History;
using SEE.Game.Drawable.ActionHelpers;
using SEE.UI;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides the option to edit a <see cref="DrawableType"/> object.
    /// </summary>
    public class EditAction : DrawableAction
    {
        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="EditAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            OpenEditMenu,
            Edit,
            Finish
        }

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="EditAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// Is the selected drawable type object that should be edited.
            /// </summary>
            public GameObject SelectedObj;
            /// <summary>
            /// The old values of the drawable type object.
            /// </summary>
            public readonly DrawableType OldValueHolder;
            /// <summary>
            /// The new edited values of the drawable type object.
            /// </summary>
            public readonly DrawableType NewValueHolder;
            /// <summary>
            /// The drawable surface on which the drawable type object is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The ID of the drawable type object.
            /// </summary>
            public readonly string ID;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="obj">The selected drawable type object that should be edit</param>
            /// <param name="oldValueHolder">The old values of the drawable type object.</param>
            /// <param name="newValueHolder">The newly edited values of the drawable type object.</param>
            /// <param name="surface">The drawable surface on which the drawable type object is displayed.</param>
            /// <param name="id">The ID of the drawable type object.</param>
            public Memento(GameObject obj, DrawableType oldValueHolder,
                DrawableType newValueHolder, GameObject surface, string id)
            {
                SelectedObj = obj;
                OldValueHolder = oldValueHolder;
                NewValueHolder = newValueHolder;
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                ID = id;
            }
        }

        /// <summary>
        /// The selected drawable type object that should be edited.
        /// </summary>
        private GameObject selectedObj;

        /// <summary>
        /// The selected object of the last run.
        /// </summary>
        private static GameObject oldSelectedObj;

        /// <summary>
        /// True if the left mouse button was released after finish.
        /// It is necessary to prevent the previously selected object from being accidentally selected again.
        /// After the action has successfully completed, it starts again, allowing for the selection of a new object.
        /// This option enables the immediate selection of another object while holding down the mouse button.
        /// </summary>
        private static bool mouseWasReleased = true;

        /// <summary>
        /// The old values of the selected drawable type.
        /// </summary>
        private DrawableType oldValueHolder;

        /// <summary>
        /// The newly edited values of the selected drawable type.
        /// </summary>
        private DrawableType newValueHolder;

        /// <summary>
        /// Resets the old selected object, if the action state will be left.
        /// </summary>
        public static void Reset()
        {
            oldSelectedObj = null;
            mouseWasReleased = true;
        }

        /// <summary>
        /// This method checks if there are any changes in the editable values.
        /// </summary>
        /// <param name="oldHolder">The holder for the old values.</param>
        /// <param name="newHolder">The holder for the new values.</param>
        /// <returns>true if all is equal</returns>
        private bool CheckEquals(DrawableType oldHolder, DrawableType newHolder)
        {
            if (oldHolder is LineConf oldLineHolder && newHolder is LineConf newLineHolder)
            {
                return oldLineHolder.PrimaryColor.Equals(newLineHolder.PrimaryColor)
                    && oldLineHolder.SecondaryColor.Equals(newLineHolder.SecondaryColor)
                    && oldLineHolder.OrderInLayer.Equals(newLineHolder.OrderInLayer)
                    && oldLineHolder.Thickness.Equals(newLineHolder.Thickness)
                    && oldLineHolder.Loop.Equals(newLineHolder.Loop)
                    && oldLineHolder.LineKind.Equals(newLineHolder.LineKind)
                    && oldLineHolder.ColorKind.Equals(newLineHolder.ColorKind)
                    && oldLineHolder.Tiling.Equals(newLineHolder.Tiling)
                    && oldLineHolder.FillOutStatus.Equals(newLineHolder.FillOutStatus)
                    && oldLineHolder.FillOutColor.Equals(newLineHolder.FillOutColor);
            }

            if (oldHolder is TextConf oldTextHolder && newHolder is TextConf newTextHolder)
            {
                return oldTextHolder.Text.Equals(newTextHolder.Text)
                    && oldTextHolder.FontSize.Equals(newTextHolder.FontSize)
                    && oldTextHolder.OrderInLayer.Equals(newTextHolder.OrderInLayer)
                    && oldTextHolder.FontStyles.Equals(newTextHolder.FontStyles)
                    && oldTextHolder.FontColor.Equals(newTextHolder.FontColor)
                    && oldTextHolder.OutlineColor.Equals(newTextHolder.OutlineColor)
                    && oldTextHolder.OutlineThickness.Equals(newTextHolder.OutlineThickness)
                    && oldTextHolder.IsOutlined.Equals(newTextHolder.IsOutlined);
            }

            if (oldHolder is ImageConf oldImageHolder && newHolder is ImageConf newImageHolder)
            {
                return oldImageHolder.OrderInLayer.Equals(newImageHolder.OrderInLayer)
                    && oldImageHolder.ImageColor.Equals(newImageHolder.ImageColor)
                    && oldImageHolder.EulerAngles.Equals(newImageHolder.EulerAngles);
            }

            if (oldHolder is MindMapNodeConf oldConf && newHolder is MindMapNodeConf newConf)
            {
                return oldConf.ParentNode.Equals(newConf.ParentNode)
                    && oldConf.NodeKind.Equals(newConf.NodeKind)
                    && CheckEquals(oldConf.BorderConf, newConf.BorderConf)
                    && CheckEquals(oldConf.TextConf, newConf.TextConf)
                    && CheckEquals(oldConf.BranchLineConf, newConf.BranchLineConf)
                    && oldConf.OrderInLayer.Equals(newConf.OrderInLayer);
            }

            /// This case will be needed for mind-map nodes.
            return oldHolder == null && newHolder == null;
        }

        /// <summary>
        /// Deactivates the blink effect if it is still active and hides the text and line menu.
        /// If the action was not completed in full, the changes are reset.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            BlinkEffect.Deactivate(selectedObj);
            if (progressState != ProgressState.Finish && selectedObj != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedObj);
                DrawableType.Edit(selectedObj, oldValueHolder, surface);
            }
            TextMenu.Instance.Disable();
            LineMenu.Instance.Disable();
            ImageMenu.Instance.Destroy();
            MindMapEditMenu.Instance.Destroy();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Edit"/>.
        /// It allows editing of the drawable type objects.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            /// Block for canceling the action.
            Cancel();

            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    case ProgressState.SelectObject:
                        Selection();
                        break;
                    case ProgressState.OpenEditMenu:
                        OpenMenu();
                        break;
                    case ProgressState.Edit:
                        Edit();
                        break;
                    case ProgressState.Finish:
                        return Finish();
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if (selectedObj != null && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                BlinkEffect.Deactivate(selectedObj);
                if (progressState != ProgressState.Finish && selectedObj != null)
                {
                    GameObject surface = GameFinder.GetDrawableSurface(selectedObj);
                    DrawableType.Edit(selectedObj, oldValueHolder, surface);
                }
                selectedObj = null;
                progressState = ProgressState.SelectObject;
                TextMenu.Instance.Disable();
                LineMenu.Instance.Disable();
                ImageMenu.Instance.Destroy();
                MindMapEditMenu.Instance.Destroy();
            }
        }

        /// <summary>
        /// Allows the selection of a drawable type object for editing,
        /// taking into account the object edited in the last run.
        /// It prevents the same object from being accidentally selected again
        /// when the left mouse button is not released.
        /// Therefore, after the last action has been successfully completed,
        /// the left mouse button must be released to select the same object again.
        /// Additionally, a ValueResetter component is added to the UI Canvas to reset
        /// the two static variables after exiting this action type.
        /// </summary>
        private void Selection()
        {
            if (Selector.SelectObject(ref selectedObj, ref oldSelectedObj, ref mouseWasReleased, UICanvas.Canvas,
                false, true, false, GetActionStateType()))
            {
                oldValueHolder = DrawableType.Get(selectedObj);
                newValueHolder = DrawableType.Get(selectedObj);
                progressState = ProgressState.OpenEditMenu;
            }

            if (SEEInput.MouseUp(MouseButton.Left))
            {
                mouseWasReleased = true;
            }
        }

        /// <summary>
        /// Opens the appropriate menu for editing the selected Drawable Type Object.
        /// Once a left mouse button up is registered, the progress state is switched to Edit.
        /// </summary>
        private void OpenMenu()
        {
            switch (selectedObj.tag)
            {
                case Tags.Line:
                    if (!LineMenu.Instance.IsOpen())
                    {
                        LineMenu.Instance.EnableForEditing(selectedObj, newValueHolder);
                    }
                    break;
                case Tags.DText:
                    if (!TextMenu.Instance.IsOpen())
                    {
                        TextMenu.EnableForEditing(selectedObj, newValueHolder);
                    }
                    break;
                case Tags.Image:
                    if (!ImageMenu.Instance.IsOpen())
                    {
                        ImageMenu.Enable(selectedObj, newValueHolder);
                    }
                    break;
                case Tags.MindMapNode:
                    if (!MindMapEditMenu.Instance.IsOpen())
                    {
                        MindMapEditMenu.Enable(selectedObj, newValueHolder);
                    }
                    break;
                default:
                    ShowNotification.Info("Object type not recognized",
                        "The menu cannot be opened because the type of the object was not recognized.");
                    break;
            }
            if (SEEInput.MouseUp(MouseButton.Left))
            {
                progressState = ProgressState.Edit;
            }
        }

        /// <summary>
        /// Provides the completion of the edit action.
        /// As soon as the left mouse button is pressed, the completion is initiated.
        /// It deactivates the <see cref="BlinkEffect"/> and sets the progress state to finish.
        /// </summary>
        private void Edit()
        {
            if (SEEInput.LeftMouseInteraction()
                && selectedObj.GetComponent<BlinkEffect>() != null)
            {
                selectedObj.GetComponent<BlinkEffect>().Deactivate();
                progressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// Completes or resets this action.
        /// If no changes were made, it resets.
        /// If there are changes the action will be successfull completed.
        /// </summary>
        /// <returns>whatever the success of the editing is.</returns>
        private bool Finish()
        {
            mouseWasReleased = false;
            if (!CheckEquals(oldValueHolder, newValueHolder))
            {
                memento = new Memento(selectedObj, oldValueHolder, newValueHolder,
                         GameFinder.GetDrawableSurface(selectedObj), selectedObj.name);
                CurrentState = IReversibleAction.Progress.Completed;
                return true;
            }
            else
            {
                selectedObj = null;
                progressState = ProgressState.SelectObject;
                LineMenu.Instance.Disable();
                TextMenu.Instance.Disable();
                ImageMenu.Instance.Destroy();
                MindMapEditMenu.Instance.Destroy();
                return false;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the old values of the selected object.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.SelectedObj == null && memento.ID != null)
            {
                memento.SelectedObj = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.ID);
            }

            if (memento.SelectedObj != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(memento.SelectedObj);
                DrawableType.Edit(memento.SelectedObj, memento.OldValueHolder, surface);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., assigns the new changed values to the selected object.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.SelectedObj == null && memento.ID != null)
            {
                memento.SelectedObj = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.ID);
            }

            if (memento.SelectedObj != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(memento.SelectedObj);
                DrawableType.Edit(memento.SelectedObj, memento.NewValueHolder, surface);
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new EditAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Edit"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Edit;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.SelectedObj == null && memento.SelectedObj == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.SelectedObj.name
                };
            }
        }
    }
}