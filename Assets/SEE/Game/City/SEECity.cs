using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.UI.RuntimeConfigMenu;
using SEE.GO;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using SEE.Game.CityRendering;
using SEE.UI;
using SEE.Utils.Config;
using Sirenix.Serialization;
using SEE.GraphProviders;
using SEE.UI.Notification;
using SEE.DataModel.DG.IO;
using SEE.DataModel;

namespace SEE.Game.City
{
    /// <summary>
    /// Manages settings of the graph data showing a single version of a software
    /// system needed at runtime.
    /// </summary>
    public class SEECity : AbstractSEECity
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="SEECity.Save(ConfigWriter)"/> and
        /// <see cref="SEECity.Restore(Dictionary{string,object})"/>,
        /// respectively. You should also extend the test cases in TestConfigIO.

        /// <summary>
        /// A provider of the data shown as code city.
        /// </summary>
        [OdinSerialize, ShowInInspector,
            Tooltip("A graph provider yielding the data to be visualized as a code city."),
            TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup),
            HideReferenceObjectPicker]
        public PipelineGraphProvider DataProvider = new();

        /// <summary>
        /// The graph that is visualized in the scene and whose visualization settings are
        /// managed here.
        /// We do not want to serialize it using Unity or Odin because both frameworks are
        /// insufficient for the highly recursive structure of all the graph objects.
        /// There are different points in time in which the underlying graph is created:
        /// (1) in the editor mode or (2) during the game. If the graph is created in the
        /// editor mode by a graph renderer, the graph renderer will attach the NodeRefs
        /// to the game objects representing the nodes. So all the information about nodes
        /// is available, for instance for the layouts or the inspector. During the game
        /// there are two different possible scenarios: (a) this SEECity is is created
        /// and configured at runtime or (b) the SEECity was created in the editor and then
        /// the game is started. For scenario (a), we expect the graph to be loaded and
        /// all NodeRefs be defined accordingly. For scenario (b), the graph attribute
        /// will not be serialized and, hence, be null. In that case, we load the graph
        /// from the GXL file, i.e., the GXL file is our persistent serialization we
        /// use to re-create the graph. We need, however, to set the NodeRefs at runtime.
        /// All that is being done in Start() below.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        /// <remarks>Do not use this field directly. Use <see cref="LoadedGraph"/> instead.</remarks>
        [NonSerialized]
        private Graph loadedGraph = null;

        /// <summary>
        /// The graph underlying this SEE city that was loaded from disk. May be null.
        /// If a new graph is assigned to this property, the selected node types will
        /// be updated, too.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        public override Graph LoadedGraph
        {
            get => loadedGraph;
            protected set
            {
                if (loadedGraph  != null)
                {
                    Reset();
                }
                Assert.IsNull(visualizedSubGraph);
                loadedGraph = value;
                InspectSchema(loadedGraph);
            }
        }

        /// <summary>
        /// Will be called whenever a new value is assigned to <see cref="ProjectPath"/>.
        /// In this case, we will update <see cref="loadedGraph.BasePath"/> with the
        /// new <see cref="ProjectPath.Path"/> if <see cref="loadedGraph"/> is not null.
        /// </summary>
        protected override void ProjectPathChanged()
        {
            if (LoadedGraph != null)
            {
                LoadedGraph.BasePath = SourceCodeDirectory.Path;
            }
        }

        /// <summary>
        /// A token that can be used to cancel the loading of the graph.
        /// </summary>
        protected CancellationTokenSource cancellationTokenSource = new();

        /// <summary>
        /// The graph to be visualized. It may be a subgraph of the loaded graph
        /// containing only nodes with relevant node types or the original <see cref="LoadedGraph"/>
        /// if all node types are relevant. It is null if no graph has been loaded yet
        /// (i.e. <see cref="LoadedGraph"/> is null).
        /// </summary>
        [NonSerialized]
        private Graph visualizedSubGraph = null;

        /// <summary>
        /// The graph to be visualized. It may be a subgraph of the loaded graph
        /// containing only nodes with relevant node types or the original <see cref="LoadedGraph"/>
        /// if all node types are relevant. It is null if no graph has been loaded yet
        /// (i.e. <see cref="LoadedGraph"/> is null).
        /// </summary>
        /// <remarks>Accessing this value has a side effect on <see cref="visualizedSubGraph"/>.
        /// If <see cref="LoadedGraph"/> is null, <see cref="visualizedSubGraph"/> will
        /// become null, too. If <see cref="LoadedGraph"/> is null and <see cref="visualizedSubGraph"/> is
        /// currently null, too, then <see cref="visualizedSubGraph"/> will be set to the
        /// <see cref="RelevantGraph(LoadedGraph)"/>.</remarks>
        protected Graph VisualizedSubGraph
        {
            get
            {
                if (LoadedGraph == null)
                {
                    visualizedSubGraph = null;
                    return null;
                }
                else if (visualizedSubGraph == null)
                {
                    visualizedSubGraph = RelevantGraph(LoadedGraph);
                    SetupCompoundSpringEmbedder(visualizedSubGraph);
                }
                return visualizedSubGraph;
            }
        }

        /// <summary>
        /// Sets up drawn city (if it has been drawn yet) and loads the metric board.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            loadedGraph = null;
            visualizedSubGraph = null;

            if (!gameObject.IsCodeCityDrawn())
            {
                Debug.LogWarning($"There is no drawn code city for {gameObject.name}.");
                return;
            }
            LoadAsync().Forget();
            return;

            async UniTaskVoid LoadAsync()
            {
                await LoadDataAsync();
                InitializeAfterDrawn();
                BoardSettings.LoadBoard();
            }
        }

        /// <summary>
        /// Sets the <see cref="NodeRef"/> and <see cref="EdgeRef"/>, respectively, for all
        /// game objects representing nodes or edges in the <see cref="VisualizedSubGraph"/>.
        ///
        /// Sets the toggle attribute <see cref="GraphElement.IsVirtualToggle"/> for all
        /// nodes and edges in <see cref="LoadedGraph"/> that are not in the <see cref="VisualizedSubGraph"/>.
        /// This toggle prevents them to be drawn. This is necessary because <see cref="LoadedGraph"/>
        /// and <see cref="VisualizedSubGraph"/> co-exist and the latter may only be a subgraph
        /// of the former.
        ///
        /// Note that this method may only be called after the code city has been drawn.
        /// </summary>
        protected virtual void InitializeAfterDrawn()
        {
            Assert.IsTrue(gameObject.IsCodeCityDrawn());
            Graph subGraph = VisualizedSubGraph;
            if (subGraph != null)
            {
                // All graph elements that are only in the LoadedGraph but not in the VisualizedSubGraph
                // are toggled as GraphElement.IsVirtualToggle. These are not intended to be drawn.
                // Because the graph elements stem from two different graphs (LoadedGraph versus subGraph),
                // we need to provide a suitable comparer taking into account only the ID.
                foreach (GraphElement graphElement in LoadedGraph.Elements().Except(subGraph.Elements(), new GraphElementIDComparer()))
                {
                    // All other elements are virtual, i.e., should not be drawn.
                    graphElement.SetToggle(GraphElement.IsVirtualToggle);
                }

                SetNodeEdgeRefs(subGraph, gameObject);
            }
            else
            {
                Debug.LogError($"Could not load city {name}.\n");
            }

            // Add EdgeMeshScheduler to convert edge lines to meshes over time.
            gameObject.AddOrGetComponent<EdgeMeshScheduler>().Init(EdgeLayoutSettings, EdgeSelectionSettings,
                                                                   subGraph);
            // This must be loadedGraph. It must not be LoadedGraph. The latter would reset the graph.
            loadedGraph = subGraph;

            UpdateGraphElementIDMap(gameObject);
        }

        /// <summary>
        /// Sets all NodeRefs and EdgeRefs for this city to the nodes and edges, respectively,
        /// they correspond to. This will take place for the graph elements of the
        /// <see cref="LoadedGraph"/> only.
        /// We assume that the game objects with a NodeRef/EdgeRef required to be
        /// defined to be immediate children of this SEECity. Moreover, we assume a child
        /// game object's name is the ID of the corresponding graph node/edge.
        /// </summary>
        public void SetNodeEdgeRefs()
        {
            if (LoadedGraph != null)
            {
                SetNodeEdgeRefs(loadedGraph, gameObject);
                Debug.Log($"Node and edge references for {gameObject.name} are resolved.\n");
            }
            else
            {
                Debug.LogError("No graph loaded.\n");
            }
        }

        /// <summary>
        /// Sets all NodeRefs and EdgeRefs for this city to the nodes and edges,
        /// respectively, they correspond to.
        /// We assume that the game objects with a NodeRef/EdgeRef required to be
        /// defined to be immediate children of this SEECity. Moreover, we assume a child
        /// game object's name is the ID of the corresponding graph node/edge.
        /// </summary>
        /// <param name="graph">graph giving us the nodes/edges who should be the
        /// target of the NodeRefs and EdgeRefs, respectively</param>
        protected static void SetNodeEdgeRefs(Graph graph, GameObject parent)
        {
            foreach (Transform childTransform in parent.transform)
            {
                GameObject child = childTransform.gameObject;
                if (child.TryGetComponent(out NodeRef nodeRef))
                {
                    nodeRef.Value = graph.GetNode(child.name);
                    if (nodeRef.Value == null)
                    {
                        Debug.LogWarning($"Could not resolve node reference {child.name}.\n");
                    }
                }
                else if (child.TryGetComponent(out EdgeRef edgeRef))
                {
                    edgeRef.Value = graph.GetEdge(child.name);
                    if (edgeRef.Value == null)
                    {
                        Debug.LogWarning($"Could not resolve edge reference {child.name}.\n");
                    }
                }
#if UNITY_EDITOR
                else if (child.CompareTag(Tags.Node) || child.CompareTag(Tags.Edge))
                {
                    Debug.LogWarning($"Game object {child.name} has neither node nor edge reference.\n");
                }
#endif
                SetNodeEdgeRefs(graph, child);
            }
        }

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath() and then draws it. Equivalent to:
        ///   LoadDataAsync();
        ///   DrawGraph();
        /// </summary>
        public virtual async UniTaskVoid LoadAndDrawGraphAsync()
        {
            await LoadDataAsync();
            DrawGraph();
        }

        /// <summary>
        /// First, if a graph was already loaded (<see cref="LoadedGraph"/> is not null),
        /// everything will be reset by calling <see cref="Reset"/>.
        /// Second, the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath() are loaded. The loaded graph is available
        /// in <see cref="LoadedGraph"/> afterwards.
        ///
        /// This method loads only the data, but does not actually render the graph.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Load Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Data")]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public virtual async UniTask LoadDataAsync()
        {
            if (DataProvider != null)
            {
                try
                {
                    using (LoadingSpinner.ShowIndeterminate($"Loading city \"{gameObject.name}\""))
                    {
                        LoadedGraph = await DataProvider.ProvideAsync(new Graph(""), this, x => ProgressBar = x,
                                                                      cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    ShowNotification.Warn("Data loading cancelled", "Data loading was cancelled.\n", log: true);
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    ShowNotification.Error("Data failure", $"Graph provider failed with: {ex.Message}\n", log: false);
                    throw;
                }
            }
            else
            {
                ShowNotification.Error("No data provider", "You must set a data provider before you can load the data.");
            }
        }

        /// <summary>
        /// Saves the graph data to a GXL file.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Save Data")]
        [PropertyOrder(DataButtonsGroupOrderSave)]
        [EnableIf(nameof(IsGraphLoaded))]
        public virtual void SaveData()
        {
            string outputFile = Application.streamingAssetsPath + "/output.gxl";
            if (LoadedGraph != null)
            {
                GraphWriter.Save(outputFile, LoadedGraph, HierarchicalEdges.First());
                Debug.Log($"Data was saved to '{outputFile}'.\n");
            }
        }

        /// <summary>
        /// Re-draws the graph without deleting the underlying loaded graph.
        /// Only the game objects generated for the nodes are deleted first.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        public void ReDrawGraph()
        {
            if (LoadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                DeleteGraphGameObjects();
                DrawGraph();
            }
        }

        /// <summary>
        /// Returns whether the graph has been loaded.
        /// </summary>
        private bool IsGraphLoaded => loadedGraph != null;

        /// <summary>
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Draw Data")]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        [EnableIf(nameof(IsGraphLoaded))]
        public virtual void DrawGraph()
        {
            if (LoadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                Graph theVisualizedSubGraph = VisualizedSubGraph;
                if (ReferenceEquals(theVisualizedSubGraph, null))
                {
                    Debug.LogError("No graph loaded.\n");
                }
                else
                {
                    using (LoadingSpinner.ShowIndeterminate($"Drawing city \"{gameObject.name}\""))
                    {
                        graphRenderer = new GraphRenderer(this, theVisualizedSubGraph);
                        // We assume here that this SEECity instance was added to a game object as
                        // a component. The inherited attribute gameObject identifies this game object.
                        graphRenderer.DrawGraph(theVisualizedSubGraph, gameObject);

                        // If we're in editmode, InitializeAfterDrawn() will be called by Start() once the
                        // game starts. Otherwise, in playmode, we have to call it ourselves.
                        if (Application.isPlaying)
                        {
                            InitializeAfterDrawn();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The graph renderer used to draw the city.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        private GraphRenderer graphRenderer;

        /// <summary>
        /// Yields the graph renderer that draws this city.
        /// </summary>
        /// <remarks>Implements <see cref="AbstractSEECity.Renderer"/>.</remarks>
        public override IGraphRenderer Renderer => graphRenderer ??= new GraphRenderer(this, VisualizedSubGraph);

        /// <summary>
        /// Saves the current layout of the city in a file named <see cref="LayoutPath"/>.
        /// The format of the written file depends upon the file extension. If the extension
        /// is <see cref="Filenames.GVLExtension"/> it is saved in the GVL format; otherwise
        /// the file is saved in the SLD format.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Save Layout")]
        [PropertyOrder(DataButtonsGroupOrderSaveLayout)]
        public void SaveLayout()
        {
            string path = NodeLayoutSettings.LayoutPath.Path;
            Debug.Log($"Saving layout data to {path}.\n");
            if (Filenames.HasExtension(path, Filenames.GVLExtension))
            {
                Layout.IO.GVLWriter.Save(path, LoadedGraph.Name, AllNodeDescendants(gameObject));
            }
            else
            {
                Layout.IO.SLDWriter.Save(path, AllNodeDescendants(gameObject));
            }
        }

        /// <summary>
        /// Reads the a saved layout of the city from a file named <see cref="LayoutPath"/>.
        /// The format of the written file depends upon the file extension. If the extension
        /// is <see cref="Filenames.GVLExtension"/> it is expected to be in the GVL format; otherwise
        /// the file is assumed to be in the SLD format.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup), RuntimeButton(DataButtonsGroup, "Load Layout")]
        [PropertyOrder(DataButtonsGroupOrderLoadLayout)]
        public void LoadLayout()
        {
            ICollection<GameObject> gameNodes = AllNodeDescendants(gameObject);
            graphRenderer.LoadLayout(gameNodes, gameObject.transform.position.y);
        }

        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the selected node types,
        /// the underlying and visualized graph, and all game objects visualizing information about it.
        /// </summary>
        /// <remarks>This method should be called whenever <see cref="loadedGraph"/> is re-assigned.</remarks>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset Data")]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Cancel any ongoing loading operation and reset the token.
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            // Delete the underlying graph.
            loadedGraph?.Destroy();
            loadedGraph = null;
            visualizedSubGraph = null;
        }

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph, that is, there is at least one node in the graph that has this
        /// metric.
        ///
        /// If no graph has been loaded yet, the empty list will be returned.
        /// </summary>
        /// <returns>names of all existing node metrics</returns>
        public override ISet<string> AllExistingMetrics()
        {
            if (LoadedGraph == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return LoadedGraph.AllNumericNodeAttributes();
            }
        }

        /// <summary>
        /// Dumps the metric names of all node types of the currently loaded graph.
        /// </summary>
        protected override void DumpNodeMetrics()
        {
            if (LoadedGraph == null)
            {
                Debug.Log("No graph loaded yet.");
            }
            else
            {
                DumpNodeMetrics(new List<Graph>() { loadedGraph });
            }
        }

        #region Config I/O
        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="DataProvider"/> in the configuration file.
        /// </summary>
        private const string dataProviderPathLabel = "data";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            DataProvider?.Save(writer, dataProviderPathLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            DataProvider = GraphProvider.Restore(attributes, dataProviderPathLabel) as PipelineGraphProvider;
        }
        #endregion
    }
}
