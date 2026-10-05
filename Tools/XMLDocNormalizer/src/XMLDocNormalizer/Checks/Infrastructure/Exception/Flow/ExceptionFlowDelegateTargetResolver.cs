using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Resolves delegate-valued expressions to one stable callable target.
    /// </summary>
    internal static class ExceptionFlowDelegateTargetResolver
    {
        /// <summary>
        /// Attempts to resolve the concrete target of a delegate expression.
        /// </summary>
        /// <param name="expression">
        /// The expression invoked through the delegate.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for target resolution.
        /// </param>
        /// <param name="targetMethod">
        /// The resolved anonymous function, local function, or method-group
        /// target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if one stable target was resolved;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryResolveDelegateTarget(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out IMethodSymbol? targetMethod)
        {
            HashSet<ISymbol> inspectedSymbols =
                new(SymbolEqualityComparer.Default);

            return TryResolveDelegateTarget(
                expression,
                semanticModel,
                inspectedSymbols,
                out targetMethod);
        }

        /// <summary>
        /// Attempts to resolve a delegate target while preventing cycles
        /// between local delegate variables.
        /// </summary>
        /// <param name="expression">
        /// The delegate-valued expression.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for target resolution.
        /// </param>
        /// <param name="inspectedSymbols">
        /// The local symbols already followed during the current resolution.
        /// </param>
        /// <param name="targetMethod">
        /// The resolved callable target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if one stable target was resolved;
        /// otherwise <see langword="false"/>.
        /// </returns>
        internal static bool TryResolveDelegateTarget(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSymbols,
            out IMethodSymbol? targetMethod)
        {
            targetMethod = null;

            ExpressionSyntax unwrappedExpression =
                UnwrapDelegateExpression(
                    expression);

            if (unwrappedExpression
                    is AnonymousFunctionExpressionSyntax
                        anonymousFunction &&
                semanticModel.GetOperation(anonymousFunction)
                    is IAnonymousFunctionOperation anonymousOperation)
            {
                targetMethod = anonymousOperation.Symbol;
                return true;
            }

            if (unwrappedExpression
                    is ObjectCreationExpressionSyntax creation &&
                creation.ArgumentList?.Arguments.Count == 1)
            {
                return TryResolveDelegateTarget(
                    creation.ArgumentList.Arguments[0].Expression,
                    semanticModel,
                    inspectedSymbols,
                    out targetMethod);
            }

            if (unwrappedExpression
                    is ImplicitObjectCreationExpressionSyntax
                        implicitCreation &&
                implicitCreation.ArgumentList.Arguments.Count == 1)
            {
                return TryResolveDelegateTarget(
                    implicitCreation.ArgumentList.Arguments[0].Expression,
                    semanticModel,
                    inspectedSymbols,
                    out targetMethod);
            }

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    unwrappedExpression);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.MethodKind !=
                    MethodKind.DelegateInvoke)
            {
                targetMethod = methodSymbol;
                return true;
            }

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                return TryResolveStableDelegateLocal(
                    localSymbol,
                    semanticModel,
                    inspectedSymbols,
                    out targetMethod);
            }

            IMethodSymbol[] candidateMethods =
                symbolInfo.CandidateSymbols
                    .OfType<IMethodSymbol>()
                    .Where(
                        static candidate =>
                            candidate.MethodKind !=
                            MethodKind.DelegateInvoke)
                    .ToArray();

            if (candidateMethods.Length == 1)
            {
                targetMethod = candidateMethods[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes syntax wrappers that do not change a delegate target.
        /// </summary>
        /// <param name="expression">
        /// The expression to unwrap.
        /// </param>
        /// <returns>The innermost delegate-valued expression.</returns>
        private static ExpressionSyntax UnwrapDelegateExpression(
            ExpressionSyntax expression)
        {
            ExpressionSyntax current =
                expression;

            while (true)
            {
                switch (current)
                {
                    case ParenthesizedExpressionSyntax parenthesized:
                        current = parenthesized.Expression;
                        continue;

                    case CastExpressionSyntax cast:
                        current = cast.Expression;
                        continue;

                    case CheckedExpressionSyntax checkedExpression:
                        current = checkedExpression.Expression;
                        continue;

                    case PostfixUnaryExpressionSyntax postfix
                        when postfix.IsKind(
                            SyntaxKind
                                .SuppressNullableWarningExpression):
                        current = postfix.Operand;
                        continue;

                    default:
                        return current;
                }
            }
        }

        /// <summary>
        /// Attempts to resolve a local delegate variable with exactly one
        /// stable initializer.
        /// </summary>
        /// <param name="localSymbol">
        /// The local delegate variable.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the invocation.
        /// </param>
        /// <param name="inspectedSymbols">
        /// The local symbols already followed during target resolution.
        /// </param>
        /// <param name="targetMethod">
        /// The resolved callable target.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the local has one stable target;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryResolveStableDelegateLocal(
            ILocalSymbol localSymbol,
            SemanticModel semanticModel,
            HashSet<ISymbol> inspectedSymbols,
            out IMethodSymbol? targetMethod)
        {
            targetMethod = null;

            ISymbol normalizedSymbol =
                localSymbol.OriginalDefinition;

            if (!inspectedSymbols.Add(
                    normalizedSymbol) ||
                localSymbol.DeclaringSyntaxReferences.Length != 1)
            {
                return false;
            }

            if (localSymbol.DeclaringSyntaxReferences[0]
                    .GetSyntax()
                is not VariableDeclaratorSyntax declarator ||
                declarator.Initializer == null)
            {
                return false;
            }

            SemanticModel? declarationSemanticModel =
                ExceptionFlowSemanticScope.GetSemanticModelForSyntaxTree(
                    semanticModel,
                    declarator.SyntaxTree);

            if (declarationSemanticModel == null ||
                HasDelegateLocalWrites(
                    localSymbol,
                    declarator,
                    declarationSemanticModel))
            {
                return false;
            }

            return TryResolveDelegateTarget(
                declarator.Initializer.Value,
                declarationSemanticModel,
                inspectedSymbols,
                out targetMethod);
        }

        /// <summary>
        /// Determines whether a local delegate can be reassigned or modified
        /// after its declaration initializer.
        /// </summary>
        /// <param name="localSymbol">
        /// The local delegate symbol.
        /// </param>
        /// <param name="declarator">
        /// The variable declaration containing its initial value.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol comparison.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if another write may target the local;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasDelegateLocalWrites(
            ILocalSymbol localSymbol,
            VariableDeclaratorSyntax declarator,
            SemanticModel semanticModel)
        {
            SyntaxNode root =
                declarator.SyntaxTree.GetRoot();

            foreach (AssignmentExpressionSyntax assignment
                     in root.DescendantNodes()
                         .OfType<AssignmentExpressionSyntax>())
            {
                if (ExceptionFlowCatchSemantics.ContainsLocalSymbolReference(
                        assignment.Left,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (PrefixUnaryExpressionSyntax prefix
                     in root.DescendantNodes()
                         .OfType<PrefixUnaryExpressionSyntax>())
            {
                if (!prefix.IsKind(
                        SyntaxKind.PreIncrementExpression) &&
                    !prefix.IsKind(
                        SyntaxKind.PreDecrementExpression))
                {
                    continue;
                }

                if (ExceptionFlowCatchSemantics.ContainsLocalSymbolReference(
                        prefix.Operand,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (PostfixUnaryExpressionSyntax postfix
                     in root.DescendantNodes()
                         .OfType<PostfixUnaryExpressionSyntax>())
            {
                if (!postfix.IsKind(
                        SyntaxKind.PostIncrementExpression) &&
                    !postfix.IsKind(
                        SyntaxKind.PostDecrementExpression))
                {
                    continue;
                }

                if (ExceptionFlowCatchSemantics.ContainsLocalSymbolReference(
                        postfix.Operand,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            foreach (ArgumentSyntax argument
                     in root.DescendantNodes()
                         .OfType<ArgumentSyntax>())
            {
                if (!argument.RefKindKeyword.IsKind(
                        SyntaxKind.RefKeyword) &&
                    !argument.RefKindKeyword.IsKind(
                        SyntaxKind.OutKeyword))
                {
                    continue;
                }

                if (ExceptionFlowCatchSemantics.ContainsLocalSymbolReference(
                        argument.Expression,
                        localSymbol,
                        semanticModel))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
