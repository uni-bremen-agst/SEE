namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes one validated Source Link document mapping.
    /// </summary>
    internal readonly record struct ExternalSourceLinkMapping
    {
        /// <summary>
        /// Initializes a Source Link mapping.
        /// </summary>
        /// <param name="documentPattern">The original document pattern.</param>
        /// <param name="target">The original source-locator target.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="documentPattern"/> or
        /// <paramref name="target"/> is <see langword="null"/>.
        /// </exception>
        public ExternalSourceLinkMapping(string documentPattern, string target)
        {
            ArgumentNullException.ThrowIfNull(documentPattern);
            ArgumentNullException.ThrowIfNull(target);

            DocumentPattern = documentPattern;
            Target = target;
        }

        /// <summary>
        /// Gets the original Source Link document pattern.
        /// </summary>
        /// <value>The unmodified document pattern.</value>
        public string DocumentPattern { get; }

        /// <summary>
        /// Gets the original Source Link target.
        /// </summary>
        /// <value>The unmodified source-locator target.</value>
        public string Target { get; }
    }
}
