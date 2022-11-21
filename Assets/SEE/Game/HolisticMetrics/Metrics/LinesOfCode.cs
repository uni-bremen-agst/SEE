using System.Collections.Generic;
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
        private const string attributeName = "Metric.Lines.LOC";
        
        /// <summary>
        /// Given a see city, gathers the lines of code metric from each node and puts it into a metric value
        /// collection.
        /// </summary>
        /// <param name="city">The city from which the metric should be fetched</param>
        /// <returns>A metric value collection of all the values for the lines of code metric</returns>
        internal override MetricValue Refresh(SEECity city)
        {
            MetricValueCollection collection = new MetricValueCollection()
            {
                MetricValues = new List<MetricValueRange>()
            };
            foreach (Node node in city.LoadedGraph.Nodes())
            {
                if (node.TryGetNumeric(attributeName, out var lines))
                {
                    MetricValueRange metricValue = new MetricValueRange()
                    {
                        DecimalPlaces = 2,
                        Higher = 300,
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
