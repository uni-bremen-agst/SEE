using System;
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
        /// Displays the given metric value on the water circle widget, means that we will change the color of the water
        /// and circle and change the percentage.
        /// </summary>
        /// <param name="metricValue">The metric value to display</param>
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue is MetricValueRange metricValueRange)
            {
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                float actual = metricValueRange.Value - metricValueRange.Lower;
                float percentage = actual / maximum;
                Color color = MapPercentToColor(percentage);
                circle.color = color;
                water.color = color;
                titleText.text = metricValueRange.Name;
                percentage *= 100;
                if (metricValueRange.DecimalPlaces < 2)
                {
                    metricValueRange.DecimalPlaces = 0;
                }
                else
                {
                    metricValueRange.DecimalPlaces -= 2;
                }
                valueText.text = $"{percentage.ToString($"F{metricValueRange.DecimalPlaces}")}%";
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
                                            $"the WaterCircle widget.");
            }
        }

        /// <summary>
        /// for a percentage between [0, 1], gives a Color that represents that percentage.
        /// </summary>
        /// <param name="fraction">The percentage to be color-coded</param>
        /// <returns>The color that represents the given percentage</returns>
        private static Color MapPercentToColor(float fraction)
        {
            if (fraction < 0f)
            {
                Debug.LogError("Percentage can't be less than 0\n");
                return Color.white;
            }

            if (fraction < 0.2f)
            {
                return Color.green;
            }

            if (fraction < 0.4f)
            {
                return Color.cyan;
            }

            if (fraction < 0.6f)
            {
                return Color.blue;
            }

            if (fraction < 0.8f)
            {
                return Color.magenta;
            }

            if (fraction <= 1.0f)
            {
                return Color.red;
            }

            Debug.LogError("Percentage can't be more than 100\n");
            return Color.white;
        }
    }
}
