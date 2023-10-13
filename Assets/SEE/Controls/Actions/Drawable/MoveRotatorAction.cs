using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using MoveNetAction = SEE.Net.Actions.Drawable.MoveNetAction;
using RTG;

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
            public readonly GameObject drawable;
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
                Vector3 oldObjectPosition, Vector3 newObjectPosition, Vector3 oldObjectLocalEulerAngles, float degree, ProgressState moveOrRotate)
            {
                this.selectedObject = selectedObject;
                this.drawable = drawable;
                this.id = id;
                this.oldObjectPosition = oldObjectPosition;
                this.newObjectPosition = newObjectPosition;
                this.oldObjectLocalEulerAngles = oldObjectLocalEulerAngles;
                this.degree = degree;
                this.moveOrRotate = moveOrRotate;
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
        /// The selected drawable type object for moveing or rotating.
        /// </summary>
        private GameObject selectedObject;
        /// <summary>
        /// The hit point of the object selection.
        /// </summary>
        private Vector3 firstPoint;
        /// <summary>
        /// The old position of the selected object.
        /// </summary>
        private Vector3 oldObjectPosition;
        /// <summary>
        /// The old local euler angles of the selected object.
        /// </summary>
        private Vector3 oldObjectLocalEulerAngles;

        /// <summary>
        /// The prefab of the rotation menu.
        /// </summary>
        private const string rotationMenuPrefab = "Prefabs/UI/Drawable/Rotate";
        /// <summary>
        /// The instance of the rotation menu
        /// </summary>
        private GameObject rotationMenu;

        /// <summary>
        /// The state of moving, true will be moved by mouse, false by arrow keys
        /// </summary>
        private bool moveByMouse = true;

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
        /// If the action was not completed in full, the changes are reset.
        /// Destoryes the rotation menu if is still active.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (selectedObject != null && selectedObject.GetComponent<BlinkEffect>() != null)
            {
                selectedObject.GetComponent<BlinkEffect>().Deactivate();
            }
            if (progressState != ProgressState.Finish && selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (progressState == ProgressState.Move)
                {
                    GameMoveRotator.MoveObject(selectedObject, oldObjectPosition);
                    new MoveNetAction(drawable.name, drawableParent, selectedObject.name, oldObjectPosition).Execute();
                }

                if (progressState == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(selectedObject, oldObjectLocalEulerAngles.z);
                    new RotatorNetAction(drawable.name, drawableParent, selectedObject.name, oldObjectLocalEulerAngles.z).Execute();
                }
            }
            if (rotationMenu != null)
            {
                Destroyer.Destroy(rotationMenu);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.MoveRotator"/>.
        /// It moves or rotate a drawable type object.
        /// </summary>
        /// <returns>Whether this Action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progressState)
                {
                    /// With this block, the user can select a Drawable Type object. 
                    /// Left-click selects the object for moving, and right-click for rotating. 
                    /// The mouse button must be released after selecting to start the chosen option.
                    case ProgressState.SelectObject:
                        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                            && Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) && selectedObject == null &&
                            GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                        {
                            selectedObject = raycastHit.collider.gameObject;
                            oldObjectPosition = selectedObject.transform.position;
                            oldObjectLocalEulerAngles = selectedObject.transform.localEulerAngles;

                            BlinkEffect effect = selectedObject.AddOrGetComponent<BlinkEffect>();
                            effect.SetAllowedActionStateType(GetActionStateType());
                            firstPoint = raycastHit.point;

                            if (selectedObject.GetComponent<MeshCollider>() != null)
                            {
                                MeshCollider collider = selectedObject.GetComponent<MeshCollider>();
                                collider.convex = true;
                                collider.isTrigger = true;

                            }
                        }
                        if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && selectedObject != null)
                        {
                            if (Input.GetMouseButtonUp(0) || Input.GetMouseButton(0))
                            {
                                progressState = ProgressState.Move;
                            }
                            else if (Input.GetMouseButtonUp(1) || Input.GetMouseButton(1))
                            {
                                progressState = ProgressState.Rotate;
                                oldObjectPosition = selectedObject.transform.localPosition;
                            }
                        }
                        break;

                    /// With this block, the user can move the object. 
                    /// The object either follows the mouse movement, or the arrow keys can be used. 
                    /// Using the arrow keys locks moving with the mouse, which can be unlocked by a right mouse click.
                    /// /// There is a faster option by holding down the left Ctrl key in addition to using the arrow keys.
                    /// To end the move, the left mouse button must be pressed and released.
                    case ProgressState.Move:
                        if (selectedObject.GetComponent<BlinkEffect>() != null && selectedObject.GetComponent<BlinkEffect>().GetLoopStatus())
                        {
                            GameObject drawable = GameDrawableFinder.FindDrawable(selectedObject);
                            string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);

                            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
                            {
                                bool turbo = false;
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    turbo = true;
                                }
                                KeyCode key = GetArrowKey();
                                moveByMouse = false;
                                newObjectPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, key, turbo);
                                new MoveNetAction(drawable.name, drawableParentName, selectedObject.name, newObjectPosition).Execute();
                            }
                            if (Input.GetMouseButtonDown(1))
                            {
                                moveByMouse = true;
                            }
                            if (moveByMouse && Raycasting.RaycastAnything(out RaycastHit hit))
                            {
                                if (hit.collider.gameObject.CompareTag(Tags.Drawable) ||
                                (GameDrawableFinder.hasDrawable(hit.collider.gameObject) && GameDrawableFinder.FindDrawable(hit.collider.gameObject).Equals(drawable)))
                                {
                                    newObjectPosition = GameMoveRotator.MoveObject(selectedObject, hit.point, firstPoint, oldObjectPosition);
                                    new MoveNetAction(drawable.name, drawableParentName, selectedObject.name, newObjectPosition).Execute();
                                }
                            }
                            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                            {
                                selectedObject.GetComponent<BlinkEffect>().Deactivate();
                            }
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            progressState = ProgressState.Finish;
                        }
                        break;

                    /// With this block the user can rotate the selected object. It opens a rotate menu.
                    /// Use the rotation menu or use the mouse wheel to rotate.
                    /// There is a faster option by holding down the left Ctrl key in addition to using the mouse wheel.
                    /// To end the rotation, the left mouse button must be pressed and released.
                    case ProgressState.Rotate:
                        if (selectedObject.GetComponent<BlinkEffect>() != null && selectedObject.GetComponent<BlinkEffect>().GetLoopStatus())
                        {
                            GameObject drawable = GameDrawableFinder.FindDrawable(selectedObject);
                            string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
                            bool rotate = false;
                            Vector3 direction = Vector3.zero;
                            float degree = 0;

                            if (rotationMenu == null)
                            {
                                rotationMenu = PrefabInstantiator.InstantiatePrefab(rotationMenuPrefab,
                                        GameObject.Find("UI Canvas").transform, false);
                                RotationSliderController slider = rotationMenu.GetComponentInChildren<RotationSliderController>();
                                SliderListener(slider, selectedObject);
                            }
                            else
                            {
                                RotationSliderController slider = rotationMenu.GetComponentInChildren<RotationSliderController>();
                                slider.AssignValue(selectedObject.transform.localEulerAngles.z);
                            }
                            if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
                            {
                                direction = Vector3.forward;
                                degree = 1;
                                rotate = true;
                            }
                            if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
                            {
                                direction = Vector3.forward;
                                degree = 10;
                                rotate = true;
                            }

                            if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
                            {
                                direction = Vector3.back;
                                degree = 1;
                                rotate = true;

                            }
                            if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
                            {
                                direction = Vector3.back;
                                degree = 10;
                                rotate = true;
                            }

                            if (rotate)
                            {
                                newObjectLocalEulerAngles = GameMoveRotator.RotateObject(selectedObject, direction, degree);
                                if (selectedObject.CompareTag(Tags.Line))
                                {
                                    newObjectPosition = selectedObject.transform.localPosition;
                                }
                                new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, direction, degree).Execute();
                            }
                            if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)))
                            {
                                selectedObject.GetComponent<BlinkEffect>().Deactivate();
                                
                            }
                        }
                        if (Input.GetMouseButtonUp(0))
                        {
                            progressState = ProgressState.Finish;
                        }
                        break;

                    /// Completes the action, if there are any changes in the position or in the euler angles.
                    /// Otherwise it will reset the action.
                    case ProgressState.Finish:
                        if (oldObjectPosition != newObjectPosition || oldObjectLocalEulerAngles != newObjectLocalEulerAngles)
                        {
                            float degree = selectedObject.transform.localEulerAngles.z;
                            ProgressState excutedAction;
                            if (oldObjectPosition != newObjectPosition)
                            {
                                excutedAction = ProgressState.Move;
                            }
                            else
                            {
                                excutedAction = ProgressState.Rotate;
                            }

                            memento = new Memento(selectedObject, GameDrawableFinder.FindDrawable(selectedObject), selectedObject.name,
                                oldObjectPosition, newObjectPosition, oldObjectLocalEulerAngles, degree, excutedAction);

                            if (selectedObject.GetComponent<MeshCollider>() != null)
                            {
                                MeshCollider collider = selectedObject.GetComponent<MeshCollider>();
                                collider.isTrigger = false;
                                collider.convex = false;
                            }
                            Destroyer.Destroy(rotationMenu);
                            currentState = ReversibleAction.Progress.Completed;
                            return true;
                        }
                        else
                        {
                            selectedObject = null;
                            Destroyer.Destroy(rotationMenu);
                            progressState = ProgressState.SelectObject;
                        }
                        break;
                }
                return false;
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
        /// Adds the AddListener for the Rotate Slider Controller.
        /// </summary>
        /// <param name="slider">The slider controller where the AddListener should be add.</param>
        /// <param name="selectedObject">The selected object to rotate</param>
        private void SliderListener(RotationSliderController slider, GameObject selectedObject)
        {
            GameObject drawable = GameDrawableFinder.FindDrawable(selectedObject);
            string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
            Transform transform = selectedObject.transform;

            slider.AssignValue(transform.localEulerAngles.z);
            slider.onValueChanged.AddListener(degree =>
            {
                float degreeToMove = 0;
                Vector3 currentDirection = Vector3.forward;
                bool unequal = false;
                if (transform.localEulerAngles.z > degree)
                {
                    degreeToMove = transform.localEulerAngles.z - degree;
                    currentDirection = Vector3.back;
                    unequal = true;
                }
                else if (transform.localEulerAngles.z < degree)
                {
                    degreeToMove = degree - transform.localEulerAngles.z;
                    currentDirection = Vector3.forward;
                    unequal = true;
                }
                if (unequal)
                {
                    GameMoveRotator.RotateObject(selectedObject, currentDirection, degreeToMove);
                    new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, currentDirection, degreeToMove).Execute();;
                }
            });
        }

        /// <summary>
        /// Reverts this action, i.e., moves / rotates back to the old position / euler angles.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.moveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.MoveObject(memento.selectedObject, memento.oldObjectPosition);
                    new MoveNetAction(drawable.name, drawableParent, memento.id, memento.oldObjectPosition).Execute();
                }
                else if (memento.moveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.selectedObject, memento.oldObjectLocalEulerAngles.z);
                    new RotatorNetAction(drawable.name, drawableParent, memento.id, memento.oldObjectLocalEulerAngles.z).Execute();
                }
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
                memento.selectedObject = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }
            if (memento.selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawable(memento.selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.moveOrRotate == ProgressState.Move)
                {
                    GameMoveRotator.MoveObject(memento.selectedObject, memento.newObjectPosition);
                    new MoveNetAction(drawable.name, drawableParent, memento.id, memento.newObjectPosition).Execute();
                }
                else if (memento.moveOrRotate == ProgressState.Rotate)
                {
                    GameMoveRotator.SetRotate(memento.selectedObject, memento.degree);
                    new RotatorNetAction(drawable.name, drawableParent, memento.id, memento.degree).Execute();
                }
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