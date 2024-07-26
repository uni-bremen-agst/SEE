using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class provides a controller for buttons,
    /// which allows the use of both left-click, right-click, and mouse wheel click.
    ///
    /// author: @braur
    /// found at: https://forum.unity.com/threads/can-the-ui-buttons-detect-a-right-mouse-click.279027/#post-3344931
    /// </summary>
    public class GUIClickController : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// The event that should be executed if the left mouse button is clicked.
        /// </summary>
        [FormerlySerializedAs("onLeft")]
        public UnityEvent OnLeft;

        /// <summary>
        /// The event that should be executed if the right mouse button is clicked.
        /// </summary>
        [FormerlySerializedAs("onRight")]
        public UnityEvent OnRight;

        /// <summary>
        /// The event that should be executed if the middle mouse button is clicked.
        /// </summary>
        [FormerlySerializedAs("onMiddle")]
        public UnityEvent OnMiddle;

        /// <summary>
        /// Registers the mouse button click and will execute the right action for the click.
        /// </summary>
        /// <param name="eventData">Contains the information about which button was pressed.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            /// Block for a left click
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnLeft.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRight.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                OnMiddle.Invoke();
            }
        }
    }
}
