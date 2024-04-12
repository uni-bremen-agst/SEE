using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// Component for hold a mouse button pressed.
    /// It will used for some menu's.
    /// 
    /// Inspired by Ahsanhabibrafy, Jul. 2020
    /// https://discussions.unity.com/t/how-to-make-the-button-respond-to-touch-and-hold-feature/213417/6
    /// </summary>
    public class ButtonHolded : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// Status indicating whether the action should be repeatedly executed. 
        /// This is the case as long as the selected mouse button is held down.
        /// </summary>
        private bool isPressed = false;

        /// <summary>
        /// The action that is executed on a holded mouse button.
        /// </summary>
        private UnityAction action;

        /// <summary>
        /// Bool state for hold left mouse click.
        /// When this is activated, the left mouse button is considered for 
        /// holding down instead of the right mouse button.
        /// </summary>
        private bool leftMouseClick;

        /// <summary>
        /// Sets the action that should be executed when the right mouse button is pressed.
        /// </summary>
        /// <param name="action"></param>
        public void SetAction(UnityAction action, bool leftMouseClick = false)
        {
            this.action = action;
            this.leftMouseClick = leftMouseClick;
        }

        /// <summary>
        /// Will be called when the button is pressed.
        /// </summary>
        /// <param name="data">The data of the pointer event</param>
        public void OnPointerDown(PointerEventData data)
        {
            bool pressedRightButton = false;

            /// Block for holding the left mouse button.
            if (leftMouseClick)
            {
                if (data.button == PointerEventData.InputButton.Left)
                {
                    pressedRightButton = true;
                }
            }
            else
            { /// Block for holding the right mouse button.
                if (data.button == PointerEventData.InputButton.Right)
                {
                    pressedRightButton = true;
                }
            }

            /// Starts the loop that executes the action until the mouse button is released.
            if (pressedRightButton)
            {
                isPressed = true;
                StartCoroutine(Pressed());
            }
        }
        /// <summary>
        /// Will be called when the button is not pressed
        /// </summary>
        /// <param name="data">The data of the pointer event.</param>
        public void OnPointerUp(PointerEventData data)
        {
            isPressed = false;
        }

        /// <summary>
        /// Will be executed as long as the right mouse button is clicked.
        /// </summary>
        /// <returns>the wait value</returns>
        IEnumerator Pressed()
        {
            while (isPressed)
            {
                action();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}