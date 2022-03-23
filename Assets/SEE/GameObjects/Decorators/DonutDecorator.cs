using System.Collections.Generic;
using SEE.DataModel.DG;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Allows one to decorate game objects with Donut charts. The inner circle and the outer circle
    /// segments depict certain metric values.
    /// </summary>
    internal class DonutDecorator
    {
        /// <summary>
        /// Constructor to create Donut chart decorations.
        /// </summary>
        /// <param name="scaler">for scaling the metrics</param>
        /// <param name="innerMetric">the metric to be visualized by the inner circle</param>
        /// <param name="metrics">the metrics to be put on the outer circle segments</param>
        public DonutDecorator(IScale scaler, string innerMetric, string[] metrics)
        {
            donutFactory = new DonutFactory(innerMetric, metrics);
            this.metrics = metrics;
            this.innerMetric = innerMetric;
            this.scaler = scaler;
        }

        /// <summary>
        /// Attaches Donut charts to all <paramref name="gameNodes"/> as children.
        /// </summary>
        /// <param name="gameNodes">list of game nodes for which to create Donut chart visualizations</param>
        public void Add(IEnumerable<GameObject> gameNodes)
        {
            foreach (GameObject node in gameNodes)
            {
                Add(node);
            }
        }

        /// <summary>
        /// Defines the fraction of the radius of the inner circle w.r.t. radius.
        /// </summary>
        private const float fractionOfInnerCircle = 0.95f;

        /// <summary>
        /// The factory that created the game objects to be decorated.
        /// </summary>
        private readonly DonutFactory donutFactory;

        /// <summary>
        /// The name of the metrics to be put onto the outer circle segments.
        /// </summary>
        private readonly string[] metrics;

        /// <summary>
        /// The name of the metric to be put onto the inner circle.
        /// </summary>
        private readonly string innerMetric;

        /// <summary>
        /// To scale the metrics.
        /// </summary>
        private readonly IScale scaler;

        /// <summary>
        /// Creates and attaches circle segments and inner circle to the given game node
        /// as children.
        /// </summary>
        /// <param name="gameNode">parent of the circle segments and inner circle</param>
        private void Add(GameObject gameNode)
        {
            float[] values = new float[metrics.Length];

            Node node = gameNode.GetComponent<NodeRef>().Value;

            for (int i = 0; i < metrics.Length; i++)
            {
                if (node.TryGetNumeric(metrics[i], out float value))
                {
                    values[i] = scaler.GetNormalizedValue(metrics[i], value);
                }
                else
                {
                    Debug.LogWarning($"No metric value { metrics[i]} for node {node.ID}.\n");
                    values[i] = 0.0f;
                }
            }

            // The inner metric must be in the range [0, 1].
            float innerMetricValue = scaler.GetNormalizedValue(innerMetric, node)
                                     / scaler.GetNormalizedMaximum(innerMetric);

            donutFactory.AttachDonutChart(gameNode, innerMetricValue, values, fractionOfInnerCircle);
        }
    }
}