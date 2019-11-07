using SEE;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TODO flo doc layout
/// </summary>
public abstract class AbstractCCARender : MonoBehaviour
{
    private float MinimalWaitTimeForNextRevision = 0.1f;
    public readonly UnityEvent AnimationStartedEvent = new UnityEvent();

    public readonly UnityEvent AnimationFinishedEvent = new UnityEvent();

    private bool _isStillAnimating = false;
    public bool IsStillAnimating { get => _isStillAnimating; set => _isStillAnimating = value; }

    private readonly List<AbstractCCAAnimator> animators = new List<AbstractCCAAnimator>();

    private float _animationTime = AbstractCCAAnimator.DefaultAnimationTime;
    public float AnimationTime
    {
        get => _animationTime;
        set
        {
            if (value >= 0)
            {
                _animationTime = value;
                animators.ForEach(animator =>
                {
                    animator.MaxAnimationTime = value;
                    animator.AnimationsDisabled = value == 0;
                });
            }
        }
    }

    private LoadedGraph _loadedGraph;
    private LoadedGraph _nextGraph;

    private readonly NodeEqualityComparer nodeEqualityComparer = new NodeEqualityComparer();
    private readonly EdgeEqualityComparer edgeEqualityComparer = new EdgeEqualityComparer();

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

    public AbstractCCARender()
    {
        RegisterAllAnimators(animators);
    }

    public void DisplayGraph(LoadedGraph loadedGraph)
    {
        loadedGraph.AssertNotNull("loadedGraph");

        if (IsStillAnimating)
        {
            Debug.LogWarning("Graph changes are blocked while animations are running.");
            return;
        }

        ClearGraphObjects();
        _nextGraph = loadedGraph;
        RenderGraph();
    }

    public void TransitionToNextGraph(LoadedGraph actual, LoadedGraph next)
    {
        actual.AssertNotNull("actual");
        next.AssertNotNull("next");

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
        actual.AssertNotNull("actual");
        previous.AssertNotNull("previous");

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
        Invoke("OnAnimationsFinished", Math.Max(AnimationTime, MinimalWaitTimeForNextRevision));
    }

    private void OnAnimationsFinished()
    {
        IsStillAnimating = false;
        AnimationFinishedEvent.Invoke();
    }

    /// <summary>
    /// Is called on Constructor
    /// </summary>
    /// <param name="animators"></param>
    protected abstract void RegisterAllAnimators(List<AbstractCCAAnimator> animators);
    protected abstract void RenderRoot(Node node);
    protected abstract void RenderInnerNode(Node node);
    protected abstract void RenderLeaf(Node node);
    protected abstract void RenderEdge(Edge edge);

    /// <summary>
    /// Object is not auto destroyed
    /// </summary>
    /// <param name="node"></param>
    protected abstract void RenderRemovedOldInnerNode(Node node);
    /// <summary>
    /// Object is not auto destroyed
    /// </summary>
    /// <param name="node"></param>
    protected abstract void RenderRemovedOldLeaf(Node node);

    /// <summary>
    /// Object is not auto destroyed
    /// </summary>
    /// <param name="edge"></param>
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
