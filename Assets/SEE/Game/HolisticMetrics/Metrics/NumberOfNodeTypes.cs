using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    public class NumberOfNodeTypes : Metric
    {
        internal override void Refresh()
        {
            HashSet<string> nodeTypes = new HashSet<string>();
            
            foreach (GameObject graphElement in GraphElements)
            {
                if (!graphElement.tag.Equals(Tags.Node))
                    continue;
                Node node = graphElement.GetComponent<NodeRef>().Value;
                nodeTypes.Add(node.Type);
            }

            MetricValueRange metricValueRange = new MetricValueRange()
            {
                Name = "Number of node types",
                Value = nodeTypes.Count
            };
            
            WidgetController.Display(metricValueRange);
        }
    }
}