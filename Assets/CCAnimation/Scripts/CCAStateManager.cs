using SEE;
using SEE.DataModel;
using SEE.Layout;

using System.Collections;
using System.Collections;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CCAStateManager : MonoBehaviour
{
    private CCALoader graphLoader = new CCALoader();
    //public CCARender graphRender;
    private LayoutTest layout;
    private IScale scaler;

    private int openGraphIndex = 0;

    private GraphSettings EditorSettings
    {
        get { return graphLoader.settings; }
    }

    private Dictionary<string, Graph> Graphs
    {
        get { return graphLoader.graphs; }
    }

    private List<string> GraphOrder
    {
        get { return graphLoader.graphOrder; }
    }

    public int OpenGraphIndex
    {
        get { return openGraphIndex; }
    }

    public int GraphCount
    {
        get { return GraphOrder.Count; }
    }


    void Start()
    {
        EditorSettings.MinimalBlockLength = 1;
        EditorSettings.MaximalBlockLength = 10;
        EditorSettings.ZScoreScale = false;
        graphLoader.Init();
        var graph = Graphs[GraphOrder[openGraphIndex]];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph available at index " + openGraphIndex);
            return;
        }
        {
            List<string> nodeMetrics = new List<string>() { EditorSettings.WidthMetric, EditorSettings.HeightMetric, EditorSettings.DepthMetric };
            nodeMetrics.AddRange(EditorSettings.IssueMap().Keys);
            if (EditorSettings.ZScoreScale)
            {
                scaler = new ZScoreScale(graph, EditorSettings.MinimalBlockLength, EditorSettings.MaximalBlockLength, nodeMetrics);
            }
            else
            {
                scaler = new LinearMultiScale(Graphs.Values.ToList(), EditorSettings.MinimalBlockLength, EditorSettings.MaximalBlockLength, nodeMetrics);
            }
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
        if (openGraphIndex == GraphOrder.Count - 1)
        {
            Debug.Log("This is already the last graph revision.");
            return;
        }
        openGraphIndex++;

        var graph = Graphs[GraphOrder[openGraphIndex]];
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

        var graph = Graphs[GraphOrder[openGraphIndex]];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph available at index " + openGraphIndex);
            return;
        }
        RenderLayout(graph);
        //graphRender.TransitionToPreviousGraph(graph, editorSettings);
    }

    LayoutTest layoutTest = null;

    void RenderLayout(Graph graph)
    {
        /*
        foreach (string tag in SEE.DataModel.Tags.All)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
            {
                DestroyImmediate(o);
            }
        }
        */
        BlockFactory blockFactory;
        if (EditorSettings.CScapeBuildings)
        {
            blockFactory = new BuildingFactory();
        }
        else
        {
            blockFactory = new CubeFactory();
        }

        layout = new LayoutTest(
            EditorSettings.ShowEdges,
            EditorSettings.WidthMetric, EditorSettings.HeightMetric, EditorSettings.DepthMetric,
            EditorSettings.IssueMap(),
            EditorSettings.InnerNodeMetrics,
            blockFactory,
            scaler,
            EditorSettings.EdgeWidth,
            EditorSettings.ShowErosions,
            EditorSettings.EdgesAboveBlocks,
            EditorSettings.ShowDonuts,
            layoutTest
        );
        layout.Draw(graph);
        layoutTest?.circleTexts.Values.ToList().ForEach(Destroy);
        layoutTest?.edges.Values.ToList().ForEach(Destroy);
        layoutTest?.gameObjects.Values.ToList().ForEach(Destroy);
        layoutTest = layout;
    }
}
