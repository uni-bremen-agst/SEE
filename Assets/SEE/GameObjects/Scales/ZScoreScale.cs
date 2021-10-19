using System.Collections.Generic;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Scaling based on z-score. The z-score of a value v that is an element of
    /// a list of values V is defined as follows: (v-mean(V))/sd(V) where
    /// mean(V) is the mean value and sd(V) is the standard deviation of all values
    /// in V. The range of possible values of z-score is infinite, but generally
    /// in the range of [-30, 30]. It is obviously exactly 0 for v = mean(V).
    /// The z-score normalizes values by how far they deviate from the mean in terms
    /// of standard deviation units. It allows one to compare different metrics that
    /// may have very different ranges of values.
    /// </summary>
    public class ZScoreScale : IScale
    {
        /// <summary>
        /// Constructor for z-score based scaling of node metrics.
        /// </summary>
        /// <param name="graphs">the set of graphs whose node metrics are to be scaled</param>
        /// <param name="metrics">node metrics for scaling</param>
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        public ZScoreScale(ICollection<Graph> graphs, IList<string> metrics,  bool leavesOnly)
            : base(graphs, metrics, leavesOnly)
        {
            DetermineStatistics(graphs, leavesOnly);
        }

        /// <summary>
        /// The statistics gathered for the node metrics.
        /// </summary>
        private Dictionary<string, Statistics> statistics;

        /// <summary>
        /// Initializes values to 0.0 for all metrics.
        /// </summary>
        /// <param name="metrics">metrics which shall be initialized to 0</param>
        /// <returns>map with value 0.0 for each metric (key)</returns>
        protected static Dictionary<string, float> Initial(IEnumerable<string> metrics)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            foreach (string metric in metrics)
            {
                result[metric] = 0.0f;
            }
            return result;
        }

        /// <summary>
        /// Determines mean and standard deviation for each metric.
        /// </summary>
        /// <param name="graphs">set of graphs whose nodes are to be considered</param>
        /// <param name="leavesOnly">if true, only the leaf nodes are considered</param>
        private void DetermineStatistics(ICollection<Graph> graphs, bool leavesOnly)
        {
            Dictionary<string, float> sum = Initial(metrics);
            Dictionary<string, float> count = Initial(metrics);

            // Count the number of metric values and sum them up for each metric.
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
                                count[metric]++;
                                sum[metric] += value;
                            }
                        }
                    }
                }
            }
            // Initialize statistics
            statistics = new Dictionary<string, Statistics>();
            foreach (string metric in metrics)
            {
                statistics[metric] = new Statistics(0.0f, 0.0f);
            }

            // Calculate the mean value of each metric
            foreach (string metric in metrics)
            {
                float c = count[metric];
                statistics[metric].mean = c > 0 ? sum[metric] / c : 0.0f;
            }

            // Calculate sum((x_i - mean)^2) over all i in [1..n]
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
                                float diff = value - statistics[metric].mean;
                                statistics[metric].standard_deviation += diff * diff;
                            }
                        }
                    }
                }
            }

            // Calculate standard deviation sd = sqrt(var(X)) where var(X) = S/n
            // is the variance of X = {x_1, ..., x_n} and S = sum((x_i - mean)^2) over all i in [1..n]
            foreach (string metric in metrics)
            {
                float c = count[metric];
                statistics[metric].standard_deviation
                     = c > 0 ? Mathf.Sqrt(statistics[metric].standard_deviation / c) : 0.0f;
            }
            // DumpStatistics();
        }

        private class Statistics
        {
            public float mean;
            public float standard_deviation;

            public Statistics(float mean, float standard_deviation)
            {
                this.mean = mean;
                this.standard_deviation = standard_deviation;
            }
            public override string ToString()
            {
                return $"mean={mean}, sd={standard_deviation}";
            }
        }

        /// <summary>
        /// Dumps the statistics for debugging.
        /// </summary>
        private void DumpStatistics()
        {
            foreach (string metric in metrics)
            {
                Debug.Log($"statistics of metric {metric}: {statistics[metric]}\n");
            }
        }

        /// <summary>
        /// Yields a z-score normalized value of the given node metric value.
        /// </summary>
        /// <param name="metric">name of the node metric</param>
        /// <param name="value">value for which to determine the normalized value</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValue(string metric, float value)
        {
            // We normalize x by z-score(x), which is defined as (x - mean)/sd where sd is
            // the standard deviation. The z-score can be viewed as a linear function
            // 1/sd * x - mean/sd where z-score(mean) = 0.
            // Note: There is no maximal value for z-score, but larger values get increasingly
            // unlikely.
            float sd = statistics[metric].standard_deviation;
            if (sd == 0.0f)
            {
                return 0;
            }
            else
            {
                return (value - statistics[metric].mean) / sd;
            }
        }

        public override float GetNormalizedValueForLevel(string metric, Node node)
        {
            // FIXME: Implement normalization per level
            throw new System.NotImplementedException();
        }

        public override float GetNormalizedValueForLevel(string metric, float value, int level)
        {
            //FIXME: Implement normalization per level
            throw new System.NotImplementedException();
        }
    }
}
