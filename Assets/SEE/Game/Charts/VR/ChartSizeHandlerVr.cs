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

namespace SEE.Game.Charts.VR
{
    /// <summary>
    /// The VR version of <see cref="ChartSizeHandler" />.
    /// </summary>
    public class ChartSizeHandlerVr : ChartSizeHandler
    {
        /// <summary>
        /// The VR version of <see cref="ChartContent" />.
        /// </summary>
        private ChartContentVr chartContentVr;

        /// <summary>
        /// A world space canvas to use the charts in VR.
        /// </summary>
        private RectTransform virtualRealityCanvas;

        /// <summary>
        /// A 3D cube serving as background of the chart to not look flat in 3D space.
        /// </summary>
        private GameObject physicalOpen;

        /// <summary>
        /// The 3D cube representing the chart when minimized.
        /// </summary>
        private GameObject physicalClosed;

        /// <summary>
        /// The background of the content selection.
        /// </summary>
        [SerializeField] private GameObject contentSelectionBackground;

        private const float originalSize = 600f;
        private const float dropdownThickness = 100f;
        private const float physicalClosedPosition = 0.4575f;

        /// <summary>
        /// Initializes some attributes.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            chartContentVr = transform.parent.GetComponent<ChartContentVr>();
            virtualRealityCanvas = Chart.parent.GetComponent<RectTransform>();
            physicalOpen = chartContentVr.PhysicalOpen;
            physicalClosed = chartContentVr.PhysicalClosed;
        }

        /// <summary>
        /// Checks the new width and height and calls <see cref="ChangeSize" /> with it.
        /// </summary>
        /// <param name="eventData">Contains position data of the pointer.</param>
        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.worldPosition == Vector3.zero)
            {
                return;
            }

            RectTransform pos = GetComponent<RectTransform>();
            Vector3 oldPos = pos.position;
            pos.position = eventData.pointerCurrentRaycast.worldPosition;
            pos.anchoredPosition3D =
                new Vector3(pos.anchoredPosition.x, pos.anchoredPosition.y, 0);
            if (pos.anchoredPosition.x < MinimumSize || pos.anchoredPosition.y < MinimumSize)
            {
                pos.position = oldPos;
            }

            ChangeSize(pos.anchoredPosition.x, pos.anchoredPosition.y);
        }

        /// <summary>
        /// Changes the size of the chart.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        protected override void ChangeSize(float width, float height)
        {
            base.ChangeSize(width, height);
            virtualRealityCanvas.sizeDelta =
                new Vector2(width + dropdownThickness, height + dropdownThickness);
            physicalOpen.transform.localScale =
                new Vector2(width / originalSize, height / originalSize);
            physicalClosed.transform.localPosition = new Vector2(
                width / originalSize * physicalClosedPosition,
                -(height / originalSize * physicalClosedPosition));
            contentSelectionBackground.transform.localScale =
                new Vector2(contentSelectionBackground.transform.localScale.x, height);
        }
    }
}