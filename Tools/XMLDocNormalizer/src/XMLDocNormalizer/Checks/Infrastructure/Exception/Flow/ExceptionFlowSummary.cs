namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Represents the compact local analysis result of one callable and call
    /// context.
    /// </summary>
    internal sealed class ExceptionFlowSummary
    {
        /// <summary>
        /// Stores the locally collected summary content.
        /// </summary>
        private readonly ExceptionFlowSummaryFragment content =
            new();

        /// <summary>
        /// Gets a value indicating whether at least one executable
        /// declaration body was analyzed.
        /// </summary>
        /// <value>
        /// <see langword="true"/> after an executable body has been analyzed;
        /// otherwise <see langword="false"/>.
        /// </value>
        public bool HasExecutableBody { get; private set; }

        /// <summary>
        /// Gets the directly collected exception sources.
        /// </summary>
        /// <value>The local exception sources.</value>
        public IReadOnlyList<ExceptionFlowSummarySource> Sources =>
            content.Sources;

        /// <summary>
        /// Gets the outgoing source-level call edges.
        /// </summary>
        /// <value>The outgoing call edges.</value>
        public IReadOnlyList<ExceptionFlowSummaryCallEdge> CallEdges =>
            content.CallEdges;

        /// <summary>
        /// Gets the unresolved callable targets.
        /// </summary>
        /// <value>The unresolved target display names.</value>
        public IReadOnlySet<string> UncertainTargets =>
            content.UncertainTargets;

        /// <summary>
        /// Marks that at least one executable declaration body was analyzed
        /// for this callable.
        /// </summary>
        public void MarkExecutableBodyAnalyzed()
        {
            HasExecutableBody = true;
        }

        /// <summary>
        /// Merges one completed syntax fragment into this callable summary.
        /// </summary>
        /// <param name="fragment">
        /// The fragment whose local content should be copied.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="fragment"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void Merge(
            ExceptionFlowSummaryFragment fragment)
        {
            ArgumentNullException.ThrowIfNull(fragment);

            content.Merge(fragment);
        }
    }
}
