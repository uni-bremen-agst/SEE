using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Stores exception types suppressed by typed catch clauses on one
    /// exception-flow call edge.
    /// </summary>
    internal sealed class ExceptionFlowCaughtTypeFilter
    {
        /// <summary>
        /// Stores normalized caught exception types.
        /// </summary>
        private readonly HashSet<INamedTypeSymbol> caughtTypes =
            new(SymbolEqualityComparer.Default);

        /// <summary>
        /// Gets the exception types caught on the associated call edge.
        /// </summary>
        /// <value>
        /// The normalized exception types suppressed during graph expansion.
        /// </value>
        public IReadOnlySet<INamedTypeSymbol> CaughtTypes =>
            caughtTypes;

        /// <summary>
        /// Adds one typed catch suppression.
        /// </summary>
        /// <param name="caughtType">
        /// The exception type handled by the catch clause.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="caughtType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public void Add(
            INamedTypeSymbol caughtType)
        {
            ArgumentNullException.ThrowIfNull(caughtType);

            caughtTypes.Add(
                caughtType.OriginalDefinition);
        }

        /// <summary>
        /// Determines whether an exception type is suppressed by one of the
        /// stored catch types.
        /// </summary>
        /// <param name="exceptionType">
        /// The exception type produced by the called target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the exception type equals or derives
        /// from a caught type; otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> is
        /// <see langword="null"/>.
        /// </exception>
        public bool Suppresses(
            INamedTypeSymbol exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            foreach (INamedTypeSymbol caughtType in caughtTypes)
            {
                if (IsSameOrDerivedFrom(
                        exceptionType,
                        caughtType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates an independent copy of this filter.
        /// </summary>
        /// <returns>
        /// A filter containing the same caught exception types.
        /// </returns>
        public ExceptionFlowCaughtTypeFilter Copy()
        {
            ExceptionFlowCaughtTypeFilter copy = new();

            foreach (INamedTypeSymbol caughtType in caughtTypes)
            {
                copy.caughtTypes.Add(caughtType);
            }

            return copy;
        }

        /// <summary>
        /// Determines whether one exception type equals or derives from
        /// another exception type.
        /// </summary>
        /// <param name="exceptionType">
        /// The potential derived exception type.
        /// </param>
        /// <param name="baseType">
        /// The expected base or identical exception type.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="exceptionType"/> equals
        /// or derives from <paramref name="baseType"/>; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="exceptionType"/> or
        /// <paramref name="baseType"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsSameOrDerivedFrom(
            INamedTypeSymbol exceptionType,
            INamedTypeSymbol baseType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);
            ArgumentNullException.ThrowIfNull(baseType);

            INamedTypeSymbol? currentType =
                exceptionType;

            while (currentType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(
                        currentType.OriginalDefinition,
                        baseType.OriginalDefinition))
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }
    }
}
