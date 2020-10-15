﻿using SEE.DataModel.DG;
using System;
using System.Collections.Generic;
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
        public IScale(ICollection<Graph> graphs, IList<string> metrics, float minimalLength, float maximalLength)
        {
            this.metrics = metrics;
            this.minimalLength = minimalLength;
            this.maximalLength = maximalLength;
            metricMaxima = DetermineMetricMaxima(graphs, metrics);
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
        /// Returns the maximal values of the given node metrics.
        /// </summary>
        /// <param name="graphs">the set of graphs for which to determine the node metric maxima</param>
        /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
        /// <returns>metric maxima</returns>
        protected Dictionary<string, float> DetermineMetricMaxima(ICollection<Graph> graphs, IList<string> metrics)
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
            return result;
        }

        /// <summary>
        /// Dumps metricMaxima for debugging.
        /// </summary>
        protected void DumpMetricMaxima(Dictionary<string, float> metricMaxima)
        {
            foreach (KeyValuePair<string, float> item in metricMaxima)
            {
                Debug.Log("maximum of " + item.Key + ": " + item.Value + "\n");
            }
        }
    }
}
