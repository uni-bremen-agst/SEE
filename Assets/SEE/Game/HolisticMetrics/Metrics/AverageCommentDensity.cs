using SEE.DataModel.DG;
using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class implements/calculates the average comment density metric.
    /// </summary>
    internal class AverageCommentDensity : Metric
    {
        /// <summary>
        /// We will try to get this attribute for each node. Should be the comment density for each node.
        /// </summary>
        private const string attributeName = "Metric.Comment.Density";
        
        /// <summary>
        /// Calculates the average comment density for the nodes of the given SEECity.
        /// </summary>
        /// <param name="city">The city for which to calculate the average comment density</param>
        /// <returns>The average comment density of the nodes of the given city</returns>
        internal override MetricValue Refresh(SEECity city)
        {
            float totalDensity = 0f;
            int totalNodes = 0;

            foreach (Node node in city.LoadedGraph.Nodes())
            {
                if (node.TryGetNumeric(attributeName, out float density))
                {
                    totalDensity += density;
                    totalNodes++;
                }
            }

            MetricValueRange metricValueRange = new MetricValueRange
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
