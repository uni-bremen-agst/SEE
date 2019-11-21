using UnityEngine;
using SEE.DataModel;
using System.Collections.Generic;

namespace SEE.Layout
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
        /// <param name="graph">the graph whose node metrics are to be scaled</param>
        /// <param name="minimalLength">the mininmal value a node length can have</param>
        /// <param name="maximalLength">the maximal value a node length can have</param>
        /// <param name="metrics">node metrics for scaling</param>
        public ZScoreScale(Graph graph, float minimalLength, float maximalLength, IList<string> metrics)
        : base(metrics, minimalLength, maximalLength)
        {
            Determine_Statistics(graph);
        }

        /// <summary>
        /// The average metric should be mapped onto this length of a building.
        /// </summary>
        public float standard_length = 1.0f; // FIXME: Re-set to 0.5f;

        // The statistics gathered for the node metrics.
        private Dictionary<string, Statistics> statistics;

        /// <summary>
        /// Initial values 0.0 for all metrics.
        /// </summary>
        /// <param name="metrics"></param>
        /// <returns>map with value 0.0 for each metric (key)</returns>
        protected Dictionary<string, float> Initial(IList<string> metrics)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            foreach(string metric in metrics)
            {
                result.Add(metric, 0.0f);
            }
            return result;
        }

        /// <summary>
        /// Determines mean and standard deviation for each metric.
        /// </summary>
        /// <param name="graph">graph whose nodes are to be considered</param>
        private void Determine_Statistics(Graph graph)
        {
            Dictionary<string, float> sum = Initial(metrics);
            Dictionary<string, float> count = Initial(metrics);

            // Count the number of metric values and sum them up for each metric.
            foreach (Node node in graph.Nodes())
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
            // Initialize statistics
            statistics = new Dictionary<string, Statistics>();
            foreach (string metric in metrics)
            {
                statistics.Add(metric, new Statistics(0.0f, 0.0f));
            }

            // Calculate the mean value of each metric
            foreach (string metric in metrics)
            {
                float c = count[metric];
                statistics[metric].mean = c > 0 ? sum[metric] / c : 0.0f;
            }

            // Calculate sum((x_i - mean)^2) over all i in [1..n]
            foreach (Node node in graph.Nodes())
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
                return "mean=" + mean + ", sd=" + standard_deviation;
            }
        }

        /// <summary>
        /// Dumps the statistics for debugging.
        /// </summary>
        private void DumpStatistics()
        {
            foreach (string metric in metrics)
            {
                Debug.LogFormat("statistics of metric {0}: {1}\n", metric, statistics[metric].ToString());
            }      
        }

        /// <summary>
        /// Yields a z-score normalized value of the given node metric.
        /// The minimal value is specified by minimalLength. There is no maximal value,
        /// but generally values greater than 30 are unlikely.
        /// </summary>
        /// <param name="node">node for which to determine the normalized value</param>
        /// <param name="metric">name of the node metric</param>
        /// <returns>normalized value of node metric</returns>
        public override float GetNormalizedValue(Node node, string metric)
        {
            // We normalize x by z-score(x), which is defined as (x - mean)/sd where sd is
            // the standard deviation. The z-score can be viewed as a linear function
            // 1/sd * x - mean/sd where z-score(mean) = 0. We want to map the average
            // value onto the standard size of a building. That is why we add 1 to the
            // factor by which we multiply the standard length. Thus, if the metric value
            // is the means, the factor is 1.0.
            {
                float result = minimalLength;

                if (node.TryGetNumeric(metric, out float value))
                {
                    float sd = statistics[metric].standard_deviation;
                    if (sd == 0.0f)
                    {
                        result = minimalLength;
                    }
                    else
                    {
                        result = ((value - statistics[metric].mean) / sd + 1.0f) * standard_length;
                        if (result < minimalLength)
                        {
                            result = minimalLength;
                        }
                    }
                }
                return WithinLimits(result);
            }
        }
    }
}
