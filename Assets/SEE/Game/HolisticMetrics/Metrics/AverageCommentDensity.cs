using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    internal class AverageCommentDensity : Metric
    {
        internal override MetricValue Refresh()
        {
            float totalDensity = 0f;
            int totalNodes = 0;

            foreach (GameObject graphElement in GraphElements)
            {
                if (!graphElement.tag.Equals(Tags.Node))
                {
                    continue;
                }
                    
                Node graphNode = graphElement.GetComponent<NodeRef>().Value;
                if (graphNode != null 
                    && graphNode.TryGetNumeric("Metric.Comment.Density", out float density))
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
                Higher = 1f
            };

            return metricValueRange;
        }
    }
}
