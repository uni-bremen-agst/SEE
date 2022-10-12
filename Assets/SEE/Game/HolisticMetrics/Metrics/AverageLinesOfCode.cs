using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class manages the average lines of code metric.
    /// </summary>
    internal class AverageLinesOfCode : Metric
    {
        [SerializeField] private int optimalValue;
        [SerializeField] private int worstValue = 300;
        
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
                               " sense");
                metricValueRange = new MetricValueRange()
                {
                    Name = "Average lines of code",
                    Value = 0,
                    Higher = worstValue,
                    Lower = optimalValue,
                    DecimalPlaces = 0
                };
            }
            else
            {
                metricValueRange = new MetricValueRange()
                {
                    Name = "Average lines of code",
                    Value = totalLines / totalNodes,
                    Higher = worstValue,
                    Lower = optimalValue,
                    DecimalPlaces = 0
                };    
            }

            return metricValueRange;
        }
    }
}
