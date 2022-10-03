using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
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
        /// This contains references to all widgets on the board each represented by one WidgetController and one
        /// Metric. This list is needed so we can refresh the metrics.
        /// </summary>
        internal readonly List<(WidgetController, Metric)> widgets = new List<(WidgetController, Metric)>();

        /// <summary>
        /// The title of the board that this controller controls.
        /// </summary>
        private string title;

        /// <summary>
        /// The list of all available metric types. This is shared between all BoardController instances and is not
        /// expected to change at runtime. 
        /// </summary>
        private Type[] metricTypes;

        /// <summary>
        /// The array of all available widget prefabs. This is shared by all BoardController instances and is not
        /// expected to change at runtime.
        /// </summary>
        private GameObject[] widgetPrefabs;

        /// <summary>
        /// Instantiates the metricTypes and widgetPrefabs arrays.
        /// </summary>
        private void Awake()
        {
            widgetPrefabs = 
                Resources.LoadAll<GameObject>(Path.Combine("Prefabs", "HolisticMetrics", "Widgets"));
            
            metricTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Metric)))
                .ToArray();
        }
        
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
            GameObject widget = Array.Find(widgetPrefabs,
                element => element.name.Equals(widgetConfiguration.WidgetName));
            Type metricType = Array.Find(metricTypes,
                type => type.Name.Equals(widgetConfiguration.MetricType));
            if (widget is null)
            {
                Debug.LogError("Could not load widget because the widget name from the configuration " +
                               "file matches no existing widget prefab. This could be because the configuration " +
                               "file was manually changed.");
            }
            else if (metricType is null)
            {
                Debug.LogError("Could not load metric because the metric type from the configuration " +
                               "file matches no existing metric type. This could be because the configuration " +
                               "file was manually changed.");
            }
            else
            {
                GameObject widgetInstance = Instantiate(widget, transform);
                WidgetController widgetController = widgetInstance.GetComponent<WidgetController>();
                Metric metricInstance = (Metric)widgetInstance.AddComponent(metricType);
                widgets.Add((widgetController, metricInstance));
            }
        }

        /// <summary>
        /// Whenever a code city changes, this method needs to be called. It will call the Refresh() methods of all
        /// Metrics.
        /// </summary>
        internal void OnGraphLoad()
        {
            foreach ((WidgetController, Metric) tuple in widgets)
            {
                // Recalculate the metric
                MetricValue metricValue = tuple.Item2.Refresh();
                
                // Display the new value on the widget
                tuple.Item1.Display(metricValue);
            }
        }
    }
}
