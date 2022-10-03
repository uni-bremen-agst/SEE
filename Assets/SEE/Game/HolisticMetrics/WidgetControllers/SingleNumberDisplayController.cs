using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    internal class SingleNumberDisplayController : WidgetController
    {
        [SerializeField] private Text valueText;
        [SerializeField] private Text titleText;

        internal override void Display(MetricValue metricValue)
        {
            if (metricValue.GetType() == typeof(MetricValueRange))
            {
                MetricValueRange metricValueRange = (MetricValueRange)metricValue;
                valueText.text = metricValueRange.Value.ToString(CultureInfo.InvariantCulture);
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
