namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a node is not an orphan (i.e., has a parent) when it's expected to be one.
    /// </summary>
    public class NotAnOrphanException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is not an orphan.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Constructs a new <see cref="NotAnOrphanException"/> with the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node that is not an orphan.</param>
        public NotAnOrphanException(Node node)
            : base($"Node '{node.ToShortString()}' is already a child of '{node.Parent.ToShortString()}'!")
        {
            Node = node;
        }
    }
}
