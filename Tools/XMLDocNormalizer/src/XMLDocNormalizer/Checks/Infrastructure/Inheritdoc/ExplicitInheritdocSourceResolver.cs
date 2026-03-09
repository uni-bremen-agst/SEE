using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Inheritdoc
{
    /// <summary>
    /// Determines whether a <c>cref</c> target is a valid explicit inheritdoc source.
    /// </summary>
    /// <remarks>
    /// This resolver is used for <c>&lt;inheritdoc cref="..." /&gt;</c>. A cref target is valid
    /// only if it is connected to the documented declaration through a real inheritance or
    /// implementation relationship, such as an overridden member, an implemented interface member,
    /// a base type, or an inherited interface.
    /// </remarks>
    internal static class ExplicitInheritdocSourceResolver
    {
        /// <summary>
        /// Gets the valid explicit inheritdoc source symbol for the documented declaration.
        /// </summary>
        /// <param name="documentedNode">The documented declaration node.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <param name="semanticModel">The semantic model used to resolve symbols.</param>
        /// <returns>
        /// The cref target symbol if it is a valid explicit inheritdoc source;
        /// otherwise <c>null</c>.
        /// </returns>
        internal static ISymbol? GetValidExplicitInheritdocSource(
            SyntaxNode documentedNode,
            ISymbol sourceSymbol,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(documentedNode);
            ArgumentNullException.ThrowIfNull(sourceSymbol);
            ArgumentNullException.ThrowIfNull(semanticModel);

            ISymbol? documentedSymbol =
                InheritdocDeclaredSymbolResolver.GetDeclaredSymbol(documentedNode, semanticModel);

            if (documentedSymbol == null)
            {
                return null;
            }

            return documentedSymbol switch
            {
                IMethodSymbol methodSymbol => GetValidExplicitSource(methodSymbol, sourceSymbol),
                IPropertySymbol propertySymbol => GetValidExplicitSource(propertySymbol, sourceSymbol),
                IEventSymbol eventSymbol => GetValidExplicitSource(eventSymbol, sourceSymbol),
                INamedTypeSymbol typeSymbol => GetValidExplicitSource(typeSymbol, sourceSymbol),
                _ => null
            };
        }

        /// <summary>
        /// Determines whether the specified cref target is a valid explicit
        /// documentation inheritance source for the documented declaration.
        /// </summary>
        /// <param name="documentedNode">The documented declaration node.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <param name="semanticModel">The semantic model used to resolve symbols.</param>
        /// <returns>
        /// <see langword="true"/> if the cref target is a valid inheritance source for the
        /// documented declaration; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsValidExplicitInheritdocSource(
            SyntaxNode documentedNode,
            ISymbol sourceSymbol,
            SemanticModel semanticModel)
        {
            return GetValidExplicitInheritdocSource(documentedNode, sourceSymbol, semanticModel) != null;
        }

        /// <summary>
        /// Gets the valid explicit inheritdoc source for the specified method symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented method symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// The source symbol if it is a valid explicit source; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetValidExplicitSource(
            IMethodSymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not IMethodSymbol sourceMethod)
            {
                return null;
            }

            if (documentedSymbol.OverriddenMethod != null &&
                SymbolEqualityComparer.Default.Equals(documentedSymbol.OverriddenMethod, sourceMethod))
            {
                return sourceMethod;
            }

            if (documentedSymbol.ExplicitInterfaceImplementations.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented, sourceMethod)))
            {
                return sourceMethod;
            }

            return GetImplicitlyImplementedInterfaceMembers(documentedSymbol)
                .OfType<IMethodSymbol>()
                .Any(interfaceMethod => SymbolEqualityComparer.Default.Equals(interfaceMethod, sourceMethod))
                ? sourceMethod
                : null;
        }

        /// <summary>
        /// Gets the valid explicit inheritdoc source for the specified property symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented property symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// The source symbol if it is a valid explicit source; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetValidExplicitSource(
            IPropertySymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not IPropertySymbol sourceProperty)
            {
                return null;
            }

            if (documentedSymbol.OverriddenProperty != null &&
                SymbolEqualityComparer.Default.Equals(documentedSymbol.OverriddenProperty, sourceProperty))
            {
                return sourceProperty;
            }

            if (documentedSymbol.ExplicitInterfaceImplementations.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented, sourceProperty)))
            {
                return sourceProperty;
            }

            return GetImplicitlyImplementedInterfaceMembers(documentedSymbol)
                .OfType<IPropertySymbol>()
                .Any(interfaceProperty => SymbolEqualityComparer.Default.Equals(interfaceProperty, sourceProperty))
                ? sourceProperty
                : null;
        }

        /// <summary>
        /// Gets the valid explicit inheritdoc source for the specified event symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented event symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// The source symbol if it is a valid explicit source; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetValidExplicitSource(
            IEventSymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not IEventSymbol sourceEvent)
            {
                return null;
            }

            if (documentedSymbol.OverriddenEvent != null &&
                SymbolEqualityComparer.Default.Equals(documentedSymbol.OverriddenEvent, sourceEvent))
            {
                return sourceEvent;
            }

            if (documentedSymbol.ExplicitInterfaceImplementations.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented, sourceEvent)))
            {
                return sourceEvent;
            }

            return GetImplicitlyImplementedInterfaceMembers(documentedSymbol)
                .OfType<IEventSymbol>()
                .Any(interfaceEvent => SymbolEqualityComparer.Default.Equals(interfaceEvent, sourceEvent))
                ? sourceEvent
                : null;
        }

        /// <summary>
        /// Gets the valid explicit inheritdoc source for the specified type symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented type symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// The source symbol if it is a valid explicit source; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetValidExplicitSource(
            INamedTypeSymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not INamedTypeSymbol sourceType)
            {
                return null;
            }

            if (documentedSymbol.TypeKind == TypeKind.Class)
            {
                return documentedSymbol.BaseType != null &&
                    SymbolEqualityComparer.Default.Equals(documentedSymbol.BaseType, sourceType)
                    ? sourceType
                    : null;
            }

            if (documentedSymbol.TypeKind == TypeKind.Interface)
            {
                return documentedSymbol.Interfaces.Any(baseInterface =>
                    SymbolEqualityComparer.Default.Equals(baseInterface, sourceType))
                    ? sourceType
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Gets all interface members that are implemented implicitly by the specified symbol.
        /// </summary>
        /// <param name="symbol">The documented member symbol.</param>
        /// <returns>
        /// All interface members that are implemented implicitly by the symbol.
        /// </returns>
        private static IEnumerable<ISymbol> GetImplicitlyImplementedInterfaceMembers(ISymbol symbol)
        {
            INamedTypeSymbol containingType = symbol.ContainingType;

            foreach (INamedTypeSymbol interfaceType in containingType.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in interfaceType.GetMembers())
                {
                    ISymbol? implementation =
                        containingType.FindImplementationForInterfaceMember(interfaceMember);

                    if (implementation != null &&
                        SymbolEqualityComparer.Default.Equals(implementation, symbol))
                    {
                        yield return interfaceMember;
                    }
                }
            }
        }
    }
}
