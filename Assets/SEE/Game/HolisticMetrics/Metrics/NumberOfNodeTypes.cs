using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class calculates the number of node types metric.
    /// </summary>
    internal class NumberOfNodeTypes : Metric
    {
        /// <summary>
        /// Calculates the number of node types of the given <paramref name="city"/>.
        /// </summary>
        /// <param name="city">The city for which to get the number of node types metric.</param>
        /// <returns>The number of node types of the given <paramref name="city"/>.</returns>
        internal override MetricValue Refresh(AbstractSEECity city)
        {
            base.Refresh(city);

            MetricValueRange metricValueRange = new MetricValueRange
            {
                Name = "Number of node types",
                Value = city.LoadedGraph.AllNodeTypes().Count,
                Lower = 0,
                Higher = 10,
                DecimalPlaces = 0
            };

            return metricValueRange;
        }
    }
}
