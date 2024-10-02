using SEE.Controls;
using SEE.Controls.Actions;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRSEEActions : MonoBehaviour
{
    /// <summary>
    /// The RayInteractor, which will be accessed.
    /// </summary>
    [SerializeField]
    XRRayInteractor m_RayInteractor;
    /// <summary>
    /// The RayInteractor, which we use, to get the current position of the Laserpointer.
    /// </summary>
    public static XRRayInteractor RayInteractor { get; set; }
    /// <summary>
    /// Is true, when the user is hovering over an interactable.
    /// </summary>
    private bool hovering = false;
    /// <summary>
    /// The GameObject the user is currently hovering over.
    /// </summary>
    public static GameObject hoveredGameObject { get; private set; }
    /// <summary>
    /// The old parent of a node. We need this for the MoveAction.
    /// </summary>
    public static Transform oldParent { get; private set; }
    /// <summary>
    /// The button, which is used for the primary actions.
    /// </summary>
    public InputActionReference inputAction;
    /// <summary>
    /// The button, which is used for the undo-action.
    /// </summary>
    public InputActionReference undo;
    /// <summary>
    /// The button, which is used for the redo-action.
    /// </summary>
    public InputActionReference redo;
    /// <summary>
    /// The button, which is used, to open the tooltip.
    /// </summary>
    public InputActionReference tooltip;
    /// <summary>
    /// Shows the source name of the hovered or selected object as a text label above the
    /// object. In between that label and the game object, a connecting bar
    /// will be shown.
    /// </summary>
    private ShowLabel showLabel;

    // Awake is always called before any Start functions.
    private void Awake()
    {
        tooltip.action.performed += Tooltip;
        undo.action.performed += Undo;
        redo.action.performed += Redo;
        inputAction.action.Enable();
        inputAction.action.performed += Action;
        RayInteractor = m_RayInteractor;
        Selected = false;
    }
    /// <summary>
    /// This method gets called, when the user begins to hover over an interactable.
    /// </summary>
    /// <param name="args">Event data associated with the event when an Interactor first initiates hovering over an Interactable.</param>
    public void OnHoverEnter(HoverEnterEventArgs args)
    {
        hovering = true;
        hoveredGameObject = args.interactableObject.transform.gameObject;
        if (GlobalActionHistory.Current() == ActionStateTypes.Move)
        {
            oldParent = args.interactableObject.transform.parent;
        }
        hoveredGameObject.transform.TryGetComponent(out showLabel);
        showLabel?.On();
        if (hoveredGameObject.transform.TryGetComponent(out InteractableObject io))
        {
            io.SetHoverFlag(HoverFlag.World, true, true);
        }
    }
    /// <summary>
    /// This method gets called, when the user stops hovering over an interactable.
    /// </summary>
    /// <param name="args">Event data associated with the event when an Interactor stops hovering over an Interactable.</param>
    public void OnHoverExited(HoverExitEventArgs args)
    {
        hovering = false;
        hoveredGameObject = args.interactableObject.transform.gameObject;
        oldParent = null;
        hoveredGameObject.transform.TryGetComponent(out showLabel);
        showLabel?.Off();
        if (hoveredGameObject.transform.TryGetComponent(out InteractableObject io))
        {
            io.SetHoverFlag(HoverFlag.World, false, true);
        }
    }
    /// <summary>
    /// Is true, when the button for the primary actions is pressed.
    /// </summary>
    public static bool Selected { get; set; }
    /// <summary>
    /// The GameObject, which should be rotated.
    /// </summary>
    public static GameObject RotateObject { get; private set; }
    /// <summary>
    /// This method gets called, when the button for the primary actions is pressed.
    /// </summary>
    /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
    private void Action(InputAction.CallbackContext context)
    {
        if (hovering)
        {
            if (GlobalActionHistory.Current() == ActionStateTypes.Move && Selected == true)
            {
                Selected = false;
            }
            else
            {
                Selected = true;
            }
            if (GlobalActionHistory.Current() == ActionStateTypes.NewEdge || GlobalActionHistory.Current() == ActionStateTypes.Delete ||
                GlobalActionHistory.Current() == ActionStateTypes.NewNode || GlobalActionHistory.Current() == ActionStateTypes.AcceptDivergence)
            {
                Selected = true;
            }
            if (GlobalActionHistory.Current() == ActionStateTypes.Rotate)
            {
                RayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit);
                RotateObject = hit.collider.transform.gameObject;
                Selected = true;
            }
        }
    }
    /// <summary>
    /// Will be true, when the TreeView is open, while the user tries to move a node.
    /// </summary>
    public static bool CloseTreeView { get; set; }
    /// <summary>
    /// Is true, when the Tooltip is getting activated.
    /// </summary>
    public static bool TooltipToggle { get; set; }
    /// <summary>
    /// Is true, while the Tooltip is open.
    /// This bool is used, to close the Tooltip, if the user does not want
    /// to use an action.
    /// </summary>
    public static bool OnSelectToggle { get; set; }
    /// <summary>
    /// Is true, when the user opens the tooltip in the treeview.
    /// </summary>
    public static bool OnTreeViewToggle { get; set; }
    /// <summary>
    /// The treview-entry for which the tooltip should be shown.
    /// </summary>
    public static GameObject TreeViewEntry { get; set; }
    /// <summary>
    /// This method gets called, when the button for the tooltip is pressed.
    /// </summary>
    /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
    private void Tooltip(InputAction.CallbackContext context)
    {
        if (OnTreeViewToggle)
        {
            TooltipToggle = true;
            TreeViewEntry.TryGetComponentOrLog(out PointerHelper pointerHelper);
            pointerHelper.ThumbstickEvent.Invoke(new PointerEventData(EventSystem.current));
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
    /// Is true, when the button for the undo-action is pressed.
    /// </summary>
    public static bool UndoToggle { get; set; }
    /// <summary>
    /// This method gets called, when the button for the undo-action is pressed.
    /// </summary>
    /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
    private void Undo(InputAction.CallbackContext context)
    {
        UndoToggle = true;
    }
    /// <summary>
    /// Is true, when the button for the redo-action is pressed.
    /// </summary>
    public static bool RedoToggle { get; set; }
    /// <summary>
    /// This method gets called, when the button for the redo-action is pressed.
    /// </summary>
    /// <param name="context">Information provided to action callbacks about what triggered an action.</param>
    private void Redo(InputAction.CallbackContext context)
    {
        RedoToggle = true;
    }

    // Update is called once per frame
    void Update()
    {
        GlobalActionHistory.Update();
    }
}
