using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    internal class NumberOfNodeTypes : Metric
    {
        [SerializeField] private int optimalValue;
        [SerializeField] private int worstValue = 10;
        
        internal override void Refresh()
        {
            HashSet<string> nodeTypes = new HashSet<string>();
            
            foreach (GameObject graphElement in GraphElements)
            {
                if (!graphElement.tag.Equals(Tags.Node))
                {
                    continue;
                }
                Node node = graphElement.GetComponent<NodeRef>().Value;
                nodeTypes.Add(node.Type);
            }

            MetricValueRange metricValueRange = new MetricValueRange()
            {
                Name = "Number of node types",
                Value = nodeTypes.Count,
                Lower = optimalValue,
                Higher = worstValue
            };
            
            WidgetController.Display(metricValueRange);
        }
    }
}