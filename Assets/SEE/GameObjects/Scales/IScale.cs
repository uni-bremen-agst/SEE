using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        protected IScale(IEnumerable<Graph> graphs, IList<string> metrics, bool leavesOnly)
        {
            this.metrics = metrics;
            this.leavesOnly = leavesOnly;
            metricMaxima = new Dictionary<string, float>();
            metricLevelMaxima = new Dictionary<int, Dictionary<string, float>>();
            DetermineMetricMaxima(graphs);
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
        /// The maximal values of all metrics on each given level, as a map from level -> metric-name -> maximal value.
        /// </summary>
        protected readonly Dictionary<int, Dictionary<string, float>> metricLevelMaxima;

        /// <summary>
        /// If true, the normalization is done only for leaf nodes.
        /// </summary>
        private readonly bool leavesOnly;

        /// <summary>
        /// Yields a normalized value of the value of given node <paramref name="metric"/>
        /// of <paramref name="node"/>. The type of normalization is determined by concrete
        /// subclasses. If the node does not have this <paramref name="metric"/>, 0 will
        /// be returned.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node for which to determine the normalized value</param>
        /// <returns>normalized value of node metric</returns>
        public float GetNormalizedValue(string metric, Node node)
        {
            if (node.TryGetNumeric(metric, out float value))
            {
                return GetNormalizedValue(metric, value);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Yields a normalized value of the given node metric. The type of normalization
        /// is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">metric name</param>
        /// <param name="value">value to be normalized</param>
        /// <returns>normalized value</returns>
        public abstract float GetNormalizedValue(string metric, float value);

        /// <summary>
        /// Yields a normalized value of the given node metric relative to the level of the node.
        /// The type of normalization is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node for which to determine the normalized value relative to its level</param>
        /// <returns>normalized value of node metric relative to its level</returns>
        public abstract float GetNormalizedValueForLevel(string metric, Node node);

        /// <summary>
        /// Yields a normalized value of the given node metric relative to the
        /// given <paramref name="level"/>.
        /// The type of normalization is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">metric name</param>
        /// <param name="value">value to be normalized</param>
        /// <param name="level">node level to which the value shall be normalized</param>
        /// <returns>normalized value relative to given node <paramref name="level"/></returns>
        public abstract float GetNormalizedValueForLevel(string metric, float value, int level);

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
                Debug.LogError($"Attempt to retrieve the normalized maximum of metric {metric} that is not known.\n");
                Debug.Log("The available normalized metric maxima are as follows:\n");
                DumpMetricMaxima(metricMaxima);
                throw new Exception("A metric named " + metric + " does not exist.");
            }
        }

        /// <summary>
        /// Yields the normalized value of the maximum of the given metric within the given <paramref name="level"/>.
        /// </summary>
        /// <param name="metric">metric for which to return the normalized maximum</param>
        /// <param name="level">level from which to get the maximum</param>
        /// <returns>normalized maximum</returns>
        public float GetNormalizedMaximumForLevel(string metric, int level)
        {
            if (metricLevelMaxima.TryGetValue(level, out Dictionary<string, float> dictionary)
                && dictionary.TryGetValue(metric, out float value))
            {
                return GetNormalizedValueForLevel(metric, value, level);
            }
            else
            {
                Debug.LogError($"Attempt to retrieve the normalized maximum of metric {metric} in level {level} "
                               + $"that is not known.\n");
                Debug.Log("The available normalized metric maxima are as follows:\n");
                DumpMetricMaxima(metricMaxima);
                throw new Exception($"A metric named {metric} does not exist in level {level}.");
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
        internal float GetRelativeNormalizedValue(string metric, Node node)
        {
            float maximum = GetNormalizedMaximumForLevel(metric, node.Level);
            return maximum == 0 ? 0 : GetNormalizedValueForLevel(metric, node) / maximum;
        }

        /// <summary>
        /// Returns the normalization value of given node <paramref name="metric"/> of
        /// <paramref name="node"/> set in relation to the maximum value of the
        /// normalized <paramref name="metric"/> within the node's level. Hence, the result is in [0,1].
        /// If the maximum normalized value of <paramref name="metric"/> is 0, 0
        /// is returned.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node whose metric is to be queried</param>
        internal float GetRelativeNormalizedValueInLevel(string metric, Node node)
        {
            float maximum = GetNormalizedMaximum(metric);
            return maximum == 0 ? 0 : GetNormalizedValue(metric, node) / maximum;
        }

        /// <summary>
        /// Returns the maximal values of the node metrics in <see cref="metrics"/>.
        /// </summary>
        /// <param name="graphs">the set of graphs for which to determine the node metric maxima</param>
        private void DetermineMetricMaxima(IEnumerable<Graph> graphs)
        {
            metricMaxima.Clear();
            metricLevelMaxima.Clear();

            // Set default maxima for metricMaxima
            foreach (string metric in metrics)
            {
                metricMaxima[metric] = 0.0f;
            }

            foreach (Graph graph in graphs)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (!metricLevelMaxima.ContainsKey(node.Level))
                    {
                        metricLevelMaxima[node.Level] = new Dictionary<string, float>();
                    }

                    if (!leavesOnly || node.IsLeaf())
                    {
                        foreach (string metric in metrics)
                        {
                            if (node.TryGetNumeric(metric, out float value))
                            {
                                if (value > metricMaxima[metric])
                                {
                                    metricMaxima[metric] = value;
                                }

                                if (!metricLevelMaxima[node.Level].ContainsKey(metric) ||
                                    value > metricLevelMaxima[node.Level][metric])
                                {
                                    metricLevelMaxima[node.Level][metric] = value;
                                }
                            }
                        }
                    }
                }
            }

            // Set default maxima for metricLevelMaxima (for each level and each metric where it no maximum has been set)
            foreach (int level in Enumerable.Range(0, (metricLevelMaxima.Keys.Any() ? metricLevelMaxima.Keys.Max() : 0) + 1))
            {
                if (!metricLevelMaxima.ContainsKey(level))
                {
                    metricLevelMaxima[level] = new Dictionary<string, float>();
                }

                foreach (string metric in metrics)
                {
                    if (!metricLevelMaxima[level].ContainsKey(metric))
                    {
                        metricLevelMaxima[level][metric] = 0.0f;
                    }
                }
            }
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