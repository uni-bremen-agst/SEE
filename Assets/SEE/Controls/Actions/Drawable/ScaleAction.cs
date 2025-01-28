using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.UI;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Scales a drawable type object.
    /// </summary>
    public class ScaleAction : DrawableAction
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
        /// The progress states of the <see cref="ScaleAction"/>.
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
        /// The new scale value.
        /// </summary>
        private Vector3 newScale;

        /// <summary>
        /// The drawable surface on which the selected object is displayed.
        /// </summary>
        private GameObject surface;

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
        /// This struct can store all the information needed to revert or repeat a <see cref="ScaleAction"/>.
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The selected drawable object to scale.
            /// </summary>
            public GameObject SelectedObject;
            /// <summary>
            /// The drawable surface on which the selected object is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The id of the selected object.
            /// </summary>
            public readonly string Id;
            /// <summary>
            /// The old scale of the selected object.
            /// </summary>
            public readonly Vector3 OldScale;
            /// <summary>
            /// The new scale of the selected object.
            /// </summary>
            public readonly Vector3 NewScale;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="selectedObject">The selected drawable object</param>
            /// <param name="surface">The drawable surface on which the selected object is displayed</param>
            /// <param name="id">The id of the selected object</param>
            /// <param name="oldScale">The old scale of the selected object</param>
            /// <param name="newScale">The new scale of the selected object</param>
            public Memento(GameObject selectedObject, GameObject surface, string id,
                Vector3 oldScale, Vector3 newScale)
            {
                SelectedObject = selectedObject;
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Id = id;
                OldScale = oldScale;
                NewScale = newScale;
            }
        }

        /// <summary>
        /// Resets the old selected object, if the action state will leave.
        /// </summary>
        public static void Reset()
        {
            oldSelectedObj = null;
            mouseWasReleased = true;
        }

        /// <summary>
        /// Deactivates the blink effect if it is still active.
        /// If the action was not completed in full, the changes are reset.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            BlinkEffect.Deactivate(selectedObj);
            CollisionDetectionManager.Disable(selectedObj);
            if (progressState != ProgressState.Finish && selectedObj != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedObj);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                GameScaler.SetScale(selectedObj, oldScale);
                new ScaleNetAction(surface.name, surfaceParentName, selectedObj.name, oldScale).Execute();
            }
            ScaleMenu.Instance.Destroy();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.Scale"/>.
        /// It scales the selected object.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            Cancel();

            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// Block for selection.
                    case ProgressState.SelectObject:
                        SelectObject();
                        break;

                    /// Block for scaling.
                    case ProgressState.Scale:
                        if (selectedObj.GetComponent<BlinkEffect>() != null)
                        {
                            /// Gathers the necessary data for scaling and performs the scaling.
                            Scale();

                            /// Initializes the end of scaling.
                            /// When the left mouse button is pressed and released,
                            /// scaling is finished and it switches to the last progress state.
                            if (SEEInput.LeftMouseInteraction())
                            {
                                BlinkEffect.Deactivate(selectedObj);
                                progressState = ProgressState.Finish;
                            }
                        }
                        return false;

                    /// This block completes or resets this action.
                    case ProgressState.Finish:
                        return Finish();
                    default:
                        return false;
                }
                return false;
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
                CollisionDetectionManager.Disable(selectedObj);
                if (progressState != ProgressState.Finish && selectedObj != null)
                {
                    GameObject surface = GameFinder.GetDrawableSurface(selectedObj);
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    GameScaler.SetScale(selectedObj, oldScale);
                    new ScaleNetAction(surface.name, surfaceParentName, selectedObj.name, oldScale).Execute();
                }
                ScaleMenu.Instance.Destroy();

                selectedObj = null;
                progressState = ProgressState.SelectObject;
            }
        }

        /// <summary>
        /// Allows the selection of a drawable type object for scaling, taking into account the object scale in the last run.
        /// It prevents the same object from being accidentally selected again when the left mouse button is not released.
        /// Therefore, after the last action has been successfully completed, the left mouse button must be released to select the same object again.
        /// Additionally, a ValueResetter component is added to the UI Canvas to reset the two static variables after exiting this action type.
        /// The blinking effect is activated to indicate which object has been chosen for scaling.
        /// </summary>
        private void SelectObject()
        {
            /// The selection
            if (Selector.SelectObject(ref selectedObj, ref oldSelectedObj, ref mouseWasReleased, UICanvas.Canvas,
                true, false, true, GetActionStateType(), false))
            {
                /// If an object was already selected,
                /// the Rigidbody and Collision Controller are removed if they are still present.
                if (selectedObj != oldSelectedObj)
                {
                    CollisionDetectionManager.Disable(oldSelectedObj);
                }

                surface = GameFinder.GetDrawableSurface(selectedObj);
                oldSelectedObj = selectedObj;
                oldScale = selectedObj.transform.localScale;
            }

            /// Tracked a released mouse button.
            if (SEEInput.MouseUp(MouseButton.Left))
            {
                mouseWasReleased = true;
            }
            /// Initiates scaling by opening the menu and switching to the corresponding state.
            if (SEEInput.MouseUp(MouseButton.Left) && selectedObj != null)
            {
                ScaleMenu.Enable(selectedObj);
                progressState = ProgressState.Scale;
            }
        }

        /// <summary>
        /// In this block, the necessary data for scaling is gathered, and then scaling is performed.
        /// The data is collected when the mouse wheel is moved.
        /// Optionally, the speed of scaling can be increased by pressing the left Ctrl key.
        /// </summary>
        private void Scale()
        {
            float scaleFactor = 0f;
            bool isScaled = false;
            /// Scale up with normal speed.
            if (SEEInput.ScrollUp() && !Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleUp;
                isScaled = true;
            }
            /// Scale up with faster speed.
            if (SEEInput.ScrollUp() && Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleUpFast;
                isScaled = true;
            }
            /// Scale down with normal speed.
            if (SEEInput.ScrollDown() && !Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleDown;
                isScaled = true;
            }
            /// Scale down with faster speed.
            if (SEEInput.ScrollDown() && Input.GetKey(KeyCode.LeftControl))
            {
                scaleFactor = ValueHolder.ScaleDownFast;
                isScaled = true;
            }

            /// If data has been collected, perform the scaling.
            if (isScaled)
            {
                Scaling(scaleFactor);
            }
        }

        /// <summary>
        /// Performs the scaling.
        /// If the object to be scaled is a Mind Map Node, the branch lines are updated.
        /// </summary>
        /// <param name="scaleFactor">The scale factor by which the existing scale should be multiplied.</param>
        private void Scaling(float scaleFactor)
        {
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            newScale = GameScaler.Scale(selectedObj, scaleFactor);
            ScaleMenu.AssignValue(selectedObj);
            bool refresh = GameMindMap.ReDrawBranchLines(selectedObj);
            new ScaleNetAction(surface.name, surfaceParentName, selectedObj.name, newScale).Execute();
            if (refresh)
            {
                new MindMapRefreshBranchLinesNetAction(surface.name, surfaceParentName,
                    MindMapNodeConf.GetNodeConf(selectedObj)).Execute();
            }
        }

        /// <summary>
        /// This block completes or resets this action.
        /// If no changes were made, it resets.
        /// If there are changes, the action will be successfully completed.
        /// </summary>
        /// <returns>Wheter the action is completed or not.</returns>
        private bool Finish()
        {
            mouseWasReleased = false;
            ScaleMenu.Instance.Destroy();
            newScale = selectedObj.transform.localScale;
            if (oldScale != newScale)
            {
                if (!selectedObj.GetComponent<CollisionController>().IsInCollision())
                {
                    CollisionDetectionManager.Disable(selectedObj);
                    memento = new Memento(selectedObj, GameFinder.GetDrawableSurface(selectedObj),
                        selectedObj.name, oldScale, newScale);
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
            }
            else
            {
                progressState = ProgressState.SelectObject;
                CollisionDetectionManager.Disable(selectedObj);
                selectedObj = null;
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., it scales the object back to its old scale value.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.SelectedObject == null && memento.Id != null)
            {
                memento.SelectedObject = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.Id);
            }

            if (memento.SelectedObject != null)
            {
                GameScaler.SetScale(memento.SelectedObject, memento.OldScale);
                bool refresh = GameMindMap.ReDrawBranchLines(memento.SelectedObject);
                new ScaleNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    memento.SelectedObject.name, memento.OldScale).Execute();
                if (refresh)
                {
                    new MindMapRefreshBranchLinesNetAction(memento.Surface.ID, memento.Surface.ParentID,
                        MindMapNodeConf.GetNodeConf(memento.SelectedObject)).Execute();
                }
            }
            if (memento.SelectedObject != null && memento.SelectedObject.TryGetComponent(out BlinkEffect currentEffect))
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
            if (memento.SelectedObject == null && memento.Id != null)
            {
                memento.SelectedObject = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.Id);
            }
            if (memento.SelectedObject != null)
            {
                GameScaler.SetScale(memento.SelectedObject, memento.NewScale);
                bool refresh = GameMindMap.ReDrawBranchLines(memento.SelectedObject);
                new ScaleNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.SelectedObject.name, memento.NewScale).Execute();
                if (refresh)
                {
                    new MindMapRefreshBranchLinesNetAction(memento.Surface.ID, memento.Surface.ParentID,
                        MindMapNodeConf.GetNodeConf(memento.SelectedObject)).Execute();
                }
            }

            if (memento.SelectedObject != null && memento.SelectedObject.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// A new instance of <see cref="ScaleAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ScaleAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ScaleAction();
        }

        /// <summary>
        /// A new instance of <see cref="ScaleAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ScaleAction"/></returns>
        public override IReversibleAction NewInstance()
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
        /// </summary>
        /// <returns>an empty set or the object name that was changed.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento.SelectedObject == null)
            {
                return new();
            }
            else
            {
                return new()
                {
                    memento.SelectedObject.name
                };
            }
        }
    }
}
