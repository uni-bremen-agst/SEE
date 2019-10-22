using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TODO Flo: Doku
/// Provides x, y, z lengths of a node based on a linear interpolation
/// of the node's metrics.
/// </summary>
public class LinearMultiScale : IScale
{
    /// <summary>
    /// Constructor for linear-interpolation based scaling of node metrics.
    /// The values are guaranteed to be in the range of [minimalLength,
    /// maximalLength].
    /// </summary>
    /// <param name="graph">the graph whose node metrics are to be scaled</param>
    /// <param name="minimalLength">the mininmal value a node length can have</param>
    /// <param name="maximalLength">the maximal value a node length can have</param>
    /// <param name="metrics">node metrics for scaling</param>
    public LinearMultiScale(List<Graph> graphs, float minimalLength, float maximalLength, IList<string> metrics)
        : base(metrics, minimalLength, maximalLength)
    {
        metricMaxima = DetermineMetricMaxima(graphs, metrics);
    }

    /// <summary>
    /// The maximal values of all metrics as a map metric-name -> maximal value.
    /// </summary>
    private readonly Dictionary<string, float> metricMaxima;

    /// <summary>
    /// Yields a linear interpolation of the normalized value of the given node metric.
    /// The range of the linear interpolation is set by [minimalLength, maximalLength].
    /// The normalization is done by dividing the value by the maximal value of
    /// the metric. The assumption is that metric values are non-negative. If a node
    /// does not have metric attribute, minimalLength will be returned.
    /// </summary>
    /// <param name="node">node for which to determine the normalized value</param>
    /// <param name="metric">name of the node metric</param>
    /// <returns>normalized value of node metric</returns>
    public override float GetNormalizedValue(Node node, string metric)
    {
        return Mathf.Lerp(minimalLength, maximalLength, NormalizedMetric(metricMaxima, node, metric));
    }

    /// <summary>
    /// Returns a value in the range [0.0, 1.0] representing the relative value of the given
    /// metric in the metrics value range for the given node.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="metric"></param>
    /// <returns></returns>
    protected float NormalizedMetric(Dictionary<string, float> metricMaxima, Node node, string metric)
    {
        float max = metricMaxima[metric];

        if (max <= 0.0f)
        {
            return 0.0f;
        }
        else if (node.TryGetNumeric(metric, out float width))
        {
            if (width <= 0.0f)
            {
                return 0.0f;
            }
            else
            {
                return (float)width / max;
            }
        }
        else
        {
            return 0.0f;
        }
    }

    /// <summary>
    /// Returns the maximal values of the given node metrics.
    /// </summary>
    /// <param name="metrics">the metrics for which the maxima are to be gathered</param>
    /// <returns>metric maxima</returns>
    protected Dictionary<string, float> DetermineMetricMaxima(List<Graph> graphs, IList<string> metrics)
    {
        Dictionary<string, float> result = new Dictionary<string, float>();
        foreach (string metric in metrics)
        {
            result.Add(metric, 0.0f);
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
        foreach (var item in metricMaxima)
        {
            Debug.Log("maximum of " + item.Key + ": " + item.Value + "\n");
        }
    }
}
