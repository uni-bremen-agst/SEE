using SEE;
using SEE.DataModel;
using SEE.Layout;

using System.Collections;
using System.Collections;

using System.Collections.Generic;
using UnityEngine;

public class CCAStateManager : MonoBehaviour
{
    private CCALoader graphLoader = new CCALoader();
    //public CCARender graphRender;
    private LayoutTest layout;

    private int openGraphIndex = 0;

    private GraphSettings editorSettings
    {
        get { return graphLoader.settings; }
    }

    private Dictionary<string, Graph> graphs
    {
        get { return graphLoader.graphs; }
    }

    private List<string> graphOrder
    {
        get { return graphLoader.graphOrder; }
    }

    public int OpenGraphIndex
    {
        get { return openGraphIndex; }
    }

    public int GraphCount
    {
        get { return graphOrder.Count; }
    }


    void Start()
    {
        editorSettings.MinimalBlockLength = 1;
        editorSettings.MaximalBlockLength = 10;
        editorSettings.ZScoreScale = false;
        graphLoader.Init();

        var graph = graphs[graphOrder[openGraphIndex]];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph available at index " + openGraphIndex);
            return;
        }
        RenderLayout(graph);
        //graphRender.DisplayStartGraph(graph, editorSettings);
    }

    void Update()
    {

    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    public void ShowNextGraph()
    {
        if (openGraphIndex == graphOrder.Count - 1)
        {
            Debug.Log("This is already the last graph revision.");
            return;
        }
        openGraphIndex++;

        var graph = graphs[graphOrder[openGraphIndex]];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph available at index " + openGraphIndex);
            return;
        }
        RenderLayout(graph);
        //graphRender.TransitionToNextGraph(graph, null, editorSettings); // TODO laoyut
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    public void ShowPreviousGraph()
    {
        if (openGraphIndex == 0)
        {
            Debug.Log("This is already the first graph revision.");
            return;
        }
        openGraphIndex--;

        var graph = graphs[graphOrder[openGraphIndex]];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph available at index " + openGraphIndex);
            return;
        }
        RenderLayout(graph);
        //graphRender.TransitionToPreviousGraph(graph, editorSettings);
    }

    void RenderLayout(Graph graph)
    {
        foreach (string tag in SEE.DataModel.Tags.All)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
            {
                DestroyImmediate(o);
            }
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
            scaler = new LinearScale(graph, editorSettings.MinimalBlockLength, editorSettings.MaximalBlockLength, nodeMetrics);
        }
        layout = new LayoutTest(
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
    }
}
