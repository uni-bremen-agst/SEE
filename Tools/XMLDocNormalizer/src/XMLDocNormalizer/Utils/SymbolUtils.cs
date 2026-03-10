using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Utils
{
    /// <summary>
    /// Provides helper methods for symbol-based semantic analysis.
    /// </summary>
    internal static class SymbolUtils
    {
        /// <summary>
        /// Determines whether the specified type is identical to or derives from the given base type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="baseType">The base type that should be matched or inherited from.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="type"/> is the same as
        /// <paramref name="baseType"/> or derives from it; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// This helper walks the inheritance chain of the inspected type using
        /// <see cref="INamedTypeSymbol.BaseType"/> and compares each level using
        /// <see cref="SymbolEqualityComparer.Default"/>.
        /// </remarks>
        public static bool InheritsFromOrEquals(
            this INamedTypeSymbol type,
            INamedTypeSymbol baseType)
        {
            INamedTypeSymbol? current = type;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }
    }
}