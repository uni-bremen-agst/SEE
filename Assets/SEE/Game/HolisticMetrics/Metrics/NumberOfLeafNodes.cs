using System.Linq;
using SEE.DataModel.DG;
using SEE.Game.City;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This metric simply returns the total number of leaves in a code city.
    /// </summary>
    internal class NumberOfLeafNodes : Metric
    {
        /// <summary>
        /// Counts the number of leaves of a given <paramref name="city"/> and returns it.
        /// </summary>
        /// <param name="city">The code city of which to get the leaf count.</param>
        /// <returns>The leaf count of the given <paramref name="city"/>.</returns>
        internal override MetricValue Refresh(AbstractSEECity city)
        {
            base.Refresh(city);

            Graph graph = city.LoadedGraph;
            int leafCount = graph.Nodes().Count(node => node.IsLeaf());
            return new MetricValueRange
            {
                DecimalPlaces = 0,
                Higher = 1000f, // FIXME: There can be more than 1000 leaves.
                Lower = 1f,
                Name = "Number of leaf nodes",
                Value = leafCount
            };
        }
    }
}
