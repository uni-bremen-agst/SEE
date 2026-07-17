namespace XMLDocNormalizer.Checks.Infrastructure.Namespace
{
    /// <summary>
    /// Holds aggregated documentation state for one (directory, namespace) key.
    /// </summary>
    /// <remarks>
    /// This state is used by <see cref="NamespaceDocumentationAggregator"/> to decide whether a namespace
    /// has central documentation and where the first missing documentation occurrence was observed.
    /// </remarks>
    internal sealed class NamespaceDocState
    {
        /// <summary>
        /// Gets or sets a value indicating whether a central namespace documentation declaration exists
        /// in a preferred namespace documentation file.
        /// </summary>
        /// <value>
        /// True if a central namespace documentation declaration exists in a preferred namespace documentation file; otherwise false.
        /// </value>
        public bool HasCentralDocumentation { get; set; }

        /// <summary>
        /// Gets or sets the first recorded location where this namespace was encountered without documentation.
        /// </summary>
        /// <value>
        /// The first recorded location where this namespace was encountered without documentation, or null if no missing location was recorded.
        /// </value>
        public NamespaceMissingLocation? FirstMissingLocation { get; set; }
    }
}