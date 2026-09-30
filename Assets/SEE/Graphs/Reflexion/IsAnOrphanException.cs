namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a node is an orphan (i.e., has a parent) when it's not expected to be one.
    /// </summary>
    public class IsAnOrphanException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is an orphan.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Constructs a new <see cref="IsAnOrphanException"/> with the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node that is an orphan.</param>
        public IsAnOrphanException(Node node) : base($"Node '{node.ToShortString()}' does not have any parents!")
        {
            Node = node;
        }
    }
}
