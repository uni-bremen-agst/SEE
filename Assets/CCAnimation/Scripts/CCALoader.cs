using SEE;
using SEE.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// TODO: doc
/// </summary>
public class CCALoader
{

    public readonly GraphSettings settings = new GraphSettings();
    public readonly Dictionary<string, Graph> graphs = new Dictionary<string, Graph>();
    public readonly List<string> graphOrder = new List<string>();

    public void Init()
    {
        string projectPath = Application.dataPath.Replace('/', '\\') + '\\';
        settings.pathPrefix = projectPath;
        settings.ShowDonuts = false;
        AddAllRevisions();
    }

    private string GetAnimatedPath()
    {
        return settings.pathPrefix + "..\\Data\\GXL\\animation-clones\\";
    }

    /// <summary>
    /// TODO: doc 
    /// </summary>
    private void AddAllRevisions()
    {
        SEE.Performance p = SEE.Performance.Begin("loading animated graph data from " + GetAnimatedPath());
        graphs.Clear();
        graphOrder.Clear();
        var settingsOldGxlPath = settings.gxlPath;
        var newGraphName = Directory
            .GetFiles(GetAnimatedPath(), "*.gxl", SearchOption.TopDirectoryOnly)
            .NumericalSort();
        graphOrder.AddRange(newGraphName);
        foreach (string gxlPath in graphOrder)
        {
            settings.gxlPath = gxlPath.Replace(settings.pathPrefix, "");
            Add(settings);
        }
        settings.gxlPath = settingsOldGxlPath;
        p.End();
        Debug.Log("Number of graphs loaded: " + graphOrder.Count + "\n");
    }

    /// <summary>
    /// TODO: doc 
    /// </summary>
    private Graph Add(GraphSettings settings)
    {
        if (string.IsNullOrEmpty(settings.GXLPath()))
        {
            Debug.LogError("No graph path given.\n");
            return null;
        }
        if (graphs.ContainsKey(settings.GXLPath()))
        {
            Debug.LogError("graph " + settings.GXLPath() + " is already loaded.");
            return null;
        }
        Graph graph = Load(settings);
        if (graph == null)
        {
            Debug.LogError("graph " + settings.GXLPath() + " could not be loaded.");
        }
        else
        {
            graphs.Add(settings.GXLPath(), graph);
        }
        return graph;
    }

    /// <summary>
    /// TODO: doc 
    /// </summary>
    private Graph Load(GraphSettings settings)
    {
        // GraphCreator graphCreator = new GraphCreator(settings.GXLPath(), settings.HierarchicalEdges, new SEELogger());
        GraphReader graphCreator = new GraphReader(settings.GXLPath(), settings.HierarchicalEdges, new SEELogger());
        if (string.IsNullOrEmpty(settings.GXLPath()))
        {
            Debug.LogError("Empty graph path.\n");
            return null;
        }
        else
        {
            graphCreator.Load();
            Graph graph = graphCreator.GetGraph();

            // TODO generate random test data for CloneRate
            graph.Traverse(leafNode => leafNode.SetFloat("Metric.Clone_Rate", leafNode.GetInt("Metric.LOC")));

            return graph;
        }
    }
}
