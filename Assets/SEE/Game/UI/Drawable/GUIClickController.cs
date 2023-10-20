using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
/// <summary>
/// author: @braur
/// found at: https://forum.unity.com/threads/can-the-ui-buttons-detect-a-right-mouse-click.279027/#post-3344931
/// </summary>
public class GUIClickController : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// The event that should be executed if the left mouse button is clicked.
    /// </summary>
    public UnityEvent onLeft;
    /// <summary>
    /// The event that should be executed if the right mouse button is clicked.
    /// </summary>
    public UnityEvent onRight;
    /// <summary>
    /// The event that should be executed if the middle mouse button is clicked.
    /// </summary>
    public UnityEvent onMiddle;

    /// <summary>
    /// This method register the mouse button click and will execute the right action for the click.
    /// </summary>
    /// <param name="eventData">Contains the information about which button was pressed.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            onLeft.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            onRight.Invoke();
        }
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            onMiddle.Invoke();
        }
    }
}