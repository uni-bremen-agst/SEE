using System;
using System.Linq;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This component handles displaying values on the single number widget.
    /// </summary>
    internal class SingleNumberDisplayController : WidgetController
    {
        /// <summary>
        /// Displays the given metric value on the single number display widget.
        /// </summary>
        /// <param name="metricValue">The metric value to display.</param>
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue is MetricValueRange metricValueRange)
            {
                ValueText.text = MetricValueRangeToString(metricValueRange);
                TitleText.text = metricValueRange.Name;
            }
            else if (metricValue is MetricValueCollection metricValueCollection)
            {
                TitleText.text = metricValueCollection.Name;
                ValueText.text = string.Join(Environment.NewLine,
                                             metricValueCollection.MetricValues.Select(x => $"{x.Name}: {MetricValueRangeToString(x)}"));
            }
            else
            {
                throw new ArgumentException($"The type {metricValue.GetType()} cannot be displayed with" +
                                            $"the SingleNumberDisplay widget.");
            }

            static string MetricValueRangeToString(MetricValueRange range) => range.Value.ToString($"F{range.DecimalPlaces}");
        }
    }
}
