using SEE.Layout;
using System;
using System.Collections.Generic;

namespace SEE
{
    /// <summary>
    /// Settings of the graph data needed at runtime.
    /// </summary>
    public class GraphSettings
    {
        /// <summary>
        /// The prefix of the absolute paths for the GXL and CSV data.
        /// </summary>        
        public string pathPrefix = null;

        /// Clone graph with one directory and two files contained therein.
        //public string gxlPath = "..\\Data\\GXL\\two_files.gxl";
        //public string csvPath = "..\\Data\\GXL\\two_files.csv";

        /// Clone graph with one directory and three files contained therein.
        //public string gxlPath = "..\\Data\\GXL\\three_files.gxl";
        //public string csvPath = "..\\Data\\GXL\\three_files.csv";

        /// Very tiny clone graph with single root, one child as a leaf and 
        /// two more children with two children each to experiment with.
        //public string gxlPath = "..\\Data\\GXL\\micro_clones.gxl";
        //public string csvPath = "..\\Data\\GXL\\micro_clones.csv";

        /// Tiny clone graph with single root to experiment with.
        public string gxlPath = "..\\Data\\GXL\\minimal_clones.gxl";
        public string csvPath = "..\\Data\\GXL\\minimal_clones.csv";

        /// Tiny clone graph with single roots to check edge bundling.
        //public string gxlPath = "..\\Data\\GXL\\controlPoints.gxl";
        //public string csvPath = "..\\Data\\GXL\\controlPoints.csv";

        // Smaller clone graph with single root (Linux directory "fs").
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\fs.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\fs.csv";

        // Smaller clone graph with single root (Linux directory "net").
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\net.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\net.csv";

        // Larger clone graph with single root (Linux directory "drivers"): 16.920 nodes, 10583 edges.
        //public string gxlPath = "..\\Data\\GXL\\linux-clones\\drivers.gxl";
        //public string csvPath = "..\\Data\\GXL\\linux-clones\\drivers.csv";

        // Medium size include graph with single root (OpenSSL).
        //public string gxlPath = "..\\Data\\GXL\\OpenSSL\\openssl-include.gxl";
        //public string csvPath = "..\\Data\\GXL\\OpenSSL\\openssl-include.csv";

        /// <summary>
        /// Returns the concatenation of pathPrefix and gxlPath. That is the complete
        /// absolute path to the GXL file containing the graph data.
        /// </summary>
        /// <returns>concatenation of pathPrefix and gxlPath</returns>
        public string GXLPath()
        {
            return pathPrefix + gxlPath;
        }

        /// <summary>
        /// Returns the concatenation of pathPrefix and csvPath. That is the complete
        /// absolute path to the CSV file containing the additional metric values.
        /// </summary>
        /// <returns>concatenation of pathPrefix and csvPath</returns>
        public string CSVPath()
        {
            return pathPrefix + csvPath;
        }
        
        /// <summary>
        /// The names of the edge types of hierarchical edges.
        /// </summary>
        public HashSet<string> HierarchicalEdges = Hierarchical_Edge_Types();

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
        // Visual attributes of a leaf node
        //---------------------------------
        //
        // Width of a building (x co-ordinate)
        public string WidthMetric = "Metric.Number_of_Tokens";
        // Height of a building (y co-ordinate)
        public string HeightMetric = "Metric.Clone_Rate";
        // Breadth of a building (y co-ordinate)
        public string DepthMetric = "Metric.LOC";
        // Metric determining the color gradient of leaf nodes.
        public string ColorMetric = "Metric.Complexity";

        // This parameter determines the minimal width, breadth, and height of each block
        // representing a graph node visually. Must not be greater than MaximalBlockLength.
        public float MinimalBlockLength = 0.1f;

        // This parameter determines the maximal width, breadth, and height of each block
        // representing a graph node visually. Must not be smaller than MinimalBlockLength.
        public float MaximalBlockLength = 100.0f;

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
            Donuts,
            Circles,
            Empty,
            Cylinders,
        }

        /// <summary>
        /// What kinds of game objects are to be created for leaf nodes in the graph.
        /// </summary>
        public LeafNodeKinds LeafObjects;

        /// <summary>
        /// What kinds of game objects are to be created for inner graph nodes.
        /// </summary>
        public InnerNodeKinds InnerNodeObjects;

        /// <summary>
        /// The kinds of node layouts available.
        /// </summary>
        public enum NodeLayouts
        {
            Balloon,
            CirclePacking,
            Manhattan,
            Treemap
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

        // The layout that should be used for nodes.
        public NodeLayouts NodeLayout;

        // The layout that should be used for edges.
        public EdgeLayouts EdgeLayout;

        // Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        // for range [0, max-value] is used, where max-value is the maximum value of a metric.
        public bool ZScoreScale = true;

        // The width of edges.
        public float EdgeWidth = 0.3f;

        /// <summary>
        /// Whether erosions should be visible above blocks.
        /// </summary>
        public bool ShowErosions = false;

        /// <summary>
        /// Orientation of the edges; 
        /// if false, the edges are drawn below the houses;
        /// if true, the edges are drawn above the houses;
        /// </summary>
        public bool EdgesAboveBlocks = true;

        //--------------------------------------------------------
        // Software erosion issues shown as icons above leaf nodes
        //--------------------------------------------------------
        //
        public string ArchitectureIssue = "Metric.Architecture_Violations";
        public string CloneIssue = "Metric.Clone";
        public string CycleIssue = "Metric.Cycle";
        public string Dead_CodeIssue = "Metric.Dead_Code";
        public string MetricIssue = "Metric.Metric";
        public string StyleIssue = "Metric.Style";
        public string UniversalIssue = "Metric.Universal";

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
        //
        public string ArchitectureIssue_SUM = "Metric.Architecture_Violations_SUM";
        public string CloneIssue_SUM = "Metric.Clone_SUM";
        public string CycleIssue_SUM = "Metric.Cycle_SUM";
        public string Dead_CodeIssue_SUM = "Metric.Dead_Code_SUM";
        public string MetricIssue_SUM = "Metric.Metric_SUM";
        public string StyleIssue_SUM = "Metric.Style_SUM";
        public string UniversalIssue_SUM = "Metric.Universal_SUM";

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
        public string InnerDonutMetric = "Metric.IssuesTotal";

        /// <summary>
        /// Yields a mapping of all node attribute names that define erosion issues 
        /// for leaf nodes in the GXL file onto the icons to be used for visualizing them.
        /// </summary>
        /// <returns>mapping of all node attribute names for leaves onto icon ids</returns>
        public SerializableDictionary<string, IconFactory.Erosion> LeafIssueMap()
        {
            SerializableDictionary<string, IconFactory.Erosion> result = new SerializableDictionary<string, IconFactory.Erosion>
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
    }
}
