using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Drawable;
using SEE.Game.UI.Menu.Drawable;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// This class provides the option to edit a <see cref="DrawableType"/> object.
    /// </summary>
    public class EditAction : AbstractPlayerAction
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
            /// Is the selected drawable type object that should be edit.
            /// </summary>
            public GameObject selectedObj;
            /// <summary>
            /// The old values of the drawable type object.
            /// </summary>
            public readonly DrawableType oldValueHolder;
            /// <summary>
            /// The new edited values of the drawable type object.
            /// </summary>
            public readonly DrawableType newValueHolder;
            /// <summary>
            /// The drawable on that the drawable type object is displayed.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// The id of the drawable type object.
            /// </summary>
            public readonly string id;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="obj">Is the selected drawable type object that should be edit</param>
            /// <param name="oldValueHolder">The old values of the drawable type object.</param>
            /// <param name="newValueHolder">The new edited values of the drawable type object.</param>
            /// <param name="drawable">The drawable on that the drawable type object is displayed.</param>
            /// <param name="id">The id of the drawable type object.</param>
            public Memento(GameObject obj, DrawableType oldValueHolder,
                DrawableType newValueHolder, GameObject drawable, string id)
            {
                this.selectedObj = obj;
                this.oldValueHolder = oldValueHolder;
                this.newValueHolder = newValueHolder;
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.id = id;
            }
        }
        /// <summary>
        /// The selected drawable type object that should be edit.
        /// </summary>
        private GameObject selectedObj;

        /// <summary>
        /// The selected object of the last run.
        /// </summary>
        private static GameObject oldSelectedObj;

        /// <summary>
        /// Bool that represents that the left mouse button was released after finish.
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
        /// The new edited values of the selected drawable type.
        /// </summary>
        private DrawableType newValueHolder;

        /// <summary>
        /// Resets the old selected object, if the action state will leave.
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
        /// <returns>whatever the comparison results in</returns>
        private bool CheckEquals(DrawableType oldHolder, DrawableType newHolder)
        {
            if (oldHolder is LineConf oldLineHolder && newHolder is LineConf newLineHolder)
            {
                return oldLineHolder.primaryColor.Equals(newLineHolder.primaryColor)
                    && oldLineHolder.secondaryColor.Equals(newLineHolder.secondaryColor)
                    && oldLineHolder.orderInLayer.Equals(newLineHolder.orderInLayer)
                    && oldLineHolder.thickness.Equals(newLineHolder.thickness)
                    && oldLineHolder.loop.Equals(newLineHolder.loop)
                    && oldLineHolder.lineKind.Equals(newLineHolder.lineKind)
                    && oldLineHolder.colorKind.Equals(newLineHolder.colorKind)
                    && oldLineHolder.tiling.Equals(newLineHolder.tiling);
            }

            if (oldHolder is TextConf oldTextHolder && newHolder is TextConf newTextHolder)
            {
                return oldTextHolder.text.Equals(newTextHolder.text)
                    && oldTextHolder.fontSize.Equals(newTextHolder.fontSize)
                    && oldTextHolder.orderInLayer.Equals(newTextHolder.orderInLayer)
                    && oldTextHolder.fontStyles.Equals(newTextHolder.fontStyles)
                    && oldTextHolder.fontColor.Equals(newTextHolder.fontColor)
                    && oldTextHolder.outlineColor.Equals(newTextHolder.outlineColor)
                    && oldTextHolder.outlineThickness.Equals(newTextHolder.outlineThickness)
                    && oldTextHolder.outlineStatus.Equals(newTextHolder.outlineStatus);
            }

            if (oldHolder is ImageConf oldImageHolder && newHolder is ImageConf newImageHolder)
            {
                return oldImageHolder.orderInLayer.Equals(newImageHolder.orderInLayer)
                    && oldImageHolder.imageColor.Equals(newImageHolder.imageColor);
            }

            if (oldHolder is MindMapNodeConf oldConf && newHolder is MindMapNodeConf newConf)
            {
                return oldConf.parentNode.Equals(newConf.parentNode)
                    && oldConf.nodeKind.Equals(newConf.nodeKind)
                    && CheckEquals(oldConf.borderConf, newConf.borderConf)
                    && CheckEquals(oldConf.textConf, newConf.textConf)
                    && CheckEquals(oldConf.branchLineConf, newConf.branchLineConf);
            }

            /// This case will be needed for mind map change node kind case.
            if (oldHolder == null && newHolder == null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deactivates the blink effect if, it is still active and hides the text and line menu.
        /// If the action was not completed in full, the changes are reset.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (selectedObj != null && selectedObj.GetComponent<BlinkEffect>() != null)
            {
                selectedObj.GetComponent<BlinkEffect>().Deactivate();
            }
            if (progressState != ProgressState.Finish && selectedObj != null)
            {
                GameObject drawable = GameFinder.GetDrawable(selectedObj);
                DrawableType.Edit(selectedObj, oldValueHolder, drawable);
            }
            TextMenu.Disable();
            LineMenu.DisableLineMenu();
            ImageMenu.Disable();
            MindMapEditMenu.Disable();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Edit"/>.
        /// It allows editing of the drawable type objects.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
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
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj == null
                && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (oldSelectedObj == null || oldSelectedObj != raycastHit.collider.gameObject
                    || (oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased))
                && Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))
            {
                selectedObj = raycastHit.collider.gameObject;
                oldSelectedObj = selectedObj;
                oldValueHolder = DrawableType.Get(selectedObj);
                newValueHolder = DrawableType.Get(selectedObj);

                selectedObj.AddOrGetComponent<BlinkEffect>();

                if (GameObject.Find("UI Canvas").GetComponent<ValueResetter>() == null)
                {
                    GameObject.Find("UI Canvas").AddComponent<ValueResetter>().
                        SetAllowedState(GetActionStateType());
                }
                progressState = ProgressState.OpenEditMenu;
            }

            if (Input.GetMouseButtonUp(0))
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
                    if (!LineMenu.IsOpen())
                        LineMenu.EnableForEditing(selectedObj, newValueHolder);
                    break;
                case Tags.DText:
                    if (!TextMenu.IsOpen())
                        TextMenu.EnableForEditing(selectedObj, newValueHolder);
                    break;
                case Tags.Image:
                    if (!ImageMenu.IsOpen())
                        ImageMenu.Enable(selectedObj, newValueHolder);
                    break;
                case Tags.MindMapNode:
                    if (!MindMapEditMenu.IsOpen())
                        MindMapEditMenu.Enable(selectedObj, newValueHolder);
                    break;

            }
            if (Input.GetMouseButtonUp(0))
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
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
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
                         GameFinder.GetDrawable(selectedObj), selectedObj.name);
                currentState = ReversibleAction.Progress.Completed;
                return true;
            }
            else
            {
                selectedObj = null;
                progressState = ProgressState.SelectObject;
                LineMenu.DisableLineMenu();
                TextMenu.Disable();
                ImageMenu.Disable();
                MindMapEditMenu.Disable();
                return false;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., restores the old values of the selected object.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.selectedObj == null && memento.id != null)
            {
                memento.selectedObj = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.id);
            }

            if (memento.selectedObj != null)
            {
                GameObject drawable = GameFinder.GetDrawable(memento.selectedObj);
                DrawableType.Edit(memento.selectedObj, memento.oldValueHolder, drawable);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., assigns the new changed values to the selected object.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.selectedObj == null && memento.id != null)
            {
                memento.selectedObj = GameFinder.FindChild(memento.drawable.GetDrawable(), memento.id);
            }

            if (memento.selectedObj != null)
            {
                GameObject drawable = GameFinder.GetDrawable(memento.selectedObj);
                DrawableType.Edit(memento.selectedObj, memento.newValueHolder, drawable);
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EditAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditAction"/></returns>
        public override ReversibleAction NewInstance()
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
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.selectedObj == null && memento.selectedObj == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.selectedObj.name
                };
            }
        }
    }
}