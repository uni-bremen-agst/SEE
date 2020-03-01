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

using SEE.DataModel;
using SEE.Layout;
using SEE.Animation.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.Animation
{
    /// <summary>
    /// The StateManager combines all necessary components for the animations.
    /// </summary>
    public class StateManager : MonoBehaviour
    {
        /// <summary>
        /// Sets the used gxl folder to load graphs from.
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

        /// <summary>
        /// Sets the maximum number of revsions to load.
        /// </summary>
        public int maxRevisionsToLoad = 500;

        #region Internal private variables

        private SEECity _settings;
        private NodeFactory _nodeFactory;
        private AbstractObjectManager _objectManager;
        private AbstractRenderer _Render;
        private bool _isAutoplay = false;
        private UnityEvent _viewDataChangedEvent = new UnityEvent();
        private int _openGraphIndex = 0;
        /// <summary>
        /// The folder where the gxl files are located
        /// </summary>
        private readonly string gxlDataFolder = "";

        /// <summary>
        /// The FPS counter used to measure animatin perfomance.
        /// </summary>
        private SEE.Animation.Internal.FPSCounter fpsCounter = new SEE.Animation.Internal.FPSCounter();

        #endregion

        #region Factory methods
        /// <summary>
        /// Factory method to create the used GraphSettings.
        /// </summary>
        /// <returns></returns>
        protected SEECity CreateGraphSetting()
        {
            // FIXME/TODO: We need a GameObject representing the graph settings at runtime,
            // which can be modified by a custom editor for GraphSettings and which replaces
            // the current CityEditor.
            var _settings = new SEECity();  
            _settings.SetDefaultAnimationSettings(gxlFolderName);
            _settings.MinimalBlockLength = 1;
            _settings.MaximalBlockLength = 100;
            return _settings;
        }

        /// <summary>
        /// Factory method to create the used NodeFactory.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Factory method to create the used AbstractRenderer.
        /// </summary>
        /// <returns></returns>
        protected AbstractRenderer CreateRender()
        {
            if (useBlockFactory)
            {
                return gameObject.AddComponent(typeof(BlockRenderer)) as AbstractRenderer;
            }
            else
            {
                return gameObject.AddComponent(typeof(HouseRenderer)) as AbstractRenderer;
            }
        }

        /// <summary>
        /// Factory method to create the used AbstractObjectManager.
        /// </summary>
        /// <returns></returns>
        protected AbstractObjectManager CreateObjectManager()
        {
            return new ObjectManager(NodeFactory);
        }

        /// <summary>
        /// Factory method to create the used NodeMetrics.
        /// </summary>
        /// <param name="graphSettings"></param>
        /// <returns></returns>
        protected List<string> CreateNodeMetrics(SEECity graphSettings)
        {
            List<string> nodeMetrics = new List<string>() { graphSettings.WidthMetric, graphSettings.HeightMetric, graphSettings.DepthMetric, graphSettings.ColorMetric };
            nodeMetrics.AddRange(graphSettings.AllLeafIssues());
            nodeMetrics.AddRange(graphSettings.AllInnerNodeIssues());
            nodeMetrics.Add(graphSettings.InnerDonutMetric);
            return nodeMetrics;
        }

        /// <summary>
        /// Factory method to create the used IScale implementation.
        /// </summary>
        /// <param name="graphs"></param>
        /// <param name="graphSettings"></param>
        /// <param name="nodeMetrics"></param>
        /// <returns></returns>
        protected IScale CreateScaler(List<Graph> graphs, SEECity graphSettings, List<string> nodeMetrics)
        {
            return new LinearMultiScale(graphs, graphSettings.MinimalBlockLength, graphSettings.MaximalBlockLength, nodeMetrics);
        }

        /// <summary>
        /// Factory method to create the used NodeLaoyout.
        /// </summary>
        /// <param name="nodeFactory"></param>
        /// <returns></returns>
        protected NodeLayout CreateLayout(NodeFactory nodeFactory)
        {
            //return new EvoStreetsNodeLayout(0, nodeFactory);
            //return new TreemapLayout(0, nodeFactory, 1000, 1000);
            return new BalloonNodeLayout(0, nodeFactory);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The GraphSettings used when calculating the layout.
        /// </summary>
        public SEECity Settings
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

        public AbstractObjectManager ObjectManager
        {
            get
            {
                if (_objectManager == null)
                    _objectManager = CreateObjectManager();
                return _objectManager;
            }
        }

        public AbstractRenderer Render
        {
            get
            {
                if (_Render == null)
                    _Render = CreateRender();
                return _Render;
            }
        }

        private Loader GraphLoader { get; } = new Loader();

        private Dictionary<Graph, SEE.Animation.Internal.Layout> Layouts { get; } = new Dictionary<Graph, SEE.Animation.Internal.Layout>();

        private IScale Scaler { get; set; }

        private List<Graph> Graphs => GraphLoader.graphs;

        public int GraphCount => Graphs.Count;

        /// <summary>
        /// The used time for the animations.
        /// </summary>
        public float AnimationTime
        {
            get => Render.AnimationTime;
            set
            {
                Render.AnimationTime = value;
                ViewDataChangedEvent.Invoke();
            }
        }

        /// <summary>
        /// Returns the index of the shown graph.
        /// </summary>
        public int OpenGraphIndex
        {
            get => _openGraphIndex;
            private set
            {
                _openGraphIndex = value;
                ViewDataChangedEvent.Invoke();
            }
        }


        public UnityEvent ViewDataChangedEvent
        {
            get
            {
                if (_viewDataChangedEvent == null)
                    _viewDataChangedEvent = new UnityEvent();
                return _viewDataChangedEvent;
            }
        }

        /// <summary>
        /// Returns true if automatic animations are active.
        /// </summary>
        public bool IsAutoPlay
        {
            get => _isAutoplay;
            private set
            {
                ViewDataChangedEvent.Invoke();
                _isAutoplay = value;
            }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StateManager()
        {
            gxlDataFolder = $"{Directory.GetCurrentDirectory()}\\Data\\GXL\\{gxlFolderName}";
        }

        #region MonoBehavior basic methods

        void Start()
        {
            Render.AssertNotNull("render");
            Render.ObjectManager = ObjectManager;

            GraphLoader.LoadGraphData(Settings, maxRevisionsToLoad);

            ViewDataChangedEvent.Invoke();

            var nodeMetrics = CreateNodeMetrics(Settings);

            Scaler = CreateScaler(Graphs, Settings, nodeMetrics);

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
                Layouts[key] = new SEE.Animation.Internal.Layout();
                Layouts[key].Calculate(ObjectManager, Scaler, CreateLayout(NodeFactory), key, Settings);
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
                Directory.CreateDirectory(gxlDataFolder);
                File.Delete(gxlDataFolder + csvFileName);
                File.WriteAllText(gxlDataFolder + csvFileName, csv.ToString());
                Debug.Log($"Saved load time to {gxlDataFolder + csvFileName}");
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

        void Update()
        {
            fpsCounter.OnUpdate();
        }

        #endregion

        #region public methods

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

        #endregion

        #region internal methods

        /// <summary>
        /// Returns true and a LoadedGraph if there is a LoadedGraph for the active graph index.
        /// </summary>
        /// <param name="loadedGraph"></param>
        /// <returns></returns>
        private bool HasLoadedGraph(out LoadedGraph loadedGraph)
        {
            return HasLoadedGraph(_openGraphIndex, out loadedGraph);
        }

        /// <summary>
        /// Returns true and a LoadedGraph if there is a LoadedGraph for the given graph index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="loadedGraph"></param>
        /// <returns></returns>
        private bool HasLoadedGraph(int index, out LoadedGraph loadedGraph)
        {
            loadedGraph = null;
            var graph = Graphs[index];
            if (graph == null)
            {
                Debug.LogError("There ist no Graph available at index " + index);
                return false;
            }
            var hasLayout = Layouts.TryGetValue(graph, out SEE.Animation.Internal.Layout layout);
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
                fpsCounter.BeginRound();
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
            fpsCounter.EndRound();
            var canShowNext = ShowNextIfPossible();
            if (!canShowNext)
            {
                try
                {
                    Directory.CreateDirectory(gxlDataFolder);
                    var framerateFilename = "\\framerate-house.csv";
                    if (useBlockFactory)
                    {
                        framerateFilename = "\\framerate-block.csv";
                    }
                    File.Delete(gxlDataFolder + framerateFilename);
                    File.WriteAllText(gxlDataFolder + framerateFilename, fpsCounter.AsCsvString);
                    Debug.Log($"Saved load time to {gxlDataFolder + framerateFilename}");
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
                ToggleAutoplay();
            }
        }

        #endregion
    }
}