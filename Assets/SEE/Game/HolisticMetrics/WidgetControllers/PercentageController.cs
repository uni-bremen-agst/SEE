using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    internal class PercentageController : WidgetController
    {
        [SerializeField] private Text valueText;
        [SerializeField] private Text titleText;
        [SerializeField] private Image fade;

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
                valueText.text = percentage.ToString("0.##", CultureInfo.InvariantCulture) + "%";
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