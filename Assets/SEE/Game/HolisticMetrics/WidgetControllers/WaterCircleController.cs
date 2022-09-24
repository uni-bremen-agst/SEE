using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    public class WaterCircleController : WidgetController
    {
        [SerializeField] private Image circle;
        [SerializeField] private Image water;
        [SerializeField] private Text titleText;
        [SerializeField] private Text valueText;

        public override void Display(MetricValue metricValue)
        {
            if (metricValue.GetType() == typeof(MetricValueRange))
            {
                MetricValueRange metricValueRange = (MetricValueRange)metricValue;
                Debug.Log(
                    $"Best: {metricValueRange.Higher}, " +
                    $"worst: {metricValueRange.Lower}, " +
                    $"current: {metricValueRange.Value}.");
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                Debug.Log(maximum);
                float actual = metricValueRange.Value - metricValueRange.Lower;
                Debug.Log(actual);
                float percentage = actual / maximum;
                Debug.Log(percentage);
                Color color = MapPercentToColor(percentage);
                circle.color = color;
                water.color = color;
                titleText.text = metricValueRange.Name;
                percentage *= 100;
                valueText.text = percentage.ToString("0.##", CultureInfo.InvariantCulture) + "%";
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
                throw new ArgumentException("Percentage can't be less than 0");
            if (percentage < 0.2f)
                return Color.green;
            if (percentage < 0.4f)
                return Color.cyan;
            if (percentage < 0.6f)
                return Color.blue;
            if (percentage < 0.8f)
                return Color.magenta;
            if (percentage <= 1.0f)
                return Color.red;
            throw new ArgumentException("Percentage can't be more than 100");
        }
    }
}