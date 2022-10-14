using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This component manages displaying values on the water circle widget. Please note that the water circle widget
    /// should only be used for displaying negative metrics meaning metrics which should be as low as possible. That is
    /// because this widget will change colors towards more negative colors like red when the values grow.
    /// </summary>
    internal class WaterCircleController : WidgetController
    {
        /// <summary>
        /// The circle that will have its color changed.
        /// </summary>
        [SerializeField] private Image circle;
        
        /// <summary>
        /// The water texture that will have its color changed.
        /// </summary>
        [SerializeField] private Image water;
        
        /// <summary>
        /// The name of the metric being displayed.
        /// </summary>
        [SerializeField] private Text titleText;
        
        /// <summary>
        /// The text showing the percentage value of the metric.
        /// </summary>
        [SerializeField] private Text valueText;

        /// <summary>
        /// Displays the given metric value on the water circle widget, means that we will change the color of the water
        /// and circle and change the percentage.
        /// </summary>
        /// <param name="metricValue">The metric value to display</param>
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue.GetType() == typeof(MetricValueRange))
            {
                MetricValueRange metricValueRange = (MetricValueRange)metricValue;
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                float actual = metricValueRange.Value - metricValueRange.Lower;
                float percentage = actual / maximum;
                Color color = MapPercentToColor(percentage);
                circle.color = color;
                water.color = color;
                titleText.text = metricValueRange.Name;
                percentage *= 100;
                if (metricValue.DecimalPlaces < 2) 
                {
                    metricValue.DecimalPlaces = 0;
                }
                else
                {
                    metricValue.DecimalPlaces -= 2;
                }
                valueText.text = percentage.ToString("F" + metricValue.DecimalPlaces) + "%";
            }
            else if (metricValue.GetType() == typeof(MetricValueCollection))
            {
                MetricValueCollection metricValueCollection = (MetricValueCollection)metricValue;
                Display(metricValueCollection.MetricValues[0]);
            }
        }

        /// <summary>
        /// for a percentage between [0, 1], gives a Color that represents that percentage.
        /// </summary>
        /// <param name="percentage">The percentage to be color-coded</param>
        /// <returns>The color that represents the given percentage</returns>
        private static Color MapPercentToColor(float percentage)
        {
            if (percentage < 0f)
            {
                Debug.LogError("Percentage can't be less than 0");
                return Color.white;
            }

            if (percentage < 0.2f)
            {
                return Color.green;
            }

            if (percentage < 0.4f)
            {
                return Color.cyan;
            }

            if (percentage < 0.6f)
            {
                return Color.blue;
            }

            if (percentage < 0.8f)
            {
                return Color.magenta;
            }

            if (percentage <= 1.0f)
            {
                return Color.red;
            }

            Debug.LogError("Percentage can't be more than 100");
            return Color.white;
        }
    }
}
