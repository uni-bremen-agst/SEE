using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TODO flo: make Abstract fpr CCAnimation to simpler Switch
/// </summary>
public class CCAStateManager : MonoBehaviour
{
    private GraphSettings _settings;

    private NodeFactory _nodeFactory;

    private AbstractCCAObjectManager _objectManager;


    public GraphSettings Settings
    {
        get
        {
            if (_settings == null)
            {
                _settings = GraphSettingsExtension.DefaultCCAnimationSettings();
            }
            return _settings;
        }
    }

    public NodeFactory NodeFactory
    {
        get
        {
            if (_nodeFactory == null)
            {
                _nodeFactory = new BuildingFactory();
            }
            return _nodeFactory;
        }
    }

    public AbstractCCAObjectManager ObjectManager
    {
        get
        {
            if (_objectManager == null)
            {
                _objectManager = new CCAObjectManager(NodeFactory);
            }
            return _objectManager;
        }
    }

    // Needs to be set via Ui
    public AbstractCCARender render;

    [Obsolete]
    private IEdgeLayout EdgeLayout;



    private readonly CCALoader graphLoader = new CCALoader();
    private readonly Dictionary<Graph, AbstractCCALayout> layouts = new Dictionary<Graph, AbstractCCALayout>();
    private IScale scaler;
    private bool _isAutoplay = false;





    private List<Graph> Graphs => graphLoader.graphs;

    public int GraphCount => Graphs.Count;

    public float AnimationTime
    {
        get => render.AnimationTime;
        set
        {
            render.AnimationTime = value;
            ViewDataChangedEvent.Invoke();
        }
    }

    private int _openGraphIndex = 0;
    public int OpenGraphIndex
    {
        get => _openGraphIndex;
        private set
        {
            _openGraphIndex = value;
            ViewDataChangedEvent.Invoke();
        }
    }

    private UnityEvent _viewDataChangedEvent = new UnityEvent();
    public UnityEvent ViewDataChangedEvent
    {
        get
        {
            if (_viewDataChangedEvent == null)
                _viewDataChangedEvent = new UnityEvent();
            return _viewDataChangedEvent;
        }
    }

    public bool IsAutoPlay
    {
        get => _isAutoplay;
        private set
        {
            ViewDataChangedEvent.Invoke();
            _isAutoplay = value;
        }
    }

    private bool HasLoadedGraph(out LoadedGraph loadedGraph)
    {
        return HasLoadedGraph(_openGraphIndex, out loadedGraph);
    }

    private bool HasLoadedGraph(int index, out LoadedGraph loadedGraph)
    {
        // TODO FLo: KeyNotFoundException
        loadedGraph = null;
        var graph = Graphs[index];
        if (graph == null)
        {
            Debug.LogError("There ist no Graph available at index " + index);
            return false;
        }
        var hasLayout = layouts.TryGetValue(graph, out AbstractCCALayout layout);
        if (layout == null || !hasLayout)
        {
            Debug.LogError("There ist no Layout available at index " + index);
            return false;
        }
        if (Settings == null)
        {
            Debug.LogError("There ist no GraphSettings available");
            return false;
        }
        loadedGraph = new LoadedGraph(graph, layout, Settings);
        return true;
    }

    void Start()
    {
        render.AssertNotNull("render");
        render.ObjectManager = ObjectManager;

        Settings.MinimalBlockLength = 1;
        Settings.MaximalBlockLength = 10;
        Settings.ZScoreScale = false;

        graphLoader.LoadGraphData(Settings);
        ViewDataChangedEvent.Invoke();



        List<string> nodeMetrics = new List<string>() { Settings.WidthMetric, Settings.HeightMetric, Settings.DepthMetric, Settings.ColorMetric };
        nodeMetrics.AddRange(Settings.AllLeafIssues());
        nodeMetrics.AddRange(Settings.AllInnerNodeIssues());
        nodeMetrics.Add(Settings.InnerDonutMetric);

        //if (settings.ZScoreScale)
        //{
        //    scaler = new ZScoreScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
        //}
        //else
        //{
        //    scaler = new LinearScale(graph, settings.MinimalBlockLength, settings.MaximalBlockLength, nodeMetrics);
        //}



        //List<string> nodeMetrics = new List<string>() { Settings.WidthMetric, Settings.HeightMetric, Settings.DepthMetric };
        //nodeMetrics.AddRange(Settings.IssueMap().Keys);
        scaler = new LinearMultiScale(Graphs, Settings.MinimalBlockLength, Settings.MaximalBlockLength, nodeMetrics);

        //TODO Flo: load layouts
        Graphs.ForEach(key =>
        {
            layouts[key] = new AbstractCCALayout();
            layouts[key].Calculate(ObjectManager, scaler, new BalloonNodeLayout(0, NodeFactory), key, Settings);
        });
        if (HasLoadedGraph(out LoadedGraph loadedGraph))
        {
            render.DisplayGraph(loadedGraph);
        }
        else
        {
            Debug.LogError("Could not create LoadedGraph to render.");
        }
    }

    public bool TryShowSpecificGraph(int value)
    {
        if (render.IsStillAnimating || IsAutoPlay)
        {
            Debug.Log("The render is already occupied with animating, wait till animations are finished.");
            return false;
        }

        if (value < 0 || value >= GraphCount)
        {
            Debug.Log("value is no valid index.");
            return false;
        }
        OpenGraphIndex = value;

        if (HasLoadedGraph(out LoadedGraph loadedGraph))
        {
            render.DisplayGraph(loadedGraph);
            return true;
        }
        else
        {
            Debug.LogError("Could not create LoadedGraph to render.");
        }
        return false;
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    public void ShowNextGraph()
    {
        if (render.IsStillAnimating || IsAutoPlay)
        {
            Debug.Log("The render is already occupied with animating, wait till animations are finished.");
            return;
        }
        var canShowNext = ShowNextIfPossible();
        if (!canShowNext)
        {
            Debug.Log("This is already the last graph revision.");
            return;
        }
    }

    /// <summary>
    /// TODO: doc
    /// </summary>
    public void ShowPreviousGraph()
    {
        if (render.IsStillAnimating || IsAutoPlay)
        {
            Debug.Log("The render is already occupied with animating, wait till animations are finished.");
            return;
        }
        if (OpenGraphIndex == 0)
        {
            Debug.Log("This is already the first graph revision.");
            return;
        }
        OpenGraphIndex--;

        if (HasLoadedGraph(out LoadedGraph loadedGraph) &&
            HasLoadedGraph(OpenGraphIndex + 1, out LoadedGraph oldLoadedGraph))
        {
            render.TransitionToPreviousGraph(oldLoadedGraph, loadedGraph);
        }
        else
        {
            Debug.LogError("Could not create LoadedGraph to render.");
        }
    }

    internal void ToggleAutoplay()
    {
        ToggleAutoplay(!IsAutoPlay);
    }

    internal void ToggleAutoplay(bool enabled)
    {
        IsAutoPlay = enabled;
        if (IsAutoPlay)
        {
            render.AnimationFinishedEvent.AddListener(OnAutoplayCanContinue);
            var canShowNext = ShowNextIfPossible();
            if (!canShowNext)
            {
                Debug.Log("This is already the last graph revision.");
            }
        }
        else
        {
            render.AnimationFinishedEvent.RemoveListener(OnAutoplayCanContinue);
        }
        ViewDataChangedEvent.Invoke();
    }

    private bool ShowNextIfPossible()
    {
        if (_openGraphIndex == Graphs.Count - 1)
        {
            return false;
        }
        OpenGraphIndex++;

        if (HasLoadedGraph(out LoadedGraph loadedGraph) &&
            HasLoadedGraph(OpenGraphIndex - 1, out LoadedGraph oldLoadedGraph))
        {
            render.TransitionToNextGraph(oldLoadedGraph, loadedGraph);
        }
        else
        {
            Debug.LogError("Could not create LoadedGraph to render.");
        }
        return true;
    }

    internal void OnAutoplayCanContinue()
    {
        var canShowNext = ShowNextIfPossible();
        if (!canShowNext)
        {
            ToggleAutoplay();
        }
    }
}
