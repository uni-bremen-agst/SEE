using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
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
                symbolContexts =
                    new HashSet<string>(StringComparer.Ordinal);

                analyzedContexts.Add(
                    normalizedSymbol,
                    symbolContexts);
            }

            return symbolContexts.Add(callContext.Key);
        }
    }
}
