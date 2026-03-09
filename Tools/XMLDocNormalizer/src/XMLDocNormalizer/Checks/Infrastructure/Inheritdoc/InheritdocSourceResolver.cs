using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Inheritdoc
{
    /// <summary>
    /// Provides helpers to determine whether declarations have valid
    /// implicit documentation inheritance sources.
    /// </summary>
    internal static class InheritdocSourceResolver
    {
        /// <summary>
        /// Gets the primary declared symbol for a documented syntax node.
        /// </summary>
        /// <param name="node">The documented syntax node.</param>
        /// <param name="semanticModel">The semantic model used to resolve symbols.</param>
        /// <returns>
        /// The primary declared symbol for the node if available; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// Some declaration nodes such as <see cref="EventFieldDeclarationSyntax"/> and
        /// <see cref="FieldDeclarationSyntax"/> declare their symbols on contained variable
        /// declarators rather than on the declaration node itself.
        /// </remarks>
        private static ISymbol? GetDeclaredSymbol(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(semanticModel);

            if (node is EventFieldDeclarationSyntax eventFieldDeclaration)
            {
                VariableDeclaratorSyntax? variable =
                    eventFieldDeclaration.Declaration?.Variables.FirstOrDefault();

                return variable != null
                    ? semanticModel.GetDeclaredSymbol(variable)
                    : null;
            }

            if (node is FieldDeclarationSyntax fieldDeclaration)
            {
                VariableDeclaratorSyntax? variable =
                    fieldDeclaration.Declaration?.Variables.FirstOrDefault();

                return variable != null
                    ? semanticModel.GetDeclaredSymbol(variable)
                    : null;
            }

            return semanticModel.GetDeclaredSymbol(node);
        }

        /// <summary>
        /// Determines whether the specified declaration has a valid implicit
        /// documentation inheritance source for an <c>&lt;inheritdoc/&gt;</c> tag.
        /// </summary>
        /// <param name="node">
        /// The syntax node representing the documented declaration.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the declared symbol.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a valid inheritance source exists; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Valid implicit sources include:
        /// <list type="bullet">
        /// <item><description>Overridden base members.</description></item>
        /// <item><description>Explicit or implicit interface implementations.</description></item>
        /// <item><description>Derived types inheriting from a base type.</description></item>
        /// <item><description>Interfaces inheriting from other interfaces.</description></item>
        /// </list>
        /// </remarks>
        internal static bool HasImplicitInheritdocSource(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(semanticModel);

            ISymbol? symbol = GetDeclaredSymbol(node, semanticModel);

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
        /// <see langword="true"/> if the base type is a user-defined type;
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
        /// Determines whether the specified symbol implements any interface member
        /// implicitly.
        /// </summary>
        /// <param name="symbol">The symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> if the symbol implements an interface member;
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
