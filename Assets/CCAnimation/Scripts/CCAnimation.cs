using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// : doc
/// </summary>
public class CCAnimation : MonoBehaviour
{
    public Text revisionNumberText;

    private CCALoader graphLoader = new CCALoader();
    private GraphSettings editorSettings;
    private Dictionary<string, Graph> graphs;
    private List<string> graphOrder;
    private int openGraphIndex = 0;

    void Start()
    {
        graphLoader.loadGraph();
        editorSettings = graphLoader.settings;
        graphs = graphLoader.graphs;
        graphOrder = graphLoader.graphOrder;
        displayGraphIntermidate(openGraphIndex);
    }

    void Update()
    {

        if (Input.GetKeyDown("k"))
        {
            ShowPreviousGraph();
        }
        else if (Input.GetKeyDown("l"))
        {
            ShowNextGraph();
        }
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    void ShowNextGraph()
    {
        if(openGraphIndex == graphOrder.Count - 1)
        {
            Debug.Log("This is already the last graph revision.");
            return;
        }
        openGraphIndex++;
        revisionNumberText.text = openGraphIndex.ToString();
        displayGraphIntermidate(openGraphIndex);
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    void ShowPreviousGraph()
    {
        if (openGraphIndex == 0)
        {
            Debug.Log("This is already the first graph revision.");
            return;
        }
        openGraphIndex--;
        revisionNumberText.text = openGraphIndex.ToString();
        displayGraphIntermidate(openGraphIndex);
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    /// <param name="index"></param>
    private void displayGraphIntermidate(int index)
    {
        SEE.Performance p = SEE.Performance.Begin("rendering new revision number " + index);
        clearGraphObjects();
        var graph = graphs[graphOrder[index]];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph availabe, load some data first.");
            return;
        }

        BlockFactory blockFactory;
        if (editorSettings.CScapeBuildings)
        {
            blockFactory = new BuildingFactory();
        }
        else
        {
            blockFactory = new CubeFactory();
        }

        IScale scaler;
        {
            List<string> nodeMetrics = new List<string>() { editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric };
            nodeMetrics.AddRange(editorSettings.IssueMap().Keys);
            if (editorSettings.ZScoreScale)
            {
                scaler = new ZScoreScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
            }
            else
            {
                scaler = new LinearScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
            }
        }
        var layout = new SEE.Layout.BalloonLayout(
            editorSettings.ShowEdges,
            editorSettings.WidthMetric, editorSettings.HeightMetric, editorSettings.DepthMetric,
            editorSettings.IssueMap(),
            editorSettings.InnerNodeMetrics,
            blockFactory,
            scaler,
            editorSettings.EdgeWidth,
            editorSettings.ShowErosions,
            editorSettings.ShowDonuts
        );
        layout.Draw(graph);
        p.End();
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    private void clearGraphObjects()
    {
        foreach (string tag in SEE.DataModel.Tags.All)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
            {
                DestroyImmediate(o);
            }
        }
    }
}
