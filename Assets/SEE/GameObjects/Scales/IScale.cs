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
        /// <param name="graphs">The set of graphs whose node metrics are to be scaled.</param>
        /// <param name="metrics">Node metrics for scaling.</param>
        /// <param name="leavesOnly">If true, only the leaf nodes are considered.</param>
        protected IScale(IEnumerable<Graph> graphs, ISet<string> metrics, bool leavesOnly)
        {
            this.Metrics = metrics;
            this.leavesOnly = leavesOnly;
            MetricMaxima = new Dictionary<string, float>();
            MetricLevelMaxima = new Dictionary<int, Dictionary<string, float>>();
            DetermineMetricMaxima(graphs);
        }

        /// <summary>
        /// The list of metrics to be scaled.
        /// </summary>
        protected readonly ISet<string> Metrics;

        /// <summary>
        /// The maximal values of all metrics as a map metric-name -> maximal value.
        /// </summary>
        protected readonly Dictionary<string, float> MetricMaxima;

        /// <summary>
        /// The maximal values of all metrics on each given level, as a map from level -> metric-name -> maximal value.
        /// </summary>
        protected readonly Dictionary<int, Dictionary<string, float>> MetricLevelMaxima;

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
        /// <param name="metric">Name of the node metric.</param>
        /// <param name="node">Node for which to determine the normalized value.</param>
        /// <returns>Normalized value of node metric.</returns>
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
        /// If <paramref name="metricName"/> can be parsed as a number, the parsed number is
        /// returned. If <paramref name="metricName"/> is the name of a metric, the corresponding
        /// normalized value for <paramref name="node"/> is returned if it exists; otherwise 0
        /// is returned.
        /// </summary>
        /// <param name="node">Node whose metric is to be returned.</param>
        /// <param name="metricName">The name of a node metric or a number.</param>
        /// <returns>The value of <paramref name="node"/>'s metric <paramref name="metricName"/>.</returns>
        public float GetMetricValue(Node node, string metricName)
        {
            if (Utils.FloatUtils.TryGetFloat(metricName, out float value))
            {
                return value;
            }
            else
            {
                return GetNormalizedValue(metricName, node);
            }
        }

        /// <summary>
        /// Yields a normalized value of the given node metric. The type of normalization
        /// is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">Metric name.</param>
        /// <param name="value">Value to be normalized.</param>
        /// <returns>Normalized value.</returns>
        public abstract float GetNormalizedValue(string metric, float value);

        /// <summary>
        /// Yields a normalized value of the given node metric relative to the level of the node.
        /// The type of normalization is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">Name of the node metric.</param>
        /// <param name="node">Node for which to determine the normalized value relative to its level.</param>
        /// <returns>Normalized value of node metric relative to its level.</returns>
        public abstract float GetNormalizedValueForLevel(string metric, Node node);

        /// <summary>
        /// Yields a normalized value of the given node metric relative to the
        /// given <paramref name="level"/>.
        /// The type of normalization is determined by concrete subclasses.
        /// </summary>
        /// <param name="metric">Metric name.</param>
        /// <param name="value">Value to be normalized.</param>
        /// <param name="level">Node level to which the value shall be normalized.</param>
        /// <returns>Normalized value relative to given node <paramref name="level"/>.</returns>
        public abstract float GetNormalizedValueForLevel(string metric, float value, int level);

        /// <summary>
        /// Yields the maximal value of the given <paramref name="metric"/> (not normalized).
        /// </summary>
        /// <param name="metric">Metric for which to return the maximum.</param>
        /// <returns>Maximum.</returns>
        public float GetMaximum(string metric)
        {
            if (MetricMaxima.TryGetValue(metric, out float max))
            {
                return max;
            }
            else
            {
                throw new Exception("A metric named " + metric + " does not exist.");
            }
        }

        /// <summary>
        /// Yields the normalized value of the maximum of the given metric.
        /// </summary>
        /// <param name="metric">Metric for which to return the normalized maximum.</param>
        /// <returns>Normalized maximum.</returns>
        public float GetNormalizedMaximum(string metric)
        {
            if (MetricMaxima.TryGetValue(metric, out float value))
            {
                return GetNormalizedValue(metric, value);
            }
            else
            {
                Debug.LogError($"Attempt to retrieve the normalized maximum of metric {metric} that is not known.\n");
                Debug.Log("The available normalized metric maxima are as follows:\n");
                DumpMetricMaxima(MetricMaxima);
                throw new Exception("A metric named " + metric + " does not exist.");
            }
        }

        /// <summary>
        /// Yields the normalized value of the maximum of the given metric within the given <paramref name="level"/>.
        /// </summary>
        /// <param name="metric">Metric for which to return the normalized maximum.</param>
        /// <param name="level">Level from which to get the maximum.</param>
        /// <returns>Normalized maximum.</returns>
        public float GetNormalizedMaximumForLevel(string metric, int level)
        {
            if (MetricLevelMaxima.TryGetValue(level, out Dictionary<string, float> dictionary)
                && dictionary.TryGetValue(metric, out float value))
            {
                return GetNormalizedValueForLevel(metric, value, level);
            }
            else
            {
                Debug.LogError($"Attempt to retrieve the normalized maximum of metric {metric} in level {level} "
                               + $"that is not known.\n");
                Debug.Log("The available normalized metric maxima are as follows:\n");
                DumpMetricMaxima(MetricMaxima);
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
        /// <param name="metric">Name of the node metric.</param>
        /// <param name="node">Node whose metric is to be queried.</param>
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
        /// <param name="metric">Name of the node metric.</param>
        /// <param name="node">Node whose metric is to be queried.</param>
        internal float GetRelativeNormalizedValueInLevel(string metric, Node node)
        {
            float maximum = GetNormalizedMaximum(metric);
            return maximum == 0 ? 0 : GetNormalizedValue(metric, node) / maximum;
        }

        /// <summary>
        /// Returns the maximal values of the node metrics in <see cref="Metrics"/>.
        /// </summary>
        /// <param name="graphs">The set of graphs for which to determine the node metric maxima.</param>
        private void DetermineMetricMaxima(IEnumerable<Graph> graphs)
        {
            MetricMaxima.Clear();
            MetricLevelMaxima.Clear();

            // Set default maxima for metricMaxima
            foreach (string metric in Metrics)
            {
                MetricMaxima[metric] = 0.0f;
            }

            foreach (Graph graph in graphs)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (!MetricLevelMaxima.ContainsKey(node.Level))
                    {
                        MetricLevelMaxima[node.Level] = new Dictionary<string, float>();
                    }

                    if (!leavesOnly || node.IsLeaf())
                    {
                        foreach (string metric in Metrics)
                        {
                            if (node.TryGetNumeric(metric, out float value))
                            {
                                if (value > MetricMaxima[metric])
                                {
                                    MetricMaxima[metric] = value;
                                }

                                if (!MetricLevelMaxima[node.Level].ContainsKey(metric) ||
                                    value > MetricLevelMaxima[node.Level][metric])
                                {
                                    MetricLevelMaxima[node.Level][metric] = value;
                                }
                            }
                        }
                    }
                }
            }

            // Set default maxima for metricLevelMaxima (for each level and each metric where it no maximum has been set)
            foreach (int level in Enumerable.Range(0, (MetricLevelMaxima.Keys.Any() ? MetricLevelMaxima.Keys.Max() : 0) + 1))
            {
                if (!MetricLevelMaxima.ContainsKey(level))
                {
                    MetricLevelMaxima[level] = new Dictionary<string, float>();
                }

                foreach (string metric in Metrics)
                {
                    if (!MetricLevelMaxima[level].ContainsKey(metric))
                    {
                        MetricLevelMaxima[level][metric] = 0.0f;
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

        /// <summary>
        /// Updates the <see cref="Graph.MetricLevel"/> key in <see cref="MetricMaxima"/>
        /// when the passed value is higher than the stored one.
        /// This is needed to enable dynamic city creation.
        /// </summary>
        /// <param name="value">The node metric level value.</param>
        public void UpdateMetricLevel(float value)
        {
            if (MetricMaxima.ContainsKey(Graph.MetricLevel)
                && MetricMaxima[Graph.MetricLevel] < value)
            {
                MetricMaxima[Graph.MetricLevel] = value;
            }
        }
    }
}
