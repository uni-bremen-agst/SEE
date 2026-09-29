using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSymbolUsageFacts;
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
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
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
                ExceptionFlowCallContext callContext,
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
                callContext,
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
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
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
                ExceptionFlowCallContext callContext,
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
                        ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                            semanticModel,
                            variableDeclarator.SyntaxTree);

                    if (declarationSemanticModel == null)
                    {
                        return false;
                    }

                    return IsGroupingSequenceProvenToContainNonNullElements(
                        variableDeclarator.Initializer.Value,
                        declarationSemanticModel,
                        callContext,
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

            if (!ExceptionFlowSequenceCollectionFactsProvider.IsElementPreservingGroupByMethod(
                    originalMethod) ||
                !ExceptionFlowSequenceCollectionFactsProvider.TryGetSequenceSourceExpression(
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
                callContext,
                inspectedSequenceSources);
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
        /// <param name="callContext">
        /// The call-site facts known for the current callable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the operation is known to preserve the
        /// invariant; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsLocalListReferenceSafeForNonNullElements(
            IdentifierNameSyntax reference,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callContext)
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
                !ExceptionFlowSequenceCollectionFactsProvider.IsListType(
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

            return GetExpressionValueFacts(
                    argument.Expression,
                    semanticModel,
                    callContext)
                .ContainsAll(ExceptionFlowValueFacts.NonNull);
        }
    }
}
