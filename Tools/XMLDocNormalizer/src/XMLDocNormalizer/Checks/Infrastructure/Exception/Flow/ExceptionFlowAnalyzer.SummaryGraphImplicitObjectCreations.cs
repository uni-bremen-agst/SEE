using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for target-typed object creation.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Collects constructor edges from target-typed <c>new()</c>
        /// expressions.
        /// </summary>
        /// <param name="node">
        /// The executable syntax node to inspect.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for constructor resolution.
        /// </param>
        /// <param name="graph">
        /// The graph receiving constructor targets.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving constructor edges.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AnalyzeSummaryImplicitObjectCreations(
            SyntaxNode node,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            foreach (ImplicitObjectCreationExpressionSyntax creation
                     in GetSummaryDescendantsAndSelf
                         <ImplicitObjectCreationExpressionSyntax>(node))
            {
                if (IsPartOfDirectThrow(
                        creation))
                {
                    continue;
                }

                SymbolInfo symbolInfo =
                    semanticModel.GetSymbolInfo(
                        creation);

                if (symbolInfo.Symbol
                    is not IMethodSymbol constructorSymbol)
                {
                    fragment.AddUncertainTarget(
                        "Target-typed object constructor");

                    continue;
                }

                ExceptionFlowCallContext targetContext =
                    CreateCallContext(
                        constructorSymbol,
                        creation.ArgumentList.Arguments,
                        semanticModel,
                        callContext);

                ExceptionFlowCallableKey targetKey =
                    new(
                        constructorSymbol,
                        targetContext.Key);

                graph.GetOrAdd(
                    targetKey,
                    targetContext);

                fragment.AddCallEdge(
                    new ExceptionFlowSummaryCallEdge(
                        targetKey,
                        CreatePathStep(
                            ExceptionFlowPathStepKind.ConstructorCall,
                            constructorSymbol,
                            creation)));
            }
        }

        /// <summary>
        /// Determines whether target-typed object creation is directly thrown
        /// and is therefore already covered by direct throw analysis.
        /// </summary>
        /// <param name="creation">
        /// The target-typed object creation to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the creation is directly thrown;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsPartOfDirectThrow(
            ImplicitObjectCreationExpressionSyntax creation)
        {
            return creation.Parent
                       is ThrowStatementSyntax ||
                   creation.Parent
                       is ThrowExpressionSyntax;
        }
    }
}
