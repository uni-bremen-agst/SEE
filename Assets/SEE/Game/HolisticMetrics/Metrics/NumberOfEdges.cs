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
        /// This method just returns the total number of edges of a given <paramref name="city"/>.
        /// </summary>
        /// <param name="city">The code city of which to retrieve the edge count</param>
        /// <returns>The edge count of the given <paramref name="city"/></returns>
        internal override MetricValue Refresh(AbstractSEECity city)
        {
            base.Refresh(city);

            return new MetricValueRange
            {
                DecimalPlaces = 0,
                Higher = 5000, // FIXME: There can be more than 5000 edges.
                Lower = 0,
                Name = "Number of edges",
                Value = city.LoadedGraph.EdgeCount
            };
        }
    }
}
