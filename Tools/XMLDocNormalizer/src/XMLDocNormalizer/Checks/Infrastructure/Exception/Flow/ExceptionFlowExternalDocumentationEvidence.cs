using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Projects external XML-documentation exception evidence into direct and
    /// summary-graph analysis results.
    /// </summary>
    /// <remarks>
    /// This stateless Roslyn-bound component supplements executable analysis
    /// without claiming that unavailable external bodies are complete. It is
    /// safe for concurrent use.
    /// </remarks>
    internal static class ExceptionFlowExternalDocumentationEvidence
    {
        /// <summary>
        /// Adds documented external exception contracts to the recursive
        /// transitive analysis result.
        /// </summary>
        /// <remarks>
        /// External documentation supplements normal target analysis. It does
        /// not make an unavailable external method body complete, so normal
        /// uncertainty handling remains active.
        /// </remarks>
        /// <param name="invocation">
        /// The external method invocation.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved external method.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="result">
        /// The accumulated exception-flow result.
        /// </param>
        internal static void AddToAnalysisResult(
                InvocationExpressionSyntax invocation,
                IMethodSymbol methodSymbol,
                SemanticModel semanticModel,
                ExceptionFlowAnalysisResult result)
        {
            IReadOnlyList<INamedTypeSymbol> documentedExceptions =
                ExternalDocumentationExceptionModel.GetDocumentedExceptionTypes(
                    methodSymbol, semanticModel.Compilation);

            foreach (INamedTypeSymbol exceptionType
                     in documentedExceptions)
            {
                result.AddExternalDocumentationEvidencePath(
                    exceptionType,
                    ExceptionFlowPathFactory.CreateTerminal(
                        ExceptionFlowPathStepKind
                            .ExternalDocumentationEvidence,
                        methodSymbol,
                        invocation));
            }
        }

        /// <summary>
        /// Adds documented external exception contracts to a summary-graph
        /// fragment.
        /// </summary>
        /// <remarks>
        /// External documentation supplements normal summary target
        /// handling. Unknown executable behavior remains uncertain.
        /// </remarks>
        /// <param name="invocation">
        /// The external method invocation.
        /// </param>
        /// <param name="methodSymbol">
        /// The resolved external method.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model of the call site.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment.
        /// </param>
        internal static void AddToSummaryFragment(
                InvocationExpressionSyntax invocation,
                IMethodSymbol methodSymbol,
                SemanticModel semanticModel,
                ExceptionFlowSummaryFragment fragment)
        {
            IReadOnlyList<INamedTypeSymbol> documentedExceptions =
                ExternalDocumentationExceptionModel.GetDocumentedExceptionTypes(
                    methodSymbol, semanticModel.Compilation);

            foreach (INamedTypeSymbol exceptionType
                     in documentedExceptions)
            {
                fragment.AddSource(
                    new ExceptionFlowSummarySource(
                        exceptionType,
                        ExceptionFlowPathFactory.CreateTerminal(
                            ExceptionFlowPathStepKind
                                .ExternalDocumentationEvidence,
                            methodSymbol,
                            invocation),
                        ExceptionFlowSourceKind
                            .ExternalDocumentationEvidence));
            }
        }
    }
}
