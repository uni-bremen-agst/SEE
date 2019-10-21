using SEE;
using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractCCARender : MonoBehaviour
{
    private LoadedGraph _loadedGraph;
    private LoadedGraph _nextGraph;

    protected CCAObjectManager objectManager;
    protected bool isDirectionNext = true;

    protected Graph Graph => _loadedGraph.Graph;
    protected CCAAbstracLayout Layout => _loadedGraph.Layout;
    protected GraphSettings Settings => _loadedGraph.Settings;

    protected Graph OldGraph => _nextGraph.Graph;
    protected CCAAbstracLayout OldLayout => _nextGraph.Layout;
    protected GraphSettings OldSettings => _nextGraph.Settings;

    public void DisplayGraph(LoadedGraph loadedGraph)
    {
        ClearGraphObjects();
        _loadedGraph = loadedGraph;
        loadedGraph.Layout.Draw(loadedGraph.Graph);
    }

    public void TransitionToNextGraph(LoadedGraph actual, LoadedGraph next)
    {
        isDirectionNext = true;
        _loadedGraph = actual;
        _nextGraph = next;
        RenderGraph();
    }

    public void TransitionToPreviousGraph(LoadedGraph actual, LoadedGraph previous)
    {
        isDirectionNext = false;
        _loadedGraph = actual;
        _nextGraph = previous;
        RenderGraph();
    }

    protected abstract void RenderGraph();

    private void ClearGraphObjects()
    {
        objectManager.Clear();
        foreach (string tag in SEE.DataModel.Tags.All)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
            {
                DestroyImmediate(o);
            }
        }
    }
}
