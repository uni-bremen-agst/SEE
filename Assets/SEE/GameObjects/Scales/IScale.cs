using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Abstract super class of all classes providing normalized node metrics.
    /// </summary>
    public abstract class IScale
    {
        /// <summary>
        /// Constructor defining the node metrics to be normalized.
        /// </summary>
        /// <param name="graphs">the set of graphs whose node metrics are to be scaled</param>
        /// <param name="metrics">node metrics for scaling</param>
        /// <param name="minimalLength">the minimal value that can be returned by this scaling</param>
        /// <param name="minimalLength">the maximal value that can be returned by this scaling</param>
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        public IScale(ICollection<Graph> graphs, IList<string> metrics, float minimalLength, float maximalLength, 
                      bool leavesOnly)
        {
            this.metrics = metrics;
            this.minimalLength = minimalLength;
            this.maximalLength = maximalLength;
            this.leavesOnly = leavesOnly;
            metricMaxima = DetermineMetricMaxima(graphs, metrics, leavesOnly);
        }

        /// <summary>
        /// The list of metrics to be scaled.
        /// </summary>
        protected readonly IList<string> metrics;

        /// <summary>
        /// The maximal values of all metrics as a map metric-name -> maximal value.
        /// </summary>
        protected readonly Dictionary<string, float> metricMaxima;

        /// <summary>
        /// the minimal value that can be returned by this scaling
        /// </summary>
        protected readonly float minimalLength;
        /// <summary>
        /// the maximal value that can be returned by this scaling
        /// </summary>
        protected readonly float maximalLength;
        /// <summary>
        /// If true, the normalization is done only for leaf nodes.
        /// </summary>
        protected readonly bool leavesOnly;

        /// <summary>
        /// Yields a normalized value of the given node metric. The type of normalization
        /// is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node for which to determine the normalized value</param>
        /// <returns>normalized value of node metric</returns>
        public abstract float GetNormalizedValue(string metric, Node node);

        /// <summary>
        /// Yields a normalized value of the given node metric. The type of normalization
        /// is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">metric name</param>
        /// <param name="value">value to be normalized</param>
        /// <returns>normalized value</returns>
        public abstract float GetNormalizedValue(string metric, float value);

        /// <summary>
        /// Yields the normalized value of the maximum of the given metric.
        /// </summary>
        /// <param name="metric">metric for which to return the normalized maximum</param>
        /// <returns>normalized maximum</returns>
        public float GetNormalizedMaximum(string metric)
        {
            if (metricMaxima.TryGetValue(metric, out float value))
            {
                return GetNormalizedValue(metric, value);
            }
            else
            {
                Debug.LogErrorFormat("Attempt to retrieve the normalized maximum of metric {0} that is not known.\n", metric);
                Debug.Log("The available normalized metric maxima are as follows:\n");
                DumpMetricMaxima(metricMaxima);
                throw new Exception("A metric named " + metric + " does not exist.");
            }
        }

        /// <summary>
        /// Returns the normalization value of given node <paramref name="metric"/> of
        /// <paramref name="node"/> set in relation to the maximum value of the
        /// normalized <paramref name="metric"/>. Hence, the result is in [0,1].
        /// If the maximum normalized value of <paramref name="metric"/> is 0, 0
        /// is returned.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node whose metric is to be queried</param>
        /// <returns></returns>
        internal float GetRelativeNormalizedValue(string metric, Node node)
        {
            float maximum = GetNormalizedMaximum(metric);
            return maximum == 0 ? 0 : GetNormalizedValue(metric, node) / maximum;
        }

        /// <summary>
        /// Returns the maximal values of the given node metrics.
        /// </summary>
        /// <param name="graphs">the set of graphs for which to determine the node metric maxima</param>
        /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        /// <returns>metric maxima</returns>
        protected static Dictionary<string, float> DetermineMetricMaxima(ICollection<Graph> graphs, IList<string> metrics, bool leavesOnly)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            foreach (string metric in metrics)
            {
                result[metric] = 0.0f;
            }

            foreach (Graph graph in graphs)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (!leavesOnly || node.IsLeaf())
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
                }
            }
            return result;
        }

        /// <summary>
        /// Dumps metricMaxima for debugging.
        /// </summary>
        protected static void DumpMetricMaxima(Dictionary<string, float> metricMaxima)
        {
            foreach (KeyValuePair<string, float> item in metricMaxima)
            {
                Debug.Log("maximum of " + item.Key + ": " + item.Value + "\n");
            }
        }
    }
}
