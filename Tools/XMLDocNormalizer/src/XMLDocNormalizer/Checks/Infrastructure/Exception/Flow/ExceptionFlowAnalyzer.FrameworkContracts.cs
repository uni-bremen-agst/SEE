using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains external XML-documentation exception contracts for
    /// transitively analyzed method invocations.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
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
        private static void
            AddExternalDocumentationContractExceptions(
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
                    CreateTerminalPath(
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
        private static void
            AddExternalDocumentationContractSummarySources(
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
                        CreateTerminalPath(
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
