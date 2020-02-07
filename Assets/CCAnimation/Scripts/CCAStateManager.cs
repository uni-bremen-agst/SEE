using Assets.CCAnimation.Scripts.Render;
using SEE;
using SEE.DataModel;
using SEE.Layout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TODO flo: make Abstract fpr CCAnimation to simpler Switch
/// </summary>
public class CCAStateManager : MonoBehaviour
{
    // TODO FPS
    public int FPS { get; private set; }
    public int LowestFPS { get; private set; }
    public int HighestFPS { get; private set; }
    public long CombinedFPS { get; private set; }
    public long FPSCounter { get; private set; }

    void Update()
    {
        FPS = (int)(1f / Time.unscaledDeltaTime);
        CombinedFPS += FPS;
        FPSCounter++;
        if (LowestFPS > FPS)
        {
            LowestFPS = FPS;
        }
        if (HighestFPS < FPS)
            HighestFPS = FPS;
    }

    void BeginFPS()
    {
        LowestFPS = FPS;
        HighestFPS = FPS;
        CombinedFPS = FPS;
        FPSCounter = 1;
    }

    void EndFps()
    {
        var fpsStr = $"{OpenGraphIndex-1}; {LowestFPS}; {(int)(CombinedFPS / FPSCounter)}; {HighestFPS}";
        //Debug.Log(fpsStr);
        frameRateString.AppendLine(fpsStr);
    }

    StringBuilder frameRateString = new StringBuilder();
    string gxlFolder = "";

    void PrepareDatafolder()
    {
        gxlFolder = $"{Directory.GetCurrentDirectory()}\\Data\\GXL\\{gxlFolderName}";
        frameRateString.Append("Graph Nr; Niedrigste FPS; Mittlere FPS; Hoechste FPS");
    }

    /// <summary>
    /// Possible Animations:
    /// "animation-clones"
    /// "animation-clones-tinylog"
    /// "animation-clones-log4j"
    /// </summary>
    public string gxlFolderName = "animation-clones";

    /// <summary>
    /// Set if the BlockFactory is use to create nodes or else
    /// the BuildingFactory is used.
    /// </summary>
    public bool useBlockFactory = false;

    public int maxRevisionsToLoad = 500;

    protected GraphSettings CreateGraphSetting()
    {
        var _settings = GraphSettingsExtension.DefaultCCAnimationSettings(gxlFolderName);
        _settings.MinimalBlockLength = 1;
        _settings.MaximalBlockLength = 100;
        return _settings;
    }

    protected NodeFactory CreateNodeFactory()
    {
        if (useBlockFactory)
        {
            return new CubeFactory();
        }
        else
        {
            return new BuildingFactory();
        }
    }

    protected AbstractCCARender CreateRender()
    {
        if (useBlockFactory)
        {
            return gameObject.AddComponent(typeof(CCABlockRender)) as AbstractCCARender;
        }
        else
        {
            return gameObject.AddComponent(typeof(CCARender)) as AbstractCCARender;
        }
    }

    protected AbstractCCAObjectManager createObjectManager()
    {
        return new CCAObjectManager(NodeFactory);
    }

    protected List<string> CreateNodeMetrics(GraphSettings graphSettings)
    {
        List<string> nodeMetrics = new List<string>() { graphSettings.WidthMetric, graphSettings.HeightMetric, graphSettings.DepthMetric, graphSettings.ColorMetric };
        nodeMetrics.AddRange(graphSettings.AllLeafIssues());
        nodeMetrics.AddRange(graphSettings.AllInnerNodeIssues());
        nodeMetrics.Add(graphSettings.InnerDonutMetric);
        return nodeMetrics;
    }

    protected IScale CreateScaler(List<Graph> graphs, GraphSettings graphSettings, List<string> nodeMetrics)
    {
        return new LinearMultiScale(graphs, graphSettings.MinimalBlockLength, graphSettings.MaximalBlockLength, nodeMetrics);
    }

    protected NodeLayout CreateLayout(NodeFactory nodeFactory)
    {
        //return new EvoStreetsNodeLayout(0, nodeFactory);
        //return new TreemapLayout(0, nodeFactory, 1000, 1000);
        return new BalloonNodeLayout(0, nodeFactory);
    }

    private GraphSettings _settings;

    private NodeFactory _nodeFactory;

    private AbstractCCAObjectManager _objectManager;

    private AbstractCCARender _Render;

    public GraphSettings Settings
    {
        get
        {
            if (_settings == null)
                _settings = CreateGraphSetting();
            return _settings;
        }
    }

    public NodeFactory NodeFactory
    {
        get
        {
            if (_nodeFactory == null)
                _nodeFactory = CreateNodeFactory();
            return _nodeFactory;
        }
    }

    public AbstractCCAObjectManager ObjectManager
    {
        get
        {
            if (_objectManager == null)
                _objectManager = createObjectManager();
            return _objectManager;
        }
    }

    public AbstractCCARender Render
    {
        get
        {
            if (_Render == null)
                _Render = CreateRender();
            return _Render;
        }
    }

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
        get => Render.AnimationTime;
        set
        {
            Render.AnimationTime = value;
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
        PrepareDatafolder();
        Render.AssertNotNull("render");
        Render.ObjectManager = ObjectManager;

        graphLoader.LoadGraphData(Settings, maxRevisionsToLoad);

        ViewDataChangedEvent.Invoke();

        var nodeMetrics = CreateNodeMetrics(Settings);

        scaler = CreateScaler(Graphs, Settings, nodeMetrics);

        var csv = new StringBuilder();
        
        var csvFileName = "\\measure-house.csv";
        if (useBlockFactory)
        {
            csvFileName = "\\measure-block.csv";
        }
        csv.AppendLine("Graph Nr; Load time");
        int index = 1;
        var stopwatch = new System.Diagnostics.Stopwatch();
        var p = Performance.Begin("Layout all Graphs");
        Graphs.ForEach(key =>
        {
            stopwatch.Reset();
            stopwatch.Start();
            layouts[key] = new AbstractCCALayout();
            layouts[key].Calculate(ObjectManager, scaler, CreateLayout(NodeFactory), key, Settings);
            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds == 0)
            {
                csv.AppendLine($"{index}; 1");
            }
            else
                csv.AppendLine($"{index}; {stopwatch.ElapsedMilliseconds}");
            index++;
        });
        p.End();
        try
        {
            Directory.CreateDirectory(gxlFolder);
            File.Delete(gxlFolder + csvFileName);
            File.WriteAllText(gxlFolder + csvFileName, csv.ToString());
            Debug.Log($"Saved load time to {gxlFolder + csvFileName}");
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }

        if (HasLoadedGraph(out LoadedGraph loadedGraph))
        {
            Render.DisplayGraph(loadedGraph);
        }
        else
        {
            Debug.LogError("Could not create LoadedGraph to render.");
        }
    }

    public bool TryShowSpecificGraph(int value)
    {
        if (Render.IsStillAnimating || IsAutoPlay)
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
            Render.DisplayGraph(loadedGraph);
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
        if (Render.IsStillAnimating || IsAutoPlay)
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
        if (Render.IsStillAnimating || IsAutoPlay)
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
            Render.TransitionToPreviousGraph(oldLoadedGraph, loadedGraph);
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
            Render.AnimationFinishedEvent.AddListener(OnAutoplayCanContinue);
            var canShowNext = ShowNextIfPossible();
            if (!canShowNext)
            {
                Debug.Log("This is already the last graph revision.");
            }
        }
        else
        {
            Render.AnimationFinishedEvent.RemoveListener(OnAutoplayCanContinue);
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
            BeginFPS();
            Render.TransitionToNextGraph(oldLoadedGraph, loadedGraph);
        }
        else
        {
            Debug.LogError("Could not create LoadedGraph to render.");
        }
        return true;
    }

    internal void OnAutoplayCanContinue()
    {
        EndFps();
        var canShowNext = ShowNextIfPossible();
        if (!canShowNext)
        {
            try
            {
                Directory.CreateDirectory(gxlFolder);
                var framerateFilename = "\\framerate-house.csv";
                if (useBlockFactory)
                {
                    framerateFilename = "\\framerate-block.csv";
                }
                File.Delete(gxlFolder + framerateFilename);
                File.WriteAllText(gxlFolder + framerateFilename, frameRateString.ToString());
                Debug.Log($"Saved load time to {gxlFolder + framerateFilename}");
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
            ToggleAutoplay();
        }
    }
}
