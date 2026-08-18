using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains recursive analysis of local functions, anonymous functions,
    /// and stable delegate invocations.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Attempts to analyze one local or anonymous callable declaration.
        /// </summary>
        /// <param name="declarationNode">
        /// The declaration syntax associated with the callable symbol.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model associated with the declaration.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callContext">
        /// The call-site facts known for the local callable.
        /// </param>
        /// <param name="analyzedBody">
        /// Receives <see langword="true"/> when an executable callable body
        /// was analyzed; otherwise <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="declarationNode"/>
        /// represents a local or anonymous callable declaration; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryAnalyzeLocalCallableDeclaration(
            SyntaxNode declarationNode,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callContext,
            out bool analyzedBody)
        {
            analyzedBody = false;

            if (declarationNode
                is LocalFunctionStatementSyntax localFunction)
            {
                if (localFunction.Body != null)
                {
                    AnalyzeNode(
                        localFunction.Body,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        ExceptionFlowTraversalMode.Transitive,
                        callContext);

                    analyzedBody = true;
                }
                else if (localFunction.ExpressionBody != null)
                {
                    AnalyzeNode(
                        localFunction.ExpressionBody.Expression,
                        semanticModel,
                        semanticContext,
                        result,
                        traversalState,
                        ExceptionFlowTraversalMode.Transitive,
                        callContext);

                    analyzedBody = true;
                }

                return true;
            }

            if (declarationNode
                is not AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                return false;
            }

            SyntaxNode? body =
                anonymousFunction switch
                {
                    ParenthesizedLambdaExpressionSyntax lambda =>
                        lambda.Body,

                    SimpleLambdaExpressionSyntax lambda =>
                        lambda.Body,

                    AnonymousMethodExpressionSyntax anonymousMethod =>
                        anonymousMethod.Block,

                    _ => null
                };

            if (body != null)
            {
                AnalyzeNode(
                    body,
                    semanticModel,
                    semanticContext,
                    result,
                    traversalState,
                    ExceptionFlowTraversalMode.Transitive,
                    callContext);

                analyzedBody = true;
            }

            return true;
        }

        /// <summary>
        /// Resolves and analyzes one invocation through a stable delegate
        /// target.
        /// </summary>
        /// <param name="invocation">
        /// The delegate invocation to analyze.
        /// </param>
        /// <param name="delegateInvokeSymbol">
        /// The delegate <c>Invoke</c> method selected at the call site.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used to resolve the concrete delegate target.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callerContext">
        /// The call-site facts known for the current callable.
        /// </param>
        private static void AnalyzeDelegateInvocation(
            InvocationExpressionSyntax invocation,
            IMethodSymbol delegateInvokeSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callerContext)
        {
            if (!TryResolveDelegateTarget(
                    invocation.Expression,
                    semanticModel,
                    out IMethodSymbol? targetMethod) ||
                targetMethod == null)
            {
                MarkUncertain(
                    result,
                    delegateInvokeSymbol);

                return;
            }

            ExceptionFlowCallContext targetContext =
                CreateCallContext(
                    targetMethod,
                    invocation.ArgumentList.Arguments,
                    semanticModel,
                    callerContext);

            if (!traversalState.TryMarkAnalyzed(
                    targetMethod,
                    targetContext))
            {
                return;
            }

            if (!AnalyzeSymbol(
                    targetMethod,
                    semanticContext,
                    result,
                    traversalState,
                    targetContext))
            {
                MarkUncertain(
                    result,
                    targetMethod);
            }
        }
    }
}
