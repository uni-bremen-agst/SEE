using SEE.DataModel;
using System.Collections.Generic;

namespace SEE.Layout.Utils
{
    /// <summary>
    /// Provides extensions to SEE.DataModel.Graph related to layout properties.
    /// </summary>
    public static class LayoutGraphExtensions
    {
        /// <summary>
        /// Returns all edges of graph whose source and target is contained in <paramref name="selectedNodes"/>.
        /// </summary>
        /// <param name="selectedNodes">the set of nodes for which to determine the connecting edges</param>
        /// <returns>all edges of graph whose source and target is contained in <paramref name="selectedNodes"/></returns>
        public static IList<Edge> ConnectingEdges(this Graph graph, ICollection<ILayoutNode> selectedNodes)
        {
            IList<Edge> result = new List<Edge>();

            foreach (Edge edge in graph.Edges())
            {
                if (FilterListForLayoutNode(edge.Source.ID, selectedNodes) && FilterListForLayoutNode(edge.Target.ID, selectedNodes))
                {
                    result.Add(edge);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if there is a node with the given <paramref name="ID"/> contained in <paramref name="layoutNodes"/>.
        /// </summary>
        /// <param name="ID">the requested node ID</param>
        /// <param name="layoutNodes">the set of nodes for which to determine whether they have a matching ID</param>
        /// <returns>true if the node with the given <paramref name="ID"/> is contained in <paramref name="layoutNodes"/></returns>
        private static bool FilterListForLayoutNode(string ID, ICollection<ILayoutNode> layoutNodes)
        {
            foreach (ILayoutNode gameNode in layoutNodes)
            {
                if (gameNode.ID == ID)
                {
                    return true;
                }
            }
            return false;
        }
    }

}