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
    }
}
