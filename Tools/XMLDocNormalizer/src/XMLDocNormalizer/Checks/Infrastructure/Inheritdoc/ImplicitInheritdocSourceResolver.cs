using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Inheritdoc
{
    /// <summary>
    /// Determines whether a documented declaration has a valid implicit inheritdoc source.
    /// </summary>
    /// <remarks>
    /// This resolver is used for <c>&lt;inheritdoc/&gt;</c> without a <c>cref</c> attribute.
    /// It can also return the concrete source symbol so that callers can inspect the
    /// inherited documentation itself.
    /// </remarks>
    internal static class ImplicitInheritdocSourceResolver
    {
        /// <summary>
        /// Gets the implicit inheritdoc source symbol for the specified documented declaration.
        /// </summary>
        /// <param name="node">The documented declaration node.</param>
        /// <param name="semanticModel">The semantic model used to resolve symbols.</param>
        /// <returns>
        /// The implicit inheritdoc source symbol if one exists; otherwise <c>null</c>.
        /// </returns>
        internal static ISymbol? GetImplicitInheritdocSource(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(semanticModel);

            ISymbol? symbol =
                InheritdocDeclaredSymbolResolver.GetDeclaredSymbol(node, semanticModel);

            if (symbol == null)
            {
                return null;
            }

            return symbol switch
            {
                IMethodSymbol methodSymbol => GetImplicitSource(methodSymbol),
                IPropertySymbol propertySymbol => GetImplicitSource(propertySymbol),
                IEventSymbol eventSymbol => GetImplicitSource(eventSymbol),
                INamedTypeSymbol typeSymbol => GetImplicitSource(typeSymbol),
                _ => null
            };
        }

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
            return GetImplicitInheritdocSource(node, semanticModel) != null;
        }

        /// <summary>
        /// Gets the implicit inheritdoc source for the specified method symbol.
        /// </summary>
        /// <param name="symbol">The method symbol to inspect.</param>
        /// <returns>
        /// The implicit source symbol if one exists; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetImplicitSource(IMethodSymbol symbol)
        {
            if (symbol.OverriddenMethod != null)
            {
                return symbol.OverriddenMethod;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return symbol.ExplicitInterfaceImplementations[0];
            }

            ISymbol? implementedInterfaceMember =
                GetImplicitlyImplementedInterfaceMembers(symbol).FirstOrDefault();

            if (implementedInterfaceMember != null)
            {
                return implementedInterfaceMember;
            }

            return GetInheritedInterfaceMember(symbol);
        }

        /// <summary>
        /// Gets the implicit inheritdoc source for the specified property symbol.
        /// </summary>
        /// <param name="symbol">The property symbol to inspect.</param>
        /// <returns>
        /// The implicit source symbol if one exists; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetImplicitSource(IPropertySymbol symbol)
        {
            if (symbol.OverriddenProperty != null)
            {
                return symbol.OverriddenProperty;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return symbol.ExplicitInterfaceImplementations[0];
            }

            ISymbol? implementedInterfaceMember =
                GetImplicitlyImplementedInterfaceMembers(symbol).FirstOrDefault();

            if (implementedInterfaceMember != null)
            {
                return implementedInterfaceMember;
            }

            return GetInheritedInterfaceMember(symbol);
        }

        /// <summary>
        /// Gets the implicit inheritdoc source for the specified event symbol.
        /// </summary>
        /// <param name="symbol">The event symbol to inspect.</param>
        /// <returns>
        /// The implicit source symbol if one exists; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetImplicitSource(IEventSymbol symbol)
        {
            if (symbol.OverriddenEvent != null)
            {
                return symbol.OverriddenEvent;
            }

            if (symbol.ExplicitInterfaceImplementations.Length > 0)
            {
                return symbol.ExplicitInterfaceImplementations[0];
            }

            ISymbol? implementedInterfaceMember =
                GetImplicitlyImplementedInterfaceMembers(symbol).FirstOrDefault();

            if (implementedInterfaceMember != null)
            {
                return implementedInterfaceMember;
            }

            return GetInheritedInterfaceMember(symbol);
        }

        /// <summary>
        /// Gets the implicit inheritdoc source for the specified type symbol.
        /// </summary>
        /// <param name="symbol">The type symbol to inspect.</param>
        /// <returns>
        /// The implicit source symbol if one exists; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetImplicitSource(INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Class)
            {
                return HasUsefulBaseType(symbol) ? symbol.BaseType : null;
            }

            if (symbol.TypeKind == TypeKind.Interface)
            {
                return symbol.Interfaces.FirstOrDefault();
            }

            return null;
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

        /// <summary>
        /// Gets a matching inherited interface member for a symbol declared in an interface.
        /// </summary>
        /// <param name="symbol">The documented member symbol.</param>
        /// <returns>
        /// The matching member from a base interface if one exists; otherwise <c>null</c>.
        /// </returns>
        private static ISymbol? GetInheritedInterfaceMember(ISymbol symbol)
        {
            if (symbol.ContainingType.TypeKind != TypeKind.Interface)
            {
                return null;
            }

            foreach (INamedTypeSymbol baseInterface in symbol.ContainingType.AllInterfaces)
            {
                foreach (ISymbol candidate in baseInterface.GetMembers(symbol.Name))
                {
                    if (AreEquivalentInterfaceMembers(symbol, candidate))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether two interface members are equivalent for documentation inheritance.
        /// </summary>
        /// <param name="currentSymbol">The current documented symbol.</param>
        /// <param name="candidateSymbol">The candidate symbol from a base interface.</param>
        /// <returns>
        /// <see langword="true"/> if both symbols represent equivalent interface members;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool AreEquivalentInterfaceMembers(ISymbol currentSymbol, ISymbol candidateSymbol)
        {
            if (currentSymbol is IMethodSymbol currentMethod &&
                candidateSymbol is IMethodSymbol candidateMethod)
            {
                return currentMethod.MethodKind == candidateMethod.MethodKind
                    && currentMethod.Parameters.Length == candidateMethod.Parameters.Length
                    && currentMethod.TypeParameters.Length == candidateMethod.TypeParameters.Length
                    && ParametersMatch(currentMethod.Parameters, candidateMethod.Parameters);
            }

            if (currentSymbol is IPropertySymbol currentProperty &&
                candidateSymbol is IPropertySymbol candidateProperty)
            {
                return currentProperty.IsIndexer == candidateProperty.IsIndexer
                    && currentProperty.Parameters.Length == candidateProperty.Parameters.Length
                    && ParametersMatch(currentProperty.Parameters, candidateProperty.Parameters);
            }

            if (currentSymbol is IEventSymbol && candidateSymbol is IEventSymbol)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether two parameter lists are equivalent by parameter type.
        /// </summary>
        /// <param name="currentParameters">The current symbol parameters.</param>
        /// <param name="candidateParameters">The candidate symbol parameters.</param>
        /// <returns>
        /// <see langword="true"/> if all parameter types match; otherwise <see langword="false"/>.
        /// </returns>
        private static bool ParametersMatch(
            ImmutableArray<IParameterSymbol> currentParameters,
            ImmutableArray<IParameterSymbol> candidateParameters)
        {
            if (currentParameters.Length != candidateParameters.Length)
            {
                return false;
            }

            for (int i = 0; i < currentParameters.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(
                    currentParameters[i].Type,
                    candidateParameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all possible implicit inheritdoc sources for the specified declaration.
        /// </summary>
        /// <param name="node">The documented syntax node.</param>
        /// <param name="semanticModel">The semantic model used for symbol resolution.</param>
        /// <returns>
        /// All possible implicit inheritance source symbols.
        /// </returns>
        internal static List<ISymbol> GetImplicitInheritdocSources(
            SyntaxNode node,
            SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(semanticModel);

            ISymbol? symbol = InheritdocDeclaredSymbolResolver.GetDeclaredSymbol(node, semanticModel);
            if (symbol == null)
            {
                return [];
            }

            List<ISymbol> sources = [];

            switch (symbol)
            {
                case IMethodSymbol method:
                    if (method.OverriddenMethod != null)
                    {
                        sources.Add(method.OverriddenMethod);
                    }

                    sources.AddRange(method.ExplicitInterfaceImplementations);

                    sources.AddRange(
                        GetImplicitlyImplementedInterfaceMembers(method)
                            .OfType<IMethodSymbol>());

                    ISymbol? inheritedMethod = GetInheritedInterfaceMember(method);
                    if (inheritedMethod != null)
                    {
                        sources.Add(inheritedMethod);
                    }

                    break;

                case IPropertySymbol property:
                    if (property.OverriddenProperty != null)
                    {
                        sources.Add(property.OverriddenProperty);
                    }

                    sources.AddRange(property.ExplicitInterfaceImplementations);

                    sources.AddRange(
                        GetImplicitlyImplementedInterfaceMembers(property)
                            .OfType<IPropertySymbol>());

                    ISymbol? inheritedProperty = GetInheritedInterfaceMember(property);
                    if (inheritedProperty != null)
                    {
                        sources.Add(inheritedProperty);
                    }

                    break;

                case IEventSymbol evt:
                    if (evt.OverriddenEvent != null)
                    {
                        sources.Add(evt.OverriddenEvent);
                    }

                    sources.AddRange(evt.ExplicitInterfaceImplementations);

                    sources.AddRange(
                        GetImplicitlyImplementedInterfaceMembers(evt)
                            .OfType<IEventSymbol>());

                    ISymbol? inheritedEvent = GetInheritedInterfaceMember(evt);
                    if (inheritedEvent != null)
                    {
                        sources.Add(inheritedEvent);
                    }

                    break;

                case INamedTypeSymbol type:
                    if (type.TypeKind == TypeKind.Class && HasUsefulBaseType(type))
                    {
                        sources.Add(type.BaseType!);
                    }

                    if (type.TypeKind == TypeKind.Interface)
                    {
                        sources.AddRange(type.Interfaces);
                    }

                    break;
            }

            return DistinctSymbols(sources);
        }

        /// <summary>
        /// Removes duplicate symbols while preserving their original order.
        /// </summary>
        /// <param name="symbols">The symbols to deduplicate.</param>
        /// <returns>A list containing each symbol only once.</returns>
        private static List<ISymbol> DistinctSymbols(IEnumerable<ISymbol> symbols)
        {
            List<ISymbol> result = [];

            foreach (ISymbol symbol in symbols)
            {
                if (result.Any(existing => SymbolEqualityComparer.Default.Equals(existing, symbol)))
                {
                    continue;
                }

                result.Add(symbol);
            }

            return result;
        }
    }
}
