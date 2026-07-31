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
        /// Stores the call contexts required to analyze graph nodes.
        /// </summary>
        private readonly Dictionary<
            ExceptionFlowCallableKey,
            ExceptionFlowCallContext> callContexts =
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
        /// Gets an existing summary or creates and schedules a new summary
        /// together with the call context required for its local analysis.
        /// </summary>
        /// <param name="key">
        /// The context-sensitive callable key.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the callable.
        /// </param>
        /// <returns>
        /// The existing or newly created callable summary.
        /// </returns>
        public ExceptionFlowSummary GetOrAdd(
            ExceptionFlowCallableKey key,
            ExceptionFlowCallContext callContext)
        {
            ExceptionFlowSummary summary =
                GetOrAdd(key);

            if (!callContexts.ContainsKey(key))
            {
                callContexts.Add(
                    key,
                    callContext);
            }

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
        /// Attempts to retrieve the call context associated with a graph
        /// node.
        /// </summary>
        /// <param name="key">
        /// The context-sensitive callable key.
        /// </param>
        /// <param name="callContext">
        /// The associated call context when one exists.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a call context was registered;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public bool TryGetCallContext(
            ExceptionFlowCallableKey key,
            out ExceptionFlowCallContext? callContext)
        {
            return callContexts.TryGetValue(
                key,
                out callContext);
        }

        /// <summary>
        /// Removes and returns the next callable key requiring local analysis.
        /// </summary>
        /// <returns>
        /// The next pending key, or <see langword="null"/> when no pending
        /// key remains.
        /// </returns>
        public ExceptionFlowCallableKey? DequeuePendingOrDefault()
        {
            if (pendingKeys.Count == 0)
            {
                return null;
            }

            return pendingKeys.Dequeue();
        }
    }
}
