using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This class manages the average lines of code metric.
    /// </summary>
    public class AverageLinesOfCode : Metric
    {
        internal override void Refresh()
        {
            int totalNodes = 0;
            float totalLines = 0.0f;

            foreach (GameObject node in GraphElementIDMap.mapping.Values)
            {
                if (!node.tag.Equals(Tags.Node))
                    continue;
                Node graphNode = node.GetComponent<NodeRef>().Value;
                if (graphNode != null && graphNode.TryGetNumeric("Metric.Lines.LOC", out var lines))
                {
                    totalLines += lines;
                    totalNodes++;
                }
            }

            if (totalNodes != 0)
            {
                RangeValue value = new RangeValue(totalLines / totalNodes);
                WidgetController.Display(value, "Average lines of code");
            }
        }
    }
}