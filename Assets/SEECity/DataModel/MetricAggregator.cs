using System;
using System.Collections.Generic;

namespace SEE.DataModel
{
    /// <summary>
    /// Allows one to aggregate or derive metrics for nodes.
    /// </summary>
    public class MetricAggregator
    {        
        /// <summary>
        /// Sum of left and right used as a function delegate.
        /// </summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>left + right</returns>
        private static float Sum(float left, float right)
        {
            return left + right;
        }

        /// <summary>
        /// Aggregates the metrics along the node decomposition tree in the graph
        /// as a sum. More precisely, for every node N in the graph and each metric M
        /// in metrics (recursively computed bottom up): 
        ///   1) if N has metric M set already, nothing happens
        ///   2) otherwise, the metric M for node N is set as the sum of this 
        ///      metric M for every direct child of N (if a child does not have this
        ///      metric set it will be skipped)
        /// </summary>
        /// <param name="graph">graph whose metric nodes are to be aggregated bottom up</param>
        /// <param name="metrics">the metrics to be aggregated</param>
        public static void AggregateSum(Graph graph, string[] metrics)
        {
            Aggregate(graph, metrics, Sum);
        }

        /// <summary>
        /// Aggregates the metrics along the node decomposition tree in the graph
        /// by function func. More precisely, for every node N in the graph and each metric M
        /// in metrics (recursively computed bottom up): 
        ///   1) if N has metric M set already, nothing happens
        ///   2) otherwise, the metric M for node N is set as an aggregation func of this 
        ///      metric M for every direct child of N (if a child does not have this
        ///      metric set it will be skipped)
        /// </summary>
        /// <param name="graph">graph whose metric nodes are to be aggregated bottom up</param>
        /// <param name="metrics">the metrics to be aggregated</param>
        /// <param name="func">function to be used for the aggregation</param>
        private static void Aggregate(Graph graph, string[] metrics, Func<float, float, float> func)
        {
            IList<Node> list = graph.GetRoots();

            foreach (string metric in metrics)
            {
                foreach (Node root in list)
                {
                    Aggregate(root, metric, func);
                }
            }

        }

        private static void Aggregate(Node node, string metric, Func<float, float, float> func)
        {
            // depth-first traversal to calculate all metrics for all children first
            foreach (Node child in node.Children())
            {
                Aggregate(child, metric, func);
            }

            // Now every child should have this attribute set.

            // We set the attribute only if node does not have this attribute already.
            if (! node.TryGetNumeric(metric, out float unused))
            {
                float nodeValue = float.NaN;
                foreach (Node child in node.Children())
                {
                    if (child.TryGetNumeric(metric, out float childValue))
                    {
                        // Note: Comparisons to NaN as nodeValue == float.NaN always return false, 
                        // no matter what the value of the float is. We must use float.IsNaN(nodeValue).
                        if (float.IsNaN(nodeValue))
                        {
                            nodeValue = childValue;
                        }
                        else
                        {
                            nodeValue = func(nodeValue, childValue);
                        }
                    }
                }
                if (nodeValue != float.NaN)
                {
                    node.SetFloat(metric, nodeValue);
                }
            }
        }

        /// <summary>
        /// Sums up all values of the given metrics of every node and sets this
        /// value as a new metric named newMetric to the node. If the node has
        /// this attribute already, it will not be set. If any of the accumulated
        /// metrics do not exist for a node, they will be skipped. If the node
        /// has none of the metrics, attribute newMetric will not be added to the 
        /// node.
        /// </summary>
        /// <param name="graph">graph whose nodes are to be treated</param>
        /// <param name="metrics">the metrics to be accumulated for each node</param>
        /// <param name="newMetric">the name of the new metric to which the accumulated value is assigned</param>
        /// <param name="Skip_Single_Root">if true, the metric value will be set to 0 for a single root node</param>
        public static void DeriveSum(Graph graph, string[] metrics, string newMetric, bool Skip_Single_Root = false)
        {
            Derive(graph, metrics, newMetric, Sum, Skip_Single_Root);
        }

        /// <summary>
        /// Accumulates all values of the given metrics of every node and sets this
        /// value as a new metric named newMetric to the node.
        /// </summary>
        /// <param name="graph">graph whose nodes are to be treated</param>
        /// <param name="metrics">the metrics to be accumulated for each node</param>
        /// <param name="newMetric">the name of the new metric to which the accumulated value is assigned</param>
        /// <param name="func">function used for the accumulation</param>
        /// <param name="Skip_Single_Root">if true, the metric value will be set to 0 for a single root node</param>
        private static void Derive(Graph graph, string[] metrics, string newMetric, Func<float, float, float> func, bool Skip_Single_Root)
        {
            IList<Node> list = graph.GetRoots();
            foreach (Node root in list)
            {
                Derive(root, metrics, newMetric, func);
                if (list.Count == 1 && Skip_Single_Root)
                {
                    root.SetFloat(newMetric, 0.0f);
                }
            }
        }

        private static void Derive(Node node, string[] metrics, string newMetric, Func<float, float, float> func)
        {
            // depth-first traversal to calculate all metrics for all children first
            foreach (Node child in node.Children())
            {
                Derive(child, metrics, newMetric, func);
            }

            // We set the attribute only if node does not have newMetric already.
            if (!node.TryGetNumeric(newMetric, out float unused))
            {
                float nodeValue = float.NaN;
                foreach (string metric in metrics)
                {
                    if (node.TryGetNumeric(metric, out float value))
                    {
                        // Note: Comparisons to NaN as nodeValue == float.NaN always return false, 
                        // no matter what the value of the float is. We must use float.IsNaN(nodeValue).
                        if (float.IsNaN(nodeValue))
                        {
                            nodeValue = value;
                        }
                        else
                        {
                            nodeValue = func(nodeValue, value);
                        }
                    }
                }

                if (nodeValue != float.NaN)
                {
                    node.SetFloat(newMetric, nodeValue);
                }
            }
        }
    }
}
