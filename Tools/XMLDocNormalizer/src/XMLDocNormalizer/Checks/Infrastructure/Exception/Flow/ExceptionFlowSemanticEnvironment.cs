using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides the compilation-bound semantic capabilities required by the
    /// exception-flow analyzer.
    /// </summary>
    /// <remarks>
    /// One environment belongs to one analysis context. Its host-supplied
    /// partial implementation provides semantic-model lookup, analysis-scope
    /// enumeration, exact supporting-source method resolution, and exact
    /// external supporting-scope lookup. The nonvirtual compile-time seam
    /// preserves one Roslyn type universe and fails closed when identity or
    /// ownership cannot be proven.
    /// </remarks>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
        private readonly Dictionary<Compilation, bool> blockingCompilationErrors =
            new(ReferenceEqualityComparer.Instance);

        /// <summary>
        /// Gets the A9 blocking-error status once per exact compilation in this
        /// sequential environment. Executable packaging error CS5001 is ignored.
        /// </summary>
        /// <param name="compilation">The immutable compilation being inspected.</param>
        /// <returns>Whether a compiler error other than CS5001 is present.</returns>
        internal bool HasBlockingCompilationErrors(Compilation compilation)
        {
            ArgumentNullException.ThrowIfNull(compilation);
            if (blockingCompilationErrors.TryGetValue(compilation, out bool cached))
                return cached;

            bool value = compilation.GetDiagnostics().Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS5001");
            // Failed computations propagate and never publish a status.
            blockingCompilationErrors.Add(compilation, value);
            return value;
        }
    }
}
