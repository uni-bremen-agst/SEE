namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if an unspecified edge was given when a specified edge was expected in an operation.
    /// </summary>
    public class ExpectedSpecifiedEdgeException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The edge that was unexpectedly unspecified.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructs a new <see cref="ExpectedSpecifiedEdgeException"/> with the given <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The edge that was unexpectedly unspecified.</param>
        public ExpectedSpecifiedEdgeException(Edge edge)
            : base($"Given edge '{edge.ToShortString()}' is not a specified edge!")
        {
            Edge = edge;
        }
    }
}
