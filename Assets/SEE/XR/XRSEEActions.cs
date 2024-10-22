using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SEE.XR
{
    /// <summary>
    /// This is a central control class for VR.
    /// It is used to provide the input from the controllers
    /// for all actions in SEE.
    /// </summary>
    public class XRSEEActions : MonoBehaviour
    {
        /// <summary>
        /// The RayInteractor that we use to get the current position of the laserpointer.
        /// </summary>
        public static XRRayInteractor RayInteractor { get; set; }

        /// <summary>
        /// Whether the user is hovering over an interactable.
        /// </summary>
        private bool hovering = false;

        /// <summary>
        /// The GameObject the user is currently hovering over.
        /// </summary>
        private static GameObject hoveredGameObject { get; set; }

        /// <summary>
        /// The old parent of a node. We need this for the MoveAction.
        /// </summary>
        public static Transform OldParent { get; set; }

        /// <summary>
        /// The button that's used for the primary actions.
        /// </summary>
        public InputActionReference inputAction;

        /// <summary>
        /// The button that's used for the undo-action.
        /// </summary>
        public InputActionReference undo;

        /// <summary>
        /// The button that's used for the redo-action.
        /// </summary>
        public InputActionReference redo;

        /// <summary>
        /// The button that's used to open the tooltip.
        /// </summary>
        public InputActionReference tooltip;

        /// <summary>
        /// Shows the source name of the hovered or selected object as a text label above the
        /// object. In between that label and the game object, a connecting bar
        /// will be shown.
        /// </summary>
        private ShowLabel showLabel;

        private void Awake()
        {
            tooltip.action.performed += Tooltip;
            undo.action.performed += Undo;
            redo.action.performed += Redo;
            inputAction.action.Enable();
            inputAction.action.performed += Action;
            RayInteractor = GameObject.Find("XRRig(Clone)/Camera Offset/Right Controller/XRRay").MustGetComponent<XRRayInteractor>();
            Selected = false;
        }

        /// <summary>
        /// This method gets called when the user begins to hover over an interactable.
        /// It provides the input data from the controller with regard to the start of a hover over an object in the CodeCity.
        /// </summary>
        /// <param name="args">Event data associated with the event when an Interactor first initiates hovering over an Interactable.</param>
        public void OnHoverEnter(HoverEnterEventArgs args)
        {
            hovering = true;
            hoveredGameObject = args.interactableObject.transform.gameObject;
            if (GlobalActionHistory.Current() == ActionStateTypes.Move)
            {
                OldParent = args.interactableObject.transform.parent;
            }
            if (hoveredGameObject.transform.TryGetComponent(out showLabel))
            {
                showLabel.On();
            }
            if (hoveredGameObject.transform.TryGetComponent(out InteractableObject io))
            {
                io.SetHoverFlag(HoverFlag.World, true, true);
            }
        }

        /// <summary>
        /// This method gets called, when the user stops hovering over an interactable.
        /// It provides the input data from the controller with regard to the end of a hover over an object in the CodeCity.
        /// </summary>
        /// <param name="args">Event data associated with the event when an Interactor stops hovering over an Interactable.</param>
        public void OnHoverExited(HoverExitEventArgs args)
        {
            hovering = false;
            hoveredGameObject = args.interactableObject.transform.gameObject;
            hoveredGameObject.transform.TryGetComponent(out showLabel);
            if (hoveredGameObject.transform.TryGetComponent(out showLabel))
            {
                showLabel.Off();
            }
            if (hoveredGameObject.transform.TryGetComponent(out InteractableObject io))
            {
                io.SetHoverFlag(HoverFlag.World, false, true);
            }
        }

        /// <summary>
        /// Whether the button for the primary actions is pressed.
        /// </summary>
        public static bool Selected { get; set; }

        /// <summary>
        /// Whether an object in the code city should marked as selected.
        /// </summary>
        public static bool SelectedFlag { get; set; }

        /// <summary>
        /// The GameObject, which should be rotated.
        /// </summary>
        public static GameObject RotateObject { get; set; }

        /// <summary>
        /// This method gets called, when the button for the primary actions is pressed.
        /// After the button got pressed, the desired action will be performed if all conditions are matching.
        /// </summary>
        /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
        private void Action(InputAction.CallbackContext context)
        {
            if (hovering)
            {
                if (GlobalActionHistory.Current() == ActionStateTypes.Move && Selected)
                {
                    Selected = false;
                    SelectedFlag = false;
                }
                else
                {
                    if (GlobalActionHistory.Current() == ActionStateTypes.Rotate)
                    {
                        if (Raycasting.RaycastGraphElement(out RaycastHit hit, out GraphElementRef _) == HitGraphElement.Node)
                        {
                            GameObject rotationObject = hit.collider.transform.gameObject;
                            rotationObject.TryGetNodeRef(out NodeRef nodeRef);
                            if (rotationObject.ContainingCity().NodeTypes[nodeRef.Value.Type].AllowManualNodeManipulation)
                            {
                                RotateObject = hit.collider.transform.gameObject;
                            }
                            else
                            {
                                Selected = false;
                                SelectedFlag = false;
                                RotateObject = null;
                                return;
                            }
                        }
                        else
                        {
                            Selected = false;
                            SelectedFlag = false;
                            RotateObject = null;
                            return;
                        }
                    }
                    Selected = true;
                    if (GlobalActionHistory.Current() != ActionStateTypes.Move && GlobalActionHistory.Current() != ActionStateTypes.Delete)
                    {
                        SelectedFlag = true;
                    }
                }
            }
            else
            {
                InteractableObject.ReplaceSelection(null, true);
            }
        }

        /// <summary>
        /// Whether the TreeView is open while the user tries to move a node.
        /// We need to know if the user has the TreeView open, while moving a node,
        /// because this can cause lags, which we prevent by closing the TreeView before
        /// the node gets moved.
        /// </summary>
        public static bool CloseTreeView { get; set; }

        /// <summary>
        /// Whether the Tooltip is getting activated.
        /// </summary>
        public static bool TooltipToggle { get; set; }

        /// <summary>
        /// Whether the Tooltip is open.
        /// This is used to close the Tooltip if the user does not want to use an action.
        /// </summary>
        public static bool OnSelectToggle { get; set; }

        /// <summary>
        /// Whether the user opened the tooltip in the treeview.
        /// </summary>
        public static bool OnTreeViewToggle { get; set; }

        /// <summary>
        /// The treeview entry for which the tooltip should be shown.
        /// </summary>
        public static GameObject TreeViewEntry { get; set; }

        /// <summary>
        /// This method gets called, when the button for the tooltip is pressed.
        /// When the user points at an object in the CodeCity, or an entry in the TreeView and
        /// presses the corresponding button, it opens up and after another
        /// button-press or the selection of an entry it gets closed.
        /// </summary>
        /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
        private void Tooltip(InputAction.CallbackContext context)
        {
            if (OnTreeViewToggle)
            {
                TooltipToggle = true;
                TreeViewEntry.MustGetComponent<PointerHelper>().ThumbstickEvent.Invoke(new PointerEventData(EventSystem.current));
            }
            else if (hovering && !OnTreeViewToggle)
            {
                TooltipToggle = true;
            }
            else if (OnSelectToggle)
            {
                TooltipToggle = true;
            }
        }

        /// <summary>
        /// Whether the button for the undo-action is pressed.
        /// </summary>
        public static bool UndoToggle { get; set; }

        /// <summary>
        /// This method gets called, when the button for the undo-action is pressed.
        /// It will undo the last action.
        /// </summary>
        /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
        private void Undo(InputAction.CallbackContext context)
        {
            UndoToggle = true;
        }

        /// <summary>
        /// Whether the button for the redo-action is pressed.
        /// </summary>
        public static bool RedoToggle { get; set; }

        /// <summary>
        /// This method gets called when the button for the redo-action is pressed.
        /// It will redo the last action that was undone.
        /// </summary>
        /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
        private void Redo(InputAction.CallbackContext context)
        {
            RedoToggle = true;
        }

        private void Update()
        {
            GlobalActionHistory.Update();
        }
    }
}
