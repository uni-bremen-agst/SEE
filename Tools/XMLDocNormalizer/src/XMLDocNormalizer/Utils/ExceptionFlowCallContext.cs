using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Describes call-site facts that are known while analyzing a callable symbol.
    /// </summary>
    internal sealed class ExceptionFlowCallContext
    {
        /// <summary>
        /// The parameter indexes that are proven to be non-null for the current call.
        /// </summary>
        private readonly HashSet<int> knownNonNullParameterIndexes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFlowCallContext"/> class.
        /// </summary>
        /// <param name="callableSymbol">The callable symbol whose body is being analyzed.</param>
        /// <param name="knownNonNullParameterIndexes">
        /// The indexes of parameters that are proven to be non-null at the call site.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="knownNonNullParameterIndexes"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowCallContext(
            ISymbol? callableSymbol,
            IEnumerable<int> knownNonNullParameterIndexes)
        {
            ArgumentNullException.ThrowIfNull(knownNonNullParameterIndexes);

            CallableSymbol = NormalizeSymbol(callableSymbol);
            this.knownNonNullParameterIndexes =
                new HashSet<int>(knownNonNullParameterIndexes);

            int[] orderedIndexes = this.knownNonNullParameterIndexes
                .OrderBy(static index => index)
                .ToArray();

            Key = string.Join(",", orderedIndexes);
        }

        /// <summary>
        /// Gets the callable symbol whose body is being analyzed.
        /// </summary>
        /// <value>
        /// The normalized callable symbol, or <see langword="null"/> if the context
        /// is not associated with a callable symbol.
        /// </value>
        public ISymbol? CallableSymbol { get; }

        /// <summary>
        /// Gets a deterministic key that identifies the known parameter facts.
        /// </summary>
        /// <value>
        /// A comma-separated sequence of the sorted parameter indexes known to be
        /// non-null, or an empty string if no parameter is known to be non-null.
        /// </value>
        public string Key { get; }

        /// <summary>
        /// Determines whether the specified parameter is proven to be non-null in this context.
        /// </summary>
        /// <param name="parameterSymbol">The parameter symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the parameter belongs to the current callable symbol and
        /// is proven to be non-null; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="parameterSymbol"/> is <see langword="null"/>.
        /// </exception>
        public bool IsParameterKnownNonNull(IParameterSymbol parameterSymbol)
        {
            ArgumentNullException.ThrowIfNull(parameterSymbol);

            ISymbol normalizedContainingSymbol =
                NormalizeSymbol(parameterSymbol.ContainingSymbol) ??
                parameterSymbol.ContainingSymbol;

            return CallableSymbol != null &&
                   SymbolEqualityComparer.Default.Equals(
                       CallableSymbol,
                       normalizedContainingSymbol) &&
                   knownNonNullParameterIndexes.Contains(parameterSymbol.Ordinal);
        }

        /// <summary>
        /// Normalizes a symbol so that constructed generic symbols and their declarations
        /// share the same identity during exception-flow analysis.
        /// </summary>
        /// <param name="symbol">The symbol to normalize.</param>
        /// <returns>
        /// The normalized symbol, or <see langword="null"/> if no symbol was supplied.
        /// </returns>
        private static ISymbol? NormalizeSymbol(ISymbol? symbol)
        {
            return symbol?.OriginalDefinition;
        }
    }

    /// <summary>
    /// Tracks callable symbols that have already been analyzed for a specific set of
    /// call-site facts.
    /// </summary>
    internal sealed class ExceptionFlowTraversalState
    {
        /// <summary>
        /// Maps normalized callable symbols to the call-context keys already analyzed for them.
        /// </summary>
        private readonly Dictionary<ISymbol, HashSet<string>> analyzedContexts =
            new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Attempts to mark the specified symbol and call context as analyzed.
        /// </summary>
        /// <param name="symbol">The callable symbol to track.</param>
        /// <param name="callContext">The call-site facts used for the analysis.</param>
        /// <returns>
        /// <see langword="true"/> if the symbol-context combination had not been analyzed before;
        /// otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="symbol"/> or <paramref name="callContext"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool TryMarkAnalyzed(
            ISymbol symbol,
            ExceptionFlowCallContext callContext)
        {
            ArgumentNullException.ThrowIfNull(symbol);
            ArgumentNullException.ThrowIfNull(callContext);

            ISymbol normalizedSymbol = symbol.OriginalDefinition;

            if (!analyzedContexts.TryGetValue(
                    normalizedSymbol,
                    out HashSet<string>? symbolContexts))
            {
                symbolContexts = new HashSet<string>(StringComparer.Ordinal);
                analyzedContexts.Add(normalizedSymbol, symbolContexts);
            }

            return symbolContexts.Add(callContext.Key);
        }
    }
}
