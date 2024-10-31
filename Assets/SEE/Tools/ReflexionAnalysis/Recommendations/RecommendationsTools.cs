using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// This class provides extension methods used for calculate candidate recommendations.
    /// </summary>
    public static class RecommendationsTools
    {
        /// <summary>
        /// Returns all edges associated with a node, which are part of the 
        /// implementation subgraph
        /// </summary>
        /// <param name="node">given implementation node</param>
        /// <returns>All incoming and outgoing edges that are associated with the node</returns>
        public static List<Edge> GetImplementationEdges(this Node node)
        {
            List<Edge> edges = new List<Edge>();
            edges.AddRange(node.Incomings);
            edges.AddRange(node.Outgoings);
            edges = edges.Distinct().Where(x => x.IsInImplementation()).ToList();
            return edges;
        }

        /// <summary>
        /// Maps a implementation node to a given architecture node, while supressing
        /// notificiations. 
        /// </summary>
        /// <param name="reflexionGraph">Given reflexion graph this method is operating on.</param>
        /// <param name="cluster">Given architecture node</param>
        /// <param name="candidate">Given implementation node</param>
        public static void AddToMappingSilent(this ReflexionGraph reflexionGraph, 
                                              Node cluster, 
                                              Node candidate,
                                              bool overrideMapping = false)
        {
            bool suppressNotifications = reflexionGraph.SuppressNotifications;
            reflexionGraph.SuppressNotifications = true;
            reflexionGraph.AddToMapping(candidate, cluster, overrideMapping);
            reflexionGraph.SuppressNotifications = suppressNotifications;
        }

        /// <summary>
        /// Removes a implementation node from a given architecture node, while supressing
        /// notificiations. 
        /// </summary>
        /// <param name="reflexionGraph">Given reflexion graph this method is operating on.</param>
        /// <param name="cluster">Given architecture node</param>
        /// <param name="candidate">Given implementation node</param>s
        public static void RemoveFromMappingSilent(this ReflexionGraph reflexionGraph, Node candidate)
        {
            bool suppressNotifications = reflexionGraph.SuppressNotifications;
            reflexionGraph.SuppressNotifications = true;
            reflexionGraph.RemoveFromMapping(candidate);
            reflexionGraph.SuppressNotifications = suppressNotifications;
        }
    }
}