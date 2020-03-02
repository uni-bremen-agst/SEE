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
    /// A SEECityEvolution combines all necessary components for the animations
    /// of an evolving SEECity.
    /// </summary>
    public class SEECityEvolution : AbstractSEECity
    {
        /// <summary>
        /// Set if the BlockFactory is use to create nodes or else
        /// the BuildingFactory is used.
        /// </summary>
        public bool useBlockFactory = false;

        /// <summary>
        /// Sets the maximum number of revsions to load.
        /// </summary>
        public int maxRevisionsToLoad = 500;

        private NodeFactory _nodeFactory;
        private AbstractObjectManager _objectManager;
        private AbstractRenderer _Render;
        private bool _isAutoplay = false;
        private UnityEvent _viewDataChangedEvent = new UnityEvent();
        private int _openGraphIndex = 0;

        /// <summary>
        /// The FPS counter used to measure animation perfomance.
        /// </summary>
        private SEE.Animation.Internal.FPSCounter fpsCounter = new SEE.Animation.Internal.FPSCounter();

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
        protected AbstractRenderer CreateRenderer()
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
        /// <returns></returns>
        protected List<string> CreateNodeMetrics()
        {
            List<string> nodeMetrics = new List<string>() { this.WidthMetric, this.HeightMetric, this.DepthMetric, this.ColorMetric };
            nodeMetrics.AddRange(this.AllLeafIssues());
            nodeMetrics.AddRange(this.AllInnerNodeIssues());
            nodeMetrics.Add(this.InnerDonutMetric);
            return nodeMetrics;
        }

        /// <summary>
        /// Factory method to create the used IScale implementation.
        /// </summary>
        /// <param name="graphs"></param>
        /// <param name="nodeMetrics"></param>
        /// <returns></returns>
        protected IScale CreateScaler(List<Graph> graphs, List<string> nodeMetrics)
        {
            return new LinearMultiScale(graphs, this.MinimalBlockLength, this.MaximalBlockLength, nodeMetrics);
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

        public AbstractRenderer Renderer
        {
            get
            {
                if (_Render == null)
                    _Render = CreateRenderer();
                return _Render;
            }
        }

        private Loader GraphLoader { get; } = new Loader();

        private Dictionary<Graph, SEE.Animation.Internal.Layout> Layouts { get; } 
            = new Dictionary<Graph, SEE.Animation.Internal.Layout>();

        private IScale Scaler { get; set; }

        private List<Graph> Graphs => GraphLoader.graphs;

        public int GraphCount => Graphs.Count;

        /// <summary>
        /// The used time for the animations.
        /// </summary>
        public float AnimationTime
        {
            get => Renderer.AnimationTime;
            set
            {
                Renderer.AnimationTime = value;
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

        void Start()
        {
            Renderer.AssertNotNull("renderer");
            Renderer.ObjectManager = ObjectManager;

            if (String.IsNullOrEmpty(PathPrefix))
            {
                PathPrefix = UnityProject.GetPath() + "..\\Data\\GXL\\animation-clones\\";
                Debug.LogErrorFormat("Path prefix not set. Using default: {0}.\n", PathPrefix);
            }
            GraphLoader.LoadGraphData(this, maxRevisionsToLoad);

            ViewDataChangedEvent.Invoke();

            var nodeMetrics = CreateNodeMetrics();

            Scaler = CreateScaler(Graphs, nodeMetrics);

            var csv = new StringBuilder();

            var csvFileName = "\\measure-house.csv";
            if (useBlockFactory)
            {
                csvFileName = "\\measure-block.csv";
            }
            csv.AppendLine("Graph Nr; Load time");
            int index = 1;
            var stopwatch = new System.Diagnostics.Stopwatch();
            var p = Performance.Begin("Layout all graphs");
            Graphs.ForEach(key =>
            {
                stopwatch.Reset();
                stopwatch.Start();
                Layouts[key] = new SEE.Animation.Internal.Layout();
                Layouts[key].Calculate(ObjectManager, Scaler, CreateLayout(NodeFactory), key, this);
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
                Directory.CreateDirectory(PathPrefix);
                File.Delete(PathPrefix + csvFileName);
                File.WriteAllText(PathPrefix + csvFileName, csv.ToString());
                Debug.Log($"Saved load time to {PathPrefix + csvFileName}");
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }

            if (HasLoadedGraph(out LoadedGraph loadedGraph))
            {
                Renderer.DisplayGraph(loadedGraph);
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

        public bool TryShowSpecificGraph(int value)
        {
            if (Renderer.IsStillAnimating || IsAutoPlay)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.");
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
                Renderer.DisplayGraph(loadedGraph);
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
            if (Renderer.IsStillAnimating || IsAutoPlay)
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
            if (Renderer.IsStillAnimating || IsAutoPlay)
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
                Renderer.TransitionToPreviousGraph(oldLoadedGraph, loadedGraph);
            }
            else
            {
                Debug.LogError("Could not create LoadedGraph to render.");
            }
        }

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
                Debug.LogError("There ist no graph available at index " + index);
                return false;
            }
            var hasLayout = Layouts.TryGetValue(graph, out SEE.Animation.Internal.Layout layout);
            if (layout == null || !hasLayout)
            {
                Debug.LogError("There ist no layout available at index " + index);
                return false;
            }
            loadedGraph = new LoadedGraph(graph, layout, this);
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
                Renderer.AnimationFinishedEvent.AddListener(OnAutoplayCanContinue);
                var canShowNext = ShowNextIfPossible();
                if (!canShowNext)
                {
                    Debug.Log("This is already the last graph revision.");
                }
            }
            else
            {
                Renderer.AnimationFinishedEvent.RemoveListener(OnAutoplayCanContinue);
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
                Renderer.TransitionToNextGraph(oldLoadedGraph, loadedGraph);
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
                    Directory.CreateDirectory(PathPrefix);
                    var framerateFilename = "\\framerate-house.csv";
                    if (useBlockFactory)
                    {
                        framerateFilename = "\\framerate-block.csv";
                    }
                    File.Delete(PathPrefix + framerateFilename);
                    File.WriteAllText(PathPrefix + framerateFilename, fpsCounter.AsCsvString);
                    Debug.Log($"Saved load time to {PathPrefix + framerateFilename}");
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
                ToggleAutoplay();
            }
        }
    }
}