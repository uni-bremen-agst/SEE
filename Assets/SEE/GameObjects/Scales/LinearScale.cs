using System.Collections.Generic;
using SEE.DataModel.DG;

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
        /// <param name="metrics">node metrics for scaling</param>
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        public LinearScale(IEnumerable<Graph> graphs, IList<string> metrics, bool leavesOnly)
            : base(graphs, metrics, leavesOnly)
        {
        }

        /// <summary>
        /// Yields the the normalized value of <paramref name="value"/>.
        ///
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric. The assumption is that metric values are non-negative.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="value">value which shall be normalized</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValue(string metric, float value)
        {
            metricMaxima.TryGetValue(metric, out float max);
            if (max <= 0.0f || value <= 0.0f)
            {
                return 0.0f;
            }
            else
            {
                return value / max;
            }
        }

        /// <summary>
        /// Yields the the normalized value of the given node <paramref name="metric"/>
        /// of <paramref name="node"/>. If the <paramref name="node"/> does not have
        /// this metric, 0 is returned.
        ///
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric within the <paramref name="node"/>'s level.
        /// The assumption is that metric values are non-negative. If a node
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
                return 0;
            }
        }

        /// <summary>
        /// Yields the the normalized value of <paramref name="value"/> within the given
        /// node nesting <paramref name="level"/> clamped into the range [minimalLength, maximalLength].
        ///
        /// The normalization is done by dividing the value by the maximal value of
        /// the metric. The assumption is that metric values are non-negative.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="value">value which shall be normalized</param>
        /// <param name="level">node level within which the normalization shall take place</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValueForLevel(string metric, float value, int level)
        {
            float max = metricLevelMaxima[level][metric];
            if (max <= 0.0f || value <= 0.0f)
            {
               return 0.0f;
            }
            else
            {
                return value / max;
            }
        }
    }
}
