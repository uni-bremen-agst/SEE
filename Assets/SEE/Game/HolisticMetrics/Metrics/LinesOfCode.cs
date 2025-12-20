using SEE.DataModel.DG;
using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class gets the lines of code metric of each node and puts it into a metric value collection. This means it
    /// could and should be displayed with a widget like the coordinate system.
    /// </summary>
    internal class LinesOfCode : Metric
    {
        /// <summary>
        /// This is the attribute name which we will try to get for each node. This should give us the lines of code
        /// metric for each node.
        /// </summary>
        private const string attributeName = DataModel.DG.Metrics.Prefix + "Lines.LOC";

        /// <summary>
        /// Returns the lines of code metric from each node of the graph underlying
        /// <paramref name="city"/> as a collection.
        /// </summary>
        /// <param name="city">The city from which the metric should be fetched.</param>
        /// <returns>A metric value collection of all the values for the lines of code
        /// metric for <paramref name="city"/>.</returns>
        internal override MetricValue Refresh(AbstractSEECity city)
        {
            base.Refresh(city);

            MetricValueCollection collection = new();
            foreach (Node node in city.LoadedGraph.Nodes())
            {
                if (node.TryGetNumeric(attributeName, out float lines))
                {
                    MetricValueRange metricValue = new()
                    {
                        DecimalPlaces = 2,
                        Higher = 300, // FIXME: There can be more than 300 LOC.
                        Lower = 0,
                        Name = "Lines of code",
                        Value = lines
                    };
                    collection.MetricValues.Add(metricValue);
                }
            }
            return collection;
        }
    }
}
