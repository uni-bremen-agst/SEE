using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    internal class WaterCircleController : WidgetController
    {
        [SerializeField] private Image circle;
        [SerializeField] private Image water;
        [SerializeField] private Text titleText;
        [SerializeField] private Text valueText;

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
