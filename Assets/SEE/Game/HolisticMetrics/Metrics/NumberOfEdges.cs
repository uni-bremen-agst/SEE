using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class just manages the number of edges metric, which is just the number of edges of a given
    /// <see cref="SEECity"/>.
    /// </summary>
    internal class NumberOfEdges : Metric
    {
        /// <summary>
        /// This method just returns the total number of edges of a given <see cref="SEECity"/>.
        /// </summary>
        /// <param name="city">The <see cref="SEECity"/> of which to retrieve the edge count</param>
        /// <returns>The edge count of the given <see cref="SEECity"/></returns>
        internal override MetricValue Refresh(SEECity city)
        {
            return new MetricValueRange
            {
                DecimalPlaces = 0,
                Higher = 5000,
                Lower = 0,
                Name = "Number of edges",
                Value = city.LoadedGraph.EdgeCount
            };
        }
    }
}