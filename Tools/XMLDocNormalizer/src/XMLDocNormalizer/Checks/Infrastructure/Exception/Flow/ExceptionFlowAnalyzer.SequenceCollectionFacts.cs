using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains non-null element reasoning for mutable framework collections
    /// and grouping sequences.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Determines whether a foreach iteration variable represents an
        /// <see cref="IGrouping{TKey,TElement}"/> whose elements originate
        /// from a sequence proven to exclude <see langword="null"/>.
        /// </summary>
        /// <param name="localSymbol">
        /// The foreach iteration-variable symbol.
        /// </param>
        /// <param name="declarationNode">
        /// The syntax node declaring the iteration variable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when every element exposed by the grouping
        /// is proven non-null; otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            IsForeachGroupingLocalProvenToExcludeNullElements(
                ILocalSymbol localSymbol,
                SyntaxNode declarationNode,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            ForEachStatementSyntax? foreachStatement =
                declarationNode as ForEachStatementSyntax ??
                declarationNode.AncestorsAndSelf()
                    .OfType<ForEachStatementSyntax>()
                    .FirstOrDefault();

            if (foreachStatement == null)
            {
                return false;
            }

            ISymbol? iterationVariable =
                semanticModel.GetDeclaredSymbol(
                    foreachStatement);

            if (!SymbolEqualityComparer.Default.Equals(
                    iterationVariable,
                    localSymbol))
            {
                return false;
            }

            return IsGroupingSequenceProvenToContainNonNullElements(
                foreachStatement.Expression,
                semanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a sequence enumerates groupings whose contained
        /// elements originate unchanged from a sequence proven to exclude
        /// <see langword="null"/>.
        /// </summary>
        /// <param name="expression">
        /// The grouping sequence expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol resolution.
        /// </param>
        /// <param name="inspectedSequenceSources">
        /// The sequence symbols currently being inspected.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when grouping elements are proven non-null;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool
            IsGroupingSequenceProvenToContainNonNullElements(
                ExpressionSyntax expression,
                SemanticModel semanticModel,
                HashSet<ISymbol> inspectedSequenceSources)
        {
            ExpressionSyntax unwrappedExpression =
                UnwrapParenthesizedExpression(
                    expression);

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (symbolInfo.Symbol
                    is ILocalSymbol localSymbol &&
                localSymbol.DeclaringSyntaxReferences.Length == 1)
            {
                if (!inspectedSequenceSources.Add(
                        localSymbol))
                {
                    return false;
                }

                try
                {
                    SyntaxNode declarationNode =
                        localSymbol.DeclaringSyntaxReferences[0]
                            .GetSyntax();

                    if (declarationNode
                            is not VariableDeclaratorSyntax variableDeclarator ||
                        variableDeclarator.Initializer == null ||
                        !IsLocalSequenceInitializerStillCurrent(
                            unwrappedExpression,
                            localSymbol,
                            variableDeclarator,
                            semanticModel))
                    {
                        return false;
                    }

                    SemanticModel? declarationSemanticModel =
                        GetSemanticModelForSyntaxTree(
                            semanticModel,
                            variableDeclarator.SyntaxTree);

                    if (declarationSemanticModel == null)
                    {
                        return false;
                    }

                    return IsGroupingSequenceProvenToContainNonNullElements(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel,
                        inspectedSequenceSources);
                }
                finally
                {
                    inspectedSequenceSources.Remove(
                        localSymbol);
                }
            }

            if (unwrappedExpression
                is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            SymbolInfo invocationSymbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (invocationSymbolInfo.Symbol
                    is not IMethodSymbol selectedMethod)
            {
                return false;
            }

            IMethodSymbol originalMethod =
                selectedMethod.ReducedFrom?.OriginalDefinition ??
                selectedMethod.OriginalDefinition;

            if (!IsElementPreservingGroupByMethod(
                    originalMethod) ||
                !TryGetSequenceSourceExpression(
                    invocation,
                    selectedMethod,
                    out ExpressionSyntax? sourceExpression) ||
                sourceExpression == null)
            {
                return false;
            }

            return IsSequenceExpressionProvenToExcludeNullElements(
                sourceExpression,
                semanticModel,
                inspectedSequenceSources);
        }

        /// <summary>
        /// Determines whether a LINQ method is a <c>GroupBy</c> overload that
        /// keeps the original source elements as grouping elements.
        /// </summary>
        /// <param name="methodSymbol">
        /// The original method definition.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for supported <c>GroupBy</c> overloads;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsElementPreservingGroupByMethod(
            IMethodSymbol methodSymbol)
        {
            return methodSymbol.IsStatic &&
                   string.Equals(
                       methodSymbol.Name,
                       "GroupBy",
                       StringComparison.Ordinal) &&
                   methodSymbol.Arity == 2 &&
                   string.Equals(
                       methodSymbol.ContainingType.ToDisplayString(),
                       "System.Linq.Enumerable",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the input sequence of an extension-style or ordinary static LINQ
        /// invocation.
        /// </summary>
        /// <param name="invocation">
        /// The invocation expression.
        /// </param>
        /// <param name="selectedMethod">
        /// The method selected at the invocation site.
        /// </param>
        /// <param name="sourceExpression">
        /// The resolved source sequence expression.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a source expression was resolved; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetSequenceSourceExpression(
            InvocationExpressionSyntax invocation,
            IMethodSymbol selectedMethod,
            out ExpressionSyntax? sourceExpression)
        {
            sourceExpression = null;

            if (invocation.Expression
                    is MemberAccessExpressionSyntax memberAccess &&
                (selectedMethod.ReducedFrom != null ||
                 (selectedMethod.IsExtensionMethod &&
                  invocation.ArgumentList.Arguments.Count <
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
        /// Determines whether a local <see cref="List{T}"/> contains only
        /// elements proven to be non-null at the specified use site.
        /// </summary>
        /// <param name="expression">
        /// The list use being analyzed.
        /// </param>
        /// <param name="localSymbol">
        /// The local list symbol.
        /// </param>
        /// <param name="variableDeclarator">
        /// The declaration of the list.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the list starts empty and all operations
        /// before the use preserve the non-null element invariant; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsLocalListProvenToExcludeNullElements(
            ExpressionSyntax expression,
            ILocalSymbol localSymbol,
            VariableDeclaratorSyntax variableDeclarator,
            SemanticModel semanticModel)
        {
            if (!IsListType(localSymbol.Type) ||
                variableDeclarator.Initializer == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                GetSemanticModelForSyntaxTree(
                    semanticModel,
                    variableDeclarator.SyntaxTree);

            if (declarationSemanticModel == null ||
                !IsKnownEmptyListCreation(
                    variableDeclarator.Initializer.Value,
                    declarationSemanticModel))
            {
                return false;
            }

            SyntaxNode? containingCallable =
                variableDeclarator.Ancestors()
                    .FirstOrDefault(
                        static node =>
                            node is MethodDeclarationSyntax ||
                            node is LocalFunctionStatementSyntax);

            if (containingCallable == null ||
                containingCallable.SyntaxTree !=
                    expression.SyntaxTree)
            {
                return false;
            }

            IEnumerable<IdentifierNameSyntax> references =
                containingCallable.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(
                        identifier =>
                            identifier.SpanStart >
                                variableDeclarator.Span.End &&
                            identifier.SpanStart <
                                expression.SpanStart &&
                            ExpressionReferencesSymbol(
                                identifier,
                                localSymbol,
                                declarationSemanticModel));

            foreach (IdentifierNameSyntax reference in references)
            {
                if (!IsLocalListReferenceSafeForNonNullElements(
                        reference,
                        declarationSemanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether a type is <see cref="List{T}"/>.
        /// </summary>
        /// <param name="typeSymbol">
        /// The type to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for the framework generic list type;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsListType(
            ITypeSymbol typeSymbol)
        {
            if (typeSymbol
                is not INamedTypeSymbol namedType)
            {
                return false;
            }

            INamedTypeSymbol originalType =
                namedType.OriginalDefinition;

            return originalType.Arity == 1 &&
                   string.Equals(
                       originalType.Name,
                       "List",
                       StringComparison.Ordinal) &&
                   string.Equals(
                       originalType.ContainingNamespace
                           .ToDisplayString(),
                       "System.Collections.Generic",
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether an expression creates an empty
        /// <see cref="List{T}"/>.
        /// </summary>
        /// <param name="creationExpression">
        /// The creation expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constructor resolution.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the list is known to start empty;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsKnownEmptyListCreation(
            ExpressionSyntax creationExpression,
            SemanticModel semanticModel)
        {
            if (creationExpression
                    is not ObjectCreationExpressionSyntax &&
                creationExpression
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

            if (initializer != null &&
                initializer.Expressions.Count != 0)
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    creationExpression);

            if (symbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol ||
                constructorSymbol.MethodKind !=
                    MethodKind.Constructor ||
                !IsListType(
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
        /// Determines whether one reference to a local list preserves the
        /// invariant that every contained element is non-null.
        /// </summary>
        /// <param name="reference">
        /// The local list reference.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value analysis.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the operation is known to preserve the
        /// invariant; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalListReferenceSafeForNonNullElements(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel)
        {
            if (reference.Parent
                    is ReturnStatementSyntax)
            {
                return true;
            }

            if (IsSupportedReadOnlySequenceObservation(
                    reference,
                    semanticModel))
            {
                return true;
            }

            if (reference.Parent
                    is not MemberAccessExpressionSyntax memberAccess ||
                !ReferenceEquals(
                    memberAccess.Expression,
                    reference) ||
                memberAccess.Parent
                    is not InvocationExpressionSyntax invocation ||
                !ReferenceEquals(
                    invocation.Expression,
                    memberAccess))
            {
                return false;
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol methodSymbol ||
                !IsListType(
                    methodSymbol.ContainingType))
            {
                return false;
            }

            if (string.Equals(
                    methodSymbol.Name,
                    "Clear",
                    StringComparison.Ordinal) ||
                string.Equals(
                    methodSymbol.Name,
                    "Remove",
                    StringComparison.Ordinal) ||
                string.Equals(
                    methodSymbol.Name,
                    "RemoveAt",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(
                    methodSymbol.Name,
                    "Add",
                    StringComparison.Ordinal) ||
                invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            ArgumentSyntax argument =
                invocation.ArgumentList.Arguments[0];

            ISymbol? enclosingSymbol =
                semanticModel.GetEnclosingSymbol(
                    argument.Expression.SpanStart);

            ExceptionFlowCallContext emptyContext =
                new(enclosingSymbol);

            ExceptionFlowValueFacts valueFacts =
                GetExpressionValueFacts(
                    argument.Expression,
                    semanticModel,
                    emptyContext);

            return valueFacts.ContainsAll(
                ExceptionFlowValueFacts.NonNull);
        }
    }
}
