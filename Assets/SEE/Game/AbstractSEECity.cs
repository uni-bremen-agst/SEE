using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OdinSerializer;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.DataModel.DG.IO;
using SEE.GO;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.Cose;
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
    public abstract class AbstractSEECity : SerializedMonoBehaviour
    {
        /// <summary>
        /// The screen relative height to use for the culling a game node [0-1].
        /// If the game node uses less than this percentage it will be culled.
        /// </summary>
        public float LODCulling = 0.01f;

        /// <summary>
        /// The path for the layout file containing the node layout information.
        /// If the file extension is <see cref="Filenames.GVLExtension"/>, the layout is expected
        /// to be stored in Axivion's Gravis layout (GVL) with 2D co-ordinates. 
        /// Otherwise is our own layout format SDL is expected, which saves the complete Transform 
        /// data of a game object.
        /// </summary>
        [OdinSerialize]
        public DataPath LayoutPath = new DataPath();

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
        protected Dictionary<string, bool> nodeTypes = new Dictionary<string, bool>();
        /// <summary>
        /// A mapping of all node types of the nodes in the graph onto whether
        /// they should be visualized or not.
        /// </summary>
        public Dictionary<string, bool> SelectedNodeTypes
        {
            get => nodeTypes;
            set
            {
                SelectedNodeTypes = value;
            }
        }

        /// <summary>
        /// The path where the settings (the attributes of this class) are stored.
        /// </summary>
        public DataPath CityPath;

        private void Awake()
        {
            if (CityPath == null)
            {
                CityPath = new DataPath(Application.dataPath + "/config" + Filenames.JSONExtension);
            }
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
            nodeTypes.Clear();
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
        private bool AllNodeTypesAreRelevant
        {
            get
            {
                foreach (bool relevant in nodeTypes.Values)
                {
                    if (!relevant)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

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
                    if (nodeTypes.ContainsKey(type))
                    {
                        // preserve existing node types
                        newNodeTypes[type] = nodeTypes[type];
                    }
                    else
                    {
                        // default is true: a node type is selected initially
                        newNodeTypes[type] = true;
                    }
                }
                nodeTypes = newNodeTypes;
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
                Debug.Log("All node types are relevant.\n");
                return graph;
            }
            else
            {
                ICollection<string> matches = nodeTypes.Where(pair => pair.Value == true)
                  .Select(pair => pair.Key).ToList();
                Debug.Log("The following node types are relevant:\n");
                foreach (string nodeType in matches)
                {
                    Debug.LogFormat("  {0}\n", nodeType);
                }
                return graph.Subgraph(matches);
            }
        }

        //---------------------------------
        // Color range of leaf nodes
        //---------------------------------
        public ColorRange LeafNodeColorRange = new ColorRange(Color.white, Color.red, 10);

        //---------------------------------
        // Color range of inner nodes
        //---------------------------------
        public ColorRange InnerNodeColorRange = new ColorRange(Color.white, Color.yellow, 10);

        //---------------------------------
        // Visual attributes of a leaf node
        //---------------------------------
        /// <summary>
        /// The attribute name of the metric to be used for the width of a building (x co-ordinate).
        /// </summary>
        public string WidthMetric = NumericAttributeNames.Number_Of_Tokens.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric to be used for the height of a building (y co-ordinate).
        /// </summary>
        public string HeightMetric = NumericAttributeNames.Clone_Rate.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric to be used for the breadth of a building (y co-ordinate).
        /// </summary>
        public string DepthMetric = NumericAttributeNames.LOC.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric to be used for determining the style of leaf nodes.
        /// </summary>
        public string LeafStyleMetric = NumericAttributeNames.Complexity.Name(); // serialized by Unity

        //----------------------------------
        // Attributes of a leaf node's label
        //----------------------------------
        /// <summary>
        /// If true, label's with the node's SourceName will be displayed above each leaf node.
        /// </summary>
        public bool ShowLabel = true;
        /// <summary>
        /// The distance between the top of the leaf node and its label.
        /// </summary>
        public float LeafLabelDistance = 0.2f;
        /// <summary>
        /// The font size of the leaf node's label.
        /// </summary>
        public float LeafLabelFontSize = 0.4f;

        /// <summary>
        /// All metrics used for visual attributes of a leaf node (WidthMetric, HeightMetric,
        /// DepthMetric, and LeafStyleMetric). 
        /// Note: A metric name occurs only once (i.e., duplicate names are removed).
        /// </summary>
        /// <returns>all metrics used for visual attributes of a leaf node</returns>
        public ICollection<string> AllLeafMetrics()
        {
            return new HashSet<string> { WidthMetric, HeightMetric, DepthMetric, LeafStyleMetric };
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
            List<string> result = new List<string>()
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
        public Dictionary<string, IconFactory.Erosion> LeafIssueMap()
        {
            Dictionary<string, IconFactory.Erosion> result = new Dictionary<string, IconFactory.Erosion>
            {
                { ArchitectureIssue, IconFactory.Erosion.Architecture_Violation },
                { CloneIssue, IconFactory.Erosion.Clone },
                { CycleIssue, IconFactory.Erosion.Cycle },
                { Dead_CodeIssue, IconFactory.Erosion.Dead_Code },
                { MetricIssue, IconFactory.Erosion.Metric },
                { StyleIssue, IconFactory.Erosion.Style },
                { UniversalIssue, IconFactory.Erosion.Universal }
            };
            return result;
        }
        //-----------------------------------
        // Visual attributes of an inner node
        //-----------------------------------

        /// <summary>
        /// The attribute name of the metric to be used for determining the height of inner nodes.
        /// </summary>
        public string InnerNodeHeightMetric = "";
        /// <summary>
        /// The attribute name of the metric to be used for determining the style of inner nodes.
        /// </summary>
        public string InnerNodeStyleMetric = NumericAttributeNames.IssuesTotal.Name(); // serialized by Unity

        //-----------------------------------
        // Visual attributes of an inner node
        //-----------------------------------

        /// <summary>
        /// If true, label's with the node's SourceName will be displayed above each inner node.
        /// </summary>
        public bool InnerNodeShowLabel = true;
        /// <summary>
        /// The distance between the top of the inner node and its label.
        /// </summary>
        public float InnerNodeLabelDistance = 0.2f;
        /// <summary>
        /// The font size of the inner node's label.
        /// </summary>
        public float InnerNodeLabelFontSize = 0.4f;


        /// <summary>
        /// All metrics used for visual attributes of inner nodes (InnerNodeStyleMetric
        /// and InnerNodeHeightMetric).
        /// Note: A metric name occurs only once (i.e., duplicate names are removed).
        /// </summary>
        /// <returns>all metrics used for visual attributes of an inner node</returns>
        public ICollection<string> AllInnerNodeMetrics()
        {
            return new HashSet<string> { InnerNodeStyleMetric, InnerNodeHeightMetric };
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
        /// How leaf graph nodes should be depicted.
        /// </summary>
        public enum LeafNodeKinds
        {
            Blocks,
        }

        /// <summary>
        /// How inner graph nodes should be depicted.
        /// </summary>
        public enum InnerNodeKinds
        {
            Blocks,
            Rectangles,
            Donuts,
            Circles,
            Empty,
            Cylinders,
        }

        /// <summary>
        /// What kinds of game objects are to be created for leaf nodes in the graph.
        /// </summary>
        public LeafNodeKinds LeafObjects; // serialized by Unity

        /// <summary>
        /// What kinds of game objects are to be created for inner graph nodes.
        /// </summary>
        public InnerNodeKinds InnerNodeObjects; // serialized by Unity

        /// <summary>
        /// The layout that should be used for nodes.
        /// </summary>
        public NodeLayoutKind NodeLayout; // serialized by Unity

        /// <summary>
        /// The layout that should be used for edges.
        /// </summary>
        public EdgeLayoutKind EdgeLayout; // serialized by Unity

        /// <summary>
        /// Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        /// for range [0, max-value] is used, where max-value is the maximum value of a metric.
        /// </summary>
        public bool ZScoreScale = true; // serialized by Unity

        /// <summary>
        /// The width of the line representing edges in world space.
        /// </summary>
        public float EdgeWidth = 0.1f; // serialized by Unity

        /// <summary>
        /// Whether erosions should be visible above blocks.
        /// </summary>
        public bool ShowErosions = false; // serialized by Unity

        /// <summary>
        /// The maximal absolute width of a sprite representing an erosion in world-space Unity units.
        /// </summary>
        public float MaxErosionWidth = 1.0f; // serialized by Unity

        /// <summary>
        /// Orientation of the edges; 
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses;
        /// </summary>
        public bool EdgesAboveBlocks = true; // serialized by Unity

        /// <summary>
        /// Determines the strength of the tension for bundling edges. This value may
        /// range from 0.0 (straight lines) to 1.0 (maximal bundling along the spline).
        /// 0.85 is the value recommended by Holten
        /// </summary>
        [Tooltip("Tension for bundling edges: 0 means no bundling at all; the maximal value"
            + " of 1 means maximal bundling. Recommended value: 0.85.")]
        public float Tension = 0.85f; // serialized by Unity

        /// <summary>
        /// Determines to which extent the polylines of the generated splines are
        /// simplified. Range: [0.0, inf] (0.0 means no simplification). More precisely,
        /// stores the epsilon parameter of the Ramer–Douglas–Peucker algorithm which
        /// is used to identify and remove points based on their distances to the line
        /// drawn between their neighbors.
        /// </summary>
        [Tooltip("Tolerance for spline simplification (Ramer–Douglas–Peucker algorithm):"
            + " line points whose distances fall below that threshold are merged. A value <= 0 means "
            + " no simplification. Recommended value: 0.05.")]
        public float RDP = 0.05f;

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
            else
            {
                if (File.Exists(filename))
                {
                    Performance p = Performance.Begin("loading graph data from " + filename);
                    GraphReader graphCreator = new GraphReader(filename, HierarchicalEdges, logger: new SEELogger());
                    graphCreator.Load();
                    Graph graph = graphCreator.GetGraph();
                    p.End();
                    Debug.Log("Loaded graph data successfully:"
                        + "\nFilename: " + filename
                        + "\nNumber of nodes: " + graph.NodeCount
                        + "\nNumber of edges: " + graph.EdgeCount
                        + "\nElapsed time: " + p.GetElapsedTime() + "[h:m:s:ms]\n");

                    LoadDataForGraphListing(graph);
                    return graph;
                }
                else
                {
                    Debug.LogErrorFormat("GXL file {0} does not exist.\n", filename);
                    return new Graph();
                }
            }
        }

        /// <summary>
        /// Cosegraph settings
        /// </summary>
        public CoseGraphSettings CoseGraphSettings = new CoseGraphSettings();

        /// <summary>
        /// measurements of the layout
        /// </summary>
        public SortedDictionary<string, string> Measurements = new SortedDictionary<string, string>();

        /// <summary>
        /// Indicates whether the measurements should be calculated or not
        /// </summary>
        public bool calculateMeasurements = false;

        /// <summary>
        /// Dictionary with all Nodelayouts for leaf and inner nodes
        /// </summary>
        public Dictionary<NodeLayoutKind, string> SubLayoutsInnerNodes = Enum.GetValues(typeof(NodeLayoutKind)).Cast<NodeLayoutKind>().Where(nodeLayout => !nodeLayout.GetModel().OnlyLeaves).OrderBy(x => x.ToString()).ToDictionary(i => i, i => i.ToString());

        /// <summary>
        ///  Dictionary with all Nodelayouts only for leaf nodes
        /// </summary>
        public Dictionary<NodeLayoutKind, string> SubLayoutsLeafNodes = Enum.GetValues(typeof(NodeLayoutKind)).Cast<NodeLayoutKind>().OrderBy(x => x.ToString()).ToDictionary(i => i, i => i.ToString());
        
        /// <summary>
        /// Saves all data needed for the listing of the dirs in gui in cosegraphSettings
        /// </summary>
        /// <param name="graph"></param>
        public void LoadDataForGraphListing(Graph graph)
        {
            if (NodeLayout == NodeLayoutKind.CompoundSpringEmbedder)
            {
                Dictionary<string, bool> dirs = CoseGraphSettings.ListDirToggle;
                // die neuen dirs 
                Dictionary<string, bool> dirsLocal = new Dictionary<string, bool>();

                Dictionary<string, NodeLayoutKind> dirsLayout = new Dictionary<string, NodeLayoutKind>();
                Dictionary<string, InnerNodeKinds> dirsShape = new Dictionary<string, InnerNodeKinds>();

                foreach (Node node in graph.Nodes())
                {
                    if (!node.IsLeaf())
                    {
                        dirsShape.Add(node.ID, InnerNodeObjects);
                        dirsLocal.Add(node.ID, false);
                        dirsLayout.Add(node.ID, NodeLayout);
                    }
                }

                // falls der key nicht in den alten dictonary ist
                //dirsLocal = dirsLocal.Where(i => !dirs.ContainsKey(i.Key)).ToDictionary(i => i.Key, i => i.Value);

                CoseGraphSettings.show = new Dictionary<string, bool>();

                bool diff1 = dirs.Keys.Except(dirsLocal.Keys).Any();
                bool diff2 = dirsLocal.Keys.Except(dirs.Keys).Any();

                if (dirs.Count == dirsLocal.Count && !diff1 && !diff2)
                {

                }
                else
                {
                    CoseGraphSettings.DirShape = dirsShape;
                    CoseGraphSettings.DirNodeLayout = dirsLayout;
                    CoseGraphSettings.ListDirToggle = dirsLocal;
                    // get roots
                    CoseGraphSettings.rootDirs = graph.GetRoots();
                }

                CoseGraphSettings.loadedForNodeTypes = SelectedNodeTypes.Where(type => type.Value == true).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                CoseGraphSettings.rootDirs = graph.GetRoots();
            }
        }

        /// <summary>
        /// Saves the settings of this code city to <see cref="CityPath"/>.
        /// </summary>
        public void Save()
        {
            Save(this, CityPath.Path);
        }

        /// <summary>
        /// Loads the settings of this code city from <see cref="CityPath"/>.
        /// </summary>
        public void Load()
        {
            Load(this, CityPath.Path);
        }

        /// <summary>
        /// Saves <paramref name="city"/> in a file with given <paramref name="filename"/>.
        /// </summary>
        /// <param name="city">the city to be stored</param>
        /// <param name="filename">the name of the file in which <paramref name="city"/> will be stored</param>
        public static void Save(AbstractSEECity city, string filename)
        {          
            CityRestorer.Persist(filename, city);
        }

        /// <summary>
        /// Loads and overwrites the <paramref name="city"/> with the city read 
        /// from the file with given <paramref name="filename"/>.
        /// </summary>
        /// <param name="city">the city to be overwritten</param>
        /// <param name="filename">the name of the file from which to read the <paramref name="city"/></param>
        public static void Load(AbstractSEECity city, string filename)
        {
            CityRestorer.RestoreCity(filename, city);
        }
    }
}
