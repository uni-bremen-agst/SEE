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
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoveNetAction = Assets.SEE.Net.Actions.Drawable.MoveNetAction;

namespace Assets.SEE.Controls.Actions.Drawable
{
    public class ScaleAction : AbstractPlayerAction
    {
        private Memento memento;
        private bool start = false;
        private bool didSomething = false;
        private bool isDone = false;

        private static GameObject selectedObj;
        private static bool isActive = false;
        private static Vector3 oldScale;
        private Vector3 newScale;
        private static GameObject drawable;
        private static MeshCollider collider;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                    && !isActive && !didSomething && !isDone && Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject))
                {
                    selectedObj = raycastHit.collider.gameObject;
                    drawable = GameDrawableFinder.FindDrawableParent(selectedObj);
                    start = true;

                    BlinkEffect effect = selectedObj.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());
                    effect.Activate(selectedObj);
                    if (selectedObj.TryGetComponent<MeshCollider>(out MeshCollider meshCollider))
                    {
                        collider = meshCollider;
                        collider.convex = true;
                        collider.isTrigger = true;
                    }
                    else
                    {
                        collider = null;
                    }
                    // TODO Schauen ob die anderen Types auch holder besitzen!
                    if (selectedObj.CompareTag(Tags.Line))
                    {
                        oldScale = selectedObj.transform.parent.localScale;
                    } else
                    {
                        oldScale = selectedObj.transform.localScale;
                    }
                }
                if (Input.GetMouseButtonUp(0) && start)
                {
                    isActive = true;
                }

                if (selectedObj != null && selectedObj.GetComponent<BlinkEffect>() != null && selectedObj.GetComponent<BlinkEffect>().GetLoopStatus())
                {
                    string drawableParentName = GameDrawableFinder.GetDrawableParentName(drawable);
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
                        didSomething = true;
                        newScale = GameScaler.Scale(selectedObj, scaleFactor);
                        new ScaleNetAction(drawable.name, drawableParentName, selectedObj.name, newScale).Execute();
                    }

                }

                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && selectedObj != null && isActive)
                {
                    memento = new Memento(selectedObj, GameDrawableFinder.FindDrawableParent(selectedObj), selectedObj.name, oldScale, newScale);
                    isActive = false;
                    isDone = true;
                    didSomething = false;
                    start = false;
                    selectedObj.GetComponent<BlinkEffect>().Deactivate();
                    if (collider != null)
                    {
                        collider.isTrigger = false;
                        collider.convex = false;
                    }

                    selectedObj = null;
                    oldScale = new Vector3();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;

                }
                return Input.GetMouseButtonUp(0);
            }
            return result;
        }

        private struct Memento
        {
            public GameObject selectedObject;
            public readonly GameObject drawable;
            public readonly string id;
            public readonly Vector3 oldScale;
            public readonly Vector3 newScale;

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
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }

            if (memento.selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawableParent(memento.selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                GameScaler.SetScale(memento.selectedObject, memento.oldScale);
                new ScaleNetAction(drawable.name, drawableParent, memento.selectedObject.name, memento.oldScale).Execute();
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
            if (memento.selectedObject == null && memento.id != null)
            {
                memento.selectedObject = GameDrawableFinder.FindChild(memento.drawable, memento.id);
            }
            if (memento.selectedObject != null)
            {
                GameObject drawable = GameDrawableFinder.FindDrawableParent(memento.selectedObject);
                string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                GameScaler.SetScale(memento.selectedObject, memento.newScale);
                new ScaleNetAction(drawable.name, drawableParent, memento.selectedObject.name, memento.newScale).Execute();
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
            return new ScaleAction();
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
            return ActionStateTypes.Scale;
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

        internal static void Reset()
        {
            isActive = false;
            if (collider != null)
            {
                collider.isTrigger = false;
                collider.convex = false;
            }
            selectedObj = null;
            oldScale = new Vector3();
        }
    }
}