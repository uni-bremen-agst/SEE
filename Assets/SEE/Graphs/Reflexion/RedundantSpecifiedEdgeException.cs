namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a redundant specified edge would come into existence as the result of an operation.
    /// </summary>
    public class RedundantSpecifiedEdgeException : ArchitectureAnalysisException
    {
        /// <summary>
        /// First of the two redundant edges.
        /// </summary>
        public readonly Edge FirstEdge;

        /// <summary>
        /// Second of the two redundant edges.
        /// <b>Note that this edge isn't necessarily contained in any graph!</b>
        /// </summary>
        public readonly Edge SecondEdge;

        /// <summary>
        /// Constructs a new <see cref="RedundantSpecifiedEdgeException"/> with the given <paramref name="firstEdge"/>
        /// and <paramref name="secondEdge"/>.
        /// </summary>
        /// <param name="firstEdge">The first of the two redundant edges.</param>
        /// <param name="secondEdge">The second of the two redundant edges.</param>
        public RedundantSpecifiedEdgeException(Edge firstEdge, Edge secondEdge)
            : base($"Edge '{firstEdge.ToShortString()}' would be redundant to '{secondEdge.ToShortString()}'!")
        {
            FirstEdge = firstEdge;
            SecondEdge = secondEdge;
        }
    }
}
