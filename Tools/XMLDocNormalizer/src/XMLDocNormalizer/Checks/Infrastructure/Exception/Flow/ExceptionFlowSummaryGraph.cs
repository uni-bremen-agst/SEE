namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Stores compact context-sensitive callable summaries and the work queue
    /// used to build them nonrecursively.
    /// </summary>
    internal sealed class ExceptionFlowSummaryGraph
    {
        /// <summary>
        /// Stores graph nodes by normalized callable and context key.
        /// </summary>
        private readonly Dictionary<
            ExceptionFlowCallableKey,
            ExceptionFlowSummary> summaries =
                new();

        /// <summary>
        /// Stores graph-node keys that still require local analysis.
        /// </summary>
        private readonly Queue<ExceptionFlowCallableKey> pendingKeys =
            new();

        /// <summary>
        /// Gets the number of callable summaries in the graph.
        /// </summary>
        /// <value>The number of distinct context-sensitive graph nodes.</value>
        public int Count =>
            summaries.Count;

        /// <summary>
        /// Gets an existing summary or creates and schedules a new summary.
        /// </summary>
        /// <param name="key">
        /// The context-sensitive callable key.
        /// </param>
        /// <returns>
        /// The existing or newly created callable summary.
        /// </returns>
        public ExceptionFlowSummary GetOrAdd(
            ExceptionFlowCallableKey key)
        {
            if (summaries.TryGetValue(
                    key,
                    out ExceptionFlowSummary? summary))
            {
                return summary;
            }

            summary = new ExceptionFlowSummary();

            summaries.Add(
                key,
                summary);

            pendingKeys.Enqueue(
                key);

            return summary;
        }

        /// <summary>
        /// Attempts to retrieve a callable summary.
        /// </summary>
        /// <param name="key">The callable key to resolve.</param>
        /// <param name="summary">
        /// The resolved summary when the key exists.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the graph contains the key; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryGetSummary(
            ExceptionFlowCallableKey key,
            out ExceptionFlowSummary? summary)
        {
            return summaries.TryGetValue(
                key,
                out summary);
        }

        /// <summary>
        /// Gets the required summary for a callable key.
        /// </summary>
        /// <param name="key">The callable key to resolve.</param>
        /// <returns>The associated callable summary.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the graph does not contain
        /// <paramref name="key"/>.
        /// </exception>
        public ExceptionFlowSummary GetRequiredSummary(
            ExceptionFlowCallableKey key)
        {
            if (summaries.TryGetValue(
                    key,
                    out ExceptionFlowSummary? summary))
            {
                return summary;
            }

            throw new KeyNotFoundException(
                "The exception-flow summary graph does not contain " +
                "the requested callable key.");
        }

        /// <summary>
        /// Attempts to dequeue the next callable key requiring local
        /// analysis.
        /// </summary>
        /// <param name="key">
        /// The next pending key when one is available.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a key was dequeued; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public bool TryDequeuePending(
            out ExceptionFlowCallableKey key)
        {
            if (pendingKeys.Count == 0)
            {
                key = default;
                return false;
            }

            key = pendingKeys.Dequeue();
            return true;
        }
    }
}
