using System.Collections.Generic;
using SEE.Game.HolisticMetrics.Metrics;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
{
    public class CanvasController : MonoBehaviour
    {
        private readonly List<Metric> _metrics = new List<Metric>();

        /// <summary>
        /// This method will be called by every Metric to register itself with this CanvasController.
        /// </summary>
        /// <param name="metric"></param>
        internal void Register(Metric metric)
        {
            _metrics.Add(metric);
        }
        
        /// <summary>
        /// Whenever a code city changes, this method needs to be called. It will call the Refresh() methods of all
        /// Metrics.
        /// </summary>
        public void OnGraphLoad()
        {
            foreach (Metric metric in _metrics)
            {
                metric.Refresh();
            }
        }
    }
}