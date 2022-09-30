using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics.Metrics;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// This class controls/manages a holistic metrics board.
    /// </summary>
    internal class BoardController : MonoBehaviour
    {
        /// <summary>
        /// Each anchor for positioning widgets on the metrics board needs to be added to this list. If you need more
        /// positions for adding widgets, just add them to this list.
        /// </summary>
        [SerializeField] private List<GameObject> anchors;
        
        /// <summary>
        /// This contains references to all the metrics that registered themselves with this controller. This list is
        /// needed so we can refresh them.
        /// </summary>
        internal readonly List<Metric> metrics = new List<Metric>();

        /// <summary>
        /// The title of the board that this controller controls.
        /// </summary>
        private string title;

        internal string getTitle()
        {
            return title;
        }
        
        internal void setTitle(string newTitle)
        {
            title = newTitle;
            gameObject.GetComponentInChildren<Text>().text = newTitle;
        }
        
        /// <summary>
        /// If there is still space on the metrics board (there are less than 6 widgets on it), adds the desired widget
        /// to the board.
        /// </summary>
        /// <param name="metricType">The type of the metric to use.</param>
        /// <param name="widget">The widget prefab to use.</param>
        internal void AddMetric(Type metricType, GameObject widget)
        {
            if (metrics.Count < anchors.Count)
            {
                GameObject widgetInstance = Instantiate(widget, anchors[metrics.Count].transform);
                Metric metricInstance = (Metric)widgetInstance.AddComponent(metricType);
                metrics.Add(metricInstance);
            }
            else
            {
                // Show a popup that tells the user the metrics board is full.
            }
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