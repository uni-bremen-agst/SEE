namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a specified edge was given when a propagated edge was expected in an operation.
    /// </summary>
    public class ExpectedPropagatedEdgeException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The edge that was unexpectedly specified.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructs a new <see cref="ExpectedPropagatedEdgeException"/> with the given <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The edge that was unexpectedly propagated.</param>
        public ExpectedPropagatedEdgeException(Edge edge)
            : base($"Given edge '{edge.ToShortString()}' is a specified (not propagated) edge!")
        {
            Edge = edge;
        }
    }
}
