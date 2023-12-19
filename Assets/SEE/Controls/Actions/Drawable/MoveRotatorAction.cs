using Michsky.UI.ModernUIPack;
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
using MoveNetAction = SEE.Net.Actions.Drawable.MoveNetAction;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Moves or rotate a drawable type object.
    /// </summary>
    public class MoveRotatorAction : AbstractPlayerAction
    {
        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="MoveRotatorAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// Is the selected drawable type object.
            /// </summary>
            public GameObject selectedObject;
            /// <summary>
            /// Is the drawable where the selected object is displayed.
            /// </summary>
            public readonly DrawableConfig drawable;
            /// <summary>
            /// Is the id of the selected object.
            /// </summary>
            public readonly string id;
            /// <summary>
            /// The old position of the selected object.
            /// </summary>
            public readonly Vector3 oldObjectPosition;
            /// <summary>
            /// The new position of the selected object.
            /// </summary>
            public readonly Vector3 newObjectPosition;
            /// <summary>
            /// The old local euler angles of the selected object.
            /// </summary>
            public readonly Vector3 oldObjectLocalEulerAngles;
            /// <summary>
            /// The degree on that the object was rotated. (Is the z value of the local euler angeles)
            /// </summary>
            public readonly float degree;
            /// <summary>
            /// The state if was moved or rotated.
            /// </summary>
            public readonly ProgressState moveOrRotate;
            /// <summary>
            /// The state if children was included if the selected object was a mind map node.
            /// </summary>
            public readonly bool includeChildren;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="selectedObject">Is the selected drawable type object.</param>
            /// <param name="drawable">Is the drawable where the selected object is displayed.</param>
            /// <param name="id">Is the id of the selected object.</param>
            /// <param name="oldObjectPosition">The old position of the selected object.</param>
            /// <param name="newObjectPosition">The new position of the selected object.</param>
            /// <param name="oldObjectLocalEulerAngles">The old local euler angles of the selected object.</param>
            /// <param name="degree">The degree on that the object was rotated. (Is the z value of the local euler angeles)</param>
            /// <param name="moveOrRotate">The state if was moved or rotated.</param>
            public Memento(GameObject selectedObject, GameObject drawable, string id,
                Vector3 oldObjectPosition, Vector3 newObjectPosition, Vector3 oldObjectLocalEulerAngles,
                float degree, ProgressState moveOrRotate, bool includeChildren)
            {
                this.selectedObject = selectedObject;
                this.drawable = DrawableConfigManager.GetDrawableConfig(drawable);
                this.id = id;
                this.oldObjectPosition = oldObjectPosition;
                this.newObjectPosition = newObjectPosition;
                this.oldObjectLocalEulerAngles = oldObjectLocalEulerAngles;
                this.degree = degree;
                this.moveOrRotate = moveOrRotate;
                this.includeChildren = includeChildren;
            }
        }

        /// <summary>
        /// Holds the current progress state.
        /// </summary>
        private ProgressState progressState = ProgressState.SelectObject;

        /// <summary>
        /// The progress states of the <see cref="MoveRotatorAction"/>
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
        /// The selected drawable type object for moveing or rotating.
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// The old selected drawable type object, if the user select a new one
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
        /// Represents that the left mouse button was released after selecting a object.
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
        /// Deactivates the blink effect if, it is still active.
        /// And destroys the rigidbody and collision controller if it is still active.
        /// If the action was not completed in full (finish), the changes are reset.
        /// Destoryes the rotation and move menu if is still active.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (selectedObject != null)
            {
                if (selectedObject.GetComponent<BlinkEffect>() != null)
                {
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();
                }
                if (selectedObject.GetComponent<Rigidbody>() != null)
                {
                    Destroyer.Destroy(selectedObject.GetComponent<Rigidbody>());
                }
                if (selectedObject.GetComponent<CollisionController>() != null)
                {
                    Destroyer.Destroy(selectedObject.GetComponent<CollisionController>());
                }
            }
            if (progressState != ProgressState.Finish && selectedObject != null)
            {
                GameObject drawable = GameFinder.GetDrawable(selectedObject);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                if (progressState == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(selectedObject, oldObjectPosition,
                        MoveMenu.includeChildren);
                    new MoveNetAction(drawable.name, drawableParent, selectedObject.name,
                        oldObjectPosition, MoveMenu.includeChildren).Execute();
                }

                if (progressState == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(selectedObject, oldObjectLocalEulerAngles.z,
                        RotationMenu.includeChildren);
                    new RotatorNetAction(drawable.name, drawableParent, selectedObject.name,
                        oldObjectLocalEulerAngles.z, RotationMenu.includeChildren).Execute();
                }
            }
            RotationMenu.Disable();
            MoveMenu.Disable();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MoveRotator"/>.
        /// It moves or rotate a chosen drawable type object.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
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
                return false;
            }
            return false;
        }

        /// <summary>
        /// Enables the user to select a <see cref="DrawableType"/> object. 
        /// After the selection, a menu is opened, providing the user with options to move and rotate. 
        /// The respective choices are initiated by using the corresponding buttons. 
        /// Furthermore, the user is allowed to switch the <see cref="DrawableType"/> object before making the move or rotate selection.
        /// </summary>
        private void Selection()
        {
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                && Raycasting.RaycastAnything(out RaycastHit raycastHit) && selectedObject == null
                && (oldSelectedObj == null || oldSelectedObj != raycastHit.collider.gameObject
                    || (oldSelectedObj == raycastHit.collider.gameObject && mouseWasReleased))
                && GameFinder.hasDrawable(raycastHit.collider.gameObject))
            {
                selectedObject = raycastHit.collider.gameObject;
                oldObjectPosition = selectedObject.transform.localPosition;
                oldObjectLocalEulerAngles = selectedObject.transform.localEulerAngles;

                /// Adds the necessary components to the object.
                /// The rigidbody and the collision controller are needed to detect a collision with a border.
                selectedObject.AddOrGetComponent<Rigidbody>().isKinematic = true;
                selectedObject.AddOrGetComponent<CollisionController>();
                selectedObject.AddOrGetComponent<BlinkEffect>();

                mouseWasReleased = false;

                switchMenu = PrefabInstantiator.InstantiatePrefab(switchMenuPrefab,
                            GameObject.Find("UI Canvas").transform, false);
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

            /// To ensure a object change.
            if (Input.GetMouseButtonUp(0) && !mouseWasReleased)
            {
                mouseWasReleased = true;
            }

            /// This block ensures that a change of the <see cref="DrawableType"/> object is possible before the operation selection.
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
            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                && selectedObject != null && mouseWasReleased)
            {
                Destroyer.Destroy(switchMenu);
                Destroyer.Destroy(selectedObject.GetComponent<BlinkEffect>());
                Destroyer.Destroy(selectedObject.GetComponent<Rigidbody>());
                Destroyer.Destroy(selectedObject.GetComponent<CollisionController>());

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
        /// The object either follows the mouse movement, or the arrow keys can be used.
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
                GameObject drawable = GameFinder.GetDrawable(selectedObject);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);

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
                MoveByKey(moveByMouse, speedUp, drawable, drawableParentName);

                /// For switching the move by mouse option.
                if (Input.GetMouseButtonDown(2))
                {
                    moveByMouse.isOn = !moveByMouse.isOn;
                    moveByMouse.UpdateUI();
                }

                /// Checks if any of the child nodes are involved in a collision.
                bool childInCollision = CheckChildrenCollision();

                /// Is executed when move by mouse is active and if the object and none of the children are in collision.
                MoveByMouse(moveByMouse, childInCollision, drawable, drawableParentName);

                /// Initializes the end of the movement.
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                {
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();
                }
            }
            /// Part 2 of initializing the end: The new position is saved, and the progress state is switched to finish.
            if (Input.GetMouseButtonUp(0))
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
        /// <param name="drawable">The drawable on that the object is displayed.</param>
        /// <param name="drawableParentName">The parent name of the drawable</param>
        private void MoveByKey(SwitchManager moveByMouse, SwitchManager speedUp,
            GameObject drawable, string drawableParentName)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
                || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
            {
                KeyCode key = GetArrowKey();
                moveByMouse.isOn = false;
                moveByMouse.UpdateUI();
                newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, key,
                    speedUp.isOn, MoveMenu.includeChildren);
                new MoveNetAction(drawable.name, drawableParentName, selectedObject.name,
                    newObjectPosition, MoveMenu.includeChildren).Execute();
            }
        }

        /// <summary>
        /// Checks if any of the child nodes are involved in a collision.
        /// </summary>
        /// <returns>true, if a child node are involved in a collision. Otherwise false.</returns>
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
        /// <param name="moveByMouse">The switch manager for the move by mouse option from the menu</param>
        /// <param name="childInCollision">Identifies whether any of the children are in collision</param>
        /// <param name="drawable">The drawable on that the object is displayed.</param>
        /// <param name="drawableParentName">The parent name of the drawable</param>
        private void MoveByMouse(SwitchManager moveByMouse, bool childInCollision,
            GameObject drawable, string drawableParentName)
        {
            if (moveByMouse.isOn && Raycasting.RaycastAnything(out RaycastHit hit)
                    && !selectedObject.GetComponent<CollisionController>().IsInCollision()
                    && !childInCollision)
            {
                if ((hit.collider.gameObject.CompareTag(Tags.Drawable)
                        && hit.collider.gameObject.Equals(drawable))
                    || (GameFinder.hasDrawable(hit.collider.gameObject)
                        && GameFinder.GetDrawable(hit.collider.gameObject).Equals(drawable)))
                {
                    newObjectPosition = GameMoveRotator.MoveObjectByMouse(selectedObject,
                        hit.point, MoveMenu.includeChildren);
                    new MoveNetAction(drawable.name, drawableParentName, selectedObject.name,
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
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                {
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();

                }
            }
            /// Part 2 of initializing the end: The progress state is switched to finish.
            if (Input.GetMouseButtonUp(0))
            {
                progressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// Checks if a mouse wheel movement is registered. 
        /// If yes, it performs the desired rotation. 
        /// The rotation can be accelerated using Left Control.
        /// </summary>
        private void RotateByWheel()
        {
            bool rotate = false;
            Vector3 direction = Vector3.zero;
            float degree = 0;

            /// Rotates forward with normal speed.
            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.forward;
                degree = ValueHolder.rotate;
                rotate = true;
            }
            /// Rotates forward with fast speed.
            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.forward;
                degree = ValueHolder.rotateFast;
                rotate = true;
            }
            /// Rotates back with normal speed.
            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.back;
                degree = ValueHolder.rotate;
                rotate = true;

            }
            /// Rotates back with fast speed.
            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.back;
                degree = ValueHolder.rotateFast;
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
            GameObject drawable = GameFinder.GetDrawable(selectedObject);
            string drawableParentName = GameFinder.GetDrawableParentName(drawable);

            newObjectLocalEulerAngles = GameMoveRotator.RotateObject(selectedObject, direction,
                degree, RotationMenu.includeChildren);
            if (Tags.DrawableTypes.Contains(selectedObject.tag))
            {
                newObjectPosition = selectedObject.transform.localPosition;
            }
            new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, direction,
                degree, RotationMenu.includeChildren).Execute();
        }

        /// <summary>
        /// Completes the action, if there are any changes in the position or in the euler angles.
        /// Otherwise it will reset the action.
        /// The action is only completed properly if the object is no longer involved in any collision. 
        /// The same applies to children when using mind map nodes with the option to include children.
        /// </summary>
        /// <returns>whatever the state of success is.</returns>
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
                /// the menus are closed, and the progress state of the action is set to completed.
                if (selectedObject.GetComponent<CollisionController>() != null
                    && !selectedObject.GetComponent<CollisionController>().IsInCollision() && !childInCollision)
                {
                    float degree = selectedObject.transform.localEulerAngles.z;
                    bool includeChildren = RotationMenu.includeChildren
                        && executedOperation == ProgressState.Rotate ||
                        MoveMenu.includeChildren && executedOperation == ProgressState.Move;
                    memento = new Memento(selectedObject, GameFinder.GetDrawable(selectedObject), selectedObject.name,
                        oldObjectPosition, newObjectPosition, oldObjectLocalEulerAngles, degree, executedOperation,
                        includeChildren);
                    Destroyer.Destroy(selectedObject.GetComponent<Rigidbody>());
                    Destroyer.Destroy(selectedObject.GetComponent<CollisionController>());
                    GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(GameFinder.
                        GetAttachedObjectsObject(selectedObject));
                    new RbAndCCDestroyerNetAction(memento.drawable.ID, memento.drawable.ParentID,
                        memento.selectedObject.name).Execute();
                    RotationMenu.Disable();
                    MoveMenu.Disable();
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                } else
                {
                    if (executedOperation == ProgressState.Rotate)
                    {
                        /// A block is needed because occasionally a trigger exit is not registered during rotation. 
                        /// This block attempts to set the isInCollision value of the Collision Controller to false. 
                        /// However, if the object is still in a collision, it will be set back to true by its OnStayCollision method.
                        selectedObject.GetComponent<CollisionController>().TrySetCollisionToFalse();
                    }
                }
            }
            else
            {
                /// Block for reset.
                selectedObject = null;
                RotationMenu.Disable();
                MoveMenu.Disable();
                progressState = ProgressState.SelectObject;
            }
            return false;
        }

        /// <summary>
        /// Get the pressed KeyCode of the arrow keys.
        /// </summary>
        /// <returns>The pressed arrow key.</returns>
        private KeyCode GetArrowKey()
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                return KeyCode.LeftArrow;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                return KeyCode.RightArrow;
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                return KeyCode.UpArrow;
            }
            else
            {
                return KeyCode.DownArrow;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., moves / rotates back to the old position / euler angles.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameFinder.FindChild(memento.drawable.GetDrawable(),
                    memento.id);
            }

            if (memento.selectedObject != null)
            {
                if (memento.moveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(memento.selectedObject, memento.oldObjectPosition,
                                            memento.includeChildren);
                    new MoveNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.id,
                        memento.oldObjectPosition, memento.includeChildren).Execute();
                }
                else if (memento.moveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.selectedObject, memento.oldObjectLocalEulerAngles.z,
                        memento.includeChildren);
                    new RotatorNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.id,
                        memento.oldObjectLocalEulerAngles.z, memento.includeChildren).Execute();
                }

                GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(
                    GameFinder.GetAttachedObjectsObject(memento.selectedObject));
                new RbAndCCDestroyerNetAction(memento.drawable.ID, memento.drawable.ParentID,
                    memento.selectedObject.name).Execute();
            }
        }

        /// <summary>
        /// Repeats this action, i.e., moves / rotates again to the new position / euler angles.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameFinder.FindChild(memento.drawable.GetDrawable(),
                    memento.id);
            }
            if (memento.selectedObject != null)
            {
                if (memento.moveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.SetPosition(memento.selectedObject, memento.newObjectPosition,
                        memento.includeChildren);
                    new MoveNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.id,
                        memento.newObjectPosition, memento.includeChildren).Execute();
                }
                else if (memento.moveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.selectedObject, memento.degree, memento.includeChildren);
                    new RotatorNetAction(memento.drawable.ID, memento.drawable.ParentID, memento.id,
                        memento.degree, memento.includeChildren).Execute();
                }
                GameMoveRotator.DestroyRigidBodysAndCollisionControllersOfChildren(
                    GameFinder.GetAttachedObjectsObject(memento.selectedObject));
                new RbAndCCDestroyerNetAction(memento.drawable.ID, memento.drawable.ParentID,
                    memento.selectedObject.name).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="MoveRotatorAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveRotatorAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MoveRotatorAction();
        }

        /// <summary>
        /// A new instance of <see cref="MoveRotatorAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveRotatorAction"/></returns>
        public override ReversibleAction NewInstance()
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
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>The id of the moved or rotated object</returns>
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