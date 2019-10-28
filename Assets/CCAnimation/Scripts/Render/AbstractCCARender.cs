using SEE;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 
/// </summary>
public abstract class AbstractCCARender : MonoBehaviour
{
    public readonly UnityEvent AnimationStartedEvent = new UnityEvent();

    public readonly UnityEvent AnimationFinishedEvent = new UnityEvent();

    private bool _isStillAnimating = false;
    public bool IsStillAnimating { get => _isStillAnimating; set => _isStillAnimating = value; }


    private LoadedGraph _loadedGraph;
    private LoadedGraph _nextGraph;

    private readonly NodeEqualityComparer nodeEqualityComparer = new NodeEqualityComparer();
    private readonly EdgeEqualityComparer edgeEqualityComparer = new EdgeEqualityComparer();

    [Obsolete("Cannot Create ObjectManager in a Constructor or initializer func")]
    private AbstractCCAObjectManager _objectManager;

    protected Graph Graph => _nextGraph?.Graph;
    protected AbstractCCALayout Layout => _nextGraph?.Layout;
    protected GraphSettings Settings => _nextGraph?.Settings;

    protected Graph OldGraph => _loadedGraph?.Graph;
    protected AbstractCCALayout OldLayout => _loadedGraph?.Layout;
    protected GraphSettings OldSettings => _loadedGraph?.Settings;

    protected enum GraphDirection { First, Next, Previous };

    protected AbstractCCAObjectManager ObjectManager
    {
        get
        {
            if (_objectManager == null)
            {
                _objectManager = new CCAObjectManager();
            }
            return _objectManager;
        }
    }

    public void DisplayGraph(LoadedGraph loadedGraph)
    {
        if (IsStillAnimating)
        {
            Debug.LogError("Graph changes are not allowed while animations are running.");
            return;
        }
        ClearGraphObjects();
        _nextGraph = loadedGraph;
        RenderGraph();
    }

    public void TransitionToNextGraph(LoadedGraph actual, LoadedGraph next)
    {
        if (IsStillAnimating)
        {
            Debug.LogError("Graph changes are not allowed while animations are running.");
            return;
        }
        _loadedGraph = actual;
        _nextGraph = next;
        RenderGraph();
    }

    public void TransitionToPreviousGraph(LoadedGraph actual, LoadedGraph previous)
    {
        if (IsStillAnimating)
        {
            Debug.LogError("Graph changes are not allowed while animations are running.");
            return;
        }
        _loadedGraph = actual;
        _nextGraph = previous;
        RenderGraph();
    }

    private void RenderGraph()
    {
        IsStillAnimating = true;
        AnimationStartedEvent.Invoke();
        OldGraph?
            .Nodes().Except(Graph.Nodes(), nodeEqualityComparer).ToList()
            .ForEach(node =>
            {
                if (node.IsLeaf())
                {
                    RenderRemovedOldLeaf(node);
                }
                else
                {
                    RenderRemovedOldInnerNode(node);
                }
            });

        OldGraph?
            .Edges().Except(Graph.Edges(), edgeEqualityComparer).ToList()
            .ForEach(RenderRemovedOldEdge);

        Graph.Traverse(RenderRoot, RenderInnerNode, RenderLeaf);
        Graph.Edges().ForEach(RenderEdge);
        Invoke("OnAnimationsFinished", 2); // TODO Flo remove: register animation and wait for Finish
    }

    private void OnAnimationsFinished()
    {
        IsStillAnimating = false;
        AnimationFinishedEvent.Invoke();
    }

    protected abstract void RenderRoot(Node node);
    protected abstract void RenderInnerNode(Node node);
    protected abstract void RenderLeaf(Node node);
    protected abstract void RenderEdge(Edge edge);

    protected abstract void RenderRemovedOldInnerNode(Node node);
    protected abstract void RenderRemovedOldLeaf(Node node);
    protected abstract void RenderRemovedOldEdge(Edge edge);

    private void ClearGraphObjects()
    {
        ObjectManager.Clear();
        foreach (string tag in SEE.DataModel.Tags.All)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
            {
                DestroyImmediate(o);
            }
        }
    }
}
