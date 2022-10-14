using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This component handles displaying values on the single number widget.
    /// </summary>
    internal class SingleNumberDisplayController : WidgetController
    {
        /// <summary>
        /// The text containing the value of the metric.
        /// </summary>
        [SerializeField] private Text valueText;
        
        /// <summary>
        /// The text stating what metric is being displayed.
        /// </summary>
        [SerializeField] private Text titleText;

        /// <summary>
        /// Displays the given metric value on the single number display widget.
        /// </summary>
        /// <param name="metricValue">The metric value to display</param>
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue.GetType() == typeof(MetricValueRange))
            {
                MetricValueRange metricValueRange = (MetricValueRange)metricValue;
                valueText.text = metricValueRange.Value.ToString("F" + metricValue.DecimalPlaces);
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
