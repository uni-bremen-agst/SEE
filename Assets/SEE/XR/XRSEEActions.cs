using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.GO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class XRSEEActions : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reads input data from the right hand controller. Input Action must be a Value action type (Vector 2).")]
    XRInputValueReader<Vector2> m_RightHandTurnInput = new XRInputValueReader<Vector2>("Right Hand Snap Turn");

    /// <summary>
    /// Reads input data from the right hand controller. Input Action must be a Value action type (Vector 2).
    /// </summary>
    public XRInputValueReader<Vector2> rightHandTurnInput
    {
        get => m_RightHandTurnInput;
        set => XRInputReaderUtility.SetInputProperty(ref m_RightHandTurnInput, value, this);
    }

    public InputHelpers.Button rightTurnButton = InputHelpers.Button.PrimaryAxis2DRight;
    public InputHelpers.Button leftTurnButton = InputHelpers.Button.SecondaryAxis2DLeft;

    [SerializeField]
    XRRayInteractor m_RayInteractor;
    public static XRRayInteractor RayInteractor { get; set; }

    private bool hovering = false;
    public static GameObject hoveredGameObject { get; set; }

    public static Transform oldParent { get; set; }

    public InputActionReference inputAction;

    public InputActionReference openContextMenu;

    public InputActionReference undo;
    public InputActionReference redo;
    public InputActionReference radialMenu;

    // Start is called before the first frame update
    private void Awake()
    {
        radialMenu.action.performed += RadialMenu;
        undo.action.performed += Undo;
        redo.action.performed += Redo;
        rightHandTurnInput.inputAction.performed += Rotate;
        inputAction.action.Enable();
        inputAction.action.performed += Action;
        openContextMenu.action.Enable();
        openContextMenu.action.performed += OpenContexMenu;
        RayInteractor = m_RayInteractor;
    }

    public void OnHoverEnter(HoverEnterEventArgs args)
    {
        Debug.Log($"{args.interactorObject} hovered over {args.interactableObject}", this);
        hovering = true;
        hoveredGameObject = args.interactableObject.transform.gameObject;
        oldParent = args.interactableObject.transform.parent;
        Debug.Log(oldParent.name + "cool");
        if (hoveredGameObject.transform.TryGetComponent(out InteractableObject io))
        {
            io.SetHoverFlag(HoverFlag.World, true, true);
        }
    }

    public void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"{args.interactorObject} stopped hovered over {args.interactableObject}", this);
        hovering = false;
        hoveredGameObject = args.interactableObject.transform.gameObject;
        oldParent = null;
        if (hoveredGameObject.transform.TryGetComponent(out InteractableObject io))
        {
            io.SetHoverFlag(HoverFlag.World, false, true);
        }
    }

    public void OnSelectEnter(SelectEnterEventArgs args)
    {
        if(GlobalActionHistory.Current() == ActionStateTypes.Move || GlobalActionHistory.Current() == ActionStateTypes.Draw || GlobalActionHistory.Current() == ActionStateTypes.ShowCode
            || GlobalActionHistory.Current() == ActionStateTypes.Rotate)
        {
            if (GlobalActionHistory.Current() == ActionStateTypes.Move && Selected == true)
            {
                Selected = false;
            }
            else
            {
                Selected = true;
            }
        }
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        if (GlobalActionHistory.Current() == ActionStateTypes.Draw || GlobalActionHistory.Current() == ActionStateTypes.ShowCode
            || GlobalActionHistory.Current() == ActionStateTypes.Rotate)
        {
            Selected = false;
        }
    }
    public static bool Selected { get; set; }
    public static bool Delete { get; set; }
    private void Action(InputAction.CallbackContext context)
    {
        if (hovering)
        {
            if(GlobalActionHistory.Current() == ActionStateTypes.NewEdge || GlobalActionHistory.Current() == ActionStateTypes.Delete ||
                GlobalActionHistory.Current() == ActionStateTypes.NewNode)
            {
                Delete = true;
            }
        }
    }

    public static bool UndoToggle { get; set; }
    private void Undo(InputAction.CallbackContext context)
    {
        UndoToggle = true;
    }
    public static bool RedoToggle { get; set; }
    private void Redo(InputAction.CallbackContext context)
    {
        RedoToggle = true;
    }
    public static bool RadialMenuTrigger { get; set; }
    private void RadialMenu(InputAction.CallbackContext context)
    {
        RadialMenuTrigger = true;
    }

    public static bool ContextMenu { get; set; }

    private void OpenContexMenu(InputAction.CallbackContext context)
    {
        if (hovering)
        {
            if (hoveredGameObject.gameObject.TryGetNode(out Node node))
            {
                ContextMenu = true;
            }
        }
    }

    private void Rotate(InputAction.CallbackContext context)
    {
        if (hovering && GlobalActionHistory.Current() == ActionStateTypes.Rotate)
        {
            if (hoveredGameObject.gameObject.TryGetNode(out Node node))
            {
                var amount = GetTurnAmount(m_RightHandTurnInput.ReadValue());
                if (Math.Abs(amount) > 0f)
                {
                    hoveredGameObject.transform.Rotate(0, amount * Time.deltaTime, 0);
                }
                ContextMenu = true;
            }
        }
    }

    /// <summary>
    /// Determines the turn amount in degrees for the given <paramref name="input"/> vector.
    /// </summary>
    /// <param name="input">Input vector, such as from a thumbstick.</param>
    /// <returns>Returns the turn amount in degrees for the given <paramref name="input"/> vector.</returns>
    protected virtual float GetTurnAmount(Vector2 input)
    {
        if (input == Vector2.zero)
            return 0f;

        var cardinal = CardinalUtility.GetNearestCardinal(input);
        switch (cardinal)
        {
            case Cardinal.North:
                break;
            case Cardinal.South:
                break;
            case Cardinal.East:
                    return 45f;
            case Cardinal.West:
                    return -45f;
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                break;
        }

        return 0f;
    }

    // Update is called once per frame
    void Update()
    {
        GlobalActionHistory.Update();
    }
}
