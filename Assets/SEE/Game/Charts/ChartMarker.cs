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

using System.Collections.Generic;
using System.Text;
using SEE.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace SEE.Game.Charts
{
    /// <summary>
    /// Contains the logic for the markers representing entries linked to objects in the chart.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class ChartMarker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// The chart content, on which this marker is displayed.
        /// </summary>
        [HideInInspector] public ChartContent chartContent;

        /// <summary>
        /// The icon of the marker.
        /// </summary>
        [SerializeField] private UnityEngine.UI.Image image;

        /// <summary>
        /// A text popup containing useful information about the marker and its linked
        /// interactable objects.
        /// </summary>
        [SerializeField] private TextMeshProUGUI infoText;

        /// <summary>
        /// The <see cref="GameObject"/> making the marker look highlighted when active.
        /// </summary>
        [SerializeField] private GameObject markerHighlight;
        
        /// <summary>
        /// The ids of the handled <see cref="InteractableObject"/>.
        /// </summary>
        public readonly List<uint> ids = new List<uint>();

        /// <summary>
        /// The ids of the hovered or selected handled <see cref="InteractableObject"/>.
        /// </summary>
        public readonly HashSet<uint> hoveredOrSelectedIds = new HashSet<uint>();

        /// <summary>
        /// Dictionary converts between <see cref="InteractableObject.ID"/> and the
        /// corresponding info-texts.
        /// </summary>
        private readonly Dictionary<uint, string> id2TextDict = new Dictionary<uint, string>();

        /// <summary>
        /// The number of handled selected objects.
        /// </summary>
        private uint selectedCount = 0;

        /// <summary>
        /// The number of handled objects, that should be visible in the chart.
        /// </summary>
        private uint showInChartCount = 0;

        /// <summary>
        /// This is shared across all <see cref="ChartMarker"/>s to reduce memory
        /// consumption and the number of memory allocations.
        /// </summary>
        private readonly static StringBuilder sharedStringBuilder = new StringBuilder();

        private void Awake()
        {
            infoText.text = string.Empty;
            infoText.color = UIColorScheme.GetLight(0);
            markerHighlight.GetComponent<UnityEngine.UI.Image>().color = UIColorScheme.GetLight(2);
        }

        internal void OnDestroy()
        {
            foreach (uint id in ids)
            {
                InteractableObject o = InteractableObject.Get(id);
                chartContent.DetachShowInChartCallbackFn(o, OnShowInChartEvent);
                o.HoverIn -= OnHoverIn;
                o.HoverOut -= OnHoverOut;
                o.SelectIn -= OnSelectIn;
                o.SelectOut -= OnSelectOut;
            }
            selectedCount = 0;
            showInChartCount = 0;
            id2TextDict.Clear();
            hoveredOrSelectedIds.Clear();
            ids.Clear();
        }

        /// <summary>
        /// Adds an interactable object with given info text to this marker.
        /// </summary>
        /// <param name="o">The interactable object to be added.</param>
        /// <param name="infoText">The text to be displayed for the given object.</param>
        public void PushInteractableObject(InteractableObject o, string infoText)
        {
            Assert.IsTrue(!ids.Contains(o.ID));

            ids.Add(o.ID);
            if (o.IsHovered || o.IsSelected)
            {
                hoveredOrSelectedIds.Add(o.ID);
            }
            id2TextDict.Add(o.ID, infoText);
            if (chartContent.ShowInChart(o))
            {
                showInChartCount++;
            }

            o.HoverIn += OnHoverIn;
            o.HoverOut += OnHoverOut;
            o.SelectIn += OnSelectIn;
            o.SelectOut += OnSelectOut;

            chartContent.AttachShowInChartCallbackFn(o, OnShowInChartEvent);

            // Note: This if-else-statepent only works, because 'OnSelectIn' does everything
            // 'OnHoverIn' does, but more.
            if (o.IsSelected)
            {
                OnSelectIn(o);
            }
            else if (o.IsHovered)
            {
                OnHoverIn(o);
            }

            UpdateVisibility();
        }

        /// <summary>
        /// Updates the info text to be displayed for this marker. The info texts for
        /// every hovered and/or selected object is displayed. If no such object exists,
        /// the info text is disabled.
        /// </summary>
        private void UpdateInfoText()
        {
            bool showInfoText = hoveredOrSelectedIds.Count > 0;
            infoText.gameObject.SetActive(showInfoText);
            if (showInfoText)
            {
                sharedStringBuilder.Clear();
                int count = 0;
                const int maxLines = 3;
                foreach (uint id in hoveredOrSelectedIds)
                {
                    InteractableObject o = InteractableObject.Get(id);
                    bool showInChart = chartContent.ShowInChart(o);
                    if (showInChart)
                    {
                        if (hoveredOrSelectedIds.Count > maxLines && ++count == maxLines)
                        {
                            sharedStringBuilder.Append("and ");
                            sharedStringBuilder.Append((hoveredOrSelectedIds.Count - maxLines + 1).ToString());
                            sharedStringBuilder.Append(" more...");
                            break;
                        }
                        sharedStringBuilder.AppendFormat("{0}\n", id2TextDict[id]);
                    }
                }
                infoText.text = sharedStringBuilder.ToString();
            }
        }

        /// <summary>
        /// Updates the visibility of the marker. If none of this marker's linked
        /// interactable objects should be shown in the chart, this marker is made
        /// invisible.
        /// </summary>
        private void UpdateVisibility()
        {
            image.enabled = showInChartCount > 0;
        }

        #region UnityEngine Callbacks

        /// <summary>
        /// Called by Unity, if this marker is clicked.
        /// 
        /// Selects/Toggles the linked interactable objects of this marker.
        /// </summary>
        public void ButtonClicked()
        {
            if (image.enabled)
            {
                if (!SEEInput.ToggleMetricHoveringSelection())
                {
                    InteractableObject.UnhoverAll(true);
                    InteractableObject.UnselectAll(true);
                }

                foreach (uint id in ids)
                {
                    InteractableObject o = InteractableObject.Get(id);
                    if (chartContent.ShowInChart(o))
                    {
                        o.SetSelect(!o.IsSelected, true);
                    }
                }
            }
        }

        /// <summary>
        /// Called by Unity, if the mouse hovers over this marker.
        /// 
        /// Hoveres every linked interactable object of this marker.
        /// </summary>
        /// <param name="eventData">Ignored.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            foreach (uint id in ids)
            {
                InteractableObject o = InteractableObject.Get(id);
                o.SetHoverFlag(HoverFlag.ChartMarker, true, true);
            }
        }

        /// <summary>
        /// Called by Unity, if the mouse stops hovering over this marker.
        /// 
        /// Unhoveres every linked interactable object of this marker.
        /// </summary>
        /// <param name="eventData">Ignored.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            foreach (uint id in ids)
            {
                InteractableObject o = InteractableObject.Get(id);
                o.SetHoverFlag(HoverFlag.ChartMarker, false, true);
            }
        }

        #endregion

        #region Internal Callbacks

        /// <summary>
        /// Called through event <see cref="InteractableObject.HoverIn"/>.
        /// 
        /// Updates the info text.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="_">ignored</param>
        public void OnHoverIn(InteractableObject interactableObject, bool _ = true)
        {
            if (!interactableObject.IsSelected)
            {
                hoveredOrSelectedIds.Add(interactableObject.ID);
            }
            UpdateInfoText();
        }

        /// <summary>
        /// Called through event <see cref="InteractableObject.HoverOut"/>.
        /// 
        /// Updates the info text.
        /// </summary>
        /// <param name="interactableObject">the object being hovered over</param>
        /// <param name="_">ignored</param>
        public void OnHoverOut(InteractableObject interactableObject, bool _ = true)
        {
            if (!interactableObject.IsSelected)
            {
                hoveredOrSelectedIds.Remove(interactableObject.ID);
            }
            UpdateInfoText();
        }

        /// <summary>
        /// Called through event <see cref="InteractableObject.SelectIn"/>.
        /// 
        /// Updates the info text and highlights this marker.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="_">ignored</param>
        public void OnSelectIn(InteractableObject interactableObject, bool _ = true)
        {
            selectedCount++;
            if (!interactableObject.IsHovered)
            {
                hoveredOrSelectedIds.Add(interactableObject.ID);
            }
            UpdateInfoText();
            markerHighlight.SetActive(true);
        }

        /// <summary>
        /// Called through event <see cref="InteractableObject.SelectOut"/>.
        /// 
        /// Updates the info text and stops highlighting this marker, if no other linked
        /// interactable object is still selected.
        /// </summary>
        /// <param name="interactableObject">the object being selected</param>
        /// <param name="_">ignored</param>
        public void OnSelectOut(InteractableObject interactableObject, bool _ = true)
        {
            selectedCount--;
            if (!interactableObject.IsHovered)
            {
                hoveredOrSelectedIds.Remove(interactableObject.ID);
            }
            UpdateInfoText();
            markerHighlight.SetActive(selectedCount > 0);
        }

        /// <summary>
        /// Called by <see cref="ChartContent.ShowInChartCallbackFn"/>, if one of the
        /// <see cref="InteractableObject"/>s with the <see cref="ids"/> should be shown
        /// or should no longer be shown within the chart.
        /// </summary>
        private void OnShowInChartEvent(bool value)
        {
            if (value)
            {
                showInChartCount++;
            }
            else
            {
                showInChartCount--;
            }
            UpdateInfoText();
            UpdateVisibility();
        }

        #endregion
    }
}