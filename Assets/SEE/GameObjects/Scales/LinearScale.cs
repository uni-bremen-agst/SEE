using System.Collections.Generic;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Provides x, y, z lengths of a node based on a linear interpolation
    /// of the node's metrics.
    /// </summary>
    public class LinearScale : IScale
    {
        /// <summary>
        /// Constructor for linear-interpolation based scaling of node metrics.
        /// The values are guaranteed to be in the range of [minimalLength, maximalLength].
        /// </summary>
        /// <param name="graphs">the set of graph whose node metrics are to be scaled</param>
        /// <param name="minimalLength">the minimal value a node length can have</param>
        /// <param name="maximalLength">the maximal value a node length can have</param>
        /// <param name="metrics">node metrics for scaling</param>
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        public LinearScale(IEnumerable<Graph> graphs, float minimalLength, float maximalLength, IList<string> metrics,
                           bool leavesOnly) : base(graphs, metrics, minimalLength, maximalLength, leavesOnly)
        {
        }

        /// <summary>
        /// Yields a linear interpolation of the normalized value of the given node metric.
        /// The range of the linear interpolation is set by [minimalLength, maximalLength].
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric. The assumption is that metric values are non-negative. If a node
        /// does not have the metric attribute, minimalLength will be returned.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node for which to determine the normalized value</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValue(string metric, Node node)
        {
            if (node.TryGetNumeric(metric, out float value))
            {
                return GetNormalizedValue(metric, value);
            }
            else
            {
                return minimalLength;
            }
        }

        /// <summary>
        /// Yields a linear interpolation of the normalized value of the given node metric.
        /// The range of the linear interpolation is set by [minimalLength, maximalLength].
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric. The assumption is that metric values are non-negative.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="value">value which shall be normalized</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValue(string metric, float value)
        {
            float result;

            float max = metricMaxima[metric];

            if (max <= 0.0f || value <= 0.0f)
            {
                result = 0.0f;
            }
            else
            {
                result = value / max;
            }
            return Mathf.Lerp(minimalLength, maximalLength, result);
        }

        /// <summary>
        /// Yields a linear interpolation of the normalized value of the given node metric within the node's level.
        /// The range of the linear interpolation is set by [minimalLength, maximalLength].
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric. The assumption is that metric values are non-negative. If a node
        /// does not have the metric attribute, minimalLength will be returned.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="node">node for which to determine the normalized value</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValueForLevel(string metric, Node node)
        {
            if (node.TryGetNumeric(metric, out float value))
            {
                return GetNormalizedValueForLevel(metric, value, node.Level);
            }
            else
            {
                return minimalLength;
            }
        }

        /// <summary>
        /// Yields a linear interpolation of the normalized value of the given node metric
        /// within the given <paramref name="level"/>.
        /// The range of the linear interpolation is set by [minimalLength, maximalLength].
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric. The assumption is that metric values are non-negative.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="value">value which shall be normalized</param>
        /// <param name="level">node level within which the normalization shall take place</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValueForLevel(string metric, float value, int level)
        {
            float result;

            float max = metricLevelMaxima[level][metric];

            if (max <= 0.0f || value <= 0.0f)
            {
                result = 0.0f;
            }
            else
            {
                result = value / max;
            }
            return Mathf.Lerp(minimalLength, maximalLength, result);
        }
    }
}
