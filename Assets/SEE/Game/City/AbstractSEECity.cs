using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.Serialization;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Tools;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Common abstract superclass of SEECity and SEECityEvolution.
    /// It maintains settings of the graph data needed at runtime. An AbstractSEECity is intended to
    /// be added to a game object in the scene as a component. That game object
    /// is the representation of a graph including the settings that have lead
    /// to its visualization.
    /// </summary>
    [Serializable]
    public abstract partial class AbstractSEECity : SerializedMonoBehaviour
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="AbstractSEECity.Save"/> and
        /// <see cref="Restore"/>,
        /// respectively (both declared in AbstractSEECityIO). You should also
        /// extend the test cases in TestConfigIO.

        /// <summary>
        /// The screen relative height to use for culling a game node [0-1].
        /// If the game node uses less than this percentage it will be culled.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float LODCulling = 0.001f;

        /// <summary>
        /// The path where the settings (the attributes of this class) are stored.
        /// </summary>
        [SerializeField, Tooltip("Path of configuration file."), FoldoutGroup(DataFoldoutGroup)]
        public FilePath ConfigurationPath = new FilePath();

        /// <summary>
        /// The path to project where the source code can be found.
        /// <see cref="SourceCodeDirectory"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private DirectoryPath sourceCodeDirectory = new DirectoryPath();

        /// <summary>
        /// The path to project where the source code can be found. This attribute
        /// is needed to show the source code of nodes and edges.
        /// </summary>
        [SerializeField, FoldoutGroup(DataFoldoutGroup)]
        [PropertyTooltip("Directory where the source code is located")]
        [HideReferenceObjectPicker]
        public DirectoryPath SourceCodeDirectory
        {
            get => sourceCodeDirectory;
            set
            {
                if (sourceCodeDirectory != value)
                {
                    sourceCodeDirectory = value;
                    ProjectPathChanged();
                }
            }
        }

        /// <summary>
        /// Will be called whenever a new value is assigned to <see cref="SourceCodeDirectory"/>.
        /// This gives our subclasses a chance to update their graphs.
        /// </summary>
        protected abstract void ProjectPathChanged();

        /// <summary>
        /// The solution path for our project. Abstractly, this is a configuration file
        /// of an IDE for a particular project. Concretely, if the IDE is Visual Studio,
        /// this is the VS solution file.
        /// </summary>
        [SerializeField, Tooltip("Path of VS solution file."), FoldoutGroup(DataFoldoutGroup)]
        public FilePath SolutionPath = new FilePath();

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        [OdinSerialize, Tooltip("Edge types of hierarchical edges.")]
        public HashSet<string> HierarchicalEdges = HierarchicalEdgeTypes(); // serialized by Odin

        /// <summary>
        /// A mapping of all node types of the nodes in the graph onto whether
        /// they should be visualized or not and if so, how.
        /// </summary>
        [OdinSerialize, Tooltip("Visual attributes of nodes.")]
        [DictionaryDrawerSettings(KeyLabel = "Node type", ValueLabel = "Visual attributes", DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
        public Dictionary<string, VisualNodeAttributes> NodeTypes = new Dictionary<string, VisualNodeAttributes>();

        /// <summary>
        /// A mapping of node metric names onto colors.
        /// </summary>
        [Tooltip("Maps metric names onto colors."), FoldoutGroup(MetricFoldoutGroup), HideReferenceObjectPicker]
        [NonSerialized, OdinSerialize]
        public ColorMap MetricToColor = new ColorMap();

        /// <summary>
        /// Returns the <see cref="ColorRange"/> for <paramref name="metricName"/> in <see cref="MetricToColor"/>
        /// if one exists; otherwise <see cref="ColorRange.Default()"/> is returned.
        /// </summary>
        /// <param name="metricName">name of a metric</param>
        /// <returns><see cref="ColorRange"/> for <paramref name="metricName"/></returns>
        public ColorRange GetColorForMetric(string metricName)
        {
            if (MetricToColor.TryGetValue(metricName, out ColorRange color))
            {
                return color;
            }
            else
            {
                Debug.LogWarning($"No specification of color for node metric {metricName}. Using a default.\n");
                return ColorRange.Default();
            }
        }

        /// <summary>
        /// Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        /// for range [0, max-value] is used, where max-value is the maximum value of a metric.
        /// </summary>
        [Tooltip("Whether metrics should be normalized by Z score."), FoldoutGroup(MetricFoldoutGroup)]
        public bool ZScoreScale = false;

        /// <summary>
        /// If true, only the metrics of leaf nodes are scaled.
        /// </summary>
        [Tooltip("Whether only leaf metrics should be normalized."), FoldoutGroup(MetricFoldoutGroup)]
        public bool ScaleOnlyLeafMetrics = true;

        /// <summary>
        /// The node layout settings.
        /// </summary>
        [Tooltip("Settings for the node layout.")]
        public NodeLayoutAttributes NodeLayoutSettings = new NodeLayoutAttributes();

        /// <summary>
        /// The edge layout settings.
        /// </summary>
        [Tooltip("Settings for the edge layout.")]
        public EdgeLayoutAttributes EdgeLayoutSettings = new EdgeLayoutAttributes();

        /// <summary>
        /// Attributes regarding the selection of edges.
        /// </summary>
        [Tooltip("Settings for the selection of edges.")]
        public EdgeSelectionAttributes EdgeSelectionSettings = new EdgeSelectionAttributes();

        /// <summary>
        /// The cose graph settings.
        /// </summary>
        [HideInInspector]
        [Obsolete]
        public CoseGraphAttributes CoseGraphSettings = new CoseGraphAttributes(); // FIXME put into CitySettings.cs

        /// <summary>
        /// The metrics for the visualization of erosions.
        /// </summary>
        [Tooltip("Settings for the visualization of software erosions.")]
        public ErosionAttributes ErosionSettings = new ErosionAttributes();

        /// <summary>
        /// Adds all game objects tagged by <see cref="Tags.Node"/> or <see cref="Tags.Edge"/>
        /// of this game object including its descendants to <see cref="GraphElementIDMap"/>.
        /// </summary>
        protected virtual void Awake()
        {
            UpdateGraphElementIDMap(gameObject);
        }

        /// <summary>
        /// Called at game start. Sets up additional components.
        /// </summary>
        protected virtual void Start()
        {
            if (!gameObject.TryGetComponent(out EdgeMeshScheduler _))
            {
                gameObject.AddComponent<EdgeMeshScheduler>()
                    .Init(EdgeLayoutSettings, EdgeSelectionSettings);
            }
        }

        /// <summary>
        /// Adds all game objects tagged by <see cref="Tags.Node"/> or <see cref="Tags.Edge"/>
        /// of <paramref name="parent"/> including its descendants to <see cref="GraphElementIDMap"/>.
        /// </summary>
        /// <param name="parent">root node of the game-object tree to be added to <see cref="GraphElementIDMap"/></param>
        protected static void UpdateGraphElementIDMap(GameObject parent)
        {
            if (parent.CompareTag(Tags.Node) || parent.CompareTag(Tags.Edge))
            {
                GraphElementIDMap.Add(parent);
            }
            foreach (Transform child in parent.transform)
            {
                UpdateGraphElementIDMap(child.gameObject);
            }
        }

        /// <summary>
        /// Saves the settings of this code city to <see cref="ConfigurationPath"/>.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(ConfigurationButtonsGroup)]
        [PropertyOrder(ConfigurationButtonsGroupSave)]
        public void SaveConfiguration()
        {
            Save(ConfigurationPath.Path);
        }

        /// <summary>
        /// Loads the settings of this code city from <see cref="ConfigurationPath"/>.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(ConfigurationButtonsGroup)]
        [PropertyOrder(ConfigurationButtonsGroupLoad)]
        public void LoadConfiguration()
        {
            Load(ConfigurationPath.Path);
        }

        /// <summary>
        /// Saves the settings of this code city to <paramref name="filename"/>
        /// </summary>
        /// <param name="filename">name of the file in which the settings are stored</param>
        public void Save(string filename)
        {
            using ConfigWriter writer = new ConfigWriter(filename);
            Save(writer);
        }

        /// <summary>
        /// Reads the settings of this city from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">name of the file from which the settings are restored</param>
        public void Load(string filename)
        {
            using ConfigReader stream = new ConfigReader(filename);
            Restore(stream.Read());
        }

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        public static HashSet<string> HierarchicalEdgeTypes()
        {
            HashSet<string> result = new HashSet<string>
            {
                "Enclosing",
                "Belongs_To",
                "Part_Of",
                "Defined_In"
            };
            return result;
        }

        /// <summary>
        /// Resets everything that is specific to a given graph. Here:
        /// all game objects created for this city.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup)]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public virtual void Reset()
        {
            DeleteGraphGameObjects();
        }

        /// <summary>
        /// Resets the selected node types to be visualized.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Node-Type Settings")]
        [ButtonGroup(ResetButtonsGroup)]
        [PropertyOrder(ResetButtonsGroupOrderReset + 1)]
        public void ResetSelectedNodeTypes()
        {
            NodeTypes.Clear();
        }

        /// <summary>
        /// Deletes all game objects that were created for rendering nodes or edges
        /// of the graph or any decoration thereof. More precisely, all transitive descendants of this
        /// game object tagged by Tags.Node, Tags.Edge, or Tags.Decoration are destroyed
        /// (in editor mode or play mode).
        /// The underlying loaded graph is not changed.
        /// </summary>
        protected void DeleteGraphGameObjects()
        {
            // Delete all descendants.
            // Note: foreach (GameObject child in transform)... would not work;
            // we really need to collect all descendants first and only then can destroy each.
            foreach (GameObject descendant in AllDescendantsTaggedBy(gameObject, new string[] { Tags.Node, Tags.Edge, Tags.Decoration }))
            {
                // descendant may have already been destroyed as part of a prior destruction
                // of another descendant
                if (descendant != null)
                {
                    descendant.transform.parent = null;
                    Destroyer.DestroyGameObject(descendant);
                }
            }
        }

        /// <summary>
        /// Returns all (transitive) descendants (tagged by <see cref="Tags.Node"/>, <see cref="Tags.Edge"/>
        /// or <see cref="Tags.Decoration"/)> of <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">game objects whose descendants are required</param>
        /// <returns>(transitive) descendants (nodes, edges, or decorations) of the game
        /// object this SEECity is attached to</returns>
        private static ICollection<GameObject> AllDescendantsTaggedBy(GameObject gameObject, string[] tags)
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in gameObject.transform)
            {
                if (tags.Contains(child.tag))
                {
                    result.Add(child.gameObject);
                }
                result.AddRange(AllDescendantsTaggedBy(child.gameObject, tags));
            }
            return result;
        }

        /// <summary>
        /// Returns all (transitive) descendants of <paramref name="go"/> that are tagged
        /// by Tags.Node (including <paramref name="go"/> if it is tagged by Tags.Node).
        /// </summary>
        /// <param name="go">game object whose node descendants are required</param>
        /// <returns>all node descendants of <paramref name="go"/></returns>
        protected static ICollection<GameObject> AllNodeDescendants(GameObject go)
        {
            return AllDescendantsTaggedBy(go, new string[] { Tags.Node });
        }

        /// <summary>
        /// True if all node types in nodeTypes are relevant.
        /// </summary>
        private bool AllNodeTypesAreRelevant => NodeTypes.Values.All(nodeAttributes => nodeAttributes.IsRelevant);

        /// <summary>
        /// If <paramref name="graph"/> is null, nothing happens. Otherwise:
        /// Inspects the node types that occur in the graph and updates <see cref="NodeTypes"/>.
        /// All new node types are considered relevant initially. If <paramref name="graph"/> contains
        /// a node type that existed in <see cref="NodeTypes"/> before, that node type's
        /// selection information will be re-used. If <see cref="NodeTypes"/> contains a node
        /// type not contained in <paramref name="graph"/>, it will be removed from <see cref="NodeTypes"/>.
        ///
        /// The node types can be retrieved and also be marked as irrelevant later via property
        /// <see cref="NodeTypes"/>.
        /// </summary>
        /// <param name="graph">graph from which to retrieve the node types (may be null)</param>
        public void InspectSchema(Graph graph)
        {
            if (graph != null)
            {
                /// <see cref="NodeTypes"/> contains the node types of the previously loaded graph.
                /// Node types in <see cref="NodeTypes"/> not in the graph will disappear
                /// because we are iterating only over those.
                Dictionary<string, VisualNodeAttributes> newNodeTypes = new Dictionary<string, VisualNodeAttributes>();
                foreach (string type in graph.AllNodeTypes())
                {
                    // preserve existing node types and create new entry for types not yet seen
                    if (NodeTypes.TryGetValue(type, out VisualNodeAttributes value))
                    {
                        newNodeTypes[type] = value;
                    }
                    else
                    {
                        newNodeTypes[type] = new VisualNodeAttributes(type);
                    }
                }
                NodeTypes = newNodeTypes;

                // TO BE DECIDED: The following code will list all available metrics.
                // That may be convenient for a user. However, a GXL file may have
                // hundreds of metrics. And then all would be listed in the inspector.
                /// Update <see cref="MetricToColor"/>.
                //ColorMap newMetricToColor = new ColorMap();
                //foreach (string metric in graph.AllNumericNodeAttributes())
                //{
                //    if (MetricToColor.TryGetValue(metric, out Color color))
                //    {
                //        newMetricToColor[metric] = color;
                //    }
                //    else
                //    {
                //        newMetricToColor[metric] = Color.white;
                //    }
                //}
                //MetricToColor = newMetricToColor;
            }
        }

        /// <summary>
        /// Returns a subgraph of <paramref name="graph"/> where all nodes were
        /// removed that have a type considered to be irrelevant. If all node
        /// types are considered relevant, <paramref name="graph"/> will be returned.
        /// If not all types are considered relevant, a copied subgraph is returned.
        /// </summary>
        /// <param name="graph">graph whose subgraph is requested</param>
        /// <returns>subgraph of <paramref name="graph"/> (copy) or <paramref name="graph"/></returns>
        public Graph RelevantGraph(Graph graph)
        {
            if (AllNodeTypesAreRelevant)
            {
                return graph;
            }
            else
            {
                ICollection<string> matches = NodeTypes.Where(pair => pair.Value.IsRelevant)
                  .Select(pair => pair.Key).ToList();
                return graph.SubgraphByNodeType(matches);
            }
        }

        /// <summary>
        /// Returns all attribute names of the different kinds of software erosions.
        /// </summary>
        /// <returns>all attribute names of the different kinds of software erosions</returns>
        public IList<string> AllLeafIssues() =>
            new List<string>
               {
                  ErosionSettings.ArchitectureIssue,
                  ErosionSettings.CloneIssue,
                  ErosionSettings.CycleIssue,
                  ErosionSettings.Dead_CodeIssue,
                  ErosionSettings.MetricIssue,
                  ErosionSettings.StyleIssue,
                  ErosionSettings.UniversalIssue
               };

        /// <summary>
        /// Returns all attribute names of the different kinds of software erosions for inner
        /// nodes (the sums of their descendants).
        /// </summary>
        /// <returns>all attribute names of the different kinds of software erosions for inner nodes</returns>
        public IList<string> AllInnerNodeIssues() =>
            new List<string>
            {
                  ErosionSettings.ArchitectureIssue_SUM,
                  ErosionSettings. CloneIssue_SUM,
                  ErosionSettings.CycleIssue_SUM,
                  ErosionSettings.Dead_CodeIssue_SUM,
                  ErosionSettings.MetricIssue_SUM,
                  ErosionSettings.StyleIssue_SUM,
                  ErosionSettings.UniversalIssue_SUM
               };

        /// <summary>
        /// Returns the names of all node metrics that truly exist in the underlying
        /// graph, that is, there is at least one node in the graph that has this
        /// metric.
        /// </summary>
        /// <returns>names of all existing node metrics</returns>
        public abstract ISet<string> AllExistingMetrics();

        /// <summary>
        /// Yields a mapping of all node attribute names that define erosion issues
        /// for leaf nodes in the GXL file onto the icons to be used for visualizing them.
        /// </summary>
        /// <returns>mapping of all node attribute names for leaves onto icon ids</returns>
        public Dictionary<string, IconFactory.Erosion> LeafIssueMap() =>
            new Dictionary<string, IconFactory.Erosion>
            {
                { ErosionSettings.ArchitectureIssue, IconFactory.Erosion.Architecture_Violation },
                { ErosionSettings.CloneIssue, IconFactory.Erosion.Clone },
                { ErosionSettings.CycleIssue, IconFactory.Erosion.Cycle },
                { ErosionSettings.Dead_CodeIssue, IconFactory.Erosion.Dead_Code },
                { ErosionSettings.MetricIssue, IconFactory.Erosion.Metric },
                { ErosionSettings.StyleIssue, IconFactory.Erosion.Style },
                { ErosionSettings.UniversalIssue, IconFactory.Erosion.Universal }
            };

        /// <summary>
        /// Yields a mapping of all node attribute names that define erosion issues
        /// for inner nodes onto the icons to be used for visualizing them.
        /// These are usually the same attributes from <see cref="LeafIssueMap"/>, appended with
        /// <see cref="MetricAggregator.SUM_EXTENSION"/>, i.e., they represent the aggregated issue metrics.
        /// </summary>
        /// <returns>mapping of all node attribute names for inner nodes onto icon ids</returns>
        public Dictionary<string, IconFactory.Erosion> InnerIssueMap() =>
            LeafIssueMap().Select(x => (Key: x.Key + MetricAggregator.SUM_EXTENSION, x.Value))
                          .ToDictionary(x => x.Key, x => x.Value);

        /// <summary>
        /// Loads and returns the graph data from the GXL file with given <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">GXL filename from which to load the graph</param>
        /// <param name="rootName">the name of the artificial root if any needs to be added;
        /// if null is given, <paramref name="filename"/> will be used instead</param>
        /// <returns>the loaded graph (may be empty if a graph could not be loaded)</returns>
        protected Graph LoadGraph(string filename, string rootName = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogError("Empty graph path.\n");
                Graph graph = new Graph(SourceCodeDirectory.Path);
                return graph;
            }

            if (File.Exists(filename))
            {
                Performance p = Performance.Begin("loading graph data from " + filename);
                GraphReader graphCreator = new GraphReader(filename, HierarchicalEdges,
                                                           basePath: SourceCodeDirectory.Path,
                                                           rootID: rootName ?? filename,
                                                           logger: new SEELogger());
                graphCreator.Load();
                Graph graph = graphCreator.GetGraph();
                p.End();
                Debug.Log($"Loaded graph data for city {name} from {filename} successfully:\n"
                          + $"Number of nodes: {graph.NodeCount}\n"
                          + $"Number of edges: {graph.EdgeCount}\n"
                          + $"Elapsed time: {p.GetElapsedTime()} [h:m:s:ms]\n");
                LoadDataForGraphListing(graph);
                return graph;
            }
            else
            {
                Debug.LogError($"GXL file {filename} of city {name} does not exist.\n");
                return new Graph(SourceCodeDirectory.Path);
            }
        }

        /// <summary>
        /// Lists the metrics for each node type.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "List Node Metrics")]
        [ButtonGroup(ResetButtonsGroup)]
        [PropertyOrder(ResetButtonsGroupOrderReset + 2)]
        private void ListNodeMetrics()
        {
            DumpNodeMetrics();
        }

        /// <summary>
        /// Dumps the metric names of all node types of the currently loaded graph.
        /// </summary>
        protected abstract void DumpNodeMetrics();

        /// <summary>
        /// Emits all known metric names for each node types in any of the <paramref name="graphs"/>
        /// to the console.
        /// </summary>
        /// <param name="graphs">graphs whose metric names are to be emitted</param>
        protected static void DumpNodeMetrics(ICollection<Graph> graphs)
        {
            IDictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();

            foreach (Graph graph in graphs)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (result.TryGetValue(node.Type, out HashSet<string> metrics))
                    {
                        metrics.UnionWith(node.AllMetrics());
                    }
                    else
                    {
                        result[node.Type] = node.AllMetrics();
                    }
                }
            }

            foreach (KeyValuePair<string, HashSet<string>> item in result)
            {
                Debug.Log($"Node type {item.Key}:\n");
                foreach (string metric in item.Value)
                {
                    Debug.Log($"  metric {metric}\n");
                }
            }
        }

        /// <summary>
        /// Saves all data needed for the listing of the dirs in gui in cosegraphSettings
        /// </summary>
        /// <param name="graph"></param>
        public void LoadDataForGraphListing(Graph graph)
        {
            if (NodeLayoutSettings.Kind == NodeLayoutKind.CompoundSpringEmbedder)
            {
                Dictionary<string, bool> dirs = CoseGraphSettings.ListInnerNodeToggle;
                // the new directories
                Dictionary<string, bool> dirsLocal = new Dictionary<string, bool>();

                Dictionary<string, NodeLayoutKind> dirsLayout = new Dictionary<string, NodeLayoutKind>();
                Dictionary<string, NodeShapes> dirsShape = new Dictionary<string, NodeShapes>();

                foreach (Node node in graph.Nodes())
                {
                    if (!node.IsLeaf())
                    {
                        dirsShape.Add(node.ID, NodeTypes[node.Type].Shape);
                        dirsLocal.Add(node.ID, false);
                        dirsLayout.Add(node.ID, NodeLayoutSettings.Kind);
                    }
                }

                // if the key isn't in the old dictionaries
                //dirsLocal = dirsLocal.Where(i => !dirs.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);

                bool diff1 = dirs.Keys.Except(dirsLocal.Keys).Any();
                bool diff2 = dirsLocal.Keys.Except(dirs.Keys).Any();

                if (dirs.Count != dirsLocal.Count || diff1 || diff2)
                {
                    CoseGraphSettings.InnerNodeShape = dirsShape;
                    CoseGraphSettings.InnerNodeLayout = dirsLayout;
                    CoseGraphSettings.ListInnerNodeToggle = dirsLocal;
                }

                CoseGraphSettings.LoadedForNodeTypes = NodeTypes.Where(type => type.Value.IsRelevant)
                                                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IsRelevant);
            }
        }

        //----------------------------------------------------------------
        // Odin Inspector Attributes
        //----------------------------------------------------------------

        /// <summary>
        /// Name of the Inspector foldout group for the data setttings.
        /// </summary>
        protected const string DataFoldoutGroup = "Data";

        /// <summary>
        /// The name of the group for the Inspector buttons managing the data.
        /// </summary>
        protected const string DataButtonsGroup = "DataButtonsGroup";

        /// <summary>
        /// The order of the Load button in the button group <see cref="DataButtonsGroup"/>.
        /// </summary>
        protected const float DataButtonsGroupOrderLoad = 1;

        /// <summary>
        /// The order of the Draw button in the button group <see cref="DataButtonsGroup"/>.
        /// </summary>
        protected const float DataButtonsGroupOrderDraw = DataButtonsGroupOrderLoad + 1;

        /// <summary>
        /// The order of the Save button in the button group <see cref="DataButtonsGroup"/>.
        /// </summary>
        protected const float DataButtonsGroupOrderSave = DataButtonsGroupOrderDraw + 1;

        /// <summary>
        /// The order of the Save-Layout button in the button group <see cref="DataButtonsGroup"/>.
        /// </summary>
        protected const float DataButtonsGroupOrderSaveLayout = DataButtonsGroupOrderSave + 1;

        /// <summary>
        /// The name of the group for the Inspector buttons resettting the data.
        /// </summary>
        protected const string ResetButtonsGroup = "ResetButtonsGroup";

        /// <summary>
        /// The order of <see cref="Reset"/> in the button group <see cref="ResetButtonsGroup"/>.
        /// </summary>
        protected const float ResetButtonsGroupOrderReset = 1;

        /// <summary>
        /// The name of the group for the Inspector buttons managing the configuration file.
        /// </summary>
        protected const string ConfigurationButtonsGroup = "ConfigurationButtonsGroup";

        /// <summary>
        /// The order of the Load button in the button group <see cref="ConfigurationButtonsGroup"/>.
        /// </summary>
        protected const float ConfigurationButtonsGroupLoad = 1;

        /// <summary>
        /// The order of the Load button in the button group <see cref="ConfigurationButtonsGroup"/>.
        /// </summary>
        protected const float ConfigurationButtonsGroupSave = ConfigurationButtonsGroupLoad + 1;

        /// <summary>
        /// Name of the Inspector foldout group for the metric setttings.
        /// </summary>
        protected const string MetricFoldoutGroup = "Metric settings";
    }
}
