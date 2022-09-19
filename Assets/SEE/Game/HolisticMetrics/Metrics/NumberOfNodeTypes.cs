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
            
            foreach (GameObject graphElement in GraphElementIDMap.MappingForHolisticMetrics.Values)
            {
                if (!graphElement.tag.Equals(Tags.Node))
                    continue;
                Node node = graphElement.GetComponent<NodeRef>().Value;
                nodeTypes.Add(node.Type);
            }
            
            WidgetController.Display(nodeTypes.Count, "Number of node types");
            Debug.Log("Nodetypes: ");
            foreach (string nodeType in nodeTypes)
            {
                Debug.Log(nodeType);
            }
        }
    }
}