using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.GO;
using SEE.UI;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.MoveRotate
{
    /// <summary>
    /// Handles object selection and the choice between moving and rotating.
    /// </summary>
    internal sealed class MoveRotateSelectionController
    {
        /// <summary>
        /// The available operations after selecting an object.
        /// </summary>
        internal enum Operation
        {
            None,
            Move,
            Rotate
        }

        /// <summary>
        /// The result of a completed selection.
        /// </summary>
        internal readonly struct Result
        {
            /// <summary>
            /// The selected object.
            /// </summary>
            internal readonly GameObject SelectedObject;

            /// <summary>
            /// The object's position before the operation starts.
            /// </summary>
            internal readonly Vector3 OldPosition;

            /// <summary>
            /// The object's local rotation before the operation starts.
            /// </summary>
            internal readonly Vector3 OldLocalEulerAngles;

            /// <summary>
            /// The selected operation.
            /// </summary>
            internal readonly Operation SelectedOperation;

            /// <summary>
            /// Creates a selection result.
            /// </summary>
            internal Result(GameObject selectedObject, Vector3 oldPosition,
                Vector3 oldLocalEulerAngles, Operation selectedOperation)
            {
                SelectedObject = selectedObject;
                OldPosition = oldPosition;
                OldLocalEulerAngles = oldLocalEulerAngles;
                SelectedOperation = selectedOperation;
            }
        }

        /// <summary>
        /// The prefab of the move or rotate selection menu.
        /// </summary>
        private const string switchMenuPrefab = "Prefabs/UI/Drawable/MoveRotatorSwitch";

        /// <summary>
        /// The currently selected object.
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// The previously selected object.
        /// </summary>
        private GameObject oldSelectedObject;

        /// <summary>
        /// Whether the mouse was released after selecting an object.
        /// </summary>
        private bool mouseWasReleased = true;

        /// <summary>
        /// The move or rotate selection menu.
        /// </summary>
        private GameObject switchMenu;

        /// <summary>
        /// The position before the operation starts.
        /// </summary>
        private Vector3 oldPosition;

        /// <summary>
        /// The local rotation before the operation starts.
        /// </summary>
        private Vector3 oldLocalEulerAngles;

        /// <summary>
        /// The operation selected in the switch menu.
        /// </summary>
        private Operation selectedOperation;

        /// <summary>
        /// Whether an object is currently selected.
        /// </summary>
        internal bool IsActive => selectedObject != null;

        /// <summary>
        /// Updates object selection and returns a result once an operation was selected.
        /// </summary>
        /// <param name="result">The completed selection result.</param>
        /// <returns>Whether the selection was completed.</returns>
        internal bool TrySelect(out Result result)
        {
            result = default;

            if (Selector.SelectObject(ref selectedObject, ref oldSelectedObject, ref mouseWasReleased,
                true, false, true))
            {
                oldPosition = selectedObject.transform.localPosition;
                oldLocalEulerAngles = selectedObject.transform.localEulerAngles;
                InitSwitchMenu();
            }

            if (SEEInput.MouseUp(MouseButton.Left) && !mouseWasReleased)
            {
                mouseWasReleased = true;
            }

            EnableObjectChange();

            if (selectedObject == null || selectedOperation == Operation.None)
            {
                return false;
            }

            result = new Result(selectedObject, oldPosition, oldLocalEulerAngles, selectedOperation);
            ResetAfterSelection();
            return true;
        }

        /// <summary>
        /// Creates the menu for choosing between moving and rotating.
        /// </summary>
        private void InitSwitchMenu()
        {
            switchMenu = PrefabInstantiator.InstantiatePrefab(switchMenuPrefab, UICanvas.Canvas.transform, false);

            GameFinder.FindAttachedOrLocalDescendant(switchMenu, "Move").GetComponent<ButtonManagerBasic>().clickEvent
                .AddListener(() =>
                {
                    selectedOperation = Operation.Move;
                    DestroySwitchMenu();
                });

            GameFinder.FindAttachedOrLocalDescendant(switchMenu, "Rotate").GetComponent<ButtonManagerBasic>().clickEvent
                .AddListener(() =>
                {
                    selectedOperation = Operation.Rotate;
                    oldPosition = selectedObject.transform.localPosition;
                    DestroySwitchMenu();
                });
        }

        /// <summary>
        /// Allows replacing the selected object before an operation was chosen.
        /// </summary>
        private void EnableObjectChange()
        {
            if (!SEEInput.LeftMouseInteraction() || selectedObject == null || !mouseWasReleased)
            {
                return;
            }

            DestroySwitchMenu();
            BlinkEffect.Deactivate(selectedObject);
            CollisionDetectionManager.Disable(selectedObject);

            Renderer renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
            else
            {
                foreach (Renderer childRenderer in selectedObject.GetComponentsInChildren<Renderer>())
                {
                    childRenderer.enabled = true;
                }
            }

            Canvas canvas = selectedObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
            }

            oldSelectedObject = selectedObject;
            selectedObject = null;
            mouseWasReleased = false;
            selectedOperation = Operation.None;
        }

        /// <summary>
        /// Cancels the current selection.
        /// </summary>
        internal void Cancel()
        {
            if (selectedObject != null)
            {
                BlinkEffect.Deactivate(selectedObject);
                CollisionDetectionManager.Disable(selectedObject);
            }

            DestroySwitchMenu();
            Reset();
        }

        /// <summary>
        /// Destroys the operation selection menu.
        /// </summary>
        private void DestroySwitchMenu()
        {
            if (switchMenu != null)
            {
                Destroyer.Destroy(switchMenu);
                switchMenu = null;
            }
        }

        /// <summary>
        /// Resets controller state after handing the selected object to the action.
        /// </summary>
        private void ResetAfterSelection()
        {
            selectedObject = null;
            oldSelectedObject = null;
            mouseWasReleased = true;
            selectedOperation = Operation.None;
            switchMenu = null;
        }

        /// <summary>
        /// Resets all selection state.
        /// </summary>
        private void Reset()
        {
            selectedObject = null;
            oldSelectedObject = null;
            mouseWasReleased = true;
            selectedOperation = Operation.None;
            oldPosition = default;
            oldLocalEulerAngles = default;
            switchMenu = null;
        }
    }
}
