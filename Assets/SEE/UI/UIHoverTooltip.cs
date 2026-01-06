using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace SEE.UI
{
    /// <summary>
    /// Adds a hover tooltip to any UI element (e.g., buttons, TextMeshProUGUI labels).
    /// Shows a message after a short delay when the pointer hovers over the element.
    /// </summary>
    public class UIHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        /// <summary>
        /// The message to be displayed when the element is hovered over.
        /// </summary>
        [FormerlySerializedAs("message")]
        public string Message;

        /// <summary>
        /// The time to wait before displaying the message when hovering over the element.
        /// </summary>
        private float waitTime = 0.3f;

        /// <summary>
        /// Stops all coroutines and starts a new one when the element is hovered over.
        /// </summary>
        /// <param name="eventData">Will not be used. It's an initial parameter.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            StopAllCoroutines();
            StartCoroutine(StartTimer());
        }

        /// <summary>
        /// Stops all coroutines and hides the <see cref="Tooltip.Tooltip"/> when the pointer leaves the element.
        /// </summary>
        /// <param name="eventData">Will not be used. It's an initial parameter.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            StopAllCoroutines();
            Tooltip.Deactivate();
        }

        /// <summary>
        /// Stops all coroutines and hides the <see cref="Tooltip.Tooltip"/> when the element will be clicked.
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
        /// <returns>
        /// An <see cref="IEnumerator"/> used to execute the coroutine over multiple frames.
        /// </returns>
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
