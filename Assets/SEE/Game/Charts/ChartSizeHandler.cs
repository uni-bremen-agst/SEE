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
    /// Handles the resizing of charts.
    /// </summary>
    public class ChartSizeHandler : MonoBehaviour, IDragHandler
    {
        /// <summary>
        /// The script attached to the chart.
        /// </summary>
        private ChartContent _chartContent;

        /// <summary>
        /// The minimum size a chart can have for width and height.
        /// </summary>
        protected int MinimumSize;

        /// <summary>
        /// The objects that have to be moved individually when resizing the chart.
        /// </summary>
        [Header("For resizing"), SerializeField]
        private Transform dragButton;

        /// <summary>
        /// The warning displayed when no data is displayed in the chart.
        /// </summary>
#pragma warning disable CS0649
        [SerializeField] private RectTransform noDataWarning;
#pragma warning restore CS0649

        /// <summary>
        /// Top right area of the chart.
        /// </summary>
        [SerializeField] private Transform topRight;

        /// <summary>
        /// Top left area of the chart.
        /// </summary>
        [SerializeField] private Transform topLeft;

        /// <summary>
        /// Bottom right area of the chart.
        /// </summary>
        [SerializeField] private Transform bottomRight;

        /// <summary>
        /// Bottom left area of the chart.
        /// </summary>
        [SerializeField] private Transform bottomLeft;

        /// <summary>
        /// Area on the right of the chart to select the nodes to be displayed in the chart.
        /// </summary>
        [SerializeField] private RectTransform contentSelection;

        /// <summary>
        /// The content of the <see cref="contentSelection" />.
        /// </summary>
        [SerializeField] private RectTransform scrollView;

        /// <summary>
        /// The text above the <see cref="scrollView" />.
        /// </summary>
        [SerializeField] private RectTransform contentSelectionHeader;

        /// <summary>
        /// Contains the size of the chart.
        /// </summary>
        protected RectTransform Chart;

        /// <summary>
        /// Initializes some attributes.
        /// </summary>
        protected virtual void Awake()
        {
            GetSettingData();
            Transform parent = transform.parent;
            _chartContent = parent.GetComponent<ChartContent>();
            Chart = parent.GetComponent<RectTransform>();
        }

        /// <summary>
        /// Links the <see cref="ChartManager" /> and gets its setting data.
        /// </summary>
        private void GetSettingData()
        {
            MinimumSize = ChartManager.Instance.MinimumSize;
        }

        /// <summary>
        /// Checks the current <see cref="PointerEventData.position" /> and calls
        /// <see cref="ChangeSize" /> to resize the chart accordingly.
        /// </summary>
        /// <param name="eventData">Contains the position data.</param>
        public virtual void OnDrag(PointerEventData eventData)
        {
            RectTransform pos = GetComponent<RectTransform>();
            Vector2 oldPos = pos.position;
            pos.position = eventData.position;
            Vector2 anchoredPos = pos.anchoredPosition;
            if (anchoredPos.x / pos.lossyScale.x < MinimumSize ||
                anchoredPos.y / pos.lossyScale.y < MinimumSize)
            {
                pos.position = oldPos;
            }

            anchoredPos = pos.anchoredPosition;
            ChangeSize(anchoredPos.x, anchoredPos.y);
        }

        /// <summary>
        /// Changes the width and height of the chart and its contents.
        /// </summary>
        /// <param name="width">The new width of the chart.</param>
        /// <param name="height">The new height of the chart.</param>
        protected virtual void ChangeSize(float width, float height)
        {
            RectTransform dataPanel = _chartContent.dataPanel;
            dataPanel.sizeDelta = new Vector2(width - 80, height - 80);
            dataPanel.anchoredPosition = new Vector2(width / 2, height / 2);
            noDataWarning.sizeDelta = new Vector2(width - 150, height - 150);

            RectTransform labelsPanel = _chartContent.labelsPanel;
            labelsPanel.sizeDelta = new Vector2(width, height);
            labelsPanel.anchoredPosition = new Vector2(width / 2, height / 2);

            RectTransform xDropdown = _chartContent.axisDropdownX.GetComponent<RectTransform>();
            xDropdown.anchoredPosition = new Vector2(width / 2, xDropdown.anchoredPosition.y);
            xDropdown.sizeDelta = new Vector2(width / 2, xDropdown.sizeDelta.y);

            RectTransform yDropdown = _chartContent.axisDropdownY.GetComponent<RectTransform>();
            yDropdown.anchoredPosition = new Vector2(yDropdown.anchoredPosition.x, height / 2);
            yDropdown.sizeDelta = new Vector2(height / 2, yDropdown.sizeDelta.y);

            Chart.sizeDelta = new Vector2(width, height);
            topRight.localPosition = new Vector2(width / 2, height / 2);
            topLeft.localPosition = new Vector2(-width / 2, height / 2);
            bottomRight.localPosition = new Vector2(width / 2, -height / 2);
            bottomLeft.localPosition = new Vector2(-width / 2, -height / 2);
            dragButton.localPosition = bottomRight.localPosition - new Vector3(20f, -20f);
            contentSelection.anchoredPosition = new Vector2(width / 2 + contentSelection.sizeDelta.x / 2, 0);
            contentSelection.sizeDelta = new Vector2(contentSelection.sizeDelta.x, height);
            scrollView.sizeDelta = new Vector2(scrollView.sizeDelta.x, height - 50);
            contentSelectionHeader.anchoredPosition = new Vector2(0, height / 2 - 20);
            _chartContent.DrawData();
        }

        /// <summary>
        /// Toggles the active state of <see cref="contentSelection" />. Called by Unity.
        /// </summary>
        public void ToggleContentSelection()
        {
            contentSelection.gameObject.SetActive(!contentSelection.gameObject.activeInHierarchy);
        }
    }
}