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

using UnityEngine;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Handles the dragging of charts.
    /// </summary>
    public class ChartDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        /// <summary>
        /// Contains position information of the chart.
        /// </summary>
        protected RectTransform Chart;

        /// <summary>
        /// The size of the screen.
        /// </summary>
        private RectTransform screenSize;

        /// <summary>
        /// The distance between the pointer and the middle of the chart.
        /// </summary>
        private Vector2 distance;

        /// <summary>
        /// Gets information about the chart and the screen it is on.
        /// </summary>
        protected virtual void Awake()
        {
            Chart = transform.parent.GetComponent<RectTransform>();
            screenSize = Chart.transform.parent.parent.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Repositions the chart at the pointers position if it is on the screen.
        /// </summary>
        /// <param name="eventData">Contains information about the pointers position.</param>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (eventData.position.x > 0 &&
                eventData.position.x < screenSize.sizeDelta.x * screenSize.lossyScale.x &&
                eventData.position.y > 0 &&
                eventData.position.y < screenSize.sizeDelta.y * screenSize.lossyScale.y)
            {
                Chart.position = new Vector2(eventData.position.x - distance.x,
                    eventData.position.y - distance.y);
            }
        }

        /// <summary>
        /// Saves the distance to the middle of the chart to keep it when dragging the chart.
        /// </summary>
        /// <param name="eventData">Contains information about the pointers position.</param>
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            distance = eventData.position - (Vector2)Chart.position;
        }
    }
}