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
        /// True if BlockFactory should be used for leaf nodes; otherwise
        /// BuildingFactory will be used instead.
        /// </summary>
        /// <returns>true if BlockFactory should be used for leaf nodes</returns>
        private bool UseBlockFactory()
        {
            return LeafObjects == LeafNodeKinds.Blocks;
        }

        /// <summary>
        /// Sets the maximum number of revsions to load.
        /// </summary>
        public int maxRevisionsToLoad = 500;

        /// <summary>
        /// The renderer for the rendering the evolution of the graph series.
        /// </summary>
        private EvolutionRenderer _Renderer;

        /// <summary>
        /// Whether the user has selected auto-play mode.
        /// </summary>
        private bool _isAutoplay = false;

        private UnityEvent _viewDataChangedEvent = new UnityEvent();

        /// <summary>
        /// The index of the currently visualized graph.
        /// </summary>
        private int currentGraphIndex = 0;

        /// <summary>
        /// Factory method to create the used EvolutionRenderer.
        /// </summary>
        /// <returns></returns>
        protected EvolutionRenderer CreateEvolutionRenderer()
        {
            EvolutionRenderer result = gameObject.AddComponent<EvolutionRenderer>();
            result.CityEvolution = this;
            return result;
        }

        /// <summary>
        /// Returns the list of visualized node metrics to be scaled.
        /// </summary>
        /// <returns>list of visualized node metrics to be scaled</returns>
        protected List<string> MetricsToBeScaled()
        {
            // We are first using a set because the user could have decided to use
            // the same node attribute more than once for one of these visual attributes.
            HashSet<string> nodeMetrics = new HashSet<string>() { this.WidthMetric, this.HeightMetric, this.DepthMetric, this.ColorMetric };
            nodeMetrics.UnionWith(this.AllLeafIssues());
            nodeMetrics.UnionWith(this.AllInnerNodeIssues());
            nodeMetrics.Add(this.InnerDonutMetric);
            return new List<string>(nodeMetrics);
        }

        /// <summary>
        /// Factory method to create the used IScale implementation.
        /// </summary>
        /// <param name="graphs"></param>
        /// <param name="nodeMetrics"></param>
        /// <returns></returns>
        protected IScale CreateScaler(List<Graph> graphs, List<string> nodeMetrics)
        {
            // TODO: Use the setting of attribute ZScoreScale.
            return new LinearMultiScale(graphs, this.MinimalBlockLength, this.MaximalBlockLength, nodeMetrics);
        }

        /// <summary>
        /// Factory method to create the used NodeLaoyout.
        /// </summary>
        /// <param name="nodeFactory"></param>
        /// <returns></returns>
        protected NodeLayout CreateLayout(NodeFactory nodeFactory)
        {
            // TODO: Use the setting of attribute NodeLayout
            // TODO: Make the layouts incremental, so that we can add and
            // remove single nodes and modify their dimensions.

            return new EvoStreetsNodeLayout(0, nodeFactory);
            //return new TreemapLayout(0, nodeFactory, 1000, 1000);
            //return new BalloonNodeLayout(0, nodeFactory);
        }

        public EvolutionRenderer Renderer
        {
            get
            {
                if (_Renderer == null)
                    _Renderer = CreateEvolutionRenderer();
                return _Renderer;
            }
        }

        private GraphsReader GraphLoader { get; } = new GraphsReader();

        private IScale Scaler { get; set; }

        private List<Graph> Graphs => GraphLoader.graphs;

        public int GraphCount => Graphs.Count;

        /// <summary>
        /// The time in seconds for showing a single graph revision during auto-play animation.
        /// </summary>
        public float AnimationLag
        {
            get => Renderer.AnimationTime;
            set
            {
                Renderer.AnimationTime = value;
                ViewDataChangedEvent.Invoke();
            }
        }

        /// <summary>
        /// Returns the index of the currently shown graph.
        /// </summary>
        public int CurrentGraphIndex
        {
            get => currentGraphIndex;
            private set
            {
                currentGraphIndex = value;
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

        /// <summary>
        /// Loads the graph data from the GXL files and the metrics from the CSV files contained 
        /// in the directory with path PathPrefix and the metrics.
        /// </summary>
        public void LoadData()
        {
            if (String.IsNullOrEmpty(PathPrefix))
            {
                PathPrefix = UnityProject.GetPath() + "..\\Data\\GXL\\animation-clones\\";
                Debug.LogErrorFormat("Path prefix not set. Using default: {0}.\n", PathPrefix);
            }
            // Load all GXL graphs in directory PathPrefix but not more than maxRevisionsToLoad many.
            GraphLoader.Load(this.PathPrefix, this.HierarchicalEdges, maxRevisionsToLoad);

            // TODO: The CSV metric files should be loaded, too.

            // TODO: Extend IScale so that it can handle a list of graphs instead of just one.
            // Create the scaling of all visualized metrics.
            Scaler = CreateScaler(Graphs, MetricsToBeScaled());
        }

        void Start()
        {
            Renderer.AssertNotNull("renderer");

            LoadData();

            ViewDataChangedEvent.Invoke();

            Renderer.CalculateGraphLayouts(Graphs);

            // 
            if (HasLaidOutGraph(out LaidOutGraph loadedGraph))
            {
                Renderer.DisplayInitialGraph(loadedGraph);
            }
            else
            {
                Debug.LogError("Could not create LoadedGraph to renderer.");
            }
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
            CurrentGraphIndex = value;

            if (HasLaidOutGraph(out LaidOutGraph loadedGraph))
            {
                Renderer.DisplayInitialGraph(loadedGraph);
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
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.");
                return;
            }
            if (!ShowNextIfPossible())
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
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.");
                return;
            }
            if (CurrentGraphIndex == 0)
            {
                Debug.Log("This is already the first graph revision.");
                return;
            }
            CurrentGraphIndex--;

            if (HasLaidOutGraph(out LaidOutGraph newShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex + 1, out LaidOutGraph currentlyShownGraph))
            {
                // Note: newShownGraph is the most recent past of currentlyShownGraph
                Renderer.TransitionToNextGraph(currentlyShownGraph, newShownGraph);
            }
            else
            {
                Debug.LogError("Could not create LaidOutGraph to render.");
            }
        }

        /// <summary>
        /// Returns true and a LoadedGraph if there is a LoadedGraph for the active graph index
        /// CurrentGraphIndex.
        /// </summary>
        /// <param name="loadedGraph"></param>
        /// <returns>true if there is graph to be visualized (index _openGraphIndex)</returns>
        private bool HasLaidOutGraph(out LaidOutGraph loadedGraph)
        {
            return HasLaidOutGraph(CurrentGraphIndex, out loadedGraph);
        }

        /// <summary>
        /// Returns true and a LaidOutGraph if there is a LaidOutGraph for the given graph index.
        /// </summary>
        /// <param name="index">index of the requested graph</param>
        /// <param name="laidOutGraph">the resulting graph with given index; defined only if this method returns true</param>
        /// <returns>true iff there is a graph at the given index</returns>
        private bool HasLaidOutGraph(int index, out LaidOutGraph laidOutGraph)
        {
            laidOutGraph = null;
            var graph = Graphs[index];
            if (graph == null)
            {
                Debug.LogError("There ist no graph available at index " + index);
                return false;
            }
            var hasLayout = _Renderer.TryGetLayout(graph, out Dictionary<GameObject, NodeTransform> layout);
            if (layout == null || !hasLayout)
            {
                Debug.LogError("There ist no layout available at index " + index);
                return false;
            }
            laidOutGraph = new LaidOutGraph(graph, layout);
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
                if (!ShowNextIfPossible())
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
            if (currentGraphIndex == Graphs.Count - 1)
            {
                return false;
            }
            CurrentGraphIndex++;

            if (HasLaidOutGraph(out LaidOutGraph newShownGraph) &&
                HasLaidOutGraph(CurrentGraphIndex - 1, out LaidOutGraph currentlyShownGraph))
            {

                // Note: newShownGraph is the very next future of currentlyShownGraph
                Renderer.TransitionToNextGraph(currentlyShownGraph, newShownGraph);
            }
            else
            {
                Debug.LogError("Could not create LoadedGraph to render.");
            }
            return true;
        }

        internal void OnAutoplayCanContinue()
        {
            if (!ShowNextIfPossible())
            {
                ToggleAutoplay();
            }
        }
    }
}