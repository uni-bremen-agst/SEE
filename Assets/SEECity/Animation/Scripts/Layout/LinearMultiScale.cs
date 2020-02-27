//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.
using SEE.DataModel;
using SEE.Layout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides x, y, z lengths of a node based on a linear interpolation
/// of the node's metrics.
/// </summary>
public class LinearMultiScale : LinearScale
{
    /// <summary>
    /// Overrides the base variable to modify the values.
    /// </summary>
    private new readonly Dictionary<string, float> metricMaxima;

    /// <summary>
    /// Constructor for linear-interpolation based scaling of node metrics.
    /// The values are guaranteed to be in the range of [minimalLength,
    /// maximalLength].
    /// </summary>
    /// <param name="graphs">the graphs whose node metrics are to be scaled</param>
    /// <param name="minimalLength">the mininmal value a node length can have</param>
    /// <param name="maximalLength">the maximal value a node length can have</param>
    /// <param name="metrics">node metrics for scaling</param>
    public LinearMultiScale(List<Graph> graphs, float minimalLength, float maximalLength, IList<string> metrics)
        : base(graphs.FirstOrDefault(), minimalLength, maximalLength, metrics)
    {
        metricMaxima = DetermineMetricMaxima(graphs, metrics);
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
}