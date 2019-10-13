using SEE.Layout;
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
        public string pathPrefix;

        /// Tiny clone graph with single root to experiment with.
        public string gxlPath = "..\\Data\\GXL\\minimal_clones.gxl";
        public string csvPath = "..\\Data\\GXL\\minimal_erosions.csv";

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

        //-------------------------------
        // Size attributes of a building
        //-------------------------------
        //
        // Width of a building (x co-ordinate)
        public string WidthMetric = "Metric.Number_of_Tokens";
        // Height of a building (y co-ordinate)
        public string HeightMetric = "Metric.Clone_Rate";
        // Breadth of a building (y co-ordinate)
        public string DepthMetric = "Metric.LOC";

        // This parameter determines the minimal width, breadth, and height of each block
        // representing a graph node visually. Must not be greater than MaximalBlockLength.
        public float MinimalBlockLength = 0.1f;

        // This parameter determines the maximal width, breadth, and height of each block
        // representing a graph node visually. Must not be smaller than MinimalBlockLength.
        public float MaximalBlockLength = 100.0f;

        //------------------------------------------------------
        // Software erosion issues shown as icons above building
        //------------------------------------------------------
        //
        public string ArchitectureIssue = "Metric.Architecture_Violations";
        public string CloneIssue = "Metric.Clone";
        public string CycleIssue = "Metric.Cycle";
        public string Dead_CodeIssue = "Metric.Dead_Code";
        public string MetricIssue = "Metric.Metric";
        public string StyleIssue = "Metric.Style";
        public string UniversalIssue = "Metric.Universal";

        public enum Layouts
        {
            Balloon = 0,
            Manhattan = 1,
            CirclePacking = 2
        }

        // The layout that should be used.
        public Layouts Layout;

        // Whether ZScore should be used for normalizing node metrics. If false, linear interpolation
        // for range [0, max-value] is used, where max-value is the maximum value of a metric.
        public bool ZScoreScale = true;

        // Whether CScape building should be used to visualize graph nodes. If false, cubes are used.
        public bool CScapeBuildings = true;

        // The width of edges.
        public float EdgeWidth = 1.0f;

        /// <summary>
        /// Whether erosions should be visible above blocks.
        /// </summary>
        public bool ShowErosions = true;

        /// <summary>
        /// Whether Donut charts should be visible for circles in the Ballon layout.
        /// </summary>
        public bool ShowDonuts = true;

        /// <summary>
        /// Whether edges should be shown.
        /// </summary>
        public bool ShowEdges = true;

        public string[] InnerNodeMetrics = new string[] { "Metric.Quality",
                                                          "Metric.McCabe_Complexity.sum",
                                                          "Metric.Number_Of_Statements.sum",
                                                          "Metric.Lines.Comment.sum",
                                                          "Metric.Lines.LOC.sum" };

        /// <summary>
        /// Yields a mapping of all node attribute names that define erosion issues in the GXL file
        /// onto the icons to be used for visualizing them.
        /// </summary>
        /// <returns>mapping of all node attribute names onto icon ids</returns>
        public SerializableDictionary<string, IconFactory.Erosion> IssueMap()
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
