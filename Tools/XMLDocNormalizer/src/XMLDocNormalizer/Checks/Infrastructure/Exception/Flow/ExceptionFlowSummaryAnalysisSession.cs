using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Owns one reusable summary graph and its construction and evaluation
    /// components for a sequential exception-flow analysis run.
    /// </summary>
    /// <remarks>
    /// A session is scoped to one semantic environment and is intentionally
    /// not thread-safe. Graph state is reused between analyzed roots; each
    /// evaluation keeps its mutable traversal state local to the call.
    /// </remarks>
    internal sealed class ExceptionFlowSummaryAnalysisSession
    {
        /// <summary>
        /// The semantic environment shared by all analyzed roots.
        /// </summary>
        private readonly ExceptionFlowSemanticEnvironment semanticContext;

        /// <summary>
        /// The graph reused by every root in this session.
        /// </summary>
        private readonly ExceptionFlowSummaryGraph graph =
            new();

        /// <summary>
        /// The environment-bound graph construction component.
        /// </summary>
        private readonly ExceptionFlowSummaryGraphBuilder builder;

        /// <summary>
        /// The graph-only evaluation component.
        /// </summary>
        private readonly ExceptionFlowSummaryGraphEvaluator evaluator =
            new();

        /// <summary>
        /// Initializes a session for one project-closure semantic environment.
        /// </summary>
        /// <param name="semanticContext">The semantic environment.</param>
        internal ExceptionFlowSummaryAnalysisSession(
            ExceptionFlowSemanticEnvironment semanticContext)
        {
            this.semanticContext =
                semanticContext;

            builder =
                new ExceptionFlowSummaryGraphBuilder(
                    semanticContext);
        }

        /// <summary>
        /// Adds one root to the shared graph, completes newly discovered
        /// summaries, and evaluates the root's escaping exception flow.
        /// </summary>
        /// <param name="member">The source-level root member.</param>
        /// <returns>
        /// The expanded exception-flow result, or an empty result when the
        /// root symbol cannot be resolved.
        /// </returns>
        internal ExceptionFlowAnalysisResult Analyze(
            MemberDeclarationSyntax member)
        {
            if (!semanticContext.TryGetSemanticModel(
                    member.SyntaxTree,
                    out SemanticModel rootSemanticModel) ||
                rootSemanticModel == null ||
                !builder.TryRegisterSummaryGraphRoot(
                    member,
                    graph,
                    out ExceptionFlowCallableKey? rootKey) ||
                rootKey == null)
            {
                return new ExceptionFlowAnalysisResult();
            }

            builder.BuildPendingSummaryNodes(
                graph);

            return evaluator.Evaluate(
                graph,
                rootKey,
                rootSemanticModel.Compilation);
        }
    }
}
