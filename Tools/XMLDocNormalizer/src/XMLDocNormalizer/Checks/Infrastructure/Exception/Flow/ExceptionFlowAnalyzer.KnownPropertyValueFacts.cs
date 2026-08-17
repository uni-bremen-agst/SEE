using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value facts for framework properties with stable documented
    /// return contracts.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Gets value facts guaranteed by a known framework property.
        /// </summary>
        /// <param name="propertySymbol">
        /// The accessed property.
        /// </param>
        /// <returns>
        /// The guaranteed property facts, or
        /// <see cref="ExceptionFlowValueFacts.None"/> when the property has no
        /// explicit model.
        /// </returns>
        private static ExceptionFlowValueFacts
            GetKnownFrameworkPropertyValueFacts(
                IPropertySymbol propertySymbol)
        {
            if (IsRoslynOriginalDefinitionProperty(
                    propertySymbol))
            {
                return ExceptionFlowValueFacts.NonNull;
            }

            return ExceptionFlowValueFacts.None;
        }

        /// <summary>
        /// Determines whether a property represents one of Roslyn's typed or
        /// untyped <c>OriginalDefinition</c> symbol properties.
        /// </summary>
        /// <param name="propertySymbol">
        /// The property to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the property returns a Roslyn symbol's
        /// original definition; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRoslynOriginalDefinitionProperty(
            IPropertySymbol propertySymbol)
        {
            if (!string.Equals(
                    propertySymbol.Name,
                    nameof(ISymbol.OriginalDefinition),
                    StringComparison.Ordinal) ||
                propertySymbol.Parameters.Length != 0 ||
                !string.Equals(
                    propertySymbol.ContainingNamespace
                        .ToDisplayString(),
                    "Microsoft.CodeAnalysis",
                    StringComparison.Ordinal))
            {
                return false;
            }

            return IsRoslynSymbolType(
                propertySymbol.Type);
        }

        /// <summary>
        /// Determines whether a type is Roslyn's base symbol interface or a
        /// more specialized Roslyn symbol interface.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type is or implements
        /// <see cref="ISymbol"/>; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRoslynSymbolType(
            ITypeSymbol typeSymbol)
        {
            if (IsRoslynBaseSymbolInterface(
                    typeSymbol))
            {
                return true;
            }

            if (typeSymbol
                is not INamedTypeSymbol namedType)
            {
                return false;
            }

            foreach (INamedTypeSymbol interfaceSymbol
                     in namedType.AllInterfaces)
            {
                if (IsRoslynBaseSymbolInterface(
                        interfaceSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a type is
        /// <see cref="ISymbol"/>.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the type is Roslyn's base symbol
        /// interface; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsRoslynBaseSymbolInterface(
            ITypeSymbol typeSymbol)
        {
            return string.Equals(
                       typeSymbol.Name,
                       nameof(ISymbol),
                       StringComparison.Ordinal) &&
                   string.Equals(
                       typeSymbol.ContainingNamespace
                           .ToDisplayString(),
                       "Microsoft.CodeAnalysis",
                       StringComparison.Ordinal);
        }
    }
}
