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
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game.Charts
{
    /// <summary>
    ///     Manages results and options of <see cref="TMP_Dropdown" />s used to select what metric to display
    ///     on a charts axis.
    /// </summary>
    public class AxisContentDropdown : MonoBehaviour
    {

        /// <summary>
        ///  Visualizes the content displayed in the chart.
        /// </summary>
        private ChartContent _chartContent;

        /// <summary>
        ///     A dropdown containing options for different metrics to display on a charts axes.
        /// </summary>
        private TMP_Dropdown _dropdown;

        /// <summary>
        ///     The currently selected option of the <see cref="_dropdown" />.
        /// </summary>
        public string CurrentlySelectedMetric { get; private set; }

        /// <summary>
        ///     Adds all possible options to the <see cref="TMP_Dropdown" />.
        /// </summary>
        public void Initialize()
        {
            _chartContent = transform.parent.parent.GetComponent<ChartContent>();
            _dropdown = GetComponent<TMP_Dropdown>();
            UpdateInternal();
        }

        private void UpdateInternal()
        {
            FillDropDown();
            if (_dropdown.options.Count > 0)
            {
                CurrentlySelectedMetric = GetEntry(_dropdown.options[0].text);
                _chartContent.SetInfoText();
                _dropdown.captionText.text = CurrentlySelectedMetric;
            }
            else
            {
                Debug.LogWarning("There are no metrics for the charts.\n");
            }
        }

        /// <summary>
        /// If <paramref name="entry"/> equals <see cref="specialEntry"/>, entry is returned.
        /// Otherwise the ChartManager.MetricPrefix is appended to <paramref name="entry"/>.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>either entry or entry with the ChartManager.MetricPrefix</returns>
        private string GetEntry(string entry)
        {
            return entry.Equals(specialEntry) ? entry : ChartManager.MetricPrefix + entry;
        }

        /// <summary>
        /// The special entry that was requsted to be added in addition to the node metrics.
        /// It can be added by clients by way of calling AddEntry(). The special entry
        /// is not a metric, but a place holder for specifying that the nodes should be
        /// enumerated on the axis.
        /// </summary>
        private string specialEntry;

        /// <summary>
        /// For requests to a add special entry in addition to the node metrics
        /// derived from AllMetricNames of the ChartContent. The special entry
        /// will be added to the drop box before AllMetricNames in verbatim
        /// (no removal of any prefix whatsoever). The special entry is not a
        /// metric, but a place holder for specifying that the nodes should be
        /// enumerated on the axis.
        /// </summary>
        /// <param name="entry">dropdown entry to be added (exactly at it appears)</param>
        public void AddNodeEnumerationEntry(string entry)
        {
            Assert.IsTrue(string.IsNullOrEmpty(specialEntry));
            specialEntry = entry;
            UpdateInternal();
        }

        /// <summary>
        /// First the special entry that was added by AddEntry() and only then all 
        /// node metrics are added to the dropdown. The MetricPrefix of the node metrics (not those
        /// added by AddEntry()) will be removed for the labels added to the 
        /// dropdown box.
        /// </summary>
        private void FillDropDown()
        {
            _dropdown.ClearOptions();
            if (!string.IsNullOrEmpty(specialEntry))
            {
                List<string> options = new List<string>() { specialEntry };
                _dropdown.AddOptions(options);
            }
            {
                // Add all node metrics without their prefix
                string[] options = _chartContent.allMetricNames.ToArray();
                int MetricPrefixLength = ChartManager.MetricPrefix.Length;
                for (int i = 0; i < options.Length; i++)
                {
                    options[i] = options[i].Remove(0, MetricPrefixLength);
                }
                _dropdown.AddOptions(options.ToList());
            }
        }

        /// <summary>
        ///     Updates <see cref="CurrentlySelectedMetric" /> to match the selected option of <see cref="_dropdown" />
        /// </summary>
        public void ChangeValue()
        {
            string currentValue = _dropdown.options[_dropdown.value].text;
            CurrentlySelectedMetric = GetEntry(currentValue);
            _chartContent.DrawData();
            _chartContent.SetInfoText();
        }

        /// <summary>
        ///     Changes the text of the dropdown.
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text)
        {
            _dropdown.captionText.text = text;
        }
    }
}