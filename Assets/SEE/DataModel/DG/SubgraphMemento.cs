using System.Collections.Generic;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// A memento for a node tree including its incoming and outgoing edges (whose targets
    /// and sources need not necessarily be nodes in this tree).
    ///
    /// Assumption: All nodes and edges in this memento are from the same graph.
    /// </summary>
    public class SubgraphMemento : GraphElementsMemento
    {
        /// <summary>
        /// Constructor setting <see cref="ItsGraph"/> to <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph from which the memorized graph elements stem</param>
        public SubgraphMemento(Graph graph) : base(graph) { }

        /// <summary>
        /// Edges of this subgraph memento. These can be edges whose source and target
        /// are both in <see cref="ItsGraph"/> but there may be also be edges where either
        /// the source or the target is a node not in this subgraph.
        /// </summary>
        public readonly ISet<Edge> Edges = new HashSet<Edge>();

        /// <summary>
        /// A mapping of every node in this subgraph memento onto its original parent.
        /// <see cref="Parents.Keys"/> constitute the subgraph. Note: There may be
        /// one node in <see cref="Parents.Values"/> not contained in <see cref="Parents.Keys"/>;
        /// this node would be the root of the deleted subgraph whose parent is not contained in this
        /// subgraph.
        /// </summary>
        public readonly IDictionary<Node, Node> Parents = new Dictionary<Node, Node>();

        /// <summary>
        /// Restores all nodes and edges and the node hierarchy in <see cref="ItsGraph"/>.
        /// </summary>
        public override void Restore()
        {
            if (Parents.Count > 0)
            {
                // We have nodes in this subgraph.
                RestoreTree(this);
            }
            else
            {
                // The subgraph has no nodes, but possibly edges.
                foreach (Edge edge in Edges)
                {
                    ItsGraph.AddEdge(edge);
                }
            }
        }

        /// <summary>
        /// Re-adds all nodes and edges in <paramref name="subgraph"/> to
        /// <paramref name="subgraph.ItsGraph"/> and restores the node hierarchy
        /// according to <paramref name="subgraph.Parents"/>.
        /// </summary>
        /// <param name="subgraph">subgraph to be restored</param>
        private static void RestoreTree(SubgraphMemento subgraph)
        {
            if (subgraph == null)
            {
                return;
            }
            foreach (Node node in subgraph.Parents.Keys)
            {
                subgraph.ItsGraph.AddNode(node);
            }
            foreach (KeyValuePair<Node, Node> node in subgraph.Parents)
            {
                Node parent = node.Value;
                parent?.AddChild(node.Key);
            }
            foreach (Edge edge in subgraph.Edges)
            {
                subgraph.ItsGraph.AddEdge(edge);
            }
        }
    }
}
