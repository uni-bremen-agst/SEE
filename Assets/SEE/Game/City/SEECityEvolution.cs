using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.Evolution;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.Game.CityRendering;
using SEE.GraphProviders;
using SEE.UI;
using SEE.UI.Notification;
using SEE.Utils.Config;
using Sirenix.Serialization;

namespace SEE.Game.City
{
    /// <summary>
    /// A SEECityEvolution combines all necessary components for the animations
    /// of an evolving SEECity.
    /// </summary>
    [Serializable]
    public class SEECityEvolution : AbstractSEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEECityEvolution.Save(ConfigWriter)"/> and
        /// <see cref="SEECityEvolution.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.
        [OdinSerialize, ShowInInspector,
         Tooltip("A graph provider yielding the data to be visualized as a code city."),
         TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup),
         HideReferenceObjectPicker]
        public MultiGraphPipelineProvider DataProvider = new();

        /// <summary>
        /// Error message that will be shown when the graph provider pipeline (<see cref="DataProvider"/>)
        /// didn't yield any graphs.
        /// </summary>
        private const string NoGraphsProvidedErrorMessage = "Graph providers didn't yield any graphs";

        /// <summary>
        /// Error message that will be shown if anything goes wrong in the pipeline.
        /// </summary>
        private const string CantShowEvolutionMessage = "An error occurred in the evolution provider pipeline";

        /// <summary>
        /// The renderer for rendering the evolution of the graph series.
        ///
        /// Neither serialized nor saved in the configuration file.
        /// </summary>
        private EvolutionRenderer evolutionRenderer; // not serialized by Unity; will be set in Start()

        /// <summary>
        /// Yields the graph renderer that draws this city.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public override IGraphRenderer Renderer => evolutionRenderer.Renderer;

        /// <summary>
        /// A token that can be used to cancel the loading of the graph.
        /// </summary>
        protected CancellationTokenSource cancellationTokenSource = new();

        /// <summary>
        /// Returns the currently drawn graph.
        /// </summary>
        public override Graph LoadedGraph
        {
            get => evolutionRenderer?.GraphCurrent;
            protected set => throw new NotImplementedException();
        }
        /// <summary>
        /// The currently loaded graph series.
        /// </summary>
        private List<Graph> loadedGraphSeries = new();

        /// <summary>
        /// Public attribute for the currently loaded graph series.
        /// </summary>
        private List<Graph> LoadedGraphSeries
        {
            get => loadedGraphSeries;

            set
            {
                if (loadedGraphSeries.Count != 0)
                {
                    Reset();
                }

                loadedGraphSeries = value;
                for (int i = 0; i < value.Count - 1; i++)
                {
                    InspectSchema(loadedGraphSeries[i]);
                    loadedGraphSeries[i] = RelevantGraph(loadedGraphSeries[i]);
                }
            }
        }

        /// <summary>
        /// Factory method to create the used <see cref="EvolutionRenderer"/>.
        /// </summary>
        /// <param name="graphs">The graphs with which the <see cref="EvolutionRenderer"/> is created</param>
        /// <returns>the current or new evolution renderer attached to this city</returns>
        protected EvolutionRenderer CreateEvolutionRenderer(IList<Graph> graphs)
        {
            EvolutionRenderer result = gameObject.AddOrGetComponent<EvolutionRenderer>();
            result.SetGraphEvolution(graphs);
            return result;
        }

        /// <summary>
        /// Will be called whenever a new value is assigned to <see cref="ProjectPath"/>.
        /// In this case, we will update all loaded graphs that we have passed to the
        /// <see cref="evolutionRenderer"/>.
        /// </summary>
        protected override void ProjectPathChanged()
        {
            evolutionRenderer?.ProjectPathChanged(SourceCodeDirectory.Path);
        }

        /// <summary>
        /// The first graph of the graph series. It is used only to let the user see
        /// his/her settings in action. It will be destroyed when the game starts.
        /// </summary>
        [NonSerialized]
        private Graph firstGraph;

        /// <summary>
        /// Dumps the metric names of all node types of the currently loaded graph.
        /// </summary>
        protected override void DumpNodeMetrics()
        {
            if (firstGraph == null)
            {
                Debug.Log("No graph loaded yet.");
            }
            else
            {
                DumpNodeMetrics(new List<Graph>() { firstGraph });
            }
        }

        /// <summary>
        /// Loads the whole graph series and sets the first graph of the series. If a graph was already
        /// loaded, that graph will be destroyed.
        /// This button is intended for the Unity Editor only, but not in the runtime configuration menu.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Load Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad), RuntimeGroupOrder(DataButtonsGroupOrderLoad)]
        public async UniTask LoadDataAsync()
        {
            if (firstGraph != null)
            {
                Reset();
            }

            try
            {
                using (LoadingSpinner.ShowDeterminate($"Loading city \"{gameObject.name}\"",
                                                      out Action<float> reportProgress))
                {
                    Debug.Log("Loading graph series from provider");

                    void ReportProgress(float x)
                    {
                        ProgressBar = x;
                        reportProgress(x);
                    }

                    if (DataProvider.Pipeline.Count == 0)
                    {
                        Debug.LogWarning("No graph provider in pipeline.\n");
                        return;
                    }
                    LoadedGraphSeries = await DataProvider.ProvideAsync(new List<Graph>(), this, ReportProgress,
                                                                        cancellationTokenSource.Token);

                    if (LoadedGraphSeries.Count == 0)
                    {
                        Debug.LogWarning("Could not load any graph.\n");
                        return;
                    }

                    ShowNotification.Info("SEECity Evolution", $"{LoadedGraphSeries.Count} graphs loaded");

                    firstGraph = LoadedGraphSeries.FirstOrDefault();
                    if (firstGraph != null)
                    {
                        Debug.Log(
                            $"Loaded the first graph with {firstGraph.NodeCount} nodes and {firstGraph.EdgeCount} edges.\n");
                    }
                    else
                    {
                        Debug.LogWarning("Could not load graph because the provider pipeline did not yield any graphs.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ShowNotification.Error("Data failure", $"Evolution graph provider failed with: {e.Message}\n",
                    log: false);
                throw;
            }
        }

        /// <summary>
        /// Starts the evolution rendering at runtime.
        /// Is intended to be used in the runtime configuration menu during play mode only,
        /// but not in the Unity Editor. Will call <see cref="LoadDataAsync"/>.
        /// Acts like Load and Draw in the Unity Editor, except that not only the first
        /// graph will drawn, but the whole series subsequently.
        /// </summary>
        [RuntimeButton(DataButtonsGroup, "Load and Draw")]
        [PropertyOrder(DataButtonsGroupOrderDraw), RuntimeGroupOrder(DataButtonsGroupOrderDraw)]
        public async UniTask StartEvolutionAsync()
        {
            Reset();
            await LoadDataAsync();

            if (!LoadedGraphSeries.Any())
            {
                ShowNotification.Error(CantShowEvolutionMessage, NoGraphsProvidedErrorMessage);
                return;
            }

            DrawGraphs(LoadedGraphSeries);
            StartEvolution();
        }

        /// <summary>
        /// Destroys <see cref="firstGraph"/> if not <c>null</c>.
        /// Postcondition: <see cref="firstGraph"/> will be <c>null</c>.
        /// This button is intended only for the Unity Editor, but not in the runtime configuration menu.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup)]
        [PropertyOrder(ResetButtonsGroupOrderReset), RuntimeGroupOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Delete the underlying graph.
            firstGraph?.Destroy();
            firstGraph = null;
        }

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// This button is intended for the Unity Editor only, but not in the runtime configuration menu.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderDraw), RuntimeGroupOrder(DataButtonsGroupOrderDraw)]
        public void DrawGraph()
        {
            if (firstGraph)
            {
                GraphRenderer graphRenderer = new(this, firstGraph);
                graphRenderer.DrawGraphAsync(firstGraph, gameObject).Forget();
            }
            else
            {
                Debug.LogWarning("No graph loaded yet.\n");
            }
        }

        /// <summary>
        /// Draws the given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph to be drawn</param>
        public void DrawGraph(Graph graph)
        {
            DrawGraphs(new List<Graph> { graph });
        }

        /// <summary>
        /// Loads all graphs, calculates their layouts, and displays the first graph in the
        /// graph series.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            StartEvolutionAsync().Forget();
        }

        /// <summary>
        /// Sets up <see cref="evolutionRenderer"/> and initiates the visualization of the
        /// evolving series of graphs.
        /// </summary>
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Start Evolution")]
        [PropertyOrder(DataButtonsGroupOrderDraw), RuntimeGroupOrder(DataButtonsGroupOrderDraw)]
        private void StartEvolution()
        {
            evolutionRenderer = CreateEvolutionRenderer(LoadedGraphSeries);
            gameObject.AddOrGetComponent<AnimationInteraction>().EvolutionRenderer = evolutionRenderer;
            evolutionRenderer.ShowGraphEvolution();
        }


        /// <summary>
        /// Creates <see cref="evolutionRenderer"/> and shows the nodes having one of the selected
        /// node types and the edges of these specific nodes of the graph evolution
        /// for given <paramref name="graphs"/> using it.
        /// </summary>
        /// <param name="graphs">the series of graph to be drawn</param>
        private void DrawGraphs(IList<Graph> graphs)
        {
            // Note: We need this kind of loop because we assign graphs[i] in its body.
            for (int i = 0; i < graphs.Count; i++)
            {
                Graph relevantGraph = RelevantGraph(graphs[i]);
                if (relevantGraph != graphs[i])
                {
                    // Node types have been filtered out. Because of that
                    // there may now be multiple roots again.
                    relevantGraph.AddSingleRoot(out Node _, name: "ROOT", type: Graph.UnknownType);
                }
                graphs[i] = relevantGraph;
            }
        }

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph, that is, there is at least one node in the graph that has this
        /// metric.
        ///
        /// The metric names are derived from the graph currently drawn by the
        /// evolution renderer.
        /// If no graph has been loaded yet, the empty list will be returned.
        /// </summary>
        /// <returns>names of all existing node metrics</returns>
        public override ISet<string> AllExistingMetrics()
        {
            return evolutionRenderer.AllExistingMetrics();
        }

        #region Config I/O
        /// <summary>
        /// The same as in <see cref="SEECity"/>
        /// </summary>
        private const string dataProviderPathLabel = "data";

        /// <summary>
        /// Saves and writes the configuration to <paramref name="writer"/>
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the configuration to</param>
        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            DataProvider?.Save(writer, dataProviderPathLabel);
        }

        /// <summary>
        /// Restores the configuration from <paramref name="attributes"/>
        /// </summary>
        /// <param name="attributes">The attributes to restore the code city from</param>
        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            DataProvider =
                MultiGraphProvider.Restore(attributes, dataProviderPathLabel) as MultiGraphPipelineProvider;
        }
        #endregion
    }
}
