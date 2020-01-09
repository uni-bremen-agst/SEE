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
    /// <summary>
    /// Die kürzeste Zeit, in der eine Animation ablaufen kann.
    /// </summary>
    private float MinimalWaitTimeForNextRevision = 0.1f;

    /// <summary>
    /// Ein Event, der beim Start der Animationen ausgelöst wird.
    /// </summary>
    public readonly UnityEvent AnimationStartedEvent = new UnityEvent();

    /// <summary>
    /// Ein Event, der nach dem abschließen der gestarteten Animation ausgelöst wird.
    /// </summary>
    public readonly UnityEvent AnimationFinishedEvent = new UnityEvent();

    private bool _isStillAnimating = false;

    /// <summary>
    /// Gibt zurück, ob gerade Animationen im gange sind.
    /// </summary>
    public bool IsStillAnimating { get => _isStillAnimating; set => _isStillAnimating = value; }

    /// <summary>
    /// Ein Sammlung der registrierten <see cref="AbstractCCAAnimator"/>, die bei 
    /// Änderungen in der Animationszeit automatisch aktualisiert werden.
    /// </summary>
    private readonly List<AbstractCCAAnimator> animators = new List<AbstractCCAAnimator>();

    private float _animationTime = AbstractCCAAnimator.DefaultAnimationTime;

    /// <summary>
    /// Die Zeit, die Animationen maximal nach ihrem Start andauern dürfen.
    /// </summary>
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

    /// <summary>
    /// Der aktuell geladene Graph.
    /// </summary>
    private LoadedGraph _loadedGraph;

    /// <summary>
    /// Der Graph, der als nächstes angezeigt werden soll.
    /// </summary>
    private LoadedGraph _nextGraph;

    /// <summary>
    /// Eine Instanz zum Vergleichen zweier <see cref="Node"/>.
    /// </summary>
    private readonly NodeEqualityComparer nodeEqualityComparer = new NodeEqualityComparer();

    /// <summary>
    /// Eine Instanz zum Vergleichen zweier <see cref="Edge"/>.
    /// </summary>
    private readonly EdgeEqualityComparer edgeEqualityComparer = new EdgeEqualityComparer();

    private AbstractCCAObjectManager _objectManager;

    /// <summary>
    /// Gibt den aktuellen, bzw. 
    /// </summary>
    protected Graph Graph => _nextGraph?.Graph;
    protected AbstractCCALayout Layout => _nextGraph?.Layout;
    protected GraphSettings Settings => _nextGraph?.Settings;

    protected Graph OldGraph => _loadedGraph?.Graph;
    protected AbstractCCALayout OldLayout => _loadedGraph?.Layout;
    protected GraphSettings OldSettings => _loadedGraph?.Settings;

    protected enum GraphDirection { First, Next, Previous };

    /// <summary>
    /// Can be null if not set
    /// TODO needs to be set
    /// </summary>
    public AbstractCCAObjectManager ObjectManager
    {
        set
        {
            value.AssertNotNull("ObjectManager");
            _objectManager = value;
        }
        get => _objectManager;
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
        ObjectManager?.Clear();
        foreach (string tag in SEE.DataModel.Tags.All)
        {
            foreach (GameObject o in GameObject.FindGameObjectsWithTag(tag))
            {
                DestroyImmediate(o);
            }
        }
    }
}
