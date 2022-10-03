using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        
        /// <summary>
        /// The list of all metric types. This is shared between all BoardController instances and is not expected to
        /// change. 
        /// </summary>
        private static readonly Type[] metricTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(domainAssembly => domainAssembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(Metric)))
            .ToArray();
        
        /// <summary>
        /// The array of all widget prefabs. This is shared by all BoardController instances and is not expected to
        /// change.
        /// </summary>
        private static GameObject[] widgetPrefabs = 
            Resources.LoadAll<GameObject>(Path.Combine("Prefabs", "HolisticMetrics", "Widgets"));

        internal string GetTitle()
        {
            return title;
        }
        
        internal void GetTitle(string newTitle)
        {
            title = newTitle;
            gameObject.GetComponentInChildren<Text>().text = newTitle;
        }
        
        /// <summary>
        /// If there is still space on the metrics board (there are less than 6 widgets on it), adds the desired widget
        /// to the board.
        /// </summary>
        /// <param name="widgetConfiguration">The configuration of the new widget.</param>
        internal void AddMetric(WidgetConfiguration widgetConfiguration)
        {
            if (metrics.Count < anchors.Count)
            {
                GameObject widget = Array.Find(widgetPrefabs,
                    element => element.name.Equals(widgetConfiguration.WidgetName));
                Type metricType = Array.Find(metricTypes, 
                    type => type.Name.Equals(widgetConfiguration.MetricType));
                if (widget is null)
                {
                    Debug.LogError("Could not load widget because the widget name from the configuration" +
                                   "file matches no existing widget prefab. This could be because the configuration" +
                                   "file was manually changed.");
                }
                else if (metricType is null)
                {
                    Debug.LogError("Could not load metric because the metric type from the configuration" +
                                   "file matches no existing metric type. This could be because the configuration" +
                                   "file was manually changed.");
                }
                else
                {
                    // TODO: Do not use the anchors, use the coordinates from the configuration instead.
                    GameObject widgetInstance = Instantiate(widget, anchors[metrics.Count].transform);
                    Metric metricInstance = (Metric)widgetInstance.AddComponent(metricType);
                    metrics.Add(metricInstance);
                }
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