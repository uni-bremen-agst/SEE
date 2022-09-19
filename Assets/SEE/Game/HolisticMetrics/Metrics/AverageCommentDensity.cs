using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    public class AverageCommentDensity : Metric
    {
        internal override void Refresh()
        {
            float totalDensity = 0f;
            int totalNodes = 0;

            foreach (GameObject graphElement in GraphElements)
            {
                if (!graphElement.tag.Equals(Tags.Node))
                    continue;
                Node graphNode = graphElement.GetComponent<NodeRef>().Value;
                if (graphNode != null 
                    && graphNode.TryGetNumeric("Metric.Comment.Density", out float density))
                {
                    totalDensity += density;
                    totalNodes++;
                }
            }

            RangeValue rangeValue = new RangeValue(totalDensity / totalNodes, 0f, 1f);
            WidgetController.Display(rangeValue, "Average comment density");
        }
    }
}