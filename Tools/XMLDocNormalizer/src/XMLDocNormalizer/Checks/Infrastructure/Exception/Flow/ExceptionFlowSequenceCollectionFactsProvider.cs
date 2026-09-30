using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides stateless facts about supported collection shapes and
    /// sequence-source projections.
    /// </summary>
    internal static class ExceptionFlowSequenceCollectionFactsProvider
    {
        /// <summary>
        /// Determines whether a LINQ method is a <c>GroupBy</c> overload that
        /// keeps the original source elements as grouping elements.
        /// </summary>
        /// <param name="methodSymbol">The original method definition.</param>
        /// <returns>
        /// <see langword="true"/> for supported <c>GroupBy</c> overloads;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsElementPreservingGroupByMethod(
            IMethodSymbol methodSymbol)
        {
            return methodSymbol.IsStatic
                && string.Equals(
                    methodSymbol.Name,
                    "GroupBy",
                    StringComparison.Ordinal)
                && methodSymbol.Arity == 2
                && string.Equals(
                    methodSymbol.ContainingType.ToDisplayString(),
                    "System.Linq.Enumerable",
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the input sequence of an extension-style or ordinary static
        /// LINQ invocation.
        /// </summary>
        /// <param name="invocation">The invocation expression.</param>
        /// <param name="selectedMethod">
        /// The method selected at the invocation site.
        /// </param>
        /// <param name="sourceExpression">
        /// The resolved source sequence expression.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a source expression was resolved;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryGetSequenceSourceExpression(
            InvocationExpressionSyntax invocation,
            IMethodSymbol selectedMethod,
            out ExpressionSyntax? sourceExpression)
        {
            sourceExpression = null;

            if (invocation.Expression
                    is MemberAccessExpressionSyntax memberAccess
                && (selectedMethod.ReducedFrom != null
                    || (selectedMethod.IsExtensionMethod
                        && invocation.ArgumentList.Arguments.Count <
                            selectedMethod.Parameters.Length)))
            {
                sourceExpression =
                    memberAccess.Expression;

                return true;
            }

            if (invocation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            sourceExpression =
                invocation.ArgumentList.Arguments[0].Expression;

            return true;
        }

        /// <summary>
        /// Determines whether a type is <see cref="List{T}"/>.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for the framework generic list type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsListType(
            ITypeSymbol typeSymbol)
        {
            if (typeSymbol
                is not INamedTypeSymbol namedType)
            {
                return false;
            }

            INamedTypeSymbol originalType =
                namedType.OriginalDefinition;

            return originalType.Arity == 1
                && string.Equals(
                    originalType.Name,
                    "List",
                    StringComparison.Ordinal)
                && string.Equals(
                    originalType.ContainingNamespace
                        .ToDisplayString(),
                    "System.Collections.Generic",
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an expression creates an empty
        /// <see cref="List{T}"/>.
        /// </summary>
        /// <param name="creationExpression">The creation expression.</param>
        /// <param name="semanticModel">
        /// The semantic model used for constructor resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the list is known to start empty;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsKnownEmptyListCreation(
            ExpressionSyntax creationExpression,
            SemanticModel semanticModel)
        {
            if (creationExpression
                    is not ObjectCreationExpressionSyntax
                && creationExpression
                    is not ImplicitObjectCreationExpressionSyntax)
            {
                return false;
            }

            InitializerExpressionSyntax? initializer =
                creationExpression switch
                {
                    ObjectCreationExpressionSyntax objectCreation =>
                        objectCreation.Initializer,
                    ImplicitObjectCreationExpressionSyntax implicitCreation =>
                        implicitCreation.Initializer,
                    _ => null
                };

            if (initializer != null
                && initializer.Expressions.Count != 0)
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    creationExpression);

            if (symbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol
                || constructorSymbol.MethodKind !=
                    MethodKind.Constructor
                || !IsListType(
                    constructorSymbol.ContainingType))
            {
                return false;
            }

            foreach (IParameterSymbol parameter
                     in constructorSymbol.Parameters)
            {
                if (parameter.Type.SpecialType ==
                    SpecialType.System_Int32)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a property is the read-only <c>Count</c>
        /// property of a supported framework collection abstraction.
        /// </summary>
        /// <param name="propertySymbol">The property symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the property is a supported framework
        /// collection count observation; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsFrameworkCollectionCountProperty(
            IPropertySymbol propertySymbol)
        {
            if (!string.Equals(
                    propertySymbol.Name,
                    "Count",
                    StringComparison.Ordinal)
                || propertySymbol.GetMethod == null
                || propertySymbol.SetMethod != null
                || propertySymbol.Parameters.Length != 0)
            {
                return false;
            }

            INamedTypeSymbol containingType =
                propertySymbol.ContainingType.OriginalDefinition;
            string namespaceName =
                containingType.ContainingNamespace.ToDisplayString();

            if (string.Equals(
                    namespaceName,
                    "System.Collections.Generic",
                    StringComparison.Ordinal))
            {
                return string.Equals(
                           containingType.Name,
                           "IReadOnlyCollection",
                           StringComparison.Ordinal)
                    || string.Equals(
                           containingType.Name,
                           "IReadOnlyList",
                           StringComparison.Ordinal)
                    || string.Equals(
                           containingType.Name,
                           "ICollection",
                           StringComparison.Ordinal)
                    || string.Equals(
                           containingType.Name,
                           "IList",
                           StringComparison.Ordinal)
                    || string.Equals(
                           containingType.Name,
                           "List",
                           StringComparison.Ordinal);
            }

            return string.Equals(
                       namespaceName,
                       "System.Collections",
                       StringComparison.Ordinal)
                && string.Equals(
                       containingType.Name,
                       "ICollection",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the source sequence of a supported element-preserving LINQ
        /// invocation.
        /// </summary>
        /// <param name="invocation">The invocation expression.</param>
        /// <param name="methodSymbol">The method selected at the call site.</param>
        /// <param name="originalMethod">
        /// The original definition of the selected method.
        /// </param>
        /// <param name="sourceExpression">The resolved source expression.</param>
        /// <returns>
        /// <see langword="true"/> when the invocation preserves element
        /// identity and its source was resolved; otherwise
        /// <see langword="false"/>.
        /// </returns>
        internal static bool TryGetElementPreservingSequenceSource(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            IMethodSymbol originalMethod,
            out ExpressionSyntax? sourceExpression)
        {
            sourceExpression = null;

            if (!IsElementPreservingSequenceMethod(originalMethod))
            {
                return false;
            }

            return TryGetSequenceSourceExpression(
                invocation,
                methodSymbol,
                out sourceExpression);
        }

        /// <summary>
        /// Determines whether a framework sequence operation preserves the
        /// identity of its input elements.
        /// </summary>
        /// <param name="methodSymbol">The original method definition.</param>
        /// <returns>
        /// <see langword="true"/> for supported filtering, ordering, and
        /// materialization methods; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsElementPreservingSequenceMethod(
            IMethodSymbol methodSymbol)
        {
            return string.Equals(
                       methodSymbol.Name,
                       "Where",
                       StringComparison.Ordinal)
                || string.Equals(
                       methodSymbol.Name,
                       "OrderBy",
                       StringComparison.Ordinal)
                || string.Equals(
                       methodSymbol.Name,
                       "ToArray",
                       StringComparison.Ordinal)
                || string.Equals(
                       methodSymbol.Name,
                       "ToList",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a property is the framework dictionary
        /// <c>Values</c> property.
        /// </summary>
        /// <param name="propertySymbol">The property symbol to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for the framework dictionary values
        /// property; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDictionaryValuesProperty(
            IPropertySymbol propertySymbol)
        {
            return string.Equals(
                       propertySymbol.Name,
                       "Values",
                       StringComparison.Ordinal)
                && propertySymbol.Parameters.Length == 0
                && IsDictionaryType(propertySymbol.ContainingType);
        }

        /// <summary>
        /// Determines whether a sequence value has a concrete framework type
        /// whose ordinary enumeration does not mutate its contents.
        /// </summary>
        /// <param name="typeSymbol">The compile-time source type.</param>
        /// <returns>
        /// <see langword="true"/> for arrays and framework
        /// <see cref="List{T}"/> values; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsKnownMaterializedSequenceType(
            ITypeSymbol typeSymbol)
        {
            return typeSymbol is IArrayTypeSymbol || IsListType(typeSymbol);
        }

        /// <summary>
        /// Determines whether a type is the framework generic dictionary type.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for the framework generic dictionary type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsDictionaryType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            INamedTypeSymbol originalType = namedType.OriginalDefinition;

            return string.Equals(
                       originalType.Name,
                       "Dictionary",
                       StringComparison.Ordinal)
                && originalType.Arity == 2
                && string.Equals(
                       originalType.ContainingNamespace.ToDisplayString(),
                       "System.Collections.Generic",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a type is the framework generic equality
        /// comparer interface.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> for the framework generic equality comparer;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsEqualityComparerType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            INamedTypeSymbol originalType = namedType.OriginalDefinition;

            return string.Equals(
                       originalType.Name,
                       "IEqualityComparer",
                       StringComparison.Ordinal)
                && originalType.Arity == 1
                && string.Equals(
                       originalType.ContainingNamespace.ToDisplayString(),
                       "System.Collections.Generic",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an expression creates an empty framework
        /// dictionary without a collection initializer or source collection.
        /// </summary>
        /// <param name="creationExpression">The creation expression.</param>
        /// <param name="semanticModel">
        /// The semantic model used for constructor resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the selected constructor cannot seed
        /// dictionary entries; otherwise <see langword="false"/>.
        /// </returns>
        internal static bool IsKnownEmptyDictionaryCreation(
            ExpressionSyntax creationExpression,
            SemanticModel semanticModel)
        {
            if (creationExpression is not ObjectCreationExpressionSyntax
                && creationExpression is not ImplicitObjectCreationExpressionSyntax)
            {
                return false;
            }

            InitializerExpressionSyntax? initializer =
                creationExpression switch
                {
                    ObjectCreationExpressionSyntax objectCreation =>
                        objectCreation.Initializer,
                    ImplicitObjectCreationExpressionSyntax implicitCreation =>
                        implicitCreation.Initializer,
                    _ => null
                };

            if (initializer != null
                && initializer.Expressions.Count != 0)
            {
                return false;
            }

            SymbolInfo constructorSymbolInfo =
                semanticModel.GetSymbolInfo(creationExpression);

            if (constructorSymbolInfo.Symbol is not IMethodSymbol constructorSymbol
                || constructorSymbol.MethodKind != MethodKind.Constructor
                || !IsDictionaryType(constructorSymbol.ContainingType))
            {
                return false;
            }

            foreach (IParameterSymbol parameter in constructorSymbol.Parameters)
            {
                if (parameter.Type.SpecialType == SpecialType.System_Int32
                    || IsEqualityComparerType(parameter.Type))
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
