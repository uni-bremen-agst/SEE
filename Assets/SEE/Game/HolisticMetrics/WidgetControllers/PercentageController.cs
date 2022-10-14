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
        /// The text displaying the percentage of the given metric value.
        /// </summary>
        [SerializeField] private Text valueText;
        
        /// <summary>
        /// The title text stating what metric is being displayed by the widget.
        /// </summary>
        [SerializeField] private Text titleText;
        
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
            if (metricValue.GetType() == typeof(MetricValueRange))
            {
                MetricValueRange metricValueRange = (MetricValueRange)metricValue;
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                float actual = metricValueRange.Value - metricValueRange.Lower;
                float percentage = actual / maximum;
                if (percentage > 100)
                {
                    Debug.LogError("Percentage must not be greater than 100");
                    return;
                }
                if (percentage < 0)
                {
                    Debug.LogError("Percentage must not be less than 0");
                    return;
                }
                if (percentage > 1)  // Assume that the caller meant it as a percentage between 0 and 100.
                {
                    percentage /= 100;
                }
                fade.fillAmount = percentage;
                percentage *= 100;
                if (metricValue.DecimalPlaces < 2) 
                {
                    metricValue.DecimalPlaces = 0;
                }
                else
                {
                    metricValue.DecimalPlaces -= 2;
                }
                // We subtract 2 from the number of decimal places because we shifted all digits to the left by 2 by
                // multiplying percentage with 100
                valueText.text = percentage.ToString("F" + metricValue.DecimalPlaces) + "%";
                titleText.text = metricValueRange.Name;
            }
            else if (metricValue.GetType() == typeof(MetricValueCollection))
            {
                MetricValueCollection metricValueCollection = (MetricValueCollection)metricValue;
                Display(metricValueCollection.MetricValues[0]);
            }
        }
    }
}
