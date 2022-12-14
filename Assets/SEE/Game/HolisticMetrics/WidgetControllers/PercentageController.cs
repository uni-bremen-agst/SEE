using System;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This component controls what will be displayed on the percentage widget.
    /// </summary>
    internal class PercentageController : WidgetController
    {
        
        /// <summary>
        /// The image that goes around the widget. We will only display a certain percentage of this that correlates to
        /// the percentage of the metric value.
        /// </summary>
        [SerializeField] private Image fade;

        /// <summary>
        /// Displays the given metric value on this widget which means setting the title, the percentage text and the
        /// fill amount of the color fade that goes around this widget.
        /// </summary>
        /// <param name="metricValue">The MetricValue to display</param>
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue is MetricValueRange metricValueRange)
            {
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                float actual = metricValueRange.Value - metricValueRange.Lower;
                float percentage = actual / maximum;
                if (percentage < 0)
                {
                    Debug.LogError("Percentage must not be less than 0\n");
                    return;
                }
                if (percentage > 1)  // Assume that the caller meant it as a percentage between 0 and 100.
                {
                    Debug.LogError("Percentage must not be greater than 100%\n");
                    return;
                }
                fade.fillAmount = percentage;
                percentage *= 100;
                if (metricValueRange.DecimalPlaces < 2) 
                {
                    metricValueRange.DecimalPlaces = 0;
                }
                else
                {
                    metricValueRange.DecimalPlaces -= 2;
                }
                // We subtract 2 from the number of decimal places because we shifted all digits to the left by 2 by
                // multiplying percentage with 100
                valueText.text = percentage.ToString("F" + metricValueRange.DecimalPlaces) + "%";
                titleText.text = metricValueRange.Name;
            }
            else if (metricValue is MetricValueCollection metricValueCollection)
            {
                Display(metricValueCollection.MetricValues[0]);
            }
            else
            {
                throw new ArgumentException($"The type {metricValue.GetType()} cannot be displayed with" +
                                            $"the Percentage widget.");
            }
        }
    }
}
