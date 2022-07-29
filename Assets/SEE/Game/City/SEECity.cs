using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

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
        /// The path to the GXL file containing the graph data.
        /// Note that any deriving class may use multiple GXL paths from which the single city is constructed.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of GXL file"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath GXLPath = new FilePath();

        /// <summary>
        /// The path to the CSV file containing the additional metric values.
        /// </summary>
        [SerializeField, ShowInInspector, Tooltip("Path of metric CSV file"), FoldoutGroup(DataFoldoutGroup)]
        public FilePath CSVPath = new FilePath();

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
        [NonSerialized]
        private Graph loadedGraph = null;

        /// <summary>
        /// The graph underlying this SEE city that was loaded from disk. May be null.
        /// If a new graph is assigned to this property, the selected node types will
        /// be updated, too.
        ///
        /// Neither serialized nor saved to the config file.
        /// </summary>
        public Graph LoadedGraph
        {
            get => loadedGraph;
            set
            {
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
            if (loadedGraph != null)
            {
                loadedGraph.BasePath = SourceCodeDirectory.Path;
            }
        }

        /// <summary>
        /// The graph to be visualized. It may be a subgraph of the loaded graph
        /// containing only nodes with relevant node types or the original LoadedGraph
        /// if all node types are relevant. It is null if no graph has been loaded yet
        /// (i.e. <see cref="LoadedGraph"/> is null).
        /// </summary>
        private Graph VisualizedSubGraph
        {
            get
            {
                if (loadedGraph == null)
                {
                    return null;
                }
                else
                {
                    Graph graph = RelevantGraph(loadedGraph);
                    LoadDataForGraphListing(graph);
                    return graph;
                }
            }
        }

        /// <summary>
        /// Loads the graph and metric data and sets all NodeRef and EdgeRef components to the
        /// loaded nodes and edges. This "deserializes" the graph to make it available at runtime.
        /// Note: <see cref="LoadedGraph"/> will be <see cref="VisualizedSubGraph"/> afterwards,
        /// that is, if node types are filtered, <see cref="LoadedGraph"/> may not contain all
        /// nodes saved in the underlying GXL file.
        /// </summary>
        protected void Awake()
        {
            LoadData();
            loadedGraph = VisualizedSubGraph;
            if (loadedGraph != null)
            {
                SetNodeEdgeRefs(loadedGraph, gameObject);
            }
            else
            {
                Debug.LogError($"SEECity.Awake: Could not load city {name}.\n");
            }
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
            if (loadedGraph != null)
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
                else if (child.CompareTag(DataModel.Tags.Node) || child.CompareTag(DataModel.Tags.Edge))
                {
                    Debug.LogWarning($"Game object {child.name} has neither node nor edge reference.\n");
                }
#endif
                SetNodeEdgeRefs(graph, child);
            }
        }

        /// <summary>
        /// Loads the metrics from CSVPath() and aggregates and adds them to the graph.
        /// Precondition: graph must have been loaded before.
        /// </summary>
        private void LoadMetrics()
        {
            LoadGraphMetrics(LoadedGraph, CSVPath.Path, ErosionSettings).Forget();
        }

        /// <summary>
        /// Loads the metrics available at the CSV file <paramref name="csvPath"/> into the given
        /// <paramref name="graph"/>. Depending on <paramref name="erosionSettings"/>, metrics will also be integrated
        /// from the Axivion Dashboard.
        /// </summary>
        /// <param name="graph">The graph into which the metrics shall be loaded</param>
        /// <param name="csvPath">The CSV file containing the metrics for the given <paramref name="graph"/></param>
        /// <param name="erosionSettings">
        /// Will be used to determine whether metric data from the Axivion Dashboard shall be imported into the graph.
        /// For this, <see cref="ErosionAttributes.LoadDashboardMetrics"/>,
        /// <see cref="ErosionAttributes.OverrideMetrics"/>, and <see cref="erosionSettings.IssuesAddedFromVersion"/>
        /// will be used.
        /// </param>
        /// <remarks>
        /// Note that the import of metrics from the dashboard will happen asynchronously due to
        /// involving a network call. If you simply want to call it synchronously without querying the dashboard,
        /// set <paramref name="erosionSettings"/> to an appropriate value and use <c>LoadGraphMetrics.Forget()</c>.
        /// </remarks>
        protected static async UniTask LoadGraphMetrics(Graph graph, string csvPath, ErosionAttributes erosionSettings)
        {
            Performance p = Performance.Begin($"loading metric data data from CSV file {csvPath}");
            int numberOfErrors = MetricImporter.LoadCsv(graph, csvPath);
            if (numberOfErrors > 0)
            {
                Debug.LogWarning($"CSV file {csvPath} has {numberOfErrors} many errors.\n");
            }
            p.End();

            // Substitute missing values from the dashboard
            if (erosionSettings.LoadDashboardMetrics)
            {
                string startVersion = string.IsNullOrEmpty(erosionSettings.IssuesAddedFromVersion) ?
                    "EMPTY" : erosionSettings.IssuesAddedFromVersion;
                Debug.Log($"Loading metrics and added issues from the Axivion Dashboard for start version {startVersion}.\n");
                await MetricImporter.LoadDashboard(graph, erosionSettings.OverrideMetrics,
                                                   erosionSettings.IssuesAddedFromVersion);
            }
        }

        /// <summary>
        /// Loads the graph data from the GXL file with GXLPath() and the metrics
        /// from the CSV file with CSVPath() and then draws it. Equivalent to:
        ///   LoadData();
        ///   DrawGraph();
        /// </summary>
        public virtual void LoadAndDrawGraph()
        {
            LoadData();
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
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderLoad)]
        public virtual void LoadData()
        {
            if (string.IsNullOrEmpty(GXLPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else
            {
                if (LoadedGraph != null)
                {
                    Reset();
                }
                LoadedGraph = LoadGraph(GXLPath.Path);
                LoadMetrics();
            }
        }

        /// <summary>
        /// Saves the graph data to the GXL file with GXLPath().
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderSave)]
        public virtual void SaveData()
        {
            if (string.IsNullOrEmpty(GXLPath.Path))
            {
                Debug.LogError("Empty graph path.\n");
            }
            else if (LoadedGraph != null)
            {
                GraphWriter.Save(GXLPath.Path, LoadedGraph, HierarchicalEdges.First());
            }
        }

        /// <summary>
        /// Re-draws the graph without deleting the underlying loaded graph.
        /// Only the game objects generated for the nodes are deleted first.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        public void ReDrawGraph()
        {
            if (loadedGraph == null)
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
        /// Draws the graph.
        /// Precondition: The graph and its metrics have been loaded.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Draw Data")]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderDraw)]
        public virtual void DrawGraph()
        {
            if (loadedGraph == null)
            {
                Debug.LogError("No graph loaded.\n");
            }
            else
            {
                Graph visualizedSubGraph = VisualizedSubGraph;
                if (ReferenceEquals(visualizedSubGraph, null))
                {
                    Debug.LogError("No graph loaded.\n");
                }
                else
                {
                    graphRenderer = new GraphRenderer(this, visualizedSubGraph);
                    // We assume here that this SEECity instance was added to a game object as
                    // a component. The inherited attribute gameObject identifies this game object.
                    graphRenderer.DrawGraph(visualizedSubGraph, gameObject);
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
        /// Color property of the shader. Lookups using this value are more efficient than lookups using the
        /// string value "_Color".
        /// </summary>
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        /// <summary>
        /// Yields a graph renderer that can draw this city.
        /// </summary>
        public GraphRenderer Renderer => graphRenderer ??= new GraphRenderer(this, VisualizedSubGraph);

        /// <summary>
        /// Saves the current layout of the city in a file named <see cref="LayoutPath"/>.
        /// The format of the written file depends upon the file extension. If the extension
        /// is <see cref="Filenames.GVLExtension"/> it is saved in the GVL format; otherwise
        /// the file is saved in the SLD format.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(DataButtonsGroup)]
        [PropertyOrder(DataButtonsGroupOrderSaveLayout)]
        public void SaveLayout()
        {
            string path = NodeLayoutSettings.LayoutPath.Path;
            Debug.Log($"Saving layout data to {path}.\n");
            if (Filenames.HasExtension(path, Filenames.GVLExtension))
            {
                Layout.IO.GVLWriter.Save(path, loadedGraph.Name, AllNodeDescendants(gameObject));
            }
            else
            {
                Layout.IO.SLDWriter.Save(path, AllNodeDescendants(gameObject));
            }
        }

        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the selected node types,
        /// the underlying graph, and all game objects visualizing information about it.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup)]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public override void Reset()
        {
            base.Reset();
            // Delete the underlying graph.
            loadedGraph?.Destroy();
            LoadedGraph = null;
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
            if (loadedGraph == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return loadedGraph.AllNumericNodeAttributes();
            }
        }

        //--------------------------------
        // Configuration file input/output
        //--------------------------------

        /// <summary>
        /// Label of attribute <see cref="GXLPath"/> in the configuration file.
        /// </summary>
        private const string GXLPathLabel = "GXLPath";
        /// <summary>
        /// Label of attribute <see cref="CSVPath"/> in the configuration file.
        /// </summary>
        private const string CSVPathLabel = "CSVPath";

        protected override void Save(ConfigWriter writer)
        {
            base.Save(writer);
            GXLPath.Save(writer, GXLPathLabel);
            CSVPath.Save(writer, CSVPathLabel);
        }

        protected override void Restore(Dictionary<string, object> attributes)
        {
            base.Restore(attributes);
            GXLPath.Restore(attributes, GXLPathLabel);
            CSVPath.Restore(attributes, CSVPathLabel);
        }
    }
}
