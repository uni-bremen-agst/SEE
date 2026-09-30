namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if given node or edge is not contained in the correct (or any) subgraph.
    /// </summary>
    public class NotInSubgraphException : ArchitectureAnalysisException
    {
        /// <summary>
        /// Subgraph the <see cref="Element"/> was expected to be in.
        /// </summary>
        public readonly ReflexionSubgraphs ExpectedSubgraph;

        /// <summary>
        /// The graph element that was not contained in <see cref="ExpectedSubgraph"/>.
        /// </summary>
        public readonly GraphElement Element;

        /// <summary>
        /// Constructs a new <see cref="NotInSubgraphException"/> with the given
        /// <paramref name="expectedSubgraph"/> and <paramref name="element"/>.
        /// </summary>
        /// <param name="expectedSubgraph">The subgraph the <paramref name="element"/> was expected to be in.</param>
        /// <param name="element">The graph element that was not contained in <paramref name="expectedSubgraph"/>.</param>
        public NotInSubgraphException(ReflexionSubgraphs expectedSubgraph, GraphElement element)
            : base($"Given {element.GetType().Name} '{element.ToShortString()}' must be contained in the {expectedSubgraph} graph!")
        {
            ExpectedSubgraph = expectedSubgraph;
            Element = element;
        }
    }
}
