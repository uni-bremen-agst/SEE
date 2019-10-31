using SEE;
using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Allows loading of multiple gxl files from a directory and stores them.
/// !!! At the moment data for Metric.Clone_Rate is copied from Metric.LOC for better visualisation during test !!!
/// </summary>
public class CCALoader
{
    /// <summary>
    /// Contains all loaded graphs after calling LoadGraphData().
    /// </summary>
    public readonly List<Graph> graphs = new List<Graph>();

    /// <summary>
    /// Loads all gxl files from GraphSettings.AnimatedPath() sorted by numbers in the file names.
    /// !!! At the moment data for Metric.Clone_Rate is copied from Metric.LOC for better visualisation during test !!!
    /// </summary>
    /// <param name="graphSettings">The GraphSettings defining the location of gxl files.</param>
    public void LoadGraphData(GraphSettings graphSettings)
    {
        graphSettings.AssertNotNull("graphSettings");
        graphs.Clear();
        AddAllRevisions(graphSettings);
    }

    /// <summary>
    /// Internal function that loads all gxl files from the path set in GraphSettings and
    /// and saves all loaded graph data.
    /// </summary>
    /// <param name="graphSettings">The GraphSettings defining the location of gxl files.</param>
    private void AddAllRevisions(GraphSettings graphSettings)
    {
        SEE.Performance p = SEE.Performance.Begin("loading animated graph data from " + graphSettings.GetAnimatedPath());

        // clear possible old data
        graphs.Clear();

        // get all gxl files sorted by numbers in their name
        var sortedGraphNames = Directory
            .GetFiles(graphSettings.GetAnimatedPath(), "*.gxl", SearchOption.TopDirectoryOnly)
            .Where(e => !string.IsNullOrEmpty(e))
            .Distinct()
            .NumericalSort();

        // for all found gxl files load and save the graph data
        foreach (string gxlPath in sortedGraphNames)
        {
            // load graph
            GraphReader graphCreator = new GraphReader(gxlPath, graphSettings.HierarchicalEdges, new SEELogger());
            graphCreator.Load();
            Graph graph = graphCreator.GetGraph();

            // TODO flo: remove for real data
            graph.Traverse(leafNode => leafNode.SetFloat("Metric.Clone_Rate", leafNode.GetInt("Metric.LOC")));

            // if graph was loaded put in graphs
            if (graph == null)
            {
                Debug.LogError("graph " + gxlPath + " could not be loaded.");
            }
            else
            {
                graphs.Add(graph);
            }
        }

        p.End();
        Debug.Log("Number of graphs loaded: " + graphs.Count + "\n");
    }
}
