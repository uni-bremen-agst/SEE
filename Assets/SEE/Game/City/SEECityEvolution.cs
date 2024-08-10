//Copyright 2020 Florian Garbade
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
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
using SEE.Utils.Paths;
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
        /// Name of the Inspector foldout group for the specific evolution setttings.
        /// </summary>
        private const string evolutionFoldoutGroup = "Evolution settings";

        private const string NoGraphsProvidedErrorMessage = "Graph providers didn't yield any graphs";

        private const string CantShowEvolutionMessage = "Can't show evolution";

        /// <summary>
        /// Sets the maximum number of revisions to load.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Maximum number of revisions to load."),
         FoldoutGroup(evolutionFoldoutGroup), RuntimeTab(evolutionFoldoutGroup)]
        public int MaxRevisionsToLoad = 500; // serialized by Unity

        /// <summary>
        /// The renderer for rendering the evolution of the graph series.
        ///
        /// Neither serialized nor saved in the configuration file.
        /// </summary>
        private EvolutionRenderer evolutionRenderer; // not serialized by Unity; will be set in Start()

        // /// <summary>
        // /// The directory in which the GXL files of the graph series are located.
        // /// </summary>
        // [SerializeField, ShowInInspector, Tooltip("The directory in which the GXL files are located."),
        //  FoldoutGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        // public DirectoryPath GXLDirectory = new();

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

        private List<Graph> _loadedGraphSeries = new();

        private List<Graph> LoadedGraphSeries
        {
            get => _loadedGraphSeries;

            set
            {
                if (_loadedGraphSeries.Count != 0)
                {
                    Reset();
                }

                _loadedGraphSeries = value;
                for (int i = 0; i < value.Count - 1; i++)
                {
                    InspectSchema(_loadedGraphSeries[i]);
                    _loadedGraphSeries[i] = RelevantGraph(_loadedGraphSeries[i]);
                }
            }
        }

        /// <summary>
        /// Factory method to create the used EvolutionRenderer.
        /// </summary>
        /// <returns>the current or new evolution renderer attached to this city</returns>
        protected EvolutionRenderer CreateEvolutionRenderer(IList<Graph> graphs)
        {
            if (!gameObject.TryGetComponent(out EvolutionRenderer result))
            {
                result = gameObject.AddComponent<EvolutionRenderer>();
                result.SetGraphEvolution(graphs);
            }

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
        [NonSerialized] private Graph firstGraph;

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
        /// Loads the first graph of the graph series. If a graph was already
        /// loaded, that graph will be destroyed.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
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
                    ShowNotification.Info("SEECity Evolution", "Loading graph");
                    Debug.Log("Loading graph series from provider");

                    void ReportProgress(float x)
                    {
                        ProgressBar = x;
                        reportProgress(x);
                    }

                    LoadedGraphSeries = await DataProvider.ProvideAsync(new List<Graph>(), this, ReportProgress,
                        cancellationTokenSource.Token);

                    if (LoadedGraphSeries.Count == 0)
                    {
                        Debug.LogWarning("Could not load graph.\n");
                        return;
                    }

                    Debug.Log($"Loaded {LoadedGraphSeries.Count} graphs");
                    ShowNotification.Info("SEECity Evolution", "Graphs loaded");

                    firstGraph = LoadedGraphSeries.First();
                    if (firstGraph != null)
                    {
                        Debug.Log(
                            $"Loaded graph with {firstGraph.NodeCount} nodes and {firstGraph.EdgeCount} edges.\n");
                    }
                    else
                    {
                        Debug.LogWarning("Could not load graph.\n");
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
        /// Destroys <see cref="firstGraph"/> if not <c>null</c>.
        /// Postcondition: <see cref="firstGraph"/> will be <c>null</c>.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset")]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
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
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
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
        /// Yields the graph of the first GXL found in the directory named <code>PathPrefix</code>.
        /// The order is ascending and alphabetic by the GXL filenames located in that directory.
        /// If the first GXL file has a corresponding CSV with additional metrics, this CSV file
        /// will be read, too, and the node metrics added to the graph.
        /// Furthermore the selection of the specific node types selected by the user is applied in case
        /// the user specified it before. By default every node type is selected.
        ///
        /// Precondition: PathPrefix must be set and denote an existing directory in the
        /// file system containing at least one GXL file.
        /// </summary>
        /// <returns>the loaded graph or null if none could be found</returns>
        private async UniTask<Graph> LoadFirstGraphAsync()
        {
            List<Graph> graphs = new List<Graph>(await DataProvider.ProvideAsync(new List<Graph>(), this));

            if (graphs.Count == 0)
            {
                return null;
            }

            Graph graph = graphs.First();
            InspectSchema(graph);
            graph = RelevantGraph(graph);
            return graph;
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
            Reset();
            LoadAsync().Forget();
            return;

            async UniTaskVoid LoadAsync()
            {
                await LoadDataAsync();
                evolutionRenderer = CreateEvolutionRenderer(LoadedGraphSeries);
                DrawGraphs(LoadedGraphSeries);
                gameObject.AddOrGetComponent<AnimationInteraction>().EvolutionRenderer = evolutionRenderer;
                evolutionRenderer.ShowGraphEvolution();
            }
        }

        /// <summary>
        /// Starts the evolution on runtime.
        /// <see cref="LoadDataAsync"/> doesn't need to be called first
        /// </summary>
        //[Button(ButtonSizes.Small, Name = "Start Evolution")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Start Evolution")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public void StartEvolutionAsync()
        {
            //Start();
            Reset();
            //await LoadDataAsync();

            LoadAsync().Forget();
            return;

            async UniTaskVoid LoadAsync()
            {
                await LoadDataAsync();
                evolutionRenderer = CreateEvolutionRenderer(LoadedGraphSeries);
                DrawGraphs(LoadedGraphSeries);
                gameObject.AddOrGetComponent<AnimationInteraction>().EvolutionRenderer = evolutionRenderer;
                evolutionRenderer.ShowGraphEvolution();
            }

            // if (!LoadedGraphSeries.Any())
            // {
            //     ShowNotification.Error(CantShowEvolutionMessage, NoGraphsProvidedErrorMessage);
            //     return;
            // }
            //
            // DrawGraphs(LoadedGraphSeries);
            // evolutionRenderer = CreateEvolutionRenderer(LoadedGraphSeries);
            // gameObject.AddOrGetComponent<AnimationInteraction>().EvolutionRenderer = evolutionRenderer;
            // evolutionRenderer.ShowGraphEvolution();
        }


        /// <summary>
        /// Creates <see cref="evolutionRenderer"/> and shows the nodes having one of the selected
        /// node types and the edges of these specific nodes of the graph evolution
        /// for given <paramref name="graphs"/> using it.
        /// </summary>
        /// <param name="graphs">the series of graph to be drawn</param>
        private void DrawGraphs(IList<Graph> graphs)
        {
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
                SetupCompoundSpringEmbedder(graphs[i]);
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

        /// <summary>
        /// The same as in <see cref="SEECity"/>
        /// </summary>
        private const string dataProviderPathLabel = "data";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            DataProvider?.Save(writer, dataProviderPathLabel);
            // writer.Save(MaxRevisionsToLoad, maxRevisionsToLoadLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            DataProvider =
                MultiGraphProvider.Restore(attributes, dataProviderPathLabel) as MultiGraphPipelineProvider;
            // ConfigIO.Restore(attributes, maxRevisionsToLoadLabel, ref MaxRevisionsToLoad);
        }
    }
}
