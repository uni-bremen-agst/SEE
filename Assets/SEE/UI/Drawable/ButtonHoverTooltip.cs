using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace SEE.UI.Drawable
{
    /// <summary>
    /// This class allows adding a hover tooltip to a button.
    /// </summary>
    public class ButtonHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        /// <summary>
        /// The message to be displayed when the button is hovered over.
        /// </summary>
        [FormerlySerializedAs("message")]
        public string Message;

        /// <summary>
        /// The time to wait before displaying the message when hovering over the button.
        /// </summary>
        private float waitTime = 0.3f;

        /// <summary>
        /// Stops all coroutines and starts a new one when the button is hovered over.
        /// </summary>
        /// <param name="eventData">Will not be used. It's an initial parameter.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            StopAllCoroutines();
            StartCoroutine(StartTimer());
        }

        /// <summary>
        /// Stops all coroutines and hides the <see cref="Tooltip.Tooltip"/> when the pointer leaves the button.
        /// </summary>
        /// <param name="eventData">Will not be used. It's an initial parameter.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            StopAllCoroutines();
            Tooltip.Deactivate();
        }

        /// <summary>
        /// Stops all coroutines and hides the <see cref="Tooltip.Tooltip"/> when the button will be clicked.
        /// </summary>
        /// <param name="eventData">Will not be used. It's an initial parameter.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            StopAllCoroutines();
            Tooltip.Deactivate();
        }

        /// <summary>
        /// Adds the <see cref="Tooltip.Tooltip"/> and shows the message.
        /// </summary>
        private void ShowMessage()
        {
            Tooltip.ActivateWith(Message);
        }

        /// <summary>
        /// The coroutine that is started waits for the specified time and then displays the message
        /// with a <see cref="Tooltip.Tooltip"/>.
        /// </summary>
        /// <returns></returns>
        private IEnumerator StartTimer()
        {
            yield return new WaitForSeconds(waitTime);
            ShowMessage();
        }

        /// <summary>
        /// Sets the message.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        public void SetMessage(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Sets the time to wait.
        /// </summary>
        /// <param name="waitTime">The time to wait before displaying the message.</param>
        public void SetWaitTime(float waitTime)
        {
            this.waitTime = waitTime;
        }
    }
}
