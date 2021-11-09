using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OdinSerializer;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Layout.NodeLayouts.Cose;
using SEE.Tools;
using SEE.Utils;
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
    public abstract partial class AbstractSEECity : SerializedMonoBehaviour
    {
        /// IMPORTANT NOTE: If you add any attribute that should be persisted in a
        /// configuration file, make sure you save and restore it in
        /// <see cref="AbstractSEECity.Save"/> and
        /// <see cref="AbstractSEECity.Restore"/>,
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
        [OdinSerialize]
        public DataPath CityPath = new DataPath();

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        [OdinSerialize]
        public HashSet<string> HierarchicalEdges = HierarchicalEdgeTypes(); // serialized by Odin

        /// <summary>
        /// A mapping of all node types of the nodes in the graph onto whether
        /// they should be visualized or not.
        /// </summary>
        [NonSerialized, OdinSerialize]
        public Dictionary<string, bool> SelectedNodeTypes = new Dictionary<string, bool>();

        /// <summary>
        /// Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        /// for range [0, max-value] is used, where max-value is the maximum value of a metric.
        /// </summary>
        public bool ZScoreScale = false;

        /// <summary>
        /// If true, only the metrics of leaf nodes are scaled.
        /// </summary>
        public bool ScaleOnlyLeafMetrics = true;

        /// <summary>
        /// The attributes of the leaf nodes.
        /// </summary>
        public LeafNodeAttributes LeafNodeSettings = new LeafNodeAttributes();

        /// <summary>
        /// The attributes of the inner nodes.
        /// </summary>
        public InnerNodeAttributes InnerNodeSettings = new InnerNodeAttributes();

        /// <summary>
        /// The node layout settings.
        /// </summary>
        public NodeLayoutAttributes NodeLayoutSettings = new NodeLayoutAttributes();

        /// <summary>
        /// The edge layout settings.
        /// </summary>
        public EdgeLayoutAttributes EdgeLayoutSettings = new EdgeLayoutAttributes();

        /// <summary>
        /// Attributes regarding the selection of edges.
        /// </summary>
        public EdgeSelectionAttributes EdgeSelectionSettings = new EdgeSelectionAttributes();

        /// <summary>
        /// The cose graph settings.
        /// </summary>
        public CoseGraphAttributes CoseGraphSettings = new CoseGraphAttributes(); // FIXME put into CitySettings.cs

        /// <summary>
        /// The metrics for the visualization of erosions.
        /// </summary>
        public ErosionAttributes ErosionSettings = new ErosionAttributes();

        /// <summary>
        /// Saves the settings of this code city to <see cref="CityPath"/>.
        /// </summary>
        public void Save()
        {
            Save(CityPath.Path);
        }

        /// <summary>
        /// Loads the settings of this code city from <see cref="CityPath"/>.
        /// </summary>
        public void Load()
        {
            Load(CityPath.Path);
        }

        /// <summary>
        /// Saves the settings of this code city to <paramref name="filename"/>
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            using ConfigWriter writer = new ConfigWriter(filename);
            Save(writer);
        }

        /// <summary>
        /// Reads the settings of this city from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename"></param>
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
        public virtual void Reset()
        {
            DeleteGraphGameObjects();
        }

        /// <summary>
        /// Resets the selected node types to be visualized.
        /// </summary>
        public void ResetSelectedNodeTypes()
        {
            SelectedNodeTypes.Clear();
        }

        /// <summary>
        /// Deletes all game objects that were created for rendering nodes or edges
        /// of the graph or any decoration thereof. More precisely, all children of this
        /// game object tagged by Tags.Node, Tags.Edge, or Tags.Decoration are destroyed
        /// (in editor mode or play mode).
        /// The underlying loaded graph is not deleted.
        /// </summary>
        protected void DeleteGraphGameObjects()
        {
            // Delete all children.
            // Note: foreach (GameObject child in transform)... would not work;
            // we really need to collect all children first and only then can destroy each.
            foreach (GameObject child in AllNodesEdgesDecorationChildren())
            {
                child.transform.parent = null;
                Destroyer.DestroyGameObject(child);
            }
        }

        /// <summary>
        /// Returns all immediate children of the game object this SEECity is attached to.
        /// </summary>
        /// <returns>immediate children of the game object this SEECity is attached to</returns>
        private List<GameObject> AllNodesEdgesDecorationChildren()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.CompareTag(Tags.Node) || child.CompareTag(Tags.Edge) || child.CompareTag(Tags.Decoration))
                {
                    result.Add(child.gameObject);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns all (transitive) descendants of <paramref name="go"/> that are tagged
        /// by Tags.Node (including <paramref name="go"/> if it is tagged by Tags.Node).
        /// </summary>
        /// <param name="go">game objects whose node descendants are required</param>
        /// <returns>all node descendants of <paramref name="go"/></returns>
        protected static ICollection<GameObject> AllNodeDescendants(GameObject go)
        {
            List<GameObject> result = new List<GameObject>();
            if (go.CompareTag(Tags.Node))
            {
                result.Add(go);
            }
            foreach (Transform child in go.transform)
            {
                ICollection<GameObject> ascendants = AllNodeDescendants(child.gameObject);
                result.AddRange(ascendants);
            }
            return result;
        }

        /// <summary>
        /// True if all node types in nodeTypes are relevant.
        /// </summary>
        private bool AllNodeTypesAreRelevant => SelectedNodeTypes.Values.All(relevant => relevant);

        /// <summary>
        /// If <paramref name="graph"/> is null, nothing happens. Otherwise:
        /// Inspects the node types that occur in the graph and updates <see cref="SelectedNodeTypes"/>.
        /// All new node types are considered relevant initially. If <paramref name="graph"/> contains
        /// a node type that existed in <see cref="SelectedNodeTypes"/> before, that node type's
        /// selection information will be re-used. If <see cref="SelectedNodeTypes"/> contains a node
        /// type not contained in <paramref name="graph"/>, it will be removed from <see cref="SelectedNodeTypes"/>.
        ///
        /// The node types can be retrieved and also be marked as irrelevant later via property
        /// <see cref="SelectedNodeTypes"/>.
        /// </summary>
        /// <param name="graph">graph from which to retrieve the node types (may be null)</param>
        public void InspectSchema(Graph graph)
        {
            if (graph != null)
            {
                // The node types in the newly loaded graph.
                HashSet<string> newTypes = new HashSet<string>();
                foreach (Node node in graph.Nodes())
                {
                    newTypes.Add(node.Type);
                }
                // nodeTypes contains the node types of the previously loaded graph.
                // Node types in nodeTypes not in newTypes will disappear
                // because we are iterating only over newTypes.
                Dictionary<string, bool> newNodeTypes = new Dictionary<string, bool>();
                foreach (string type in newTypes)
                {
                    if (SelectedNodeTypes.ContainsKey(type))
                    {
                        // preserve existing node types
                        newNodeTypes[type] = SelectedNodeTypes[type];
                    }
                    else
                    {
                        // default is true: a node type is selected initially
                        newNodeTypes[type] = true;
                    }
                }
                SelectedNodeTypes = newNodeTypes;
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
                ICollection<string> matches = SelectedNodeTypes.Where(pair => pair.Value)
                  .Select(pair => pair.Key).ToList();
                return graph.SubgraphByNodeType(matches);
            }
        }

        /// <summary>
        /// All metrics used for visual attributes of a leaf node (WidthMetric, HeightMetric,
        /// DepthMetric, and LeafStyleMetric).
        /// Note: A metric name occurs only once (i.e., duplicate names are removed).
        /// </summary>
        /// <returns>all metrics used for visual attributes of a leaf node</returns>
        public ICollection<string> AllLeafMetrics() =>
            new List<string>(4)
            {
                LeafNodeSettings.WidthMetric,
                LeafNodeSettings.HeightMetric,
                LeafNodeSettings.DepthMetric,
                LeafNodeSettings.ColorMetric
            };

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
        /// Returns the names of all node metric attributes that are visualized somehow.
        /// More precisely, the resulting list consists of the following metrics:
        /// WidthMetric, HeightMetric, DepthMetric, LeafStyleMetric, AllLeafIssues(),
        /// AllInnerNodeIssues(), and InnerDonutMetric.
        /// </summary>
        /// <returns>all node metric attributes</returns>
        public List<string> AllMetricAttributes()
        {
            List<string> nodeMetrics = new List<string>(AllLeafMetrics());
            nodeMetrics.AddRange(AllInnerNodeMetrics());
            nodeMetrics.AddRange(AllLeafIssues());
            nodeMetrics.AddRange(AllInnerNodeIssues());
            nodeMetrics.Add(InnerNodeSettings.InnerDonutMetric);
            return nodeMetrics;
        }

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
        /// All metrics used for visual attributes of inner nodes (InnerNodeStyleMetric
        /// and InnerNodeHeightMetric).
        /// Note: A metric name occurs only once (i.e., duplicate names are removed).
        /// </summary>
        /// <returns>all metrics used for visual attributes of an inner node</returns>
        public ICollection<string> AllInnerNodeMetrics() => 
            new List<string> { InnerNodeSettings.ColorMetric, InnerNodeSettings.HeightMetric };

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
                return new Graph();
            }

            if (File.Exists(filename))
            {
                Performance p = Performance.Begin("loading graph data from " + filename);
                GraphReader graphCreator = new GraphReader(filename, HierarchicalEdges,
                                                           rootName: rootName ?? filename,
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
                return new Graph();
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
                Dictionary<string, InnerNodeKinds> dirsShape = new Dictionary<string, InnerNodeKinds>();

                foreach (Node node in graph.Nodes())
                {
                    if (!node.IsLeaf())
                    {
                        dirsShape.Add(node.ID, InnerNodeSettings.Kind);
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

                CoseGraphSettings.LoadedForNodeTypes = SelectedNodeTypes.Where(type => type.Value)
                                                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }
    }
}
