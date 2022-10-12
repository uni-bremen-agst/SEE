using SEE.DataModel.DG;
using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    internal class AverageCommentDensity : Metric
    {
        internal override MetricValue Refresh(SEECity city)
        {
            float totalDensity = 0f;
            int totalNodes = 0;

            foreach (Node node in city.LoadedGraph.Nodes())
            {
                if (node.TryGetNumeric("Metric.Comment.Density", out float density))
                {
                    totalDensity += density;
                    totalNodes++;
                }
            }

            MetricValueRange metricValueRange = new MetricValueRange()
            {
                Name = "Average comment density",
                Value = totalDensity / totalNodes,
                Lower = 0f,
                Higher = 1f,
                DecimalPlaces = 4
            };

            return metricValueRange;
        }
    }
}
