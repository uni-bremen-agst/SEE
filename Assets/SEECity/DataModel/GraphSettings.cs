
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
        
        /// Small clone graph with multiple roots to experiment with.
        //public string graphPath = "C:\\Users\\raine\\develop\\seecity\\data\\gxl\\minimal_clones.gxl";

        // Medium-size clone graph with single root (Linux directories whose path include "arch").
        public string graphPath = "C:\\Users\\raine\\develop\\SEECity\\Data\\GXL\\linux-clones\\arch-single-root.gxl";

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
        public static HashSet<string> HierarchicalEdges = Hierarchical_Edge_Types();

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

    }
}
