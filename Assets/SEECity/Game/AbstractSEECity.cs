using System.Collections.Generic;
using UnityEngine;
using OdinSerializer;

using SEE.DataModel;
using SEE.DataModel.IO;
using SEE.GO;
using System;
using System.Linq;

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
        /// The prefix of the absolute paths for the GXL and CSV data; that is,
        /// the directory where these data are located in.
        /// Note: This attribute will be set in the SEECityEditor. 
        /// Application.dataPath (used within ProjectPath()) must not be called in a 
        /// constructor. That is why we need to defer its definition to the 
        /// SEECityEditor.
        /// </summary>        
        public string PathPrefix; // serialized by Unity

        /// <summary>
        /// The center origin where the graph should be placed in the world scene.
        /// </summary>
        public Vector3 origin = Vector3.zero; // serialized by Unity

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
        private Dictionary<string, bool> nodeTypes = new Dictionary<string, bool>();
        /// <summary>
        /// A mapping of all node types of the nodes in the graph onto whether
        /// they should be visualized or not.
        /// </summary>
        public Dictionary<string, bool> SelectedNodeTypes
        {
            get => nodeTypes;
        }
        
        /// <summary>
        /// Resets everything that is specific to a given graph. Here: the 
        /// node types.
        /// </summary>
        public virtual void Reset()
        {
            nodeTypes = new Dictionary<string, bool>();
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
        /// Inspects the node types and attributes that occur in the graph.
        /// All node types are considered relevant initially. The node types 
        /// can be retrieved and also be marked as irrelevant later via property 
        /// SelectedNodeTypes.
        /// </summary>
        /// <param name="graph">graph from which to retrieve the node types (may be null)</param>
        public void InspectSchema(Graph graph)
        {
            if (graph != null)
            {
                HashSet<string> types = new HashSet<string>();
                foreach (Node node in graph.Nodes())
                {
                    types.Add(node.Type);
                }
                nodeTypes = new Dictionary<string, bool>();
                foreach (string type in types)
                {
                    nodeTypes[type] = true;
                }
            }
            else
            {
                nodeTypes.Clear();
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
        protected Graph RelevantGraph(Graph graph)
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
                Debug.Log("Considering only a subgraph.\n");
                return graph.Subgraph(matches);
            }
        }

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
        /// The attribute name of the metric to be used for determining the style of inner nodes.
        /// </summary>
        public string InnerNodeStyleMetric = NumericAttributeNames.IssuesTotal.Name(); // serialized by Unity

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
            Buildings,
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
        /// The kinds of node layouts available.
        /// </summary>
        public enum NodeLayouts
        {
            EvoStreets,
            Balloon,
            RectanglePacking,
            Treemap,
            CirclePacking,
            Manhattan,
        }

        /// <summary>
        /// The kinds of edge layouts available.
        /// </summary>
        public enum EdgeLayouts
        {
            None = 0,        // no edges are to be drawn
            Straight = 1,
            Spline = 2,
            Bundling = 3
        }

        /// <summary>
        /// The layout that should be used for nodes.
        /// </summary>
        public NodeLayouts NodeLayout; // serialized by Unity

        /// <summary>
        /// The layout that should be used for edges.
        /// </summary>
        public EdgeLayouts EdgeLayout; // serialized by Unity

        /// <summary>
        /// Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        /// for range [0, max-value] is used, where max-value is the maximum value of a metric.
        /// </summary>
        public bool ZScoreScale = true; // serialized by Unity

        /// <summary>
        /// The width of the line representing edges.
        /// </summary>
        public float EdgeWidth = 0.3f; // serialized by Unity

        /// <summary>
        /// Whether erosions should be visible above blocks.
        /// </summary>
        public bool ShowErosions = false; // serialized by Unity

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
            + " no simplification. Recommended value: 0.05." )]
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
                SEE.Performance p = SEE.Performance.Begin("loading graph data from " + filename);
                GraphReader graphCreator = new GraphReader(filename, HierarchicalEdges, "ROOT", new SEELogger());
                graphCreator.Load();
                Graph graph = graphCreator.GetGraph();
                p.End();
                Debug.Log("Number of nodes loaded: " + graph.NodeCount + "\n");
                Debug.Log("Number of edges loaded: " + graph.EdgeCount + "\n");
                return graph;
            }
        }
    }
}
