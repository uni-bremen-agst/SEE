using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Represents one source-level call edge between two callable summary
    /// nodes.
    /// </summary>
    internal sealed class ExceptionFlowSummaryCallEdge
    {
        /// <summary>
        /// Stores typed catch suppressions applied to this call edge.
        /// </summary>
        private readonly ExceptionFlowCaughtTypeFilter caughtTypeFilter =
            new();

        /// <summary>
        /// Initializes a new call edge without typed catch suppressions.
        /// </summary>
        /// <param name="target">
        /// The context-sensitive target callable.
        /// </param>
        /// <param name="callSiteStep">
        /// The source-level path step representing the call site.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="callSiteStep"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowSummaryCallEdge(
            ExceptionFlowCallableKey target,
            ExceptionFlowPathStep callSiteStep)
        {
            ArgumentNullException.ThrowIfNull(callSiteStep);

            Target = target;
            CallSiteStep = callSiteStep;
        }

        /// <summary>
        /// Gets the context-sensitive target callable.
        /// </summary>
        /// <value>The target graph-node key.</value>
        public ExceptionFlowCallableKey Target { get; }

        /// <summary>
        /// Gets the source-level call-site path step.
        /// </summary>
        /// <value>
        /// The method, constructor, property, or indexer access step.
        /// </value>
        public ExceptionFlowPathStep CallSiteStep { get; }

        /// <summary>
        /// Gets the typed catch suppressions associated with this edge.
        /// </summary>
        /// <value>
        /// The caught exception types that must be filtered during graph
        /// expansion.
        /// </value>
        public IReadOnlySet<INamedTypeSymbol> CaughtExceptionTypes =>
            caughtTypeFilter.CaughtTypes;

        /// <summary>
        /// Adds one typed catch suppression to this edge.
        /// </summary>
        /// <param name="caughtType">
        /// The exception type handled around this call site.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="caughtType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void AddCaughtExceptionType(
            INamedTypeSymbol caughtType)
        {
            ArgumentNullException.ThrowIfNull(caughtType);

            caughtTypeFilter.Add(caughtType);
        }

        /// <summary>
        /// Determines whether a produced exception type is suppressed on
        /// this call edge.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type produced by the target callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a surrounding typed catch handles the
        /// exception; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool Suppresses(
            INamedTypeSymbol exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            return caughtTypeFilter.Suppresses(
                exceptionType);
        }
    }
}
