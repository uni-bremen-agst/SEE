namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Thrown if an already explicitly mapped node is mapped somewhere else.
    /// </summary>
    public class AlreadyExplicitlyMappedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is already mapped to <see cref="MappedTo"/>.
        /// </summary>
        public readonly Node AlreadyMapped;

        /// <summary>
        /// The node that <see cref="AlreadyMapped"/> is mapped to.
        /// </summary>
        public readonly Node MappedTo;

        /// <summary>
        /// Constructs a new <see cref="AlreadyExplicitlyMappedException"/> with the given <paramref name="alreadyMapped"/>
        /// and <paramref name="mappedTo"/>.
        /// </summary>
        /// <param name="alreadyMapped">The node that is already mapped to <paramref name="mappedTo"/>.</param>
        /// <param name="mappedTo">The node that <paramref name="alreadyMapped"/> is mapped to.</param>
        public AlreadyExplicitlyMappedException(Node alreadyMapped, Node mappedTo)
            : base($"Node '{alreadyMapped.ToShortString()}' is already explicitly mapped to '{mappedTo.ToShortString()}'.")
        {
            SEE.Utils.Assertion.IsNotNull(mappedTo);
            AlreadyMapped = alreadyMapped;
            MappedTo = mappedTo;
        }
    }
}
