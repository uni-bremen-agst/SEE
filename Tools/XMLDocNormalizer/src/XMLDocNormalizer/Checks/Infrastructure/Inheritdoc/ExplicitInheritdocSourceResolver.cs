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
            ArgumentNullException.ThrowIfNull(documentedNode);
            ArgumentNullException.ThrowIfNull(sourceSymbol);
            ArgumentNullException.ThrowIfNull(semanticModel);

            ISymbol? documentedSymbol =
                InheritdocDeclaredSymbolResolver.GetDeclaredSymbol(documentedNode, semanticModel);

            if (documentedSymbol == null)
            {
                return false;
            }

            return documentedSymbol switch
            {
                IMethodSymbol methodSymbol => IsValidExplicitSource(methodSymbol, sourceSymbol),
                IPropertySymbol propertySymbol => IsValidExplicitSource(propertySymbol, sourceSymbol),
                IEventSymbol eventSymbol => IsValidExplicitSource(eventSymbol, sourceSymbol),
                INamedTypeSymbol typeSymbol => IsValidExplicitSource(typeSymbol, sourceSymbol),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether the specified source symbol is a valid explicit inheritdoc source
        /// for the given method symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented method symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// <see langword="true"/> if the source symbol is a valid explicit source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsValidExplicitSource(
            IMethodSymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not IMethodSymbol sourceMethod)
            {
                return false;
            }

            if (documentedSymbol.OverriddenMethod != null &&
                SymbolEqualityComparer.Default.Equals(documentedSymbol.OverriddenMethod, sourceMethod))
            {
                return true;
            }

            if (documentedSymbol.ExplicitInterfaceImplementations.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented, sourceMethod)))
            {
                return true;
            }

            return GetImplicitlyImplementedInterfaceMembers(documentedSymbol)
                .OfType<IMethodSymbol>()
                .Any(interfaceMethod => SymbolEqualityComparer.Default.Equals(interfaceMethod, sourceMethod));
        }

        /// <summary>
        /// Determines whether the specified source symbol is a valid explicit inheritdoc source
        /// for the given property symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented property symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// <see langword="true"/> if the source symbol is a valid explicit source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsValidExplicitSource(
            IPropertySymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not IPropertySymbol sourceProperty)
            {
                return false;
            }

            if (documentedSymbol.OverriddenProperty != null &&
                SymbolEqualityComparer.Default.Equals(documentedSymbol.OverriddenProperty, sourceProperty))
            {
                return true;
            }

            if (documentedSymbol.ExplicitInterfaceImplementations.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented, sourceProperty)))
            {
                return true;
            }

            return GetImplicitlyImplementedInterfaceMembers(documentedSymbol)
                .OfType<IPropertySymbol>()
                .Any(interfaceProperty => SymbolEqualityComparer.Default.Equals(interfaceProperty, sourceProperty));
        }

        /// <summary>
        /// Determines whether the specified source symbol is a valid explicit inheritdoc source
        /// for the given event symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented event symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// <see langword="true"/> if the source symbol is a valid explicit source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsValidExplicitSource(
            IEventSymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not IEventSymbol sourceEvent)
            {
                return false;
            }

            if (documentedSymbol.OverriddenEvent != null &&
                SymbolEqualityComparer.Default.Equals(documentedSymbol.OverriddenEvent, sourceEvent))
            {
                return true;
            }

            if (documentedSymbol.ExplicitInterfaceImplementations.Any(implemented =>
                SymbolEqualityComparer.Default.Equals(implemented, sourceEvent)))
            {
                return true;
            }

            return GetImplicitlyImplementedInterfaceMembers(documentedSymbol)
                .OfType<IEventSymbol>()
                .Any(interfaceEvent => SymbolEqualityComparer.Default.Equals(interfaceEvent, sourceEvent));
        }

        /// <summary>
        /// Determines whether the specified source symbol is a valid explicit inheritdoc source
        /// for the given named type symbol.
        /// </summary>
        /// <param name="documentedSymbol">The documented type symbol.</param>
        /// <param name="sourceSymbol">The resolved cref target symbol.</param>
        /// <returns>
        /// <see langword="true"/> if the source symbol is a valid explicit source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsValidExplicitSource(
            INamedTypeSymbol documentedSymbol,
            ISymbol sourceSymbol)
        {
            if (sourceSymbol is not INamedTypeSymbol sourceType)
            {
                return false;
            }

            if (documentedSymbol.TypeKind == TypeKind.Class)
            {
                return documentedSymbol.BaseType != null &&
                    SymbolEqualityComparer.Default.Equals(documentedSymbol.BaseType, sourceType);
            }

            if (documentedSymbol.TypeKind == TypeKind.Interface)
            {
                return documentedSymbol.Interfaces.Any(baseInterface =>
                    SymbolEqualityComparer.Default.Equals(baseInterface, sourceType));
            }

            return false;
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
