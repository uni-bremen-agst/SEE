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
        public HashSet<INamedTypeSymbol> ThrownExceptions { get; } =
            new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets or sets a value indicating whether at least one relevant transitive analysis path
        /// could not be evaluated conclusively.
        /// </summary>
        public bool HasUncertainPaths { get; set; }
    }
}
