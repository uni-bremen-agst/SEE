using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    internal class CircularProgressionController : WidgetController
    {
        [SerializeField] private Image circle;
        [SerializeField] private Text text;
        [SerializeField] private Text title;

        internal override void Display(MetricValue metricValue)
        {
            if (metricValue.GetType() == typeof(MetricValueRange))
            {
                MetricValueRange metricValueRange = (MetricValueRange)metricValue;
                title.text = metricValueRange.Name;
                text.text = metricValueRange.Value.ToString("F" + metricValue.DecimalPlaces);
                float maximum = metricValueRange.Higher - metricValueRange.Lower;
                float actual = metricValueRange.Value - metricValueRange.Lower;
                circle.fillAmount = actual / maximum;    
            }
            else if (metricValue.GetType() == typeof(MetricValueCollection))
            {
                MetricValueCollection metricValueCollection = (MetricValueCollection)metricValue;
                Display(metricValueCollection.MetricValues[0]);
            }
        }
    }
}
