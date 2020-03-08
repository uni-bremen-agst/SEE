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
            // FIXME: Do we really need to attach the evolution renderer as a component to
            // the game object? That was likely done because EvolutionRenderer derives from
            // MonoBehaviour and MonoBehaviours cannot be created by the new operator.
            EvolutionRenderer result = gameObject.AddComponent<EvolutionRenderer>();
            result.CityEvolution = this;
            return result;
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

        /// <summary>
        /// The graph loader used to load all graphs of this graph series. It is
        /// kept because it holds all graphs that we need to retrieve during the
        /// visualization.
        /// </summary>
        private GraphsReader GraphLoader { get; } = new GraphsReader();

        /// <summary>
        /// The series of underlying graphs of this evolving city.
        /// </summary>
        public List<Graph> Graphs => GraphLoader.graphs;

        /// <summary>
        /// The number of loaded graphs of the graph series.
        /// </summary>
        public int GraphCount => Graphs.Count;

        /// <summary>
        /// The time in seconds for showing a single graph revision during auto-play animation.
        /// 
        /// FIXME: Should this be moved to EvolutionRenderer?
        /// </summary>
        public float AnimationLag
        {
            get => Renderer.AnimationDuration;
            set
            {
                Renderer.AnimationDuration = value;
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

        /// <summary>
        /// An event fired when the viewn data have changed. Returns
        /// always the same UnityEvent instance.
        /// </summary>
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
        }

        /// <summary>
        /// Called by Unity when this SEECityEvolution instances comes into existence 
        /// and can enter the game for the first time. Loads all graphs, calculates their
        /// layouts, and displays the first graph in the graph series.
        /// </summary>
        void Start()
        {
            Renderer.AssertNotNull("renderer");

            LoadData();

            ViewDataChangedEvent.Invoke();

            Renderer.CalculateAllGraphLayouts(Graphs);

            if (HasLaidOutGraph(out LaidOutGraph loadedGraph))
            {
                Renderer.DisplayGraphAsNew(loadedGraph);
            }
            else
            {
                Debug.LogError("Could not create LoadedGraph to renderer.");
            }
        }

        /// <summary>
        /// If animations are still ongoing, auto-play mode is turned on, or <paramref name="index"/> 
        /// does not denote a valid index in the graph series, false is returned and nothing else
        /// happens. Otherwise the graph with the given index in the graph series becomes the new
        /// currently shown graph.
        /// </summary>
        /// <param name="index">index of the graph to be shown in the graph series</param>
        /// <returns>true if that graph could be shown successfully</returns>
        public bool TryShowSpecificGraph(int index)
        {
            if (Renderer.IsStillAnimating)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.");
                return false;
            }
            if (IsAutoPlay)
            {
                Debug.Log("Auto-play mode is turned on. You cannot move to the next graph manually.");
                return false;
            }
            if (index < 0 || index >= GraphCount)
            {
                Debug.Log("value is no valid index.");
                return false;
            }
            CurrentGraphIndex = index;

            if (HasLaidOutGraph(out LaidOutGraph loadedGraph))
            {
                Renderer.DisplayGraphAsNew(loadedGraph);
                return true;
            }
            else
            {
                Debug.LogError("Could not create LoadedGraph to render.");
            }
            return false;
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

        /// <summary>
        /// If animation is still ongoing, auto-play mode is turned on, or we are at 
        /// the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its 
        /// direct successor graph in the graph series.
        /// </summary>
        public void ShowNextGraph()
        {
            if (Renderer.IsStillAnimating)
            {
                Debug.Log("The renderer is already occupied with animating, wait till animations are finished.");
                return;
            }
            if (IsAutoPlay)
            {
                Debug.Log("Auto-play mode is turned on. You cannot move to the next graph manually.");
                return;
            }
            if (!ShowNextIfPossible())
            {
                Debug.Log("This is already the last graph revision.");
                return;
            }
        }

        /// <summary>
        /// If we are at the end of the graph series, false is returned and nothing else happens.
        /// Otherwise we make the transition from the currently shown graph to its 
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly.
        /// </summary>
        /// <returns>true iff we are not at the end of the graph series</returns>
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

        /// <summary>
        /// If we are at the begin of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its 
        /// direct predecessor graph in the graph series. CurrentGraphIndex is decreased
        /// by one accordingly.
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
        /// Toggles the auto-play mode. Equivalent to: SetAutoPlay(!IsAutoPlay)
        /// where IsAutoPlay denotes the current state of the auto-play mode.
        /// </summary>
        internal void ToggleAutoPlay()
        {
            SetAutoPlay(!IsAutoPlay);
        }

        /// <summary>
        /// Sets auto-play mode to <paramref name="enabled"/>. If <paramref name="enabled"/>
        /// is true, the next graph in the series is shown and from there all other 
        /// following graphs until we reach the end of the graph series or auto-play
        /// mode is turned off again. If <paramref name="enabled"/> is false instead,
        /// the currently shown graph remains visible.
        /// </summary>
        /// <param name="enabled"></param>
        internal void SetAutoPlay(bool enabled)
        {
            IsAutoPlay = enabled;
            if (IsAutoPlay)
            {
                Renderer.AnimationFinishedEvent.AddListener(OnAutoPlayCanContinue);
                if (!ShowNextIfPossible())
                {
                    Debug.Log("This is already the last graph revision.");
                }
            }
            else
            {
                Renderer.AnimationFinishedEvent.RemoveListener(OnAutoPlayCanContinue);
            }
            ViewDataChangedEvent.Invoke();
        }

        /// <summary>
        /// If we at the end of the graph series, nothing happens.
        /// Otherwise we make the transition from the currently shown graph to its next
        /// direct successor graph in the graph series. CurrentGraphIndex is increased
        /// by one accordingly and auto-play mode is toggled (switched off actually).
        /// </summary>
        private void OnAutoPlayCanContinue()
        {
            if (!ShowNextIfPossible())
            {
                ToggleAutoPlay();
            }
        }
    }
}