using UnityEngine;
using UnityEngine.InputSystem;

namespace SEE.Controls.Architecture
{
    
    public delegate void PenInteractionEvent(GameObject initiator);

    /// <summary>
    /// Tooltip component that is attached to GraphElements. Handles hovering and enabling of the tooltip.
    /// The original source code was provided by https://github.com/bfollington/unity-tooltip-system
    /// It has been slightly modified to fit the project use-case.
    /// </summary>
    ///
    public class PenInteraction : MonoBehaviour

    {
    /// <summary>
    /// The main controller instance for handling tooltips.
    /// </summary>
    public PenInteractionController controller;

    private bool _focused = false;
    private bool _focusTriggered = false;
    public bool selected;


    public PenInteractionEvent OnPenEntered;
    public PenInteractionEvent OnPenExited;
    public PenInteractionEvent OnPenSelected;
    public PenInteractionEvent OnPenDeselected;


    private void Start()
    {
        controller.PrimaryPenClicked += () =>
        {
            if (!_focused || !gameObject.activeInHierarchy || controller.selectedObject != gameObject)
            {
                selected = false;
                OnPenDeselected?.Invoke(gameObject);
            }
            else
            {
                selected = true;
                OnPenSelected?.Invoke(gameObject);
            }
        };
    }


    private void Update()
    {
        bool isTouching = Pen.current.pressure.ReadValue() > 0f;

        if (isTouching)
        {
            return;
        }

        bool focused = controller.PrimaryHoveredObject != null && controller.PrimaryHoveredObject == gameObject;
        if (focused && !_focusTriggered)
        {
            _focusTriggered = true;
            OnPenEntered?.Invoke(gameObject);

        }

        if (_focused && !focused)
        {
            OnPenExited?.Invoke(gameObject);
        }

        if (!focused)
        {
            _focusTriggered = false;
        }

        _focused = focused;
    }
    }
}