namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a propagated edge can't be created because such a propagated edge already exists.
    /// </summary>
    public class AlreadyPropagatedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The edge that was already propagated.
        /// </summary>
        public readonly Edge PropagatedEdge;

        /// <summary>
        /// The edge from which <see cref="PropagatedEdge"/> originates from.
        ///
        /// NOTE: This is the edge that was passed along to the function which is supposed to construct the
        /// propagated edge. As such, it may not necessarily be accurate.
        /// </summary>
        public readonly Edge OriginatingEdge;

        /// <summary>
        /// Constructs a new <see cref="AlreadyPropagatedException"/> with the given <paramref name="propagatedEdge"/>
        /// and <paramref name="originatingEdge"/>.
        /// </summary>
        /// <param name="propagatedEdge">The edge that was already propagated.</param>
        /// <param name="originatingEdge">The edge from which <paramref name="propagatedEdge"/> originates.</param>
        public AlreadyPropagatedException(Edge propagatedEdge, Edge originatingEdge)
            : base($"Propagated edge already exists: '{propagatedEdge.ToShortString()}' "
                   + $"(originated from '{originatingEdge.ToShortString()}')!")
        {
            PropagatedEdge = propagatedEdge;
            OriginatingEdge = originatingEdge;
        }
    }
}
