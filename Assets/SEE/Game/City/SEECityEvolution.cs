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
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.Evolution;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using Sirenix.OdinInspector;
using UnityEngine;
using SEE.Game.CityRendering;
using SEE.GraphProviders;
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
        public PipelineGraphProvider<List<Graph>> DataProvider = new();


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
        /// Returns the currently drawn graph.
        /// </summary>
        public override Graph LoadedGraph
        {
            get => evolutionRenderer?.GraphCurrent;
            protected set => throw new NotImplementedException();
        }

        /// <summary>
        /// Factory method to create the used EvolutionRenderer.
        /// </summary>
        /// <returns>the current or new evolution renderer attached to this city</returns>
        protected EvolutionRenderer CreateEvolutionRenderer(List<Graph> graphs)
        {
            if (!gameObject.TryGetComponent(out EvolutionRenderer result))
            {
                result = gameObject.AddComponent<EvolutionRenderer>();
                result.SetGraphEvolution(graphs);
            }

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
        /// Loads the graph data from the GXL files and the metrics from the CSV files contained
        /// in the directory with path PathPrefix and the metrics.
        /// </summary>
        private async UniTask<IEnumerable<Graph>> LoadDataSeries()
        {
            // GraphsReader graphsReader = new();
            // // Load all GXL graphs and CSV files in directory PathPrefix but not more than maxRevisionsToLoad many.
            // graphsReader.Load(GXLDirectory.Path, HierarchicalEdges, basePath: SourceCodeDirectory.Path, rootName: GXLDirectory.Path, MaxRevisionsToLoad);
            return await DataProvider.ProvideAsync(new List<Graph>(), this);
            //return graphsReader.Graphs;
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
        public async void LoadData()
        {
            if (firstGraph != null)
            {
                Reset();
            }

            firstGraph = await LoadFirstGraph();
            if (firstGraph != null)
            {
                Debug.Log($"Loaded graph with {firstGraph.NodeCount} nodes and {firstGraph.EdgeCount} edges.\n");
            }
            else
            {
                Debug.LogWarning("Could not load graph.\n");
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
                GraphRenderer graphRenderer = new GraphRenderer(this, firstGraph);
                // For some reason SetScaler needs to be called here a second time, the attribute Metic.Level
                // can't be found otherwise.
                // EvolutionRenderer.SetGraph doese this too.
                //graphRenderer.SetScaler(new List<Graph>() { firstGraph });
                graphRenderer.DrawGraph(firstGraph, gameObject);
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
        private async UniTask<Graph> LoadFirstGraph()
        {
            List<Graph> graphs = new List<Graph>(await DataProvider.ProvideAsync(new List<Graph>(), this));

            // GraphsReader reader = new();
            // reader.Load(GXLDirectory.Path, HierarchicalEdges, basePath: SourceCodeDirectory.Path,
            //     rootName: GXLDirectory.Path, 1);
            //List<Graph> graphs = reader.Graphs;
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
            DrawGraphs(new List<Graph>() { graph });
        }

        /// <summary>
        /// Loads all graphs, calculates their layouts, and displays the first graph in the
        /// graph series.
        /// </summary>
        protected override async void Start()
        {
            base.Start();
            Reset();

            List<Graph> graphs = new List<Graph>(await DataProvider.ProvideAsync(new List<Graph>(), this));
            if (!graphs.Any())
            {
                ShowNotification.Error(CantShowEvolutionMessage, NoGraphsProvidedErrorMessage);
                return;
            }

            evolutionRenderer = CreateEvolutionRenderer(graphs);
            DrawGraphs(graphs);
            gameObject.AddOrGetComponent<AnimationInteraction>().EvolutionRenderer = evolutionRenderer;
            evolutionRenderer.ShowGraphEvolution();
        }

        /// <summary>
        /// Starts the evolution on runtime.
        /// <see cref="LoadData"/> doesn't need to be called first
        /// </summary>
        //[Button(ButtonSizes.Small, Name = "Start Evolution")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Start Evo")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public async void StatEvolution()
        {
            Reset();
            List<Graph> graphs = new List<Graph>(await DataProvider.ProvideAsync(new List<Graph>(), this));

            if (!graphs.Any())
            {
                ShowNotification.Error(CantShowEvolutionMessage, NoGraphsProvidedErrorMessage);
                return;
            }

            DrawGraphs(graphs);

            DestroyImmediate(CreateEvolutionRenderer(graphs));
            DestroyImmediate(gameObject.AddOrGetComponent<AnimationInteraction>());

            evolutionRenderer = CreateEvolutionRenderer(graphs);

            gameObject.AddOrGetComponent<AnimationInteraction>().EvolutionRenderer = evolutionRenderer;

            evolutionRenderer.ShowGraphEvolution();
        }


        /// <summary>
        /// Creates <see cref="evolutionRenderer"/> and shows the nodes having one of the selected
        /// node types and the edges of these specific nodes of the graph evolution
        /// for given <paramref name="graphs"/> using it.
        /// </summary>
        /// <param name="graphs">the series of graph to be drawn</param>
        private void DrawGraphs(List<Graph> graphs)
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

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GXLDirectory"/> in the configuration file.
        /// </summary>
        private const string gxlDirectoryLabel = "GXLDirectory";

        /// <summary>
        /// Label of attribute <see cref="MaxRevisionsToLoad"/> in the configuration file.
        /// </summary>
        private const string maxRevisionsToLoadLabel = "MaxRevisionsToLoad";

        /// <summary>
        /// Label of attribute <see cref="MarkerHeight"/> in the configuration file.
        /// </summary>
        private const string markerHeightLabel = "MarkerHeight";

        /// <summary>
        /// Label of attribute <see cref="MarkerWidth"/> in the configuration file.
        /// </summary>
        private const string markerWidthLabel = "MarkerWidth";

        /// <summary>
        /// Label of attribute <see cref="AdditionBeamColor"/> in the configuration file.
        /// </summary>
        private const string additionBeamColorLabel = "AdditionBeamColor";

        /// <summary>
        /// Label of attribute <see cref="ChangeBeamColor"/> in the configuration file.
        /// </summary>
        private const string changeBeamColorLabel = "ChangeBeamColor";

        /// <summary>
        /// Label of attribute <see cref="DeletionBeamColor"/> in the configuration file.
        /// </summary>
        private const string deletionBeamColorLabel = "DeletionBeamColor";

        /// <summary>
        /// The same as in <see cref="SEECity"/>
        /// </summary>
        private const string dataProviderPathLabel = "data";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            DataProvider?.Save(writer, dataProviderPathLabel);
            //GXLDirectory.Save(writer, gxlDirectoryLabel);
            writer.Save(MaxRevisionsToLoad, maxRevisionsToLoadLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            DataProvider =
                GraphProvider<List<Graph>>.Restore(attributes, dataProviderPathLabel) as
                    PipelineGraphProvider<List<Graph>>;
            //GXLDirectory.Restore(attributes, gxlDirectoryLabel);
            ConfigIO.Restore(attributes, maxRevisionsToLoadLabel, ref MaxRevisionsToLoad);
        }
    }
}