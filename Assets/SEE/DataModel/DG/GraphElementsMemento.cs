namespace SEE.DataModel.DG
{
    /// <summary>
    /// A memento of elements of a graph.
    /// </summary>
    public abstract class GraphElementsMemento
    {
        /// <summary>
        /// Constructor setting <see cref="ItsGraph"/> to given <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">graph from which the graph elements memorized stem</param>
        public GraphElementsMemento(Graph graph)
        {
            ItsGraph = graph;
        }

        /// <summary>
        /// The graph from which the nodes and edges of this subgraph memento stem.
        /// </summary>
        public readonly Graph ItsGraph;

        /// <summary>
        /// Restores the graph, that is, re-adds the elements to <see cref="ItsGraph"/>.
        /// </summary>
        public abstract void Restore();
    }
}