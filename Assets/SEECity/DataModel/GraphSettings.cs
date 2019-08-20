
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
        public string graphPath = "C:\\Users\\raine\\develop\\seecity\\data\\gxl\\minimal_clones.gxl";
        //public string graphPath = "C:\\Users\\raine\\develop\\evostreets\\data\\gxl\\linux-clones\\clones.gxl";
        // The following graph will not work because it does not have the necessary metrics.
        // public string graphPath = "C:\\Users\\raine\\Downloads\\codefacts.gxl";

        /// <summary>
        /// The name of the edge type of hierarchical edges.
        /// </summary>
        public string hierarchicalEdgeType = "Enclosing";
    }
}
