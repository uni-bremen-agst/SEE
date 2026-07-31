using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains value-fact analysis for values returned by directly bound
    /// source methods.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Attempts to derive facts guaranteed by the normal return value of a
        /// directly and statically bound source method.
        /// </summary>
        /// <param name="invocation">
        /// The invocation expression whose result is inspected.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the call site.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known for the caller.
        /// </param>
        /// <param name="inspectedValueSources">
        /// The immutable members and source methods currently being inspected
        /// recursively.
        /// </param>
        /// <param name="facts">
        /// The facts guaranteed for the returned value when analysis
        /// succeeds.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when a supported source return expression
        /// provides at least one value fact; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetSourceInvocationReturnValueFacts(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            ExceptionFlowCallContext callerContext,
            HashSet<ISymbol> inspectedValueSources,
            out ExceptionFlowValueFacts facts)
        {
            facts =
                ExceptionFlowValueFacts.None;

            SymbolInfo symbolInfo =
                semanticModel.GetSymbolInfo(
                    invocation);

            if (symbolInfo.Symbol
                    is not IMethodSymbol selectedMethod ||
                selectedMethod.ReducedFrom != null ||
                selectedMethod.ReturnsVoid ||
                selectedMethod.IsAsync ||
                selectedMethod.IsExtern ||
                selectedMethod.IsAbstract ||
                selectedMethod.IsIterator ||
                selectedMethod.ReturnsByRef ||
                selectedMethod.ReturnsByRefReadonly ||
                RequiresSummaryRuntimeDispatch(
                    selectedMethod))
            {
                return false;
            }

            IMethodSymbol targetMethod =
                selectedMethod.OriginalDefinition;

            if (targetMethod.DeclaringSyntaxReferences.Length != 1 ||
                !inspectedValueSources.Add(
                    targetMethod))
            {
                return false;
            }

            try
            {
                SyntaxNode declaration =
                    targetMethod.DeclaringSyntaxReferences[0]
                        .GetSyntax();

                if (!TryGetSingleSourceReturnExpression(
                        declaration,
                        out ExpressionSyntax? returnExpression) ||
                    returnExpression == null)
                {
                    return false;
                }

                SemanticModel? returnSemanticModel =
                    GetSemanticModelForSyntaxTree(
                        semanticModel,
                        returnExpression.SyntaxTree);

                if (returnSemanticModel == null)
                {
                    return false;
                }

                ExceptionFlowCallContext calleeContext =
                    CreateCallContext(
                        selectedMethod,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callerContext);

                ExceptionFlowValueFacts returnFacts =
                    GetExpressionValueFacts(
                        returnExpression,
                        returnSemanticModel,
                        calleeContext,
                        inspectedValueSources);

                if (returnFacts ==
                    ExceptionFlowValueFacts.None)
                {
                    return false;
                }

                facts =
                    returnFacts.Normalize();

                return true;
            }
            finally
            {
                inspectedValueSources.Remove(
                    targetMethod);
            }
        }

        /// <summary>
        /// Gets the only return expression represented by a supported source
        /// method or local-function declaration.
        /// </summary>
        /// <param name="declaration">
        /// The source declaration to inspect.
        /// </param>
        /// <param name="returnExpression">
        /// The uniquely identified return expression when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when exactly one return expression exists
        /// outside nested functions; otherwise <see langword="false"/>.
        /// </returns>
        private static bool TryGetSingleSourceReturnExpression(
            SyntaxNode declaration,
            out ExpressionSyntax? returnExpression)
        {
            returnExpression =
                null;

            ArrowExpressionClauseSyntax? expressionBody;
            BlockSyntax? body;

            switch (declaration)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    expressionBody =
                        methodDeclaration.ExpressionBody;

                    body =
                        methodDeclaration.Body;
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    expressionBody =
                        localFunction.ExpressionBody;

                    body =
                        localFunction.Body;
                    break;

                default:
                    return false;
            }

            if (expressionBody != null)
            {
                returnExpression =
                    expressionBody.Expression;

                return true;
            }

            if (body == null)
            {
                return false;
            }

            List<ReturnStatementSyntax> returnStatements =
                body.DescendantNodesAndSelf(
                        static node =>
                            node
                                is not AnonymousFunctionExpressionSyntax &&
                            node
                                is not LocalFunctionStatementSyntax)
                    .OfType<ReturnStatementSyntax>()
                    .ToList();

            if (returnStatements.Count != 1 ||
                returnStatements[0].Expression == null)
            {
                return false;
            }

            returnExpression =
                returnStatements[0].Expression;

            return true;
        }
    }
}
