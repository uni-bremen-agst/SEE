using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using Assets.SEE.Net.Actions.Drawable;
using Assets.SEE.Net.Actions.Whiteboard;
using RTG;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.UI.ConfigMenu;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoveNetAction = Assets.SEE.Net.Actions.Drawable.MoveNetAction;

namespace Assets.SEE.Controls.Actions.Drawable
{
    public class MoveRotatorAction : AbstractPlayerAction
    {
        private Memento memento;
        private bool didSomething = false;
        private bool isDone = false;

        private enum ClickState
        {
            None,
            Left,
            Right
        }

        private static GameObject selectedObject;
        private static bool isActive = false;
        private static ClickState clickState = ClickState.None;
        private static Vector3 firstPoint;
        private static Vector3 oldObjectPosition;
        private static Quaternion oldObjectRotation;
        private static Vector3 direction = Vector3.zero;
        private static float degree = 0;
        private const string rotationMenuPrefab = "Prefabs/UI/DrawableRotate";
        private static GameObject rotationMenu;

        private Vector3 newObjectPosition;
        
        public void SetSelectedObject(GameObject obj)
        {
            selectedObject = obj;
            oldObjectPosition = obj.transform.position;
            oldObjectRotation = obj.transform.rotation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) 
                    && !isActive && !didSomething && !isDone && Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject) && clickState == ClickState.None)
                {
                    selectedObject = raycastHit.collider.gameObject;
                    isActive = true;
                    if (Input.GetMouseButtonDown(0)||Input.GetMouseButton(0))
                    {
                        clickState = ClickState.Left;
                    } else if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                    {
                        clickState = ClickState.Right;
                    }
                    BlinkEffect effect = selectedObject.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());
                    effect.Activate(selectedObject);
                    SetSelectedObject(selectedObject);
                    firstPoint = raycastHit.point;
                }
                // MOVE
                if (selectedObject != null && selectedObject.GetComponent<BlinkEffect>() != null && selectedObject.GetComponent<BlinkEffect>().GetLoopStatus() && clickState == ClickState.Left)
                {   
                    GameObject drawable = GameDrawableFinder.FindDrawableParent(selectedObject);
                    string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
                    
                    if (Raycasting.RaycastAnything(out RaycastHit hit))
                    {
                        if (hit.collider.gameObject.CompareTag(Tags.Drawable))
                        {
                            Vector3 offset = Vector3.zero;
                            switch (DrawableHelper.checkDirection(drawable))
                            {
                                case DrawableHelper.Direction.Front:
                                    offset = new Vector3(hit.point.x - firstPoint.x, hit.point.y - firstPoint.y, 0);
                                    break;
                                case DrawableHelper.Direction.Back:
                                    offset = new Vector3(hit.point.x - firstPoint.x, hit.point.y - firstPoint.y, 0);
                                    break;
                                case DrawableHelper.Direction.Left:
                                    offset = new Vector3(0, hit.point.y - firstPoint.y, hit.point.z - firstPoint.z);
                                    break;
                                case DrawableHelper.Direction.Right:
                                    offset = new Vector3(0, hit.point.y - firstPoint.y, hit.point.z - firstPoint.z);
                                    break;
                                case DrawableHelper.Direction.Below:
                                    offset = new Vector3(hit.point.x - firstPoint.x, 0, hit.point.z - firstPoint.z);
                                    break;
                                case DrawableHelper.Direction.Above:
                                    offset = new Vector3(hit.point.x - firstPoint.x, 0, hit.point.z - firstPoint.z);
                                    break;
                                default:
                                    break;
                            }
                            didSomething = true;
                            Vector3 objectPosition = oldObjectPosition;
                            newObjectPosition = objectPosition + offset;
                            GameMoveRotator.MoveObject(selectedObject, newObjectPosition);
                            new MoveNetAction(drawable.name, drawableParentName, selectedObject.name, newObjectPosition).Execute();
                        }
                    }
                }
                // Rotate
                if (selectedObject != null && selectedObject.GetComponent<BlinkEffect>() != null && selectedObject.GetComponent<BlinkEffect>().GetLoopStatus() && clickState == ClickState.Right)
                {
                    GameObject drawable = GameDrawableFinder.FindDrawableParent(selectedObject);
                    string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
                    bool rotate = false;
                    if (rotationMenu == null)
                    {
                        rotationMenu = PrefabInstantiator.InstantiatePrefab(rotationMenuPrefab,
                                GameObject.Find("UI Canvas").transform, false);
                        RotationSliderController slider = rotationMenu.GetComponentInChildren<RotationSliderController>();
                        MenuDestroyer destroyer = rotationMenu.AddOrGetComponent<MenuDestroyer>();
                        destroyer.SetAllowedState(GetActionStateType());
                        if (DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(drawable)) == DrawableHelper.Direction.Below ||
                            DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(drawable)) == DrawableHelper.Direction.Above)
                        {
                           // SliderListener(slider, selectedObject.transform.parent.gameObject, drawable, drawableParentName);
                        }
                        else
                        {
                            SliderListener(slider, selectedObject, drawable, drawableParentName);
                        }
                    } else
                    {
                        RotationSliderController slider = rotationMenu.GetComponentInChildren<RotationSliderController>();
                        slider.AssignValue(GetRotatedAxis(selectedObject));
                    }
                    if (Input.mouseScrollDelta.y > 0 && !Input.GetKey(KeyCode.LeftControl))
                    {
                        direction = GetForwardVector(drawable);
                        degree = 1;
                        rotate = true;
                    }
                    if (Input.mouseScrollDelta.y > 0 && Input.GetKey(KeyCode.LeftControl))
                    {
                        direction = GetForwardVector(drawable);
                        degree = 10;
                        rotate = true;
                    }

                    if (Input.mouseScrollDelta.y < 0 && !Input.GetKey(KeyCode.LeftControl))
                    {
                        direction = GetBackVector(drawable);
                        degree = 1;
                        rotate = true;

                    }
                    if (Input.mouseScrollDelta.y < 0 && Input.GetKey(KeyCode.LeftControl))
                    {
                        direction = GetBackVector(drawable);
                        degree = 10;
                        rotate = true;
                    }

                    if (rotate)
                    {
                        GameMoveRotator.RotateObject(selectedObject, firstPoint, direction, degree, oldObjectPosition);
                        new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, firstPoint, direction, degree, oldObjectPosition).Execute();
                        didSomething = true;
                    }
                }
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObject != null && didSomething && isActive)
                {
                    memento = new Memento(selectedObject, GameDrawableFinder.FindDrawableParent(selectedObject), selectedObject.name,
                        oldObjectPosition, newObjectPosition, oldObjectRotation, firstPoint, direction, GetRotatedAxis(selectedObject) - GetRotatedAxis(selectedObject, oldObjectRotation), clickState);
                    clickState = ClickState.None;   
                    isActive = false;
                    isDone = true;
                    didSomething = false;
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();
                    selectedObject = null;
                    oldObjectPosition = new Vector3();
                    oldObjectRotation = new Quaternion();
                    Destroyer.Destroy(rotationMenu);
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                return Input.GetMouseButtonUp(0);
            }
            return result;
        }

        private void SliderListener(RotationSliderController slider, GameObject selectedObject, GameObject drawable, string drawableParentName)
        {
            Debug.Log(DateTime.Now + " - selectedObject: " + selectedObject.name + " assign with value: " + GetRotatedAxis(selectedObject));
            slider.AssignValue(GetRotatedAxis(selectedObject));
            slider.onValueChanged.AddListener(degree =>
            {
                float degreeToMove = 0;
                Vector3 currentDirection = GetForwardVector(drawable);
                bool unequal = false;
                if (GetRotatedAxis(selectedObject) > degree)
                {
                    degreeToMove = GetRotatedAxis(selectedObject) - degree;
                    currentDirection = GetBackVector(drawable);
                    unequal = true;
                }
                else if (GetRotatedAxis(selectedObject) < degree)
                {
                    degreeToMove = degree - GetRotatedAxis(selectedObject);
                    currentDirection = GetForwardVector(drawable);
                    unequal = true;
                }
                if (unequal)
                {
                    Debug.Log(DateTime.Now + " - axis: " + GetRotatedAxis(selectedObject) + ", direction: " + currentDirection);
                    GameMoveRotator.RotateObject(selectedObject, firstPoint, currentDirection, degreeToMove, oldObjectPosition);
                    new RotatorNetAction(drawable.name, drawableParentName, selectedObject.name, firstPoint, currentDirection, degreeToMove, oldObjectPosition).Execute();
                    didSomething = true;
                }
            });
        }

        private float GetRotatedAxis(GameObject obj)
        {
            float axis = 0;
            switch (DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(obj)))
            {
                case DrawableHelper.Direction.Front:

                case DrawableHelper.Direction.Back:
                    axis = obj.transform.rotation.eulerAngles.z;
                    break;
                case DrawableHelper.Direction.Left:
                case DrawableHelper.Direction.Right:
                case DrawableHelper.Direction.Below:
                case DrawableHelper.Direction.Above:
                    axis = obj.transform.rotation.eulerAngles.x;
                    break;
                default:
                    break;
            }
            Debug.Log(DateTime.Now + " - GetRotatedAxis: " + DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(obj)) + ", eulerAngles: " + obj.transform.rotation.eulerAngles + ", selected: " + axis);
            return axis;
        }

        private float GetRotatedAxis(GameObject obj, Quaternion rotation)
        {
            float axis = 0;
            switch (DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(obj)))
            {
                case DrawableHelper.Direction.Front:

                case DrawableHelper.Direction.Back:
                    axis = rotation.eulerAngles.z;
                    break;
                case DrawableHelper.Direction.Left:
                case DrawableHelper.Direction.Right:
                case DrawableHelper.Direction.Below:
                case DrawableHelper.Direction.Above:
                    axis = rotation.eulerAngles.x;
                    break;
                default:
                    break;
            }
            return axis;
        }

        private Vector3 GetForwardVector(GameObject obj)
        {
            Vector3 forward = Vector3.zero;
            switch (DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(obj)))
            {
                case DrawableHelper.Direction.Front:
                case DrawableHelper.Direction.Back:
                    forward = Vector3.forward;
                    break;
                case DrawableHelper.Direction.Left:
                case DrawableHelper.Direction.Right:
                case DrawableHelper.Direction.Below:
                case DrawableHelper.Direction.Above:
                    forward = Vector3.right;
                    break;
                default:
                    break;
            }
            return forward;
        }

        private Vector3 GetBackVector(GameObject obj)
        {
            Vector3 back = Vector3.zero;
            switch (DrawableHelper.checkDirection(GameDrawableFinder.GetHighestParent(obj)))
            {
                case DrawableHelper.Direction.Front:
                case DrawableHelper.Direction.Back:
                    back = Vector3.back;
                    break;
                case DrawableHelper.Direction.Left:
                case DrawableHelper.Direction.Right:
                case DrawableHelper.Direction.Below:
                case DrawableHelper.Direction.Above:
                    back = Vector3.left;
                    break;
                default:
                    break;
            }
            return back;
        }

        public static void Reset()
        {
            isActive = false;
            degree = 0;
            selectedObject = null;
            firstPoint = Vector3.zero;
            oldObjectPosition = Vector3.zero;
            oldObjectRotation = new Quaternion();
            rotationMenu = null;
            clickState = ClickState.None;
        }

        private struct Memento
        {
            public GameObject selectedObject;
            public readonly GameObject drawable;
            public readonly string currentObjectName;
            public readonly Vector3 oldObjectPosition;
            public readonly Vector3 newObjectPosition;
            public readonly Quaternion oldObjectRotation;
            public readonly Vector3 firstPoint;
            public readonly Vector3 direction;
            public readonly float degree;
            public readonly ClickState clickState;

            public Memento(GameObject selectedObject, GameObject drawable, string currentLineName,
                Vector3 oldObjectPosition, Vector3 newObjectPosition, Quaternion oldObjectRotation, Vector3 firstPoint, Vector3 direction, float degree, ClickState clickState)
            {
                this.selectedObject = selectedObject;
                this.drawable = drawable;
                this.currentObjectName = currentLineName;
                this.oldObjectPosition = oldObjectPosition;
                this.newObjectPosition = newObjectPosition;
                this.oldObjectRotation = oldObjectRotation;
                this.firstPoint = firstPoint;
                this.direction = direction;
                this.degree = degree;
                this.clickState = clickState;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.selectedObject == null && memento.currentObjectName != null)
            {
                memento.selectedObject = GameDrawableFinder.FindChild(memento.drawable, memento.currentObjectName);
            }

            if (memento.selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawableParent(memento.selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.clickState == ClickState.Left)
                {
                    GameMoveRotator.MoveObject(memento.selectedObject, memento.oldObjectPosition);
                    new MoveNetAction(drawable.name, drawableParent, memento.currentObjectName, memento.oldObjectPosition).Execute();
                }
                else if (memento.clickState == ClickState.Right)
                {
                    Vector3 newDirection = memento.direction == Vector3.forward ? Vector3.back : Vector3.forward;
                    GameMoveRotator.RotateObject(memento.selectedObject, memento.firstPoint, newDirection, memento.degree, memento.oldObjectPosition);
                    new RotatorNetAction(drawable.name, drawableParent, memento.currentObjectName, memento.firstPoint, newDirection, memento.degree, memento.oldObjectPosition).Execute();
                }  
            }
            if (memento.selectedObject != null && memento.selectedObject.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.selectedObject == null && memento.currentObjectName != null)
            {
                memento.selectedObject = GameDrawableFinder.FindChild(memento.drawable, memento.currentObjectName);
            }
            if (memento.selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawableParent(memento.selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                if (memento.clickState == ClickState.Left)
                {
                    GameMoveRotator.MoveObject(memento.selectedObject, memento.newObjectPosition);
                    new MoveNetAction(drawable.name, drawableParent, memento.currentObjectName, memento.newObjectPosition).Execute();
                } 
                else if (memento.clickState == ClickState.Right)
                {
                    GameMoveRotator.RotateObject(memento.selectedObject, memento.firstPoint, memento.direction, memento.degree, memento.oldObjectPosition);
                    new RotatorNetAction(drawable.name, drawableParent, memento.currentObjectName, memento.firstPoint, memento.direction, memento.degree, memento.oldObjectPosition).Execute();
                }
            }

            if (memento.selectedObject != null && memento.selectedObject.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditLineAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditLineAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MoveRotatorAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditLineAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditLineAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MoveRotator;
        }

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