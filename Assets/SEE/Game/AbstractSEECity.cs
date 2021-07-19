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

namespace SEE.Game
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
        /// <see cref="Save(ConfigWriter)"/> and 
        /// <see cref="Restore(Dictionary{string,object})"/>, 
        /// respectively (both declared in AbstractSEECityIO). You should also
        /// extend the test cases in TestConfigIO.

        /// <summary>
        /// The global attributes.
        /// </summary>
        public GlobalCityAttributes globalCityAttributes = new GlobalCityAttributes();

        /// <summary>
        /// The attributes of the leaf nodes per kind. They are indexed by <see cref="Node.NodeDomain"/>
        /// casted to an integer.
        /// </summary>
        public LeafNodeAttributes[] leafNodeAttributesPerKind = ArrayUtils.New((int)Node.NodeDomain.Count, _ => new LeafNodeAttributes());

        /// <summary>
        /// The attributes of the inner nodes per kind. They are indexed by <see cref="Node.NodeDomain"/>
        /// casted to an integer.
        /// </summary>
        public InnerNodeAttributes[] innerNodeAttributesPerKind = ArrayUtils.New((int)Node.NodeDomain.Count, _ => new InnerNodeAttributes());

        /// <summary>
        /// The node layout settings.
        /// </summary>
        public NodeLayoutSettings nodeLayoutSettings = new NodeLayoutSettings();

        /// <summary>
        /// The edge layout settings.
        /// </summary>
        public EdgeLayoutSettings edgeLayoutSettings = new EdgeLayoutSettings();

        /// <summary>
        /// The cose graph settings.
        /// </summary>
        public CoseGraphSettings coseGraphSettings = new CoseGraphSettings(); // FIXME put into CitySettings.cs

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
        [OdinSerialize]
        public HashSet<string> HierarchicalEdges = Hierarchical_Edge_Types(); // serialized by Odin

        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        public static HashSet<string> Hierarchical_Edge_Types()
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

        //---------------------------------
        // Relevant node types
        //---------------------------------
        /// <summary>
        /// A mapping of all node types of the nodes in the graph onto whether
        /// they should be visualized or not.
        /// </summary>
        [NonSerialized, OdinSerialize]
        public Dictionary<string, bool> SelectedNodeTypes = new Dictionary<string, bool>();

        /// <summary>
        /// The path where the settings (the attributes of this class) are stored.
        /// </summary>
        [OdinSerialize]
        public DataPath CityPath = new DataPath();

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
                if (child.CompareTag(Tags.Node) || child.CompareTag(Tags.Edge) || child.CompareTag(Tags.Decoration) || child.CompareTag(Tags.Whiteboard))
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
                ICollection<string> matches = SelectedNodeTypes.Where(pair => pair.Value == true)
                  .Select(pair => pair.Key).ToList();
                return graph.Subgraph(matches);
            }
        }

        /// <summary>
        /// All metrics used for visual attributes of a leaf node (WidthMetric, HeightMetric,
        /// DepthMetric, and LeafStyleMetric). 
        /// Note: A metric name occurs only once (i.e., duplicate names are removed).
        /// </summary>
        /// <returns>all metrics used for visual attributes of a leaf node</returns>
        public ICollection<string> AllLeafMetrics()
        {
            List<string> result = new List<string>(leafNodeAttributesPerKind.Length * 4);
            foreach (LeafNodeAttributes leafNodeAttributes in leafNodeAttributesPerKind)
            {
                result.Add(leafNodeAttributes.widthMetric);
                result.Add(leafNodeAttributes.heightMetric);
                result.Add(leafNodeAttributes.depthMetric);
                result.Add(leafNodeAttributes.styleMetric);
            }
            return result;
        }

        //--------------------------------------------------------
        // Software erosion issues shown as icons above leaf nodes
        //--------------------------------------------------------

        /// <summary>
        /// The attribute name of the metric representing architecture violations.
        /// </summary>
        public string ArchitectureIssue = NumericAttributeNames.Architecture_Violations.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing duplicated code.
        /// </summary>
        public string CloneIssue = NumericAttributeNames.Clone.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing cylces.
        /// </summary>
        public string CycleIssue = NumericAttributeNames.Cycle.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing dead code.
        /// </summary>
        public string Dead_CodeIssue = NumericAttributeNames.Dead_Code.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing metric violations.
        /// </summary>
        public string MetricIssue = NumericAttributeNames.Metric.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing code-style violations.
        /// </summary>
        public string StyleIssue = NumericAttributeNames.Style.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing other kinds of violations.
        /// </summary>
        public string UniversalIssue = NumericAttributeNames.Universal.Name(); // serialized by Unity

        /// <summary>
        /// Returns all attribute names of the different kinds of software erosions.
        /// </summary>
        /// <returns>all attribute names of the different kinds of software erosions</returns>
        public IList<string> AllLeafIssues()
        {
            List<string> result = new List<string>()
               {
                  ArchitectureIssue,
                  CloneIssue,
                  CycleIssue,
                  Dead_CodeIssue,
                  MetricIssue,
                  StyleIssue,
                  UniversalIssue
               };
            return result;
        }

        //-----------------------------------------------------------------------
        // Software erosion issues shown as icons on Donut charts for inner nodes
        //-----------------------------------------------------------------------
        public const string SUM_Postfix = "_SUM";
        /// <summary>
        /// The attribute name of the metric representing the sum of all architecture violations
        /// for an inner node.
        /// </summary>
        public string ArchitectureIssue_SUM = NumericAttributeNames.Architecture_Violations.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all clones
        /// for an inner node.
        /// </summary>
        public string CloneIssue_SUM = NumericAttributeNames.Clone.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all cycles
        /// for an inner node.
        /// </summary>
        public string CycleIssue_SUM = NumericAttributeNames.Cycle.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all dead entities
        /// for an inner node.
        /// </summary>
        public string Dead_CodeIssue_SUM = NumericAttributeNames.Dead_Code.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all metric violations
        /// for an inner node.
        /// </summary>
        public string MetricIssue_SUM = NumericAttributeNames.Metric.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all style violations
        /// for an inner node.
        /// </summary>
        public string StyleIssue_SUM = NumericAttributeNames.Style.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all other kinds of
        /// software erosions for an inner node.
        /// </summary>
        public string UniversalIssue_SUM = NumericAttributeNames.Universal.Name() + SUM_Postfix; // serialized by Unity

        /// <summary>
        /// Returns all attribute names of the different kinds of software erosions for inner
        /// nodes (the sums of their descendants).
        /// </summary>
        /// <returns>all attribute names of the different kinds of software erosions for inner nodes</returns>
        public IList<string> AllInnerNodeIssues()
        {
            List<string> result = new List<string>
            {
                  ArchitectureIssue_SUM,
                  CloneIssue_SUM,
                  CycleIssue_SUM,
                  Dead_CodeIssue_SUM,
                  MetricIssue_SUM,
                  StyleIssue_SUM,
                  UniversalIssue_SUM
               };
            return result;
        }

        /// <summary>
        /// The metric to be put in the inner circle of a Donut chart.
        /// </summary>
        public string InnerDonutMetric = NumericAttributeNames.IssuesTotal.Name(); // serialized by Unity

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
            nodeMetrics.Add(InnerDonutMetric);
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
                { ArchitectureIssue, IconFactory.Erosion.Architecture_Violation },
                { CloneIssue, IconFactory.Erosion.Clone },
                { CycleIssue, IconFactory.Erosion.Cycle },
                { Dead_CodeIssue, IconFactory.Erosion.Dead_Code },
                { MetricIssue, IconFactory.Erosion.Metric },
                { StyleIssue, IconFactory.Erosion.Style },
                { UniversalIssue, IconFactory.Erosion.Universal }
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
        public ICollection<string> AllInnerNodeMetrics()
        {
            return innerNodeAttributesPerKind.SelectMany(x => new[] {x.styleMetric, x.heightMetric}).ToList();
        }

        //--------------------------------------
        // Other visual attributes of leaf nodes
        // -------------------------------------
        /// <summary>
        /// This parameter determines the minimal width, breadth, and height of each block
        /// representing a graph node visually. Must not be greater than MaximalBlockLength.
        /// </summary>
        public float MinimalBlockLength = 0.1f; // serialized by Unity

        /// <summary>
        /// This parameter determines the maximal width, breadth, and height of each block
        /// representing a graph node visually. Must not be smaller than MinimalBlockLength.
        /// </summary>
        public float MaximalBlockLength = 100.0f; // serialized by Unity

        /// <summary>
        /// Loads and returns the graph data from the GXL file with given <paramref name="filename"/>.
        /// </summary>
        /// <returns>the loaded graph (may be empty if a graph could not be loaded)</returns>
        public Graph LoadGraph(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogError("Empty graph path.\n");
                return new Graph();
            }

            if (File.Exists(filename))
            {
                Performance p = Performance.Begin("loading graph data from " + filename);
                GraphReader graphCreator = new GraphReader(filename, HierarchicalEdges, logger: new SEELogger());
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
            if (nodeLayoutSettings.kind == NodeLayoutKind.CompoundSpringEmbedder)
            {
                Dictionary<string, bool> dirs = coseGraphSettings.ListInnerNodeToggle;
                // the new directories
                Dictionary<string, bool> dirsLocal = new Dictionary<string, bool>();

                Dictionary<string, NodeLayoutKind> dirsLayout = new Dictionary<string, NodeLayoutKind>();
                Dictionary<string, InnerNodeKinds> dirsShape = new Dictionary<string, InnerNodeKinds>();

                foreach (Node node in graph.Nodes())
                {
                    if (!node.IsLeaf())
                    {
                        dirsShape.Add(node.ID, innerNodeAttributesPerKind[(int)node.Domain].kind);
                        dirsLocal.Add(node.ID, false);
                        dirsLayout.Add(node.ID, nodeLayoutSettings.kind);
                    }
                }

                // if the key isn't in the old dictionaries
                //dirsLocal = dirsLocal.Where(i => !dirs.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);

                bool diff1 = dirs.Keys.Except(dirsLocal.Keys).Any();
                bool diff2 = dirsLocal.Keys.Except(dirs.Keys).Any();

                if (dirs.Count != dirsLocal.Count || diff1 || diff2)
                {
                    coseGraphSettings.InnerNodeShape = dirsShape;
                    coseGraphSettings.InnerNodeLayout = dirsLayout;
                    coseGraphSettings.ListInnerNodeToggle = dirsLocal;
                }

                coseGraphSettings.LoadedForNodeTypes = SelectedNodeTypes.Where(type => type.Value)
                                                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }
    }
}
