using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class AbstractCCARender : MonoBehaviour
{
    protected UnityEvent _animationStartedEvent;

    public UnityEvent AnimationStartedEvent
    {
        get
        {
            if (_animationStartedEvent == null)
            {
                _animationStartedEvent = new UnityEvent();
            }
            return _animationStartedEvent;
        }
    }

    protected UnityEvent _animationFinishedEvent;
    public UnityEvent AnimationFinishedEvent
    {
        get
        {
            if (_animationFinishedEvent == null)
            {
                _animationFinishedEvent = new UnityEvent();
            }
            return _animationFinishedEvent;
        }
    }

    private bool _isStillAnimating = false;
    public bool IsStillAnimating => _isStillAnimating;

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
        _isStillAnimating = true;
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
        _isStillAnimating = false;
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

    private class NodeEqualityComparer : IEqualityComparer<Node>
    {
        public bool Equals(Node x, Node y)
        {
            return x.LinkName.Equals(y.LinkName);
        }

        public int GetHashCode(Node obj)
        {
            return obj.LinkName.GetHashCode();
        }
    }

    private class EdgeEqualityComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge x, Edge y)
        {
            return (x.Source.LinkName + x.Target.LinkName).Equals((y.Source.LinkName + y.Target.LinkName));
        }

        public int GetHashCode(Edge obj)
        {
            return (obj.Source.LinkName + obj.Target.LinkName).GetHashCode();
        }
    }
}
