using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    internal class DonutDecorator
    {
        public DonutDecorator(NodeFactory nodeFactory, IScale scaler, string innerMetric, string[] metrics)
        {
            this.nodeFactory = nodeFactory;
            this.donutFactory = new DonutFactory(innerMetric, metrics);
            this.metrics = metrics;
            this.innerMetric = innerMetric;
            this.scaler = scaler;
        }

        /// <summary>
        /// Creates sprites for software-erosion indicators for all given game nodes.
        /// </summary>
        /// <param name="gameNodes">list of game nodes for which to create erosion visualizations</param>
        public void Add(ICollection<GameObject> gameNodes)
        {
            foreach (GameObject node in gameNodes)
            {
                Add(node);
            }
        }

        /// <summary>
        /// This number multiplied by the radius yields the radius of the inner donut chart.
        /// </summary>
        private const float radiusFraction = 0.2f;

        private const float innerScale = 0.95f;

        private readonly NodeFactory nodeFactory;

        private readonly DonutFactory donutFactory;

        private readonly string[] metrics;

        private readonly string innerMetric;

        private readonly IScale scaler;

        private void Add(GameObject gameNode)
        {
            Vector3 extent = nodeFactory.GetSize(gameNode) / 2.0f;
            // We want the circle to fit into gameNode, that is why we choose
            // the shorter value of the x and z co-ordinates. If the object
            // is a circle, then both are alike anyway.
            float radius = Mathf.Min(extent.x, extent.z);

            float[] values = new float[metrics.Length];

            Node node = gameNode.GetComponent<NodeRef>().node;

            for (int i = 0; i < metrics.Length; i++)
            {
                if (node.TryGetNumeric(metrics[i], out float value))
                {
                    values[i] = scaler.GetNormalizedValue(metrics[i], value);
                }
                else
                {
                    Debug.LogWarningFormat("no metric value {0} for node {1}.\n", metrics[i], node.LinkName);
                    values[i] = 0.0f;
                }
            }

            // The inner metric must be in the range [0, 1].
            float innerMetricValue = scaler.GetNormalizedValue(innerMetric, node)
                                     / scaler.GetNormalizedMaximum(innerMetric);
            GameObject donut;
            if (true)
            {
                donut = donutFactory.DonutChart(nodeFactory.GetCenterPosition(gameNode), radius, innerMetricValue, values, innerScale);
            }
            else
            {
                // FIXME: Remove this code.
                donut = donutFactory.DonutChart(nodeFactory.GetCenterPosition(gameNode), radiusFraction * radius, innerMetricValue, values);
            }
            
            donut.name = "Donut " + gameNode.name;
        }
    }
}