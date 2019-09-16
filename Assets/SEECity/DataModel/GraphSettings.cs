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
        /// The path to the graph data.
        /// </summary>

        /// Small clone graph with multiple roots to experiment with (we cannot use Application.dataPath
        /// unfortunately, that is why this path is currently hard coded).
        //public string graphPath = "C:\\Users\\raine\\develop\\seecity\\data\\gxl\\minimal_clones.gxl";
        //public string CSVPath = "C:\\Users\\raine\\develop\\seecity\\data\\gxl\\minimal_erosions.csv";

        // Smaller Linux subsystem net with 1375 files
        //public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\net.gxl";
        //public string CSVPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\net.csv";

        // Smaller Linux subsystem fs with 1288 nodes
        public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\fs.gxl";
        public string CSVPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\fs.csv";

        // Medium-size clone graph with single root (Linux directories whose path include "arch").
        //public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\arch-single-root.gxl";
        //public string CSVPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\arch-single-root.csv";

        // Larger clone graph with single root (Linux subsystem 'drivers'), but with only those subsystems containing clones.
        // 14.500 nodes, 10.300 edges.
        // This graph is currently too large to play. The frame rate drops to 3 frames per second.
        //public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\drivers-clones-only-single-root.gxl";

        // Larger clone graph with single root (Linux subsystem 'drivers').
        // This graph is currently too large to play. The frame rate drops to 3 frames per second.
        // public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\drivers-single-root.gxl";

        // Large clone graph with multiple roots.
        //public string graphPath = "C:\\Users\\raine\\develop\\evostreets\\data\\gxl\\linux-clones\\clones.gxl";

        // Medium-size clone graph with multiple root (Linux directories whose path include "arch").
        //public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\arch.gxl";

        // Large clone graph with single root.
        //public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\linux-clones.gxl";

        // The following graph will not work because it does not have the necessary metrics.
        // public string graphPath = "C:\\Users\\raine\\Downloads\\codefacts.gxl";

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
        public string BreadthMetric = "Metric.LOC";

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
