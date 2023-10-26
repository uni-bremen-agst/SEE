using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Scales a drawable type object.
    /// </summary>
    public class ScaleAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="ScaleAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            Scale,
            Finish
        }

        /// <summary>
        /// The selected game object that should be scaled.
        /// </summary>
        private GameObject selectedObj = null;

        /// <summary>
        /// The old scale value.
        /// </summary>
        private Vector3 oldScale;

        /// <summary>
        /// The new scale value
        /// </summary>
        private Vector3 newScale;

        /// <summary>
        /// The drawable on that the selected object is displayed.
        /// </summary>
        private GameObject drawable;

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
        /// Resets the old selected object, if the action state will leave.
        /// </summary>
        public static void Reset()
        {
            oldSelectedObj = null;
            mouseWasReleased = true;
        }

        /// <summary>
        /// Deactivates the blink effect if, it is still active.
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
                GameScaler.SetScale(selectedObj, oldScale);
                new ScaleNetAction(drawable.name, drawableParent, selectedObj.name, oldScale).Execute();
            }
            ScaleMenu.Disable();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Scale"/>.
        /// It scales the selected objec.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// This block allows the selection of a drawable type object for scaling, taking into account the object scales in the last run. 
                    /// It prevents the same object from being accidentally selected again when the left mouse button is not released. 
                    /// Therefore, after the last action has been successfully completed, the left mouse button must be released to select the same object again. 
                    /// Additionally, a ValueResetter component is added to the UI Canvas to reset the two static variables after exiting this action type.
                    /// The blinking effect is activated to indicate which object has been chosen for scaling.
                    case ProgressState.SelectObject:
                        if (Input.GetMouseButtonUp(0))
                        {
                            mouseWasReleased = true;
                        }
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj == null
                            && Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                            (oldSelectedObj == null || oldSelectedObj != raycastHit.collider.gameObject || (oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased)) &&
                            GameFinder.hasDrawable(raycastHit.collider.gameObject))
                        {
                            selectedObj = raycastHit.collider.gameObject;
                            drawable = GameFinder.FindDrawable(selectedObj);
                            oldSelectedObj = selectedObj;

                            BlinkEffect effect = selectedObj.AddOrGetComponent<BlinkEffect>();
                            effect.SetAllowedActionStateType(GetActionStateType());
                            oldScale = selectedObj.transform.localScale;
                            if (GameObject.Find("UI Canvas").GetComponent<ValueResetter>() == null)
                            {
                                GameObject.Find("UI Canvas").AddComponent<ValueResetter>().SetAllowedState(GetActionStateType());
                            }
                        }
                        if (Input.GetMouseButtonUp(0) && selectedObj != null)
                        {
                            ScaleMenu.Enable(selectedObj);
                            progressState = ProgressState.Scale;
                        }
                        break;

                    /// In this block, scaling is performed using the mouse wheel, and optionally, the left Ctrl key (faster). 
                    /// When the left mouse button is pressed and released, scaling is finish and it switches to the last progress state.
                    case ProgressState.Scale:
                        if (selectedObj.GetComponent<BlinkEffect>() != null && selectedObj.GetComponent<BlinkEffect>().GetLoopStatus())
                        {
                            string drawableParentName = GameFinder.GetDrawableParentName(drawable);
                            float scaleFactor = 0f;
                            bool isScaled = false;

                            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
                            {
                                scaleFactor = 1.01f;
                                isScaled = true;
                            }
                            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
                            {
                                scaleFactor = 1.1f;
                                isScaled = true;
                            }

                            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
                            {
                                scaleFactor = 0.99f;
                                isScaled = true;

                            }
                            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
                            {
                                scaleFactor = 0.9f;
                                isScaled = true;
                            }
                            if (isScaled)
                            {
                                newScale = GameScaler.Scale(selectedObj, scaleFactor);
                                ScaleMenu.AssignValue(selectedObj);
                                new ScaleNetAction(drawable.name, drawableParentName, selectedObj.name, newScale).Execute();
                            }
                            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj.GetComponent<BlinkEffect>() != null)
                            {
                                selectedObj.GetComponent<BlinkEffect>().Deactivate();
                                progressState = ProgressState.Finish;
                            }
                        }
                        return false;

                    /// This block completes or resets this action.
                    /// If no changes were made, it resets.
                    /// If there are changes the action will be successfull completed.
                    case ProgressState.Finish:
                        mouseWasReleased = false;
                        ScaleMenu.Disable();
                        if (oldScale != newScale)
                        {
                            memento = new Memento(selectedObj, GameFinder.FindDrawable(selectedObj), selectedObj.name, oldScale, newScale);
                            currentState = ReversibleAction.Progress.Completed;
                            return true;
                        } else
                        {
                            selectedObj = null;
                            progressState = ProgressState.SelectObject;
                        }
                        break;
                    default:
                        return false;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="ScaleAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The selected drawable object to scale
            /// </summary>
            public GameObject selectedObject;
            /// <summary>
            /// The drawable on that the selected object is displayed.
            /// </summary>
            public readonly GameObject drawable;
            /// <summary>
            /// The id of the selected object
            /// </summary>
            public readonly string id;
            /// <summary>
            /// The old scale of the selected object.
            /// </summary>
            public readonly Vector3 oldScale;
            /// <summary>
            /// The new scale of the selected object.
            /// </summary>
            public readonly Vector3 newScale;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="selectedObject">The selected drawable object</param>
            /// <param name="drawable">The drawable on that the selected object is displayed</param>
            /// <param name="id">The id of the selected object</param>
            /// <param name="oldScale">The old scale of the selected object</param>
            /// <param name="newScale">The new scale of the selected object</param>
            public Memento(GameObject selectedObject, GameObject drawable, string id,
                Vector3 oldScale, Vector3 newScale)
            {
                this.selectedObject = selectedObject;
                this.drawable = drawable;
                this.id = id;
                this.oldScale = oldScale;
                this.newScale = newScale;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it scales the object back to it's old scale value.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObject != null)
            {
                GameObject drawable = GameFinder.FindDrawable(memento.selectedObject);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                GameScaler.SetScale(memento.selectedObject, memento.oldScale);
                new ScaleNetAction(drawable.name, drawableParent, memento.selectedObject.name, memento.oldScale).Execute();
            }
            if (memento.selectedObject != null && memento.selectedObject.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// Repeats this action, i.e., it scales the object to the new scale value.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameFinder.FindChild(memento.drawable, memento.id);
            }
            if (memento.selectedObject != null)
            {
                GameObject drawable = GameFinder.FindDrawable(memento.selectedObject);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                GameScaler.SetScale(memento.selectedObject, memento.newScale);
                new ScaleNetAction(drawable.name, drawableParent, memento.selectedObject.name, memento.newScale).Execute();
            }

            if (memento.selectedObject != null && memento.selectedObject.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// A new instance of <see cref="ScaleAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ScaleAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new ScaleAction();
        }

        /// <summary>
        /// A new instance of <see cref="ScaleAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ScaleAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Scale"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Scale;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>an empty set or the object name that was changed.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.selectedObject == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.selectedObject.name
                };
            }
        }

    }
}