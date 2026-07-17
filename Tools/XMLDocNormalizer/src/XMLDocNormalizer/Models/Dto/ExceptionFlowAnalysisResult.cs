using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents the result of transitive exception-flow analysis.
    /// </summary>
    internal sealed class ExceptionFlowAnalysisResult
    {
        /// <summary>
        /// Gets the exception types that were proven to be thrown directly or transitively.
        /// </summary>
        /// <value>
        /// The exception types proven to be thrown directly or transitively.
        /// </value>
        public HashSet<INamedTypeSymbol> ThrownExceptions { get; } =
            new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets the set of callable targets whose exception flow could not be decided.
        /// </summary>
        /// <value>
        /// The callable targets whose exception flow could not be decided.
        /// </value>
        public HashSet<string> UncertainTargets { get; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets a value indicating whether at least one relevant transitive analysis path
        /// could not be evaluated conclusively.
        /// </summary>
        /// <value>
        /// True if at least one relevant transitive analysis path could not be evaluated conclusively; otherwise false.
        /// </value>
        public bool HasUncertainPaths => UncertainTargets.Count > 0;
    }
}
