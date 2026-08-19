using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Represents one exception source found directly inside a callable
    /// summary.
    /// </summary>
    internal sealed class ExceptionFlowSummarySource
    {
        /// <summary>
        /// Gets the semantic strength of this exception-flow source.
        /// </summary>
        /// <value>The source kind.</value>
        public ExceptionFlowSourceKind Kind { get; }

        /// <summary>
        /// Initializes a local exception-flow source with the specified semantic
        /// strength.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type associated with the source.
        /// </param>
        /// <param name="localPath">
        /// The path local to the analyzed callable.
        /// </param>
        /// <param name="kind">
        /// The semantic strength of the source.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> or
        /// <paramref name="localPath"/> is <see langword="null"/>.
        /// </exception>
        public ExceptionFlowSummarySource(
            INamedTypeSymbol exceptionType,
            ExceptionFlowPath localPath,
            ExceptionFlowSourceKind kind =
                ExceptionFlowSourceKind.ProvenException)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);
            ArgumentNullException.ThrowIfNull(localPath);

            ExceptionType = exceptionType;
            LocalPath = localPath;
            Kind = kind;
        }

        /// <summary>
        /// Gets the directly produced exception type.
        /// </summary>
        /// <value>The exception type associated with the local source.</value>
        public INamedTypeSymbol ExceptionType { get; }

        /// <summary>
        /// Gets the path local to the analyzed callable.
        /// </summary>
        /// <value>
        /// A path containing no transitively expanded callgraph suffix.
        /// </value>
        public ExceptionFlowPath LocalPath { get; }
    }
}
