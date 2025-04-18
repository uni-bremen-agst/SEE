using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.HolisticMetrics.WidgetControllers
{
    /// <summary>
    /// This class is the controller of the coordinate system widget.
    /// </summary>
    /// <remarks>It is attached to the CoordinateSystem.prefab.</remarks>
    internal class CoordinatesController : WidgetController
    {
        /// <summary>
        /// The labels on the y axis. The one at index 0 is the lowest, the one at index 5 is the highest.
        /// </summary>
        [SerializeField] private Text[] yLabels;

        /// <summary>
        /// The anchor GameObject for all points. This will be the parent of all points.
        /// </summary>
        [SerializeField] private GameObject coordinatesAnchor;

        /// <summary>
        ///  The prefab for the points that will be drawn in the coordinate system.
        /// </summary>
        [SerializeField] private GameObject pointPrefab;

        /// <summary>
        /// The length of the x axis of this coordinate system.
        /// </summary>
        private const float xAxisLength = 900f;

        /// <summary>
        /// Sets the title of the widget, the labels on the y axis and draws all the values as points.
        /// </summary>
        /// <param name="metricValue">The value that is to be displayed.</param>
        internal override void Display(MetricValue metricValue)
        {
            // First we need to remove all current points
            DestroyChildren(coordinatesAnchor.transform);

            if (metricValue is MetricValueCollection valueCollection)
            {
                // Cast the metric value so we can use its collection feature
                IList<MetricValueRange> metricValues = valueCollection.MetricValues;

                // Set the title of the widget
                TitleText.text = valueCollection.Name;

                if (metricValues.Count == 0)
                {
                    return;
                }
                // First we need to get the x axis coordinate. For dividing the length of the axis (900) by the number
                // of given values (nodes) in the collection, we want to subtract one from the number of values. That is
                // because The first point can be at the x coordinate 0, not x-width/|nodes|.
                float xDistance = 0f;
                if (metricValues.Count > 1)
                {
                    xDistance = xAxisLength / (metricValues.Count - 1);
                }

                // Before we can start, we need to find out the largest and smallest metric value for scaling the
                // coordinate system.
                float minimum = metricValues.Min(x => x.Lower);
                float maximum = metricValues.Max(x => x.Higher);
                float range = maximum - minimum;

                // Divide by (number of labels - 1) because the first label will be at "minimum".
                float stepLength = range / 5f;

                // Now we set the labels on the y axis.
                bool isPercentage = minimum >= 0 && maximum <= 1;  // Handle differently if is percentage in [0, 1]
                for (int i = 0; i < Mathf.Min(yLabels.Length, metricValues.Count); i++)
                {
                    float yValue = minimum + stepLength * i;
                    if (isPercentage)
                    {
                        yValue *= 100;
                    }
                    string labelText = isPercentage ? $"{yValue.ToString($"F{metricValues[i].DecimalPlaces - 2}")}%"
                                                    : yValue.ToString("F" + metricValues[i].DecimalPlaces);

                    yLabels[i].text = labelText;
                }

                // Now we draw all the points.
                for (int i = 0; i < metricValues.Count; i++)
                {
                    Vector3 coordinates = new Vector3(
                        xDistance * i,
                        metricValues[i].Value / range * 330f);  // FIXME: Magic number
                    GameObject point = Instantiate(pointPrefab, coordinatesAnchor.transform);
                    point.transform.localPosition = coordinates;
                }
            }
            else if (metricValue is MetricValueRange range)
            {
                // It would be weird to give this widget a single metric value. But in case that is ever done, we still
                // handle that case by putting that value in a MetricValueCollection so we can recursively call this
                // method. (Otherwise we would have to add a lot of functionality in this else branch also.
                MetricValueCollection collection = new MetricValueCollection
                {
                    MetricValues = new List<MetricValueRange> { range }
                };
                Display(collection);
            }
            else
            {
                throw new ArgumentException($"The type {metricValue.GetType()} cannot be displayed with"
                                            + "the CoordinateSystem widget.");
            }
        }
    }
}
