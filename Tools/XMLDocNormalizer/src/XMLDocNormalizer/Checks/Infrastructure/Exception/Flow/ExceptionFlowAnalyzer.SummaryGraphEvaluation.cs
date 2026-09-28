using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides summary-graph analysis orchestration entry points.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Creates a reusable productive analysis session for transitive
        /// exception-flow analysis within the supplied semantic scope.
        /// </summary>
        /// <param name="semanticContext">
        /// The project-closure semantic context shared by the complete run.
        /// </param>
        /// <returns>A sequential reusable analysis session.</returns>
        internal static ExceptionFlowSummaryAnalysisSession
            CreateSummaryAnalysisSession(
                ExceptionFlowSemanticEnvironment semanticContext)
        {
            return new ExceptionFlowSummaryAnalysisSession(
                semanticContext);
        }

        /// <summary>
        /// Analyzes one member through a newly created productive
        /// summary-graph session.
        /// </summary>
        /// <param name="member">The source-level member to evaluate.</param>
        /// <param name="semanticContext">The semantic environment.</param>
        /// <returns>The escaping exception-flow result.</returns>
        public static ExceptionFlowAnalysisResult
            AnalyzeSolutionTransitivelyThrownExceptions(
                MemberDeclarationSyntax member,
                ExceptionFlowSemanticEnvironment semanticContext)
        {
            ExceptionFlowSummaryAnalysisSession session =
                CreateSummaryAnalysisSession(
                    semanticContext);

            return session.Analyze(
                member);
        }
    }
}
