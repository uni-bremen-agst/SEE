using UnityEngine;
using SEE.DataModel;
using System.Collections.Generic;

namespace SEE.Layout
{
    /// <summary>
    /// Provides x, y, z lengths of a node based on a linear interpolation
    /// of the node's metrics.
    /// </summary>
    internal class LinearScale : IScale
    {      
        public LinearScale(Graph graph, float minimalLength, float maximalLength,
                           string widthMetric, string heightMetric, string breadthMetric)
            : base(widthMetric, heightMetric, breadthMetric)
        {
            this.metricMaxima = DetermineMetricMaxima(graph, widthMetric, heightMetric, breadthMetric);
            this.minimalLength = minimalLength;
            this.maximalLength = maximalLength;
        }

        private readonly Dictionary<string, float> metricMaxima;
        private readonly float minimalLength;
        private readonly float maximalLength;

        /// <summary>
        /// Yields a vector where each element (x, y, z) is a linear interpolation of the normalized
        /// value of the metrics that determine the width, height, and breadth of the given node.
        /// The range of the linear interpolation is set by [minimalLength, maximalLength].
        /// </summary>
        /// <param name="node">node for which to determine the x, y, z lengths</param>
        /// <returns>x, y, z lengths of node</returns>
        public override Vector3 Lengths(Node node)
        {
            float x;
            float y;
            float z;

            if (node != null)
            {
                x = Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, widthMetric));
                y = Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, heightMetric));
                z = Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, breadthMetric));
            }
            else
            {
                x = minimalLength;
                y = minimalLength;
                z = minimalLength;
            }
            return new Vector3(x, z, y);
        }

        /// <summary>
        /// Returns a value in the range [0.0, 1.0] representing the relative value of the given
        /// metric in the metrics value range for the given node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="metric"></param>
        /// <returns></returns>
        protected float NormalizedMetric(Dictionary<string, float> metricMaxima, Node node, string metric)
        {
            float max = metricMaxima[metric];

            if (max <= 0.0f)
            {
                return 0.0f;
            }
            else if (node.TryGetNumeric(metric, out float width))
            {
                if (width <= 0.0f)
                {
                    return 0.0f;
                }
                else
                {
                    return (float)width / max;
                }
            }
            else
            {
                return 0.0f;
            }
        }

        /// <summary>
        /// Returns the maximal values of the given node metrics.
        /// </summary>
        /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
        /// <returns>metric maxima</returns>
        protected Dictionary<string, float> DetermineMetricMaxima(Graph graph, params string[] metrics)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            foreach (string metric in metrics)
            {
                result.Add(metric, 0.0f);
            }

            foreach (Node node in graph.Nodes())
            {
                foreach (string metric in metrics)
                {
                    if (node.TryGetNumeric(metric, out float value))
                    {
                        if (value > result[metric])
                        {
                            result[metric] = value;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Dumps metricMaxima for debugging.
        /// </summary>
        protected void DumpMetricMaxima(Dictionary<string, float> metricMaxima)
        {
            foreach (var item in metricMaxima)
            {
                Debug.Log("maximum of " + item.Key + ": " + item.Value + "\n");
            }
        }
    }
}
