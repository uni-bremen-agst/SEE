using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Assets.SEE.Game.Drawable.GameDrawer;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;
using TextConf = SEE.Game.Drawable.Configurations.TextConf;
using UnityEngine.Events;
using SEE.Game.UI.PropertyDialog.Drawable;
using SEE.Game.UI.Notification;
using Toggle = UnityEngine.UI.Toggle;
using TMPro;
using SEE.Game.Drawable;

namespace SEE.Controls.Actions.Drawable
{
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
        /// This struct can store all the information needed to revert or repeat a <see cref="EditAction"/>
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
            public readonly GameObject drawable;
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
                this.drawable = drawable;
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
        /// <returns></returns>
        private bool CheckEquals(DrawableType oldHolder, DrawableType newHolder)
        {
            if (oldHolder is LineConf oldLineHolder && newHolder is LineConf newLineHolder)
            {
                return oldLineHolder.primaryColor.Equals(newLineHolder.primaryColor) &&
                    oldLineHolder.orderInLayer.Equals(newLineHolder.orderInLayer) &&
                    oldLineHolder.thickness.Equals(newLineHolder.thickness) &&
                    oldLineHolder.loop.Equals(newLineHolder.loop) &&
                    oldLineHolder.lineKind.Equals(newLineHolder.lineKind) &&
                    oldLineHolder.tiling.Equals(newLineHolder.tiling);
            }
            if (oldHolder is TextConf oldTextHolder && newHolder is TextConf newTextHolder)
            {
                return oldTextHolder.text.Equals(newTextHolder.text) &&
                    oldTextHolder.fontSize.Equals(newTextHolder.fontSize) &&
                    oldTextHolder.orderInLayer.Equals(newTextHolder.orderInLayer) &&
                    oldTextHolder.fontStyles.Equals(newTextHolder.fontStyles) &&
                    oldTextHolder.fontColor.Equals(newTextHolder.fontColor) &&
                    oldTextHolder.outlineColor.Equals(newTextHolder.outlineColor) &&
                    oldTextHolder.outlineThickness.Equals(newTextHolder.outlineThickness);
            }
            if (oldHolder is ImageConf oldImageHolder && newHolder is ImageConf newImageHolder)
            {
                return oldImageHolder.orderInLayer.Equals(newImageHolder.orderInLayer) &&
                    oldImageHolder.imageColor.Equals(newImageHolder.imageColor);
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
                GameObject drawable = GameFinder.FindDrawable(selectedObj);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                if (oldValueHolder is LineConf oldLineHolder)
                {
                    GameEdit.ChangeLine(selectedObj, oldLineHolder);
                    new EditLineNetAction(drawable.name, drawableParent, oldLineHolder).Execute();
                }
                if (oldValueHolder is TextConf oldTextHolder)
                {
                    GameEdit.ChangeText(selectedObj, oldTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, oldTextHolder).Execute();
                }
                if (oldValueHolder is ImageConf oldImageHolder)
                {
                    GameEdit.ChangeImage(selectedObj, oldImageHolder);
                    new EditImageNetAction(drawable.name, drawableParent, oldImageHolder).Execute();
                }
            }
            TextMenu.Disable();
            LineMenu.disableLineMenu();
            ImageMenu.Disable();
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
                    /// This block allows the selection of a drawable type object for editing, taking into account the object edited in the last run. 
                    /// It prevents the same object from being accidentally selected again when the left mouse button is not released. 
                    /// Therefore, after the last action has been successfully completed, the left mouse button must be released to select the same object again. 
                    /// Additionally, a ValueResetter component is added to the UI Canvas to reset the two static variables after exiting this action type.
                    case ProgressState.SelectObject:
                        if (Input.GetMouseButtonUp(0))
                        {
                            mouseWasReleased = true;
                        }
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj == null &&
                            Raycasting.RaycastAnything(out RaycastHit raycastHit) && 
                            (oldSelectedObj == null || oldSelectedObj != raycastHit.collider.gameObject || (oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased)) &&
                            Tags.DrawableTypes.Contains(raycastHit.collider.gameObject.tag))
                        {
                            selectedObj = raycastHit.collider.gameObject;
                            oldSelectedObj = selectedObj;
                            oldValueHolder = new DrawableType().Get(selectedObj);
                            newValueHolder = new DrawableType().Get(selectedObj);

                            BlinkEffect effect = selectedObj.AddOrGetComponent<BlinkEffect>();
                            effect.SetAllowedActionStateType(GetActionStateType());

                            if (GameObject.Find("UI Canvas").GetComponent<ValueResetter>() == null)
                            {
                                GameObject.Find("UI Canvas").AddComponent<ValueResetter>().SetAllowedState(GetActionStateType());
                            }
                            progressState = ProgressState.OpenEditMenu;
                        }
                        
                        break;
                    /// In this block the right menu for the chosen drawable type will be opened.
                    case ProgressState.OpenEditMenu:
                        if (selectedObj.CompareTag(Tags.Line))
                        {
                            LineMenu.EnableForEditing(selectedObj, newValueHolder);
                        }
                        if (selectedObj.CompareTag(Tags.DText))
                        {
                            TextMenu.EnableForEditing(selectedObj, newValueHolder);
                        }
                        if (selectedObj.CompareTag(Tags.Image))
                        {
                            ImageMenu.Enable(selectedObj, newValueHolder);
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            progressState = ProgressState.Edit;
                        }
                        
                        break;
                    /// This block provides the completion of the action. As soon as the left mouse button is pressed, the completion is initiated.
                    case ProgressState.Edit:
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj.GetComponent<BlinkEffect>() != null)
                        {
                            selectedObj.GetComponent<BlinkEffect>().Deactivate();
                            progressState = ProgressState.Finish;
                        }
                        break;
                    /// This block completes or resets this action.
                    /// If no changes were made, it resets.
                    /// If there are changes the action will be successfull completed.
                    case ProgressState.Finish:
                        mouseWasReleased = false;
                        if (!CheckEquals(oldValueHolder, newValueHolder))
                        {
                            memento = new Memento(selectedObj, oldValueHolder, newValueHolder,
                                     GameFinder.FindDrawable(selectedObj), selectedObj.name);
                            currentState = ReversibleAction.Progress.Completed;
                            return true;
                        } else
                        {
                            selectedObj = null;
                            progressState = ProgressState.SelectObject;
                            LineMenu.disableLineMenu();
                            TextMenu.Disable();
                            ImageMenu.Disable();
                        }
                        break;
                    default:
                        return false;
                }
            }
            return false;     
        }

        /// <summary>
        /// Reverts this action, i.e., restores the old values of the selected object.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.selectedObj == null && memento.id != null)
            {
                memento.selectedObj = GameFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObj != null)
            {
                GameObject drawable = GameFinder.FindDrawable(memento.selectedObj);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                if (memento.oldValueHolder is LineConf oldLineHolder)
                {
                    GameEdit.ChangeLine(memento.selectedObj, oldLineHolder);
                    new EditLineNetAction(drawable.name, drawableParent, oldLineHolder).Execute();
                }
                if (memento.oldValueHolder is TextConf oldTextHolder)
                {
                    GameEdit.ChangeText(memento.selectedObj, oldTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, oldTextHolder).Execute();
                }
                if (memento.oldValueHolder is ImageConf oldImageHolder)
                {
                    GameEdit.ChangeImage(memento.selectedObj, oldImageHolder);
                    new EditImageNetAction(drawable.name, drawableParent, oldImageHolder).Execute();
                }
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
                memento.selectedObj = GameFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObj != null)
            {
                GameObject drawable = GameFinder.FindDrawable(memento.selectedObj);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                if (memento.newValueHolder is LineConf newLineValueHolder)
                {
                    GameEdit.ChangeLine(memento.selectedObj, newLineValueHolder);
                    new EditLineNetAction(drawable.name, drawableParent, newLineValueHolder).Execute();
                }
                if (memento.newValueHolder is TextConf newTextHolder)
                {
                    GameEdit.ChangeText(memento.selectedObj, newTextHolder);
                    new EditTextNetAction(drawable.name, drawableParent, newTextHolder).Execute();
                }
                if (memento.newValueHolder is ImageConf newImageHolder)
                {
                    GameEdit.ChangeImage(memento.selectedObj, newImageHolder);
                    new EditImageNetAction(drawable.name, drawableParent, newImageHolder).Execute();
                }
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