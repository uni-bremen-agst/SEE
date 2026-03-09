using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Inheritdoc
{
    /// <summary>
    /// Determines whether a documented declaration has a valid implicit inheritdoc source.
    /// </summary>
    /// <remarks>
    /// This resolver is used for <c>&lt;inheritdoc/&gt;</c> without a <c>cref</c> attribute.
    /// Valid implicit sources include overridden members, implemented interface members,
    /// base types, and inherited interfaces.
    /// </remarks>
    internal static class ImplicitInheritdocSourceResolver
    {
        /// <summary>
        /// Determines whether the specified declaration has a valid implicit documentation
        /// inheritance source.
        /// </summary>
        /// <param name="node">The documented declaration node.</param>
        /// <param name="semanticModel">The semantic model used to resolve symbols.</param>
        /// <returns>
        /// <see langword="true"/> if the declaration has a valid implicit inheritdoc source;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasImplicitInheritdocSource(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(semanticModel);

            ISymbol? symbol =
                InheritdocDeclaredSymbolResolver.GetDeclaredSymbol(node, semanticModel);

            if (symbol == null)
            {
                return false;
            }

            return symbol switch
            {
                IMethodSymbol methodSymbol => HasImplicitSource(methodSymbol),
                IPropertySymbol propertySymbol => HasImplicitSource(propertySymbol),
                IEventSymbol eventSymbol => HasImplicitSource(eventSymbol),
                INamedTypeSymbol typeSymbol => HasImplicitSource(typeSymbol),
                _ => false
            };
        }

        /// <summary>
        /// Determines whether a method symbol has a valid implicit documentation source.
        /// </summary>
        /// <param name="symbol">The method symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the method overrides a base method or implements
        /// an interface member; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasImplicitSource(IMethodSymbol symbol)
        {
            if (symbol.OverriddenMethod != null)
            {
                return true;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return true;
            }

            return ImplementsAnyInterfaceMember(symbol);
        }

        /// <summary>
        /// Determines whether a property symbol has a valid implicit documentation source.
        /// </summary>
        /// <param name="symbol">The property symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the property overrides a base property or implements
        /// an interface property; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasImplicitSource(IPropertySymbol symbol)
        {
            if (symbol.OverriddenProperty != null)
            {
                return true;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return true;
            }

            return ImplementsAnyInterfaceMember(symbol);
        }

        /// <summary>
        /// Determines whether an event symbol has a valid implicit documentation source.
        /// </summary>
        /// <param name="symbol">The event symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the event overrides a base event or implements
        /// an interface event; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasImplicitSource(IEventSymbol symbol)
        {
            if (symbol.OverriddenEvent != null)
            {
                return true;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return true;
            }

            return ImplementsAnyInterfaceMember(symbol);
        }

        /// <summary>
        /// Determines whether a type symbol has a valid implicit documentation source.
        /// </summary>
        /// <param name="symbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the type inherits documentation from a base type
        /// or a base interface; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasImplicitSource(INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Class)
            {
                return HasUsefulBaseType(symbol);
            }

            if (symbol.TypeKind == TypeKind.Interface)
            {
                return symbol.Interfaces.Length > 0;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a class has a meaningful base type that can serve as
        /// a documentation inheritance source.
        /// </summary>
        /// <param name="symbol">The type symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the base type is meaningful for documentation inheritance;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasUsefulBaseType(INamedTypeSymbol symbol)
        {
            INamedTypeSymbol? baseType = symbol.BaseType;

            if (baseType == null)
            {
                return false;
            }

            return baseType.SpecialType != SpecialType.System_Object
                && baseType.SpecialType != SpecialType.System_ValueType
                && baseType.SpecialType != SpecialType.System_Enum;
        }

        /// <summary>
        /// Determines whether the specified symbol implements any interface member implicitly.
        /// </summary>
        /// <param name="symbol">The symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the symbol implements an interface member implicitly;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool ImplementsAnyInterfaceMember(ISymbol symbol)
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
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
