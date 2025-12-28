// Copyright 2020 Robert Bohnsack
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Handles the dragging and minimization of charts.
    /// </summary>
    public class ChartMoveHandler : MonoBehaviour, IDragHandler, IPointerDownHandler,
        IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// Contains the position of the chart on the <see cref="Canvas" />.
        /// </summary>
        private RectTransform chart;

        /// <summary>
        /// The current size of the screen the charts can be displayed on.
        /// </summary>
        private RectTransform screenSize;

        /// <summary>
        /// The time between <see cref="OnPointerDown" /> and <see cref="OnPointerUp" /> to be recognized as
        /// click instead of a drag.
        /// </summary>
        private float dragDelay;

        /// <summary>
        /// Tracks the time between <see cref="OnPointerDown" /> and <see cref="OnPointerUp" />.
        /// </summary>
        private float timer;

        /// <summary>
        /// If the pointer is currently down or not.
        /// </summary>
        protected bool PointerDown;

        /// <summary>
        /// If the chart is currently minimized or not.
        /// </summary>
        protected bool Minimized;

        /// <summary>
        /// The sprite for the drag button when the chart is maximized.
        /// </summary>
        private Sprite maximizedSprite;

        /// <summary>
        /// The sprite for the drag button when the chart is minimized.
        /// </summary>
        private Sprite minimizedSprite;

        /// <summary>
        /// Contains information about what is displayed on the chart.
        /// </summary>
        [SerializeField] private GameObject chartInfo;

        /// <summary>
        /// The button to resize the chart with. Needs to be minimized too.
        /// </summary>
        [SerializeField] protected GameObject SizeButton;

        /// <summary>
        /// The sidebar in which the user can select what nodes will be displayed in the chart.
        /// </summary>
#pragma warning disable CS0649
        [SerializeField] private GameObject contentSelection;
#pragma warning restore CS0649

        /// <summary>
        /// Links the <see cref="Scripts.ChartManager" /> and initializes attributes.
        /// </summary>
        protected virtual void Awake()
        {
            GetSettingData();
            chart = transform.parent.GetComponent<RectTransform>();
            screenSize = chart.transform.parent.parent.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Links the <see cref="Scripts.ChartManager" /> and gets its setting data.
        /// </summary>
        protected virtual void GetSettingData()
        {
            dragDelay = ChartManager.Instance.DragDelay;
            maximizedSprite = ChartManager.Instance.MaximizedSprite;
            minimizedSprite = ChartManager.Instance.MinimizedSprite;
        }

        /// <summary>
        /// Adds the time passed since the last frame to the <see cref="timer" />
        /// </summary>
        protected virtual void Update()
        {
            if (PointerDown)
            {
                timer += Time.deltaTime;
            }
        }

        /// <summary>
        /// Moves the chart to the position the player dragged it to.
        /// </summary>
        /// <param name="eventData">Contains the position data.</param>
        public virtual void OnDrag(PointerEventData eventData)
        {
            RectTransform pos = GetComponent<RectTransform>();
            if (eventData.position.x > 0 &&
                eventData.position.x < screenSize.sizeDelta.x * screenSize.lossyScale.x &&
                eventData.position.y > 0 &&
                eventData.position.y < screenSize.sizeDelta.y * screenSize.lossyScale.y)
            {
                chart.position =
                    new Vector2(eventData.position.x - pos.anchoredPosition.x * pos.lossyScale.x,
                        eventData.position.y - pos.anchoredPosition.y * pos.lossyScale.y);
            }
        }

        /// <summary>
        /// Starts the pointer down timer.
        /// </summary>
        /// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            timer = 0f;
            PointerDown = true;
            chartInfo.SetActive(false);
        }

        /// <summary>
        /// Stops the pointer down timer and triggers a click depending on the time the pointer was down for.
        /// </summary>
        /// <param name="eventData">Event payload associated with pointer (mouse / touch) events.</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            PointerDown = false;
            if (timer < dragDelay)
            {
                ToggleMinimize();
            }

            if (Minimized)
            {
                chartInfo.SetActive(true);
            }
        }

        /// <summary>
        /// Activates the <see cref="chartInfo" /> when hovering over the minimized chart.
        /// </summary>
        /// <param name="eventData">The event data associated with the pointer entering the UI element.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Minimized && !PointerDown)
            {
                chartInfo.SetActive(true);
            }
        }

        /// <summary>
        /// Deactivated the <see cref="chartInfo" /> when no longer hovering over the minimized chart.
        /// </summary>
        /// <param name="eventData">The event data associated with the pointer exiting the UI element.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            chartInfo.SetActive(false);
        }

        /// <summary>
        /// Changes the text displayed by <see cref="chartInfo" />.
        /// </summary>
        /// <param name="text">The new text to display in the chart info.</param>
        public void SetInfoText(string text)
        {
            chartInfo.GetComponent<TextMeshProUGUI>().text = text;
        }

        /// <summary>
        /// Toggles the minimization of the chart.
        /// </summary>
        protected virtual void ToggleMinimize()
        {
            ChartContent chartContent = chart.GetComponent<ChartContent>();
            chartContent.LabelsPanel.gameObject.SetActive(Minimized);
            chartContent.DataPanel.gameObject.SetActive(Minimized);
            SizeButton.SetActive(Minimized);
            contentSelection.SetActive(Minimized);
            GetComponent<Image>().sprite = Minimized ? maximizedSprite : minimizedSprite;
            Minimized = !Minimized;
        }
    }
}
