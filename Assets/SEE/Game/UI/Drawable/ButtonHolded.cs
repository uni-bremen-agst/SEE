using System.Collections;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// Component for hold a button pressed with a right click.
    /// Inspired by Ahsanhabibrafy, Jul. 2020
    /// https://discussions.unity.com/t/how-to-make-the-button-respond-to-touch-and-hold-feature/213417/6
    /// </summary>
    public class ButtonHolded : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// State of clicked the button with right click.
        /// </summary>
        private bool isPressed;
        /// <summary>
        /// The action that will be executed.
        /// </summary>
        private UnityAction action;
        
        /// <summary>
        /// Sets the action that should be executed when the right mouse button is pressed.
        /// </summary>
        /// <param name="action"></param>
        public void SetAction(UnityAction action)
        {
            this.action = action;
        }

        /// <summary>
        /// Will be called when the button is pressed.
        /// </summary>
        /// <param name="data">The data of the pointer event</param>
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Right)
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