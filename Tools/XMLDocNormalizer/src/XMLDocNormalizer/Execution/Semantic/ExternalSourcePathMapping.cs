namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Maps one Portable PDB document-name prefix to an explicitly permitted
    /// local source root without assigning source identity to either path.
    /// </summary>
    internal sealed record ExternalSourcePathMapping
    {
        /// <summary>
        /// Initializes one immutable local source projection.
        /// </summary>
        /// <param name="documentPrefix">
        /// The nonempty Portable PDB document-name prefix.
        /// </param>
        /// <param name="localRoot">The fully qualified permitted local root.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when an argument is <see langword="null"/>.
        /// </exception>
        public ExternalSourcePathMapping(string documentPrefix, string localRoot)
        {
            ArgumentNullException.ThrowIfNull(documentPrefix);
            ArgumentNullException.ThrowIfNull(localRoot);

            DocumentPrefix = documentPrefix;
            LocalRoot = localRoot;
        }

        /// <summary>
        /// Gets the Portable PDB document-name prefix.
        /// </summary>
        /// <value>The exact caller-supplied prefix.</value>
        public string DocumentPrefix { get; }

        /// <summary>
        /// Gets the permitted local source root.
        /// </summary>
        /// <value>The caller-supplied root before acquisition normalization.</value>
        public string LocalRoot { get; }
    }
}
