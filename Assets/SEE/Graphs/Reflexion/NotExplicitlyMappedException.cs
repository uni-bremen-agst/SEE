namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a node is not explicitly mapped, but was expected to.
    /// </summary>
    public class NotExplicitlyMappedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is not explicitly mapped.
        /// </summary>
        public readonly Node UnmappedNode;

        /// <summary>
        /// Constructs a new <see cref="NotExplicitlyMappedException"/> with the given <paramref name="unmappedNode"/>.
        /// </summary>
        /// <param name="unmappedNode">The node that is not explicitly mapped.</param>
        public NotExplicitlyMappedException(Node unmappedNode)
            : base($"Implementation node '{unmappedNode.ToShortString()}' is not explicitly mapped.")
        {
            SEE.Utils.Assertion.IsTrue(unmappedNode.IsInImplementation());
            UnmappedNode = unmappedNode;
        }
    }
}
