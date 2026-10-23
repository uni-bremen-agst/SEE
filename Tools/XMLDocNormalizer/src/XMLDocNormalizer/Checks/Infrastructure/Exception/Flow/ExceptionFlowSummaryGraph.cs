namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Stores compact context-sensitive callable summaries and the work queue
    /// used to build them nonrecursively.
    /// </summary>
    internal sealed class ExceptionFlowSummaryGraph : IExceptionFlowSummaryTargetSink
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

        private ExceptionFlowFiniteOrbitSeal? admission;
        private ExceptionFlowSemanticEnvironment? productionEnvironment;
        private readonly List<ExceptionFlowCallableKey> transactionNodes = [];
        private string? scopeDigest;

        /// <summary>Gets or sets the active sequential construction frame.</summary>
        internal ExceptionFlowCallableKey? BuildingKey { get; set; }

        /// <summary>Gets or sets the active invocation context.</summary>
        internal ExceptionFlowCallContext? BuildingContext { get; set; }

        /// <summary>Gets or sets the compilation interpreting the active body.</summary>
        internal Microsoft.CodeAnalysis.Compilation? BuildingCompilation { get; set; }

        /// <summary>Starts a sealed production construction transaction.</summary>
        /// <param name="inventory">The successfully admitted declaration inventory.</param>
        internal void BeginProduction(ExceptionFlowFiniteOrbitSeal inventory,
            ExceptionFlowSemanticEnvironment environment)
        {
            if (!inventory.ScopeUnchanged(environment.GetAnalysisScopes().Select(scope => scope.Compilation)))
                throw new ExceptionFlowProfileViolation("ScopeMapNotClosed", "Scope changed before production registration.");
            if (scopeDigest != null && scopeDigest != inventory.ScopeDigest)
            {
                summaries.Clear(); callContexts.Clear(); pendingKeys.Clear();
            }
            scopeDigest = inventory.ScopeDigest;
            admission = inventory;
            productionEnvironment = environment;
            transactionNodes.Clear();
        }

        /// <summary>Removes newly registered nodes if construction did not complete under its seal.</summary>
        /// <param name="completed">Whether all pending bodies were constructed under the seal.</param>
        internal void EndProduction(bool completed)
        {
            if (!completed)
            {
                foreach (ExceptionFlowCallableKey key in transactionNodes)
                { summaries.Remove(key); callContexts.Remove(key); }
                pendingKeys.Clear();
            }
            transactionNodes.Clear(); BuildingKey = null; BuildingContext = null; BuildingCompilation = null; admission = null; productionEnvironment = null;
        }

        /// <summary>
        /// Gets the number of callable summaries in the graph.
        /// </summary>
        /// <value>The number of distinct context-sensitive graph nodes.</value>
        public int Count =>
            summaries.Count;

        /// <summary>Registers a target using the graph's normal scheduling rules.</summary>
        /// <param name="symbol">The target declaration handle.</param>
        /// <param name="context">The complete call-site context.</param>
        /// <returns>The graph key associated with the target.</returns>
        public ExceptionFlowCallableKey RegisterTarget(
            Microsoft.CodeAnalysis.ISymbol symbol,
            ExceptionFlowCallContext context)
        {
            Microsoft.CodeAnalysis.ISymbol symbolic = context.InvocationSymbol ?? symbol;
            if (admission != null && (BuildingKey == null || !admission.ContainsTransition(BuildingKey, symbol, symbolic)))
                throw new ExceptionFlowProfileViolation("TargetOrAnalysisCoverageIncomplete", "Discovered declaration/map was not sealed: "
                    + ExceptionFlowFiniteOrbitSeal.BodyId(symbol));
            if (BuildingContext?.InvocationSymbol != null && BuildingCompilation != null)
                context = context.WithInvocation(new ExceptionFlowInvocationSubstitution(BuildingContext.InvocationSymbol, BuildingCompilation).ApplyCallable(symbolic));
            ExceptionFlowCallableKey key = new(symbol, context);
            GetOrAdd(key, context);
            return key;
        }

        /// <summary>Local fragments are stored by the session after body analysis.</summary>
        /// <param name="fragment">The observed local fragment.</param>
        public void ObserveLocalFragment(ExceptionFlowSummaryFragment fragment)
        {
            // The raw graph remains a construction-only diagnostic facility.
            // Discovery receivers independently retain pre-suppression coverage.
        }

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
            if (admission != null && (!admission.ScopeUnchanged(productionEnvironment!.GetAnalysisScopes().Select(scope => scope.Compilation))
                || !admission.ContainsContext(key.Symbol, key.CallContext)))
                throw new ExceptionFlowProfileViolation("ScopeMapNotClosed", "Unsealed, open, or changed-scope context: "
                    + ExceptionFlowFiniteOrbitSeal.BodyId(key.Symbol));
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

            if (admission != null) transactionNodes.Add(key);

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
