using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Serialization;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SEE.UI.RuntimeConfigMenu;
using SEE.Game.CityRendering;
using SEE.Utils.Config;
using SEE.Utils.Paths;
using UnityEngine.Rendering;

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
        protected virtual void Awake()
        {
            LabelLineMaterial = new Material(LineMaterial(Color.white));
            AddRootNodeType();
        }

        protected virtual void Start()
        {
            // Intentionally left blank
        }

        protected virtual void Update()
        {
            // Intentionally left blank
        }

        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="AbstractSEECity.Save"/> and
        /// <see cref="Restore"/>,
        /// respectively (both declared in AbstractSEECityIO). You should also
        /// extend the test cases in TestConfigIO.
        /// <summary>
        /// The graph underlying this code city that was loaded from disk. May be null.
        /// Neither serialized nor saved to the config file.
        /// </summary>
        public abstract Graph LoadedGraph
        {
            get;
            protected set;
        }

        /// <summary>
        /// The screen relative height to use for culling a game node [0-1].
        /// If the game node uses less than this percentage it will be culled.
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float LODCulling = 0.001f;

        /// <summary>
        /// The path where the settings (the attributes of this class) are stored.
        /// </summary>
        [Tooltip("Path of configuration file."), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public DataPath ConfigurationPath = new();

        /// <summary>
        /// The path to project where the source code can be found.
        /// <see cref="SourceCodeDirectory"/>.
        /// </summary>
        [SerializeField, HideInInspector]
        private DataPath sourceCodeDirectory = new();

        /// <summary>
        /// The path to project where the source code can be found. This attribute
        /// is needed to show the source code of nodes and edges.
        /// </summary>
        [TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup), ShowInInspector]
        [PropertyTooltip("Directory where the source code is located")]
        [HideReferenceObjectPicker]
        public DataPath SourceCodeDirectory
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
        [Tooltip("Path of Visual Studio solution file."), TabGroup(DataFoldoutGroup), RuntimeTab(DataFoldoutGroup)]
        public DataPath SolutionPath = new();

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        [OdinSerialize, Tooltip("Edge types of hierarchical edges."), TabGroup(EdgeFoldoutGroup), RuntimeTab(EdgeFoldoutGroup)]
        public HashSet<string> HierarchicalEdges = HierarchicalEdgeTypes();

        [OdinSerialize, Tooltip("Edge types of hidden edges (will only be shown upon hovering)."),
         TabGroup(EdgeFoldoutGroup), RuntimeTab(EdgeFoldoutGroup)]
        public HashSet<string> HiddenEdges = new();

        /// <summary>
        /// A mapping of all node types of the nodes in the graph onto whether
        /// they should be visualized or not and if so, how.
        /// </summary>
        [NonSerialized, OdinSerialize, Tooltip("Visual attributes of nodes."), HideReferenceObjectPicker]
        [DictionaryDrawerSettings(KeyLabel = "Node type", ValueLabel = "Visual attributes",
                                  DisplayMode = DictionaryDisplayOptions.CollapsedFoldout), TabGroup(NodeFoldoutGroup), RuntimeTab(NodeFoldoutGroup)]
        public NodeTypeVisualsMap NodeTypes = new();

        /// <summary>
        /// Attributes to mark changes of nodes.
        /// </summary>
        [Tooltip("How changes of nodes should be marked."),
         TabGroup(NodeFoldoutGroup), RuntimeTab(NodeFoldoutGroup), HideReferenceObjectPicker]
        [NonSerialized, OdinSerialize]
        public MarkerAttributes MarkerAttributes = new();

        /// <summary>
        /// If true, lifted edges whose source and target nodes are the same are ignored.
        /// </summary>
        [Tooltip("If true, lifted edges whose source and target nodes are the same are ignored.")]
        public bool IgnoreSelfLoopsInLifting = false;

        /// <summary>
        /// The maximal height of a single antenna segment.
        /// </summary>
        [LabelText("Antenna Segment Height"), Tooltip("The maximal height of a single antenna segment.")]
        public float MaximalAntennaSegmentHeight = 0.5f;

        /// <summary>
        /// The width of an antenna.
        /// </summary>
        [Tooltip("The width of an antenna.")]
        public float AntennaWidth = 0.1f;

        /// <summary>
        /// The base duration of an animation.
        /// Animations will only apply factors to this base duration.
        /// </summary>
        [Tooltip("The base duration for all animations.")]
        [Range(0.0f, 5.0f)]
        public float BaseAnimationDuration = 1.0f;

        /// <summary>
        /// A mapping of node metric names onto colors.
        /// </summary>
        [Tooltip("Maps metric names onto colors."), TabGroup(MetricFoldoutGroup), RuntimeTab(MetricFoldoutGroup), HideReferenceObjectPicker]
        [NonSerialized, OdinSerialize]
        public ColorMap MetricToColor = new();

        /// <summary>
        /// A progress bar, shown in the editor.
        /// Can be used to indicate loading progress during city creation.
        /// </summary>
        [ProgressBar(0, 1, Height = 20, ColorGetter = nameof(GetProgressBarColor),
                     CustomValueStringGetter = "$" + nameof(ProgressBarValueString))]
        [PropertyOrder(999)]
        [ShowIf(nameof(ShowProgressBar))]
        [HideLabel]
        [ReadOnly]
        public float ProgressBar;

        /// <summary>
        /// Whether the progress bar should be shown.
        /// </summary>
        private bool ShowProgressBar => ProgressBar > 0;

        /// <summary>
        /// The string representation of the progress bar value.
        /// </summary>
        private string ProgressBarValueString => $"{ProgressBar * 100:F0}%";

        /// <summary>
        /// Returns the color for the progress bar, based on the current <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value (from 0–1) for which to determine the color.</param>
        /// <returns>The color for the progress bar.</returns>
        private static Color GetProgressBarColor(float value)
        {
            return Color.Lerp(Color.red, Color.green, Mathf.Pow(value, 2));
        }

        /// <summary>
        /// Yields a graph renderer that can draw this city.
        /// </summary>
        public abstract IGraphRenderer Renderer
        {
            get;
        }

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
        [Tooltip("Whether metrics should be normalized by Z score."), TabGroup(MetricFoldoutGroup), RuntimeTab(MetricFoldoutGroup)]
        public bool ZScoreScale = false;

        /// <summary>
        /// If true, only the metrics of leaf nodes are scaled.
        /// </summary>
        [Tooltip("Whether only leaf metrics should be normalized."), TabGroup(MetricFoldoutGroup), RuntimeTab(MetricFoldoutGroup)]
        public bool ScaleOnlyLeafMetrics = true;

        /// <summary>
        /// The node layout settings.
        /// </summary>
        [Tooltip("Settings for the node layout."), TabGroup(NodeFoldoutGroup), RuntimeTab(NodeFoldoutGroup)]
        public NodeLayoutAttributes NodeLayoutSettings = new();

        /// <summary>
        /// The edge layout settings.
        /// </summary>
        [Tooltip("Settings for the edge layout."), TabGroup(EdgeFoldoutGroup), RuntimeTab(EdgeFoldoutGroup)]
        public EdgeLayoutAttributes EdgeLayoutSettings = new();

        /// <summary>
        /// Attributes regarding the selection of edges.
        /// </summary>
        [Tooltip("Settings for the selection of edges."), TabGroup(EdgeFoldoutGroup), RuntimeTab(EdgeFoldoutGroup)]
        public EdgeSelectionAttributes EdgeSelectionSettings = new();

        /// <summary>
        /// The cose graph settings.
        /// </summary>
        [HideInInspector]
        [Obsolete]
        public CoseGraphAttributes CoseGraphSettings = new(); // FIXME put into CitySettings.cs

        /// <summary>
        /// The metrics for the visualization of erosions.
        /// </summary>
        [Tooltip("Settings for the visualization of software erosions."), TabGroup(ErosionFoldoutGroup), RuntimeTab(ErosionFoldoutGroup)]
        public ErosionAttributes ErosionSettings = new();

        /// <summary>
        /// Holistic metric boards.
        /// </summary>
        [Tooltip("Settings for holistic metric boards.")]
        public BoardAttributes BoardSettings = new();

        #region LabelLineMaterial

        /// <summary>
        /// The material for the line connecting a node and its label. We use exactly one material
        /// for all connecting lines within this city, different from the lines used for labels in
        /// other cities. This allows us to set the portal independently from other other cities.
        /// </summary>
        [HideInInspector]
        public Material LabelLineMaterial
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the material for the line connecting a node and its label.
        /// </summary>
        /// <param name="lineColor">the color for the requested line material</param>
        /// <returns>a new material for the line connecting a node and its label</returns>
        private static Material LineMaterial(Color lineColor)
        {
            return Materials.New(Materials.ShaderType.TransparentLine, lineColor, texture: null,
                                 renderQueueOffset: (int)(RenderQueue.Transparent + 1));
        }

        #endregion

        /// <summary>
        /// Recurses into the game-object hierarchy rooted by <paramref name="root"/> and adds
        /// all visited game objects (including <paramref name="root"/>) tagged by <see cref="Tags.Node"/>
        /// or <see cref="Tags.Edge"/> to <see cref="GraphElementIDMap"/>.
        /// </summary>
        /// <param name="root">root node of the game-object tree to be added to <see cref="GraphElementIDMap"/></param>
        /// <remarks>Generally, <paramref name="root"/> will be a game object representing a code city;
        /// that is, a game object where a <see cref="AbstractSEECity"/> component is attached to.</remarks>
        protected static void UpdateGraphElementIDMap(GameObject root)
        {
            if ((root.CompareTag(Tags.Node) || root.CompareTag(Tags.Edge)) && !GraphElementIDMap.Has(root.name))
            {
                GraphElementIDMap.Add(root);
            }
            foreach (Transform child in root.transform)
            {
                UpdateGraphElementIDMap(child.gameObject);
            }
        }

        /// <summary>
        /// Saves the settings of this code city to <see cref="ConfigurationPath"/>.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(ConfigurationButtonsGroup), RuntimeButton(ConfigurationButtonsGroup, "Save Configuration")]
        [PropertyOrder(ConfigurationButtonsGroupSave)]
        public void SaveConfiguration()
        {
            Save(ConfigurationPath.Path);
        }

        /// <summary>
        /// Loads the settings of this code city from <see cref="ConfigurationPath"/>.
        /// </summary>
        [Button(ButtonSizes.Small)]
        [ButtonGroup(ConfigurationButtonsGroup), RuntimeButton(ConfigurationButtonsGroup, "Load Configuration")]
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
            using ConfigWriter writer = new(filename);
            Save(writer);
        }

        /// <summary>
        /// Reads the settings of this city from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">name of the file from which the settings are restored</param>
        public void Load(string filename)
        {
            using ConfigReader stream = new(filename);
            Restore(stream.Read());
        }

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        public static HashSet<string> HierarchicalEdgeTypes() => new()
        {
            "Enclosing",
            "Belongs_To",
            "Part_Of",
            "Defined_In"
        };

        /// <summary>
        /// Resets everything that is specific to a given graph. Here:
        /// all game objects created for this city.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Data")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset Data")]
        [PropertyOrder(ResetButtonsGroupOrderReset)]
        public virtual void Reset()
        {
            DeleteGraphGameObjects();
            ProgressBar = 0;
        }

        /// <summary>
        /// Resets the selected node types to be visualized.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Reset Node-Type Settings")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Reset Node-Type Settings")]
        [PropertyOrder(ResetButtonsGroupOrderReset + 1)]
        public void ResetSelectedNodeTypes()
        {
            NodeTypes.Clear();
        }

        /// <summary>
        /// Dumps the content of <see cref="GraphElementIDMap"/>.
        /// Used for debugging.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Dump Map")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Dump Map")]
        [PropertyOrder(ResetButtonsGroupOrderReset + 2)]
        public void DumpGraphElementIDMap()
        {
            GraphElementIDMap.Dump();
        }

        /// <summary>
        /// Clears the content of <see cref="GraphElementIDMap"/>.
        /// Used for debugging.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "Clear Map")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "Clear Map")]
        [PropertyOrder(ResetButtonsGroupOrderReset + 3)]
        public void ClearGraphElementIDMap()
        {
            GraphElementIDMap.Clear();
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
            DestroyTree(gameObject);
        }

        /// <summary>
        /// Recurses into the game-object hierarchy rooted by <paramref name="parent"/>
        /// and destroys everything tagged by <see cref="Tags.Node"/>, <see cref="Tags.Edge"/>,
        /// or <see cref="Tags.Decoration"/>.
        /// </summary>
        /// <param name="parent">root of the game-object hierarchy to be destroyed</param>
        private static void DestroyTree(GameObject parent)
        {
            // We cannot traverse the children and destroy them at the same time.
            // We first need to collect all children.
            GameObject[] allChildren = new GameObject[parent.transform.childCount];
            int i = 0;
            foreach (Transform child in parent.transform)
            {
                allChildren[i] = child.gameObject;
                i += 1;
            }

            // Only now we can destroy all children.
            foreach (GameObject child in allChildren)
            {
                DestroyTree(child);
            }
            if (parent.CompareTag(Tags.Node) || parent.CompareTag(Tags.Edge) || parent.CompareTag(Tags.Decoration))
            {
                Destroyer.Destroy(parent);
            }
        }

        /// <summary>
        /// Returns all (transitive) descendants of <paramref name="gameObject"/> tagged by any of
        /// the <paramref name="tags"/>.
        /// </summary>
        /// <param name="gameObject">game objects whose descendants are required</param>
        /// <param name="tags">the list of tags against which to check the descendants</param>
        /// <returns>(transitive) descendants of the game object this AbstractSEECity is attached to tagged by
        /// any of the <paramref name="tags"/></returns>
        private static ICollection<GameObject> AllDescendantsTaggedBy(GameObject gameObject, string[] tags)
        {
            List<GameObject> result = new();
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
        /// type not contained in <paramref name="graph"/>, a new entry with default values will be added.
        /// </summary>
        /// <param name="graph">graph from which to retrieve the node types (may be null)</param>
        public void InspectSchema(Graph graph)
        {
            if (graph != null)
            {
                foreach (string type in graph.AllNodeTypes())
                {
                    // preserve existing node types and create new entry for types not yet seen
                    if (!NodeTypes.TryGetValue(type, out VisualNodeAttributes _))
                    {
                        NodeTypes[type] = new VisualNodeAttributes();
                    }
                }
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
                ICollection<string> relevantNodeTypes = NodeTypes.Where(pair => pair.Value.IsRelevant)
                                                                 .Select(pair => pair.Key).ToList();
                return graph.SubgraphByNodeType(relevantNodeTypes, IgnoreSelfLoopsInLifting);
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
                ErosionSettings.DeadCodeIssue,
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
                ErosionSettings.ArchitectureIssueSum,
                ErosionSettings.CloneIssueSum,
                ErosionSettings.CycleIssueSum,
                ErosionSettings.DeadCodeIssueSum,
                ErosionSettings.MetricIssueSum,
                ErosionSettings.StyleIssueSum,
                ErosionSettings.UniversalIssueSum
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
        /// for nodes in the GXL file onto the icons to be used for visualizing them.
        /// </summary>
        /// <returns>mapping of all node attribute names onto icon ids</returns>
        public Dictionary<string, IconFactory.Erosion> IssueMap() =>
            new()
            {
                { ErosionSettings.ArchitectureIssue, IconFactory.Erosion.ArchitectureViolation },
                { ErosionSettings.CloneIssue, IconFactory.Erosion.Clone },
                { ErosionSettings.CycleIssue, IconFactory.Erosion.Cycle },
                { ErosionSettings.DeadCodeIssue, IconFactory.Erosion.DeadCode },
                { ErosionSettings.MetricIssue, IconFactory.Erosion.Metric },
                { ErosionSettings.StyleIssue, IconFactory.Erosion.Style },
                { ErosionSettings.UniversalIssue, IconFactory.Erosion.Universal },
                { ErosionSettings.LspError, IconFactory.Erosion.LspError },
                { ErosionSettings.LspWarning, IconFactory.Erosion.LspWarning },
                { ErosionSettings.LspInfo, IconFactory.Erosion.LspInfo },
                { ErosionSettings.LspHint, IconFactory.Erosion.LspHint }
            };

        /// <summary>
        /// Lists the metrics for each node type.
        /// </summary>
        [Button(ButtonSizes.Small, Name = "List Node Metrics")]
        [ButtonGroup(ResetButtonsGroup), RuntimeButton(ResetButtonsGroup, "List Node Metrics")]
        [PropertyOrder(ResetButtonsGroupOrderReset + 2)]
        private void ListNodeMetrics()
        {
            DumpNodeMetrics();
        }

        /// <summary>
        /// Returns true if the user is currently hovering over the plane (area) of the code
        /// city represented by <paramref name="gameObject"/>.
        ///
        /// Precondition: <paramref name="gameObject"/> has a <see cref="GO.Plane"/> attached
        /// to it.
        /// </summary>
        /// <returns>true if user is hovering over the code city represented by <paramref name="gameObject"/></returns>
        public static bool UserIsHoveringCity(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out GO.Plane clippingPlane) || clippingPlane == null)
            {
                Debug.LogError($"Code city {gameObject.FullName()} has no {typeof(GO.Plane)}.\n");
                return false;
            }

            Raycasting.RaycastClippingPlane(clippingPlane, out _, out bool hitInsideClippingArea, out Vector3 _);
            return hitInsideClippingArea;
        }

        /// <summary>
        /// Returns true if the user is currently hovering over the plane (area) of this city.
        /// </summary>
        /// <returns>true if user is hovering over this city</returns>
        public bool UserIsHoveringCity()
        {
            return UserIsHoveringCity(gameObject);
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
            IDictionary<string, ISet<string>> result = new Dictionary<string, ISet<string>>();

            foreach (Graph graph in graphs)
            {
                foreach (Node node in graph.Nodes())
                {
                    if (result.TryGetValue(node.Type, out ISet<string> metrics))
                    {
                        metrics.UnionWith(node.AllMetrics());
                    }
                    else
                    {
                        result[node.Type] = node.AllMetrics();
                    }
                }
            }

            foreach (KeyValuePair<string, ISet<string>> item in result)
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
        [Obsolete]
        public void SetupCompoundSpringEmbedder(Graph graph)
        {
            if (NodeLayoutSettings.Kind == NodeLayoutKind.CompoundSpringEmbedder)
            {
                Dictionary<string, bool> dirs = CoseGraphSettings.ListInnerNodeToggle;
                // the new directories
                Dictionary<string, bool> dirsLocal = new();

                Dictionary<string, NodeLayoutKind> dirsLayout = new();
                Dictionary<string, NodeShapes> dirsShape = new();

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

        private void AddRootNodeType()
        {
            if (!NodeTypes.TryGetValue(Graph.RootType, out VisualNodeAttributes _))
            {
                NodeTypes[Graph.RootType] = new VisualNodeAttributes();
            }
            VisualNodeAttributes root = NodeTypes[Graph.RootType];
            root.ShowNames = false;
            root.ColorProperty.TypeColor = new Color(0.2549f, 0.4118f, 0.8824f);
        }

        #region Odin Inspector Attributes

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
        /// The order of the Load-Layout button in the button group <see cref="DataButtonsGroup"/>.
        /// </summary>
        protected const float DataButtonsGroupOrderLoadLayout = DataButtonsGroupOrderSaveLayout + 1;

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

        /// <summary>
        /// Name of the Inspector foldout group for the node settings.
        /// </summary>
        protected const string NodeFoldoutGroup = "Nodes";

        /// <summary>
        /// Name of the Inspector foldout group for the edge settings.
        /// </summary>
        protected const string EdgeFoldoutGroup = "Edges";

        /// <summary>
        /// Name of the Inspector foldout group for the erosion settings.
        /// </summary>
        protected const string ErosionFoldoutGroup = "Erosion";

        #endregion
    }
}
