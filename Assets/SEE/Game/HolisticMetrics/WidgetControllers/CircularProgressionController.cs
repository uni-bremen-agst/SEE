using System;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This class controls what is displayed on the circular progression widget.
    /// </summary>
    internal class CircularProgressionController : WidgetController
    {
        /// <summary>
        /// The image of the progression circle. When displaying the value, we will change how many percent of this will
        /// be visible.
        /// </summary>
        [SerializeField] private Image circle;

        /// <summary>
        /// Displays the given metric value which means setting how far around the widget the color fade goes and what
        /// the value in the center says.
        /// </summary>
        /// <param name="metricValue">The MetricValue to display</param>
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue is MetricValueRange metricValueRange)
            {
                TitleText.text = metricValueRange.Name;
                ValueText.text = metricValueRange.Value.ToString($"F{metricValueRange.DecimalPlaces}");
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                float actual = metricValueRange.Value - metricValueRange.Lower;
                circle.fillAmount = actual / maximum;
            }
            else if (metricValue is MetricValueCollection metricValueCollection)
            {
                if (metricValueCollection.MetricValues.Count > 0)
                {
                    Display(metricValueCollection.MetricValues[0]);
                }
            }
            else
            {
                throw new ArgumentException($"The type {metricValue.GetType()} cannot be displayed with" +
                                            $"the CircularProgression widget.");
            }
        }
    }
}
