//Copyright 2020 Florian Garbade

//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
//USE OR OTHER DEALINGS IN THE SOFTWARE.
using SEE;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Abstract Render class that serves as an interface to optimally animate different display formats or layouts
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
    protected CCALayout Layout => _nextGraph?.Layout;
    protected GraphSettings Settings => _nextGraph?.Settings;

    protected Graph OldGraph => _loadedGraph?.Graph;
    protected CCALayout OldLayout => _loadedGraph?.Layout;
    protected GraphSettings OldSettings => _loadedGraph?.Settings;

    protected enum GraphDirection { First, Next, Previous };

    /// <summary>
    /// Can be null if not set
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

    /// <summary>
    /// Constructor
    /// </summary>
    public AbstractCCARender()
    {
        RegisterAllAnimators(animators);
    }

    /// <summary>
    /// Displays the given LoadedGraph instantly if all animations are finished.
    /// </summary>
    /// <param name="loadedGraph"></param>
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

    /// <summary>
    /// Starts the animations to transition to the given next graph.
    /// </summary>
    /// <param name="actual"></param>
    /// <param name="next"></param>
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

    /// <summary>
    /// Starts the animations to transition to the given previous graph.
    /// </summary>
    /// <param name="actual"></param>
    /// <param name="previous"></param>
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

    /// <summary>
    /// Renders the as active set graph.
    /// </summary>
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

    /// <summary>
    /// Event function triggered when alls animations are finished.
    /// </summary>
    private void OnAnimationsFinished()
    {
        IsStillAnimating = false;
        AnimationFinishedEvent.Invoke();
    }

    /// <summary>
    /// Is called on Constructor the register all given animator,
    /// so they can be updated accordingly.
    /// </summary>
    /// <param name="animators"></param>
    protected abstract void RegisterAllAnimators(List<AbstractCCAAnimator> animators);

    /// <summary>
    ///Determines how the main node of the active graph is displayed.
    /// </summary>
    /// <param name="node"></param>
    protected abstract void RenderRoot(Node node);

    /// <summary>
    /// Determines how an inner node that contains other nodes is displayed.
    /// </summary>
    /// <param name="node"></param>
    protected abstract void RenderInnerNode(Node node);

    /// <summary>
    /// Determines how a leaf node is displayed.
    /// </summary>
    /// <param name="node"></param>
    protected abstract void RenderLeaf(Node node);

    /// <summary>
    /// Determines how an edge is displayed.
    /// </summary>
    /// <param name="edge"></param>
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

    /// <summary>
    /// Clears all GameObjects created by the used ObjectManager
    /// </summary>
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
