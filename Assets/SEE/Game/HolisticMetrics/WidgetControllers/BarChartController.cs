using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This class provides an interface for displaying multiple metric values on a bar chart that this script will be
    /// attached to.
    /// </summary>
    internal class BarChartController : WidgetController
    {
        /// <summary>
        /// The parent game object for all bars we will add to the bar chart.
        /// </summary>
        [SerializeField] private GameObject barsAnchor;
        
        /// <summary>
        /// The labels of the diagram on the y axis.
        /// </summary>
        [SerializeField] private Text[] yLabels;

        /// <summary>
        /// The prefab of the bar that will be instantiated for each element whose metric value will be displayed.
        /// </summary>
        [SerializeField] private GameObject barPrefab;

        /// <summary>
        /// The length of the x axis of the coordinate system underlying the bar chart.
        /// </summary>
        private const float xAxisLength = 870f;
        
        internal override void Display(MetricValue metricValue)
        {
            if (metricValue is MetricValueCollection metrics)
            {
                // First, remove all old bars 
                DestroyChildren(barsAnchor.transform);
                
                IList<MetricValueRange> metricValues = metrics.MetricValues;
                
                // Set the name of the widget
                titleText.text = metrics.Name;

                // Now set the text of the labels to the correct values
                float minimum = metricValues.Min(x => x.Lower);
                float maximum = metricValues.Max(x => x.Higher);
                float range = maximum - minimum;
                float stepLength = range / 5f;  // Divide by |labels| -1 because first label is at y=0, not range/6
                if (minimum >= 0 && maximum <= 1)  // It is a percentage in [0, 1]
                {
                    for (int i = 0; i < yLabels.Length; i++)
                    {
                        float yValue = (minimum + stepLength * i) * 100;
                        yLabels[i].text = $"{yValue.ToString($"F{metricValues[0].DecimalPlaces - 2}")}%";
                    }
                }
                else
                {
                    for (int i = 0; i < yLabels.Length; i++)
                    {
                        float yValue = minimum + stepLength * i;
                        yLabels[i].text = yValue.ToString($"F{metricValues[0].DecimalPlaces}");
                    }
                }
                
                // Now draw all the bars
                float barDistance = 0f;
                if (metricValues.Count > 1)
                {
                    barDistance = xAxisLength / (metricValues.Count - 1);
                }
                for (int i = 0; i < metricValues.Count; i++)
                {
                    GameObject bar = Instantiate(barPrefab, barsAnchor.transform);
                    Vector3 coordinates = new Vector3(barDistance * i, 0f);
                    bar.transform.localPosition = coordinates;
                    bar.GetComponent<Image>().fillAmount = metricValues[i].Value / maximum;
                }
                
                // FIXME: Metric names currently aren't shown on X axis. This makes the chart hard to read.
            }
            else if (metricValue is MetricValueRange range)
            {
                // In case this method gets a single metric value, we put it into a new metric value collection and then
                // recursively call this method with that collection.
                
                MetricValueCollection collection = new MetricValueCollection
                {
                    MetricValues = new List<MetricValueRange> { range }
                };
                Display(collection);
            }
            else
            {
                throw new ArgumentException($"The type {metricValue.GetType()} cannot be displayed with" +
                                            $"the BarChart widget.");
            }
        }
    }
}