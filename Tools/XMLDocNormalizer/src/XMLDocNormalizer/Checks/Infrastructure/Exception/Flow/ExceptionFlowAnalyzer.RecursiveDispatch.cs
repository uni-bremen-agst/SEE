using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains runtime-dispatch analysis for recursive transitive exception
    /// flow.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Resolves and analyzes the known runtime targets of one virtual or
        /// interface invocation.
        /// </summary>
        /// <remarks>
        /// Runtime-target discovery and completeness checks are shared with
        /// summary-graph analysis so that both transitive engines apply the
        /// same dispatch semantics within their respective semantic scopes.
        /// Known targets are analyzed even when additional external targets
        /// may exist; incomplete target sets additionally preserve
        /// uncertainty.
        /// </remarks>
        /// <param name="invocation">
        /// The invocation syntax to inspect.
        /// </param>
        /// <param name="methodSymbol">
        /// The method selected by compile-time binding.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="semanticContext">
        /// The semantic context defining the project analysis scope.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        /// <param name="traversalState">
        /// The traversal state used to prevent recursive analysis cycles.
        /// </param>
        /// <param name="callerContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the invocation requires runtime
        /// dispatch and was handled here; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryAnalyzeRecursiveRuntimeDispatch(
            InvocationExpressionSyntax invocation,
            IMethodSymbol methodSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowAnalysisResult result,
            ExceptionFlowTraversalState traversalState,
            ExceptionFlowCallContext callerContext)
        {
            if (semanticModel.GetOperation(invocation)
                    is not IInvocationOperation invocationOperation ||
                !invocationOperation.IsVirtual)
            {
                return false;
            }

            ITypeSymbol? receiverType =
                invocationOperation.Instance?.Type;

            INamedTypeSymbol? exactReceiverType =
                GetSummaryExactReceiverType(
                    invocationOperation.Instance);

            IReadOnlyList<IMethodSymbol> runtimeTargets =
                ResolveSummaryRuntimeTargets(
                    methodSymbol,
                    receiverType,
                    exactReceiverType,
                    semanticContext);

            if (!IsSummaryDispatchTargetSetComplete(
                    methodSymbol,
                    receiverType,
                    exactReceiverType,
                    semanticContext))
            {
                result.UncertainTargets.Add(
                    CreateSummaryDispatchUncertainty(
                        methodSymbol,
                        receiverType));
            }

            foreach (IMethodSymbol runtimeTarget
                     in runtimeTargets)
            {
                ExceptionFlowCallContext targetContext =
                    CreateDispatchCallContext(
                        methodSymbol,
                        runtimeTarget,
                        invocation.ArgumentList.Arguments,
                        semanticModel,
                        callerContext);

                if (!traversalState.TryMarkAnalyzed(
                        runtimeTarget,
                        targetContext))
                {
                    continue;
                }

                if (!AnalyzeSymbol(
                        runtimeTarget,
                        semanticContext,
                        result,
                        traversalState,
                        targetContext))
                {
                    MarkUncertain(
                        result,
                        runtimeTarget);
                }
            }

            return true;
        }
    }
}
