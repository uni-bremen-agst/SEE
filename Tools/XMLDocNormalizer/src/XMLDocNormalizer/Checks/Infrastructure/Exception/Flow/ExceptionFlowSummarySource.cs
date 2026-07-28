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
        /// Initializes a new local exception source.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type produced by the source.
        /// </param>
        /// <param name="localPath">
        /// The path local to the analyzed callable.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> or
        /// <paramref name="localPath"/> is <see langword="null"/>.
        /// </exception>
        public ExceptionFlowSummarySource(
            INamedTypeSymbol exceptionType,
            ExceptionFlowPath localPath)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);
            ArgumentNullException.ThrowIfNull(localPath);

            ExceptionType = exceptionType;
            LocalPath = localPath;
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
