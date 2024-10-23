using Michsky.UI.ModernUIPack;
using SEE.Game;
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
using MoveNetAction = SEE.Net.Actions.Drawable.MoveNetAction;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Moves or rotate a drawable type object.
    /// </summary>
    public class MoveRotateAction : DrawableAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MoveRotateAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The selected drawable type object.
            /// </summary>
            public GameObject SelectedObject;
            /// <summary>
            /// The drawable surface where the selected object is displayed.
            /// </summary>
            public readonly DrawableConfig Surface;
            /// <summary>
            /// The id of the selected object.
            /// </summary>
            public readonly string Id;
            /// <summary>
            /// The old position of the selected object.
            /// </summary>
            public readonly Vector3 OldObjectPosition;
            /// <summary>
            /// The new position of the selected object.
            /// </summary>
            public readonly Vector3 NewObjectPosition;
            /// <summary>
            /// The old local euler angles of the selected object.
            /// </summary>
            public readonly Vector3 OldObjectLocalEulerAngles;
            /// <summary>
            /// The degree by which the object was rotated (the z value of the local euler angles).
            /// </summary>
            public readonly float Degree;
            /// <summary>
            /// Whether it was moved or rotated.
            /// </summary>
            public readonly ProgressState MoveOrRotate;
            /// <summary>
            /// Whether children were included in case the selected object was a mind map node.
            /// </summary>
            public readonly bool IncludeChildren;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="selectedObject">Is the selected drawable type object.</param>
            /// <param name="surface">Is the drawable surface where the selected object is displayed.</param>
            /// <param name="id">Is the id of the selected object.</param>
            /// <param name="oldObjectPosition">The old position of the selected object.</param>
            /// <param name="newObjectPosition">The new position of the selected object.</param>
            /// <param name="oldObjectLocalEulerAngles">The old local euler angles of the selected object.</param>
            /// <param name="degree">The degree on that the object was rotated. (Is the z value of the local euler angeles)</param>
            /// <param name="moveOrRotate">The state if was moved or rotated.</param>
            public Memento(GameObject selectedObject, GameObject surface, string id,
                Vector3 oldObjectPosition, Vector3 newObjectPosition, Vector3 oldObjectLocalEulerAngles,
                float degree, ProgressState moveOrRotate, bool includeChildren)
            {
                SelectedObject = selectedObject;
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Id = id;
                OldObjectPosition = oldObjectPosition;
                NewObjectPosition = newObjectPosition;
                OldObjectLocalEulerAngles = oldObjectLocalEulerAngles;
                Degree = degree;
                MoveOrRotate = moveOrRotate;
                IncludeChildren = includeChildren;
            }
        }

        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="MoveRotateAction"/>
        /// </summary>
        private enum ProgressState
        {
            SelectObject,
            Move,
            Rotate,
            Finish
        }

        /// <summary>
        /// The executed progress state (Move or Rotate)
        /// </summary>
        private ProgressState executedOperation;

        /// <summary>
        /// The selected drawable type object for moving or rotating.
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// The old selected drawable type object, if the user selected a new one
        /// in the same action run.
        /// </summary>
        private GameObject oldSelectedObj;

        /// <summary>
        /// The old position of the selected object.
        /// </summary>
        private Vector3 oldObjectPosition;

        /// <summary>
        /// The old local euler angles of the selected object.
        /// </summary>
        private Vector3 oldObjectLocalEulerAngles;

        /// <summary>
        /// The prefab of the move menu.
        /// </summary>
        private const string switchMenuPrefab = "Prefabs/UI/Drawable/MoveRotatorSwitch";

        /// <summary>
        /// The instance of the switch menu
        /// </summary>
        private GameObject switchMenu;

        /// <summary>
        /// True if the left mouse button was released after selecting a object.
        /// </summary>
        private bool mouseWasReleased = true;

        /// <summary>
        /// The new object position.
        /// </summary>
        private Vector3 newObjectPosition;

        /// <summary>
        /// The new local euler angles.
        /// </summary>
        private Vector3 newObjectLocalEulerAngles;

        /// <summary>
        /// Deactivates the blink effect if it is still active
        /// and destroys the rigidbody and collision controller if there are still active.
        /// If the action was not completed in full (finish), the changes are reset.
        /// Destroys the rotation and move menu if it is still active.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            BlinkEffect.Deactivate(selectedObject);
            CollisionDetectionManager.Disable(selectedObject);
            if (progressState != ProgressState.Finish && selectedObject != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                if (progressState == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(selectedObject, oldObjectPosition,
                        MoveMenu.includeChildren);
                    new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                        oldObjectPosition, MoveMenu.includeChildren).Execute();
                }

                if (progressState == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(selectedObject, oldObjectLocalEulerAngles.z,
                        RotationMenu.includeChildren);
                    new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                        oldObjectLocalEulerAngles.z, RotationMenu.includeChildren).Execute();
                }
            }
            RotationMenu.Instance.Destroy();
            MoveMenu.Instance.Destroy();
            if (switchMenu != null)
            {
                Destroyer.Destroy(switchMenu);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MoveRotator"/>.
        /// It moves or rotates a chosen drawable type object.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            Cancel();

            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// With this block, the user can select a Drawable Type object.
                    case ProgressState.SelectObject:
                        Selection();
                        break;

                    /// With this block, the user can move the object.
                    case ProgressState.Move:
                        Move();
                        break;

                    /// With this block the user can rotate the selected object.
                    case ProgressState.Rotate:
                        Rotate();
                        break;

                    /// With this block, it is checked whether changes have been made.
                    /// If so, the action is completed; otherwise, it is reset.
                    case ProgressState.Finish:
                        return Finish();
                }
            }

            return false;
        }

        /// <summary>
        /// Provides the option to cancel the action.
        /// </summary>
        private void Cancel()
        {
            if (selectedObject != null && SEEInput.Cancel())
            {
                ShowNotification.Info("Canceled", "The action was canceled by the user.");
                BlinkEffect.Deactivate(selectedObject);
                CollisionDetectionManager.Disable(selectedObject);
                if (progressState != ProgressState.Finish && selectedObject != null)
                {
                    GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    if (progressState == ProgressState.Move)
                    {
                        GameMoveRotator.SetPosition(selectedObject, oldObjectPosition,
                            MoveMenu.includeChildren);
                        new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                            oldObjectPosition, MoveMenu.includeChildren).Execute();
                    }

                    if (progressState == ProgressState.Rotate)
                    {
                        GameMoveRotator.SetRotate(selectedObject, oldObjectLocalEulerAngles.z,
                            RotationMenu.includeChildren);
                        new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                            oldObjectLocalEulerAngles.z, RotationMenu.includeChildren).Execute();
                    }
                }
                RotationMenu.Instance.Destroy();
                MoveMenu.Instance.Destroy();
                if (switchMenu != null)
                {
                    Destroyer.Destroy(switchMenu);
                }

                progressState = ProgressState.SelectObject;
                selectedObject = null;
            }
        }

        /// <summary>
        /// Creates the switch menu for selection of move or rotate.
        /// </summary>
        private void InitSwitchMenu()
        {
            switchMenu = PrefabInstantiator.InstantiatePrefab(switchMenuPrefab,
                                                              UICanvas.Canvas.transform, false);
            /// Adds the functionality to the move button
            GameFinder.FindChild(switchMenu, "Move").GetComponent<ButtonManagerBasic>().clickEvent
                .AddListener(() =>
                {
                    progressState = ProgressState.Move;
                    executedOperation = ProgressState.Move;
                    Destroyer.Destroy(switchMenu);
                });
            /// Adds the funcitonality to the rotate button.
            GameFinder.FindChild(switchMenu, "Rotate").GetComponent<ButtonManagerBasic>().clickEvent.
                AddListener(() =>
                {
                    progressState = ProgressState.Rotate;
                    executedOperation = ProgressState.Rotate;
                    oldObjectPosition = selectedObject.transform.localPosition;
                    Destroyer.Destroy(switchMenu);
                });
        }

        /// <summary>
        /// Enables the user to select a <see cref="DrawableType"/> object.
        /// After the selection, a menu is opened, providing the user with options to move and rotate.
        /// The respective choices are initiated by using the corresponding buttons.
        /// Furthermore, the user is allowed to switch the <see cref="DrawableType"/> object before
        /// making the move or rotate selection.
        /// </summary>
        private void Selection()
        {
            if (Selector.SelectObject(ref selectedObject, ref oldSelectedObj, ref mouseWasReleased, UICanvas.Canvas,
                true, false, true))
            {
                oldObjectPosition = selectedObject.transform.localPosition;
                oldObjectLocalEulerAngles = selectedObject.transform.localEulerAngles;
                InitSwitchMenu();
            }

            /// To ensure a object change.
            if (SEEInput.MouseUp(MouseButton.Left) && !mouseWasReleased)
            {
                mouseWasReleased = true;
            }

            /// This block ensures that a change of the <see cref="DrawableType"/> object is possible
            /// before the operation selection.
            EnableObjectChange();
        }

        /// <summary>
        /// Ensures that a change of the <see cref="DrawableType"/> object is possible.
        /// Deselects the current object.
        /// The menu is closed.
        /// The blink effect, rigidbody, and collision controller are removed.
        /// Ensures that the visibility of the Renderer/Canvas is activated.
        /// </summary>
        private void EnableObjectChange()
        {
            if (SEEInput.LeftMouseInteraction()
                && selectedObject != null && mouseWasReleased)
            {
                Destroyer.Destroy(switchMenu);
                BlinkEffect.Deactivate(selectedObject);
                CollisionDetectionManager.Disable(selectedObject);

                /// The following part is needed to ensure that the renderer(s)/canvas is/are enabled.
                if (selectedObject.GetComponent<Renderer>() != null)
                {
                    selectedObject.GetComponent<Renderer>().enabled = true;
                }
                else if (selectedObject.GetComponentsInChildren<Renderer>().Length > 0)
                {
                    foreach (Renderer renderer in selectedObject.GetComponentsInChildren<Renderer>())
                    {
                        renderer.enabled = true;
                    }
                }
                if (selectedObject.GetComponent<Canvas>() != null)
                {
                    selectedObject.GetComponent<Canvas>().enabled = true;
                }
                oldSelectedObj = selectedObject;
                selectedObject = null;
                mouseWasReleased = false;
            }
        }

        /// <summary>
        /// Provides the functionality for moving the object.
        /// The object either follows the mouse movement or the arrow keys can be used.
        /// The mouse movement is stopped and blocked if a collision with any of the borders of the Drawable is detected.
        /// (When moving mind map nodes with the setting that includes children,
        /// the collision controllers of the children are also taken into account.)
        /// Using the arrow keys locks moving with the mouse, which can be unlocked by a middle mouse click.
        /// A middle mouse click toggles the move by mouse state.
        /// There is a faster option for moving by key. It can be toggled by clicking the left Ctrl key down.
        /// To end the move, the left mouse button must be pressed and released.
        ///
        /// Alternatively, through the menu, the object can be moved, and settings for speed, 'move by mouse'
        /// and include children can be chosen as well.
        /// </summary>
        private void Move()
        {
            if (selectedObject.GetComponent<BlinkEffect>() != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                MoveMenu.Enable(selectedObject);
                SwitchManager speedUp = MoveMenu.GetSpeedUpManager();
                SwitchManager moveByMouse = MoveMenu.GetMoveByMouseManager();
                /// For switching the speed.
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    speedUp.isOn = !speedUp.isOn;
                    speedUp.UpdateUI();
                }

                /// Invocation for the option to move the object by key.
                MoveByKey(moveByMouse, speedUp, surface, surfaceParentName);

                /// For switching the move by mouse option.
                if (SEEInput.MouseDown(MouseButton.Middle))
                {
                    moveByMouse.isOn = !moveByMouse.isOn;
                    moveByMouse.UpdateUI();
                }

                /// Checks if any of the child nodes are involved in a collision.
                bool childInCollision = CheckChildrenCollision();

                /// Is executed when move by mouse is active and if the object and none of the children are in collision.
                MoveByMouse(moveByMouse, childInCollision, surface, surfaceParentName);

                /// Initializes the end of the movement.
                if (SEEInput.LeftMouseInteraction())
                {
                    BlinkEffect.Deactivate(selectedObject);
                }
            }
            /// Part 2 of initializing the end: The new position is saved, and the progress state is switched to finish.
            if (SEEInput.MouseUp(MouseButton.Left))
            {
                newObjectPosition = selectedObject.transform.localPosition;
                progressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// To detect a move by key.
        /// It locks the moving by mouse and
        /// moves the object in the respective direction based on the chosen speed
        /// </summary>
        /// <param name="moveByMouse">The switch manager for the move by mouse option from the menu</param>
        /// <param name="speedUp">The switch manager for the speed up option from the menu</param>
        /// <param name="surface">The drawable surface on which the object is displayed.</param>
        /// <param name="surfaceParentName">The parent name of the drawable surface</param>
        private void MoveByKey(SwitchManager moveByMouse, SwitchManager speedUp,
            GameObject surface, string surfaceParentName)
        {
            if (SEEInput.MoveObjectLeft() || SEEInput.MoveObjectRight()
                || SEEInput.MoveObjectUp() || SEEInput.MoveObjectDown())
            {
                ValueHolder.MoveDirection direction = GetDirection();
                moveByMouse.isOn = false;
                moveByMouse.UpdateUI();
                newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, direction,
                    speedUp.isOn, MoveMenu.includeChildren);
                new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                    newObjectPosition, MoveMenu.includeChildren).Execute();
            }
        }

        /// <summary>
        /// Checks if any of the child nodes are involved in a collision.
        /// </summary>
        /// <returns>true if a child node is involved in a collision; otherwise false</returns>
        private bool CheckChildrenCollision()
        {
            bool childInCollision = false;
            if (selectedObject.CompareTag(Tags.MindMapNode))
            {
                CollisionController[] ccs = GameFinder.GetAttachedObjectsObject(selectedObject)
                    .GetComponentsInChildren<CollisionController>();
                foreach (CollisionController cc in ccs)
                {
                    childInCollision = childInCollision || cc.IsInCollision();
                }
            }
            return childInCollision;
        }

        /// <summary>
        /// Moves the selected object based on the mouse position.
        /// This means it follows the mouse position.
        /// Will be executed when move by mouse is active
        /// and if the object and none of the children are in collision.
        /// The children are only necessary for a mind map node with the
        /// include children option.
        /// </summary>
        /// <param name="moveByMouse">The switch manager for the move-by-mouse option from the menu</param>
        /// <param name="childInCollision">Identifies whether any of the children are in collision</param>
        /// <param name="surface">The drawable surface on which the object is displayed.</param>
        /// <param name="surfaceParentName">The parent name of the drawable surface</param>
        private void MoveByMouse(SwitchManager moveByMouse, bool childInCollision,
            GameObject surface, string surfaceParentName)
        {
            if (moveByMouse.isOn && Raycasting.RaycastAnything(out RaycastHit hit)
                && !selectedObject.GetComponent<CollisionController>().IsInCollision()
                && !childInCollision)
            {
                if ((hit.collider.gameObject.CompareTag(Tags.Drawable)
                        && hit.collider.gameObject.Equals(surface))
                    || (GameFinder.HasDrawableSurface(hit.collider.gameObject)
                        && GameFinder.GetDrawableSurface(hit.collider.gameObject).Equals(surface)))
                {
                    newObjectPosition = GameMoveRotator.MoveObjectByMouse(selectedObject,
                        hit.point, MoveMenu.includeChildren);
                    new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                        newObjectPosition, MoveMenu.includeChildren).Execute();
                }
            }
        }

        /// <summary>
        /// With this block the user can rotate the selected object.
        /// A rotation menu is opened.
        /// Use the rotation menu or use the mouse wheel to rotate.
        /// There is a faster option by holding down the left Ctrl key in addition to using the mouse wheel.
        /// To end the rotation, the left mouse button must be pressed and released.
        /// </summary>
        private void Rotate()
        {
            if (selectedObject.GetComponent<BlinkEffect>() != null)
            {
                /// Enables the rotation menu and provides the rotation via menu.
                RotationMenu.Enable(selectedObject);

                /// Checks for mouse wheel movement and sets the required data for rotation via wheel.
                RotateByWheel();

                /// Initializes the end of the rotation.
                if (SEEInput.LeftMouseInteraction())
                {
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();

                }
            }
            /// Part 2 of initializing the end: The progress state is switched to finish.
            if (SEEInput.MouseUp(MouseButton.Left))
            {
                progressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// Checks whether a mouse wheel movement is registered.
        /// If yes, it performs the desired rotation.
        /// The rotation can be accelerated using Left Control.
        /// </summary>
        private void RotateByWheel()
        {
            bool rotate = false;
            Vector3 direction = Vector3.zero;
            float degree = 0;

            /// Rotates forward with normal speed.
            if (SEEInput.ScrollUp() && !Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.forward;
                degree = ValueHolder.Rotate;
                rotate = true;
            }
            /// Rotates forward with fast speed.
            if (SEEInput.ScrollUp() && Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.forward;
                degree = ValueHolder.RotateFast;
                rotate = true;
            }
            /// Rotates back with normal speed.
            if (SEEInput.ScrollDown() && !Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.back;
                degree = ValueHolder.Rotate;
                rotate = true;

            }
            /// Rotates back with fast speed.
            if (SEEInput.ScrollDown() && Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.back;
                degree = ValueHolder.RotateFast;
                rotate = true;
            }

            /// If a mouse wheel movement has been registered, perform the rotation.
            if (rotate)
            {
                PerformRotate(direction, degree);
            }
        }

        /// <summary>
        /// Performs the desired rotation.
        /// </summary>
        /// <param name="direction">The direction of the rotation.</param>
        /// <param name="degree">The degree by which the object is to be rotated.</param>
        private void PerformRotate(Vector3 direction, float degree)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            newObjectLocalEulerAngles = GameMoveRotator.RotateObject(selectedObject, direction,
                degree, RotationMenu.includeChildren);
            if (Tags.DrawableTypes.Contains(selectedObject.tag))
            {
                newObjectPosition = selectedObject.transform.localPosition;
            }
            new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name, direction,
                degree, RotationMenu.includeChildren).Execute();
        }

        /// <summary>
        /// Completes the action, if there are any changes in the position or in the euler angles.
        /// Otherwise it will reset the action.
        /// The action is only completed properly if the object is no longer involved in any collision.
        /// The same applies to children when using mind map nodes with the option to include children.
        /// </summary>
        /// <returns>state of success</returns>
        private bool Finish()
        {
            if (oldObjectPosition != newObjectPosition
                || oldObjectLocalEulerAngles != newObjectLocalEulerAngles)
            {
                /// Checks if a child is in collision.
                bool childInCollision = false;
                if (selectedObject.CompareTag(Tags.MindMapNode))
                {
                    CollisionController[] ccs = GameFinder.GetAttachedObjectsObject(selectedObject)
                        .GetComponentsInChildren<CollisionController>();
                    foreach (CollisionController cc in ccs)
                    {
                        childInCollision = childInCollision || cc.IsInCollision();
                    }
                }

                /// If there is no collision, the action is completed.
                /// To do this, necessary data is queried, and then a Memento is created.
                /// Subsequently, the rigidbodies and collision controllers are destroyed,
                /// the menus are closed, and the progress state of the action is set to
                /// be completed.
                if (selectedObject.GetComponent<CollisionController>() != null
                    && !selectedObject.GetComponent<CollisionController>().IsInCollision() && !childInCollision)
                {
                    float degree = selectedObject.transform.localEulerAngles.z;
                    bool includeChildren = RotationMenu.includeChildren
                        && executedOperation == ProgressState.Rotate ||
                        MoveMenu.includeChildren && executedOperation == ProgressState.Move;
                    memento = new Memento(selectedObject, GameFinder.GetDrawableSurface(selectedObject), selectedObject.name,
                        oldObjectPosition, newObjectPosition, oldObjectLocalEulerAngles, degree, executedOperation,
                        includeChildren);
                    Destroyer.Destroy(selectedObject.GetComponent<Rigidbody>());
                    Destroyer.Destroy(selectedObject.GetComponent<CollisionController>());
                    GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(selectedObject);
                    new RbAndCCDestroyerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                        memento.SelectedObject.name).Execute();
                    RotationMenu.Instance.Destroy();
                    MoveMenu.Instance.Destroy();
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                } else
                {
                    if (executedOperation == ProgressState.Rotate)
                    {
                        /// This code is needed because occasionally a trigger exit is not registered during rotation.
                        /// This block attempts to set the isInCollision value of the Collision Controller to false.
                        /// However, if the object is still in a collision, it will be set back to true by its OnStayCollision method.
                        selectedObject.GetComponent<CollisionController>().SetCollisionToFalse();
                    }
                }
            }
            else
            {
                /// Block for reset.
                CollisionDetectionManager.Disable(selectedObject);
                GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(selectedObject);
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                new RbAndCCDestroyerNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                    selectedObject.name).Execute();
                selectedObject = null;
                RotationMenu.Instance.Destroy();
                MoveMenu.Instance.Destroy();
                progressState = ProgressState.SelectObject;
            }
            return false;
        }

        /// <summary>
        /// Returns the pressed KeyCode of the arrow keys.
        /// </summary>
        /// <returns>The pressed arrow key.</returns>
        private ValueHolder.MoveDirection GetDirection()
        {
            if (SEEInput.MoveObjectLeft())
            {
                return ValueHolder.MoveDirection.Left;
            }
            else if (SEEInput.MoveObjectRight())
            {
                return ValueHolder.MoveDirection.Right;
            }
            else if (SEEInput.MoveObjectUp())
            {
                return ValueHolder.MoveDirection.Up;
            }
            else
            {
                return ValueHolder.MoveDirection.Down;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., moves / rotates back to the old position / euler angles.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.SelectedObject == null && memento.Id != null)
            {
                memento.SelectedObject = GameFinder.FindChild(memento.Surface.GetDrawableSurface(),
                    memento.Id);
            }

            if (memento.SelectedObject != null)
            {
                if (memento.MoveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(memento.SelectedObject, memento.OldObjectPosition,
                                            memento.IncludeChildren);
                    new MoveNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Id,
                        memento.OldObjectPosition, memento.IncludeChildren).Execute();
                }
                else if (memento.MoveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.SelectedObject, memento.OldObjectLocalEulerAngles.z,
                        memento.IncludeChildren);
                    new RotatorNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Id,
                        memento.OldObjectLocalEulerAngles.z, memento.IncludeChildren).Execute();
                }

                GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(
                    GameFinder.GetAttachedObjectsObject(memento.SelectedObject));
                new RbAndCCDestroyerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    memento.SelectedObject.name).Execute();
            }
        }

        /// <summary>
        /// Repeats this action, i.e., moves / rotates again to the new position / euler angles.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.SelectedObject == null && memento.Id != null)
            {
                memento.SelectedObject = GameFinder.FindChild(memento.Surface.GetDrawableSurface(),
                    memento.Id);
            }
            if (memento.SelectedObject != null)
            {
                if (memento.MoveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(memento.SelectedObject, memento.NewObjectPosition,
                        memento.IncludeChildren);
                    new MoveNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Id,
                        memento.NewObjectPosition, memento.IncludeChildren).Execute();
                }
                else if (memento.MoveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.SelectedObject, memento.Degree, memento.IncludeChildren);
                    new RotatorNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Id,
                        memento.Degree, memento.IncludeChildren).Execute();
                }
                GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(
                    GameFinder.GetAttachedObjectsObject(memento.SelectedObject));
                new RbAndCCDestroyerNetAction(memento.Surface.ID, memento.Surface.ParentID,
                    memento.SelectedObject.name).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="MoveRotateAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveRotateAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new MoveRotateAction();
        }

        /// <summary>
        /// A new instance of <see cref="MoveRotateAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveRotateAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.MoveRotator"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MoveRotator;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The id of the moved or rotated object</returns>
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
