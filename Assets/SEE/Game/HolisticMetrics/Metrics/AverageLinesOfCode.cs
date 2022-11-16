using SEE.DataModel.DG;
using SEE.Game.City;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class manages the average lines of code metric.
    /// </summary>
    internal class AverageLinesOfCode : Metric
    {
        /// <summary>
        /// Calculates the average lines of code for the nodes of the given SEECity.
        /// </summary>
        /// <param name="city">The city for which to calculate the average lines of code</param>
        /// <returns>The average lines of code of the given city</returns>
        internal override MetricValue Refresh(SEECity city)
        {
            int totalNodes = 0;
            float totalLines = 0.0f;

            foreach (Node node in city.LoadedGraph.Nodes())
            {
                if (node.TryGetNumeric("Metric.Lines.LOC", out var lines))
                {
                    totalLines += lines;
                    totalNodes++;
                }
            }

            MetricValueRange metricValueRange;
            
            if (totalNodes == 0)
            {
                Debug.LogError("No nodes were found so the average lines of code metric does not make any " +
                               " sense\n");
                metricValueRange = new MetricValueRange()
                {
                    Name = "Average lines of code",
                    Value = 0,
                    Higher = 300,
                    Lower = 0,
                    DecimalPlaces = 0
                };
            }
            else
            {
                metricValueRange = new MetricValueRange()
                {
                    Name = "Average lines of code",
                    Value = totalLines / totalNodes,
                    Higher = 300,
                    Lower = 0,
                    DecimalPlaces = 0
                };    
            }

            return metricValueRange;
        }
    }
}
