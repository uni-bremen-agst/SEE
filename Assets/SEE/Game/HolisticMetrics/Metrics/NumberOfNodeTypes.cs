using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    internal class NumberOfNodeTypes : Metric
    {
        internal override MetricValue Refresh(SEECity city)
        {
            MetricValueRange metricValueRange = new MetricValueRange()
            {
                Name = "Number of node types",
                Value = city.LoadedGraph.AllNodeTypes().Count,
                Lower = 0,
                Higher = 10
            };

            return metricValueRange;
        }
    }
}
