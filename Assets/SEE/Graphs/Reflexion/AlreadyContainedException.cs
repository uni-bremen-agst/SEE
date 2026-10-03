namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if a new graph element was already added to a graph.
    /// </summary>
    public class AlreadyContainedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The element that already exists in the graph.
        /// </summary>
        public readonly GraphElement ExistingElement;

        /// <summary>
        /// Constructs a new <see cref="AlreadyContainedException"/> with the given <paramref name="existingElement"/>.
        /// </summary>
        /// <param name="existingElement">The element that already exists in the graph.</param>
        public AlreadyContainedException(GraphElement existingElement)
            : base($"'{existingElement.ToShortString()}' is already present in the graph!")
        {
            ExistingElement = existingElement;
        }
    }
}
