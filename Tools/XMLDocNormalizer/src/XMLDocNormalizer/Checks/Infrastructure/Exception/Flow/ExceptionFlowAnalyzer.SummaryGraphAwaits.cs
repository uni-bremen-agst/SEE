using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for explicit await expressions and
    /// the implicit awaits performed by asynchronous using constructs.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects awaiter calls belonging to explicit await expressions and
        /// asynchronous using constructs in the current callable.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for await binding information.
        /// </param>
        /// <param name="graph">
        /// The graph receiving awaiter targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving call edges or uncertainty.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryAwaitOperations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (AwaitExpressionSyntax awaitExpression
                     in GetSummaryDescendantsAndSelf
                         <AwaitExpressionSyntax>(node))
            {
                AwaitExpressionInfo awaitInfo =
                    semanticModel.GetAwaitExpressionInfo(
                        awaitExpression);

                AddSummaryExplicitAwaitEdges(
                    awaitInfo,
                    awaitExpression,
                    awaitExpression.Expression,
                    "Await expression",
                    semanticModel,
                    graph,
                    fragment,
                    callContext);
            }

            AnalyzeSummaryAwaitUsingOperations(
                node,
                semanticModel,
                graph,
                fragment,
                callContext);
        }

        /// <summary>
        /// Adds the awaiter chains used to consume the results of implicit
        /// <c>DisposeAsync</c> calls from await-using statements and
        /// declarations.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for resource and disposal resolution.
        /// </param>
        /// <param name="graph">
        /// The graph receiving awaiter targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving awaiter edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the containing callable.
        /// </param>
        private static void AnalyzeSummaryAwaitUsingOperations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            List<SummaryDisposalResource> resources =
                CollectSummaryDisposalResources(
                    node,
                    semanticModel);

            resources.Sort(
                static (left, right) =>
                {
                    int disposalPositionComparison =
                        left.DisposalPosition.CompareTo(
                            right.DisposalPosition);

                    if (disposalPositionComparison != 0)
                    {
                        return disposalPositionComparison;
                    }

                    return right.SourceNode.SpanStart.CompareTo(
                        left.SourceNode.SpanStart);
                });

            foreach (SummaryDisposalResource resource
                     in resources)
            {
                if (!resource.IsAsynchronous ||
                    resource.IsKnownNull ||
                    resource.ResourceType == null ||
                    resource.ResourceType.TypeKind ==
                        TypeKind.Dynamic)
                {
                    continue;
                }

                bool resolved =
                    TryResolveSummaryDisposalMethod(
                        resource.ResourceType,
                        resource.SourceNode,
                        true,
                        semanticModel,
                        out IMethodSymbol? disposalMethod);

                if (!resolved ||
                    disposalMethod == null)
                {
                    continue;
                }

                AddSummaryImplicitAwaitEdges(
                    disposalMethod.ReturnType,
                    resource.SourceNode,
                    "Await-using DisposeAsync",
                    semanticModel,
                    graph,
                    fragment,
                    callContext);
            }
        }
    }
}
