using System.Collections.Generic;
using SEE.Game.HolisticMetrics.Metrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    internal class CanvasController : MonoBehaviour
    {
        private readonly List<Metric> metrics = new List<Metric>();

        /// <summary>
        /// This method will be called by every Metric to register itself with this CanvasController.
        /// </summary>
        /// <param name="metric"></param>
        internal void Register(Metric metric)
        {
            metrics.Add(metric);
        }
        
        /// <summary>
        /// Whenever a code city changes, this method needs to be called. It will call the Refresh() methods of all
        /// Metrics.
        /// </summary>
        internal void OnGraphLoad()
        {
            // Before iterating through the list, ensure no metric is null.
            metrics.RemoveAll(metric => metric is null);
            
            foreach (Metric metric in metrics)
            {
                metric.Refresh();
            }
        }
    }
}