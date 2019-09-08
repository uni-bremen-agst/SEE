using UnityEngine;
using SEE.DataModel;
using System;

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
    internal class ZScoreScale : IScale
    {
        /// <summary>
        /// Constructor for z-score based scaling of node metrics. 
        /// </summary>
        /// <param name="graph">the graph whose node metrics are to be scaled</param>
        /// <param name="minimalLength">the mininmal value a node length should have</param>
        /// <param name="widthMetric">metric for node width</param>
        /// <param name="heightMetric">metric for node height</param>
        /// <param name="breadthMetric">metric for node breadth</param>
        public ZScoreScale(Graph graph, float minimalLength,
                           string widthMetric, string heightMetric, string breadthMetric)
        : base(widthMetric, heightMetric, breadthMetric)
        {
            this.minimalLength = minimalLength;
            Determine_Statistics(graph);
        }

        /// <summary>
        /// The average metric should be mapped onto this length of a building.
        /// </summary>
        public float standard_length = 1.0f; // FIXME: Re-set to 0.5f;

        /// <summary>
        /// The minimal length of a building.
        /// </summary>
        private readonly float minimalLength;

        /// <summary>
        /// Determines mean and standard deviation for each metric.
        /// </summary>
        /// <param name="graph">graph whose nodes are to be considered</param>
        private void Determine_Statistics(Graph graph)
        {
            float width_sum = 0.0f;
            float height_sum = 0.0f;
            float breadth_sum = 0.0f;

            int width_count = 0;
            int height_count = 0;
            int breadth_count = 0;

            foreach (Node node in graph.Nodes())
            {
                { 
                    if (node.TryGetNumeric(widthMetric, out float value))
                    {
                        width_count++;
                        width_sum += value;
                    }
                }
                {
                    if (node.TryGetNumeric(heightMetric, out float value))
                    {
                        height_count++;
                        height_sum += value;
                    }
                }
                {
                    if (node.TryGetNumeric(breadthMetric, out float value))
                    {
                        breadth_count++;
                        breadth_sum += value;
                    }
                }
            }
           
            width_statistics.mean = (width_count > 0) ? width_sum / width_count : 0.0f;
            height_statistics.mean = (height_count > 0) ? height_sum / height_count : 0.0f;
            breadth_statistics.mean = (breadth_count > 0) ? breadth_sum / breadth_count : 0.0f;

            foreach (Node node in graph.Nodes())
            {
                {
                    if (node.TryGetNumeric(widthMetric, out float value))
                    {
                        float diff = value - width_statistics.mean;
                        width_statistics.standard_deviation += diff * diff;
                    }
                }
                {
                    if (node.TryGetNumeric(heightMetric, out float value))
                    {
                        float diff = value - height_statistics.mean;
                        height_statistics.standard_deviation += diff * diff;
                    }
                }
                {
                    if (node.TryGetNumeric(breadthMetric, out float value))
                    {
                        float diff = value - breadth_statistics.mean;
                        breadth_statistics.standard_deviation += diff * diff;
                    }
                }
            }

            width_statistics.standard_deviation
                = (width_count > 0) ? Mathf.Sqrt(width_statistics.standard_deviation / width_count) : 0.0f;
            height_statistics.standard_deviation
                = (height_count > 0) ? Mathf.Sqrt(height_statistics.standard_deviation / height_count) : 0.0f;
            breadth_statistics.standard_deviation
                = (breadth_count > 0) ? Mathf.Sqrt(breadth_statistics.standard_deviation / breadth_count) : 0.0f;

            // DumpStatistics();
        }

        private struct Statistics
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

        private Statistics width_statistics = new Statistics(0.0f, 0.0f);
        private Statistics height_statistics = new Statistics(0.0f, 0.0f);
        private Statistics breadth_statistics = new Statistics(0.0f, 0.0f);

        private void DumpStatistics()
        {
            Debug.LogFormat("width statistics {0}\n", width_statistics.ToString());
            Debug.LogFormat("height statistics {0}\n", height_statistics.ToString());
            Debug.LogFormat("breadth statistics {0}\n", breadth_statistics.ToString());
        }

        /// <summary>
        /// Yields a vector where each element (x, y, z) is a z-score normalized
        /// value of the metrics that determine the width, height, and breadth of the given node.
        /// The minimal value is specified by minimalLength. There is no maximal value,
        /// but generally values greater than 30 are unlikely.
        /// </summary>
        /// <param name="node">node for which to determine the x, y, z lengths</param>
        /// <returns>x, y, z lengths of node</returns>
        public override Vector3 Lengths(Node node)
        {
            return new Vector3(GetLength(node, width_statistics, widthMetric),
                               GetLength(node, height_statistics, heightMetric),
                               GetLength(node, breadth_statistics, breadthMetric));
        }

        private float GetLength(Node node, Statistics statistics, string metric)
        {
            // We normalize x by z-score(x), which is defined as (x - mean)/sd where sd is
            // the standard deviation. The z-score be viewed as a linear function
            // 1/sd * x - mean/sd where z-score(mean) = 0. We want to map the average
            // value onto the standard size of a building. That is why we add 1 to the
            // factor by which we multiply the standard length. Thus, if the metric value
            // is the means, the factor is 1.0.
            {
                float result = minimalLength;

                if (node.TryGetNumeric(metric, out float value))
                {
                    result = ((value - statistics.mean) / statistics.standard_deviation + 1.0f) * standard_length;
                    if (result < minimalLength)
                    {
                        result = minimalLength;
                    }
                }
                return result;
            }
        }
    }
}
