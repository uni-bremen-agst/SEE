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
    internal sealed partial class ExceptionFlowSummaryAnalysisSession
    {
        /// <summary>
        /// The semantic environment shared by all analyzed roots.
        /// </summary>
        private readonly ExceptionFlowSemanticEnvironment semanticContext;
        private readonly ExceptionFlowFiniteOrbitAdmission.FactStore admissionFacts;

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
            ExceptionFlowSemanticEnvironment semanticContext, Action<ISymbol>? inspectingAdmission = null)
        {
            this.semanticContext =
                semanticContext;
            admissionFacts = new(semanticContext, inspectingAdmission);

            builder =
                new ExceptionFlowSummaryGraphBuilder(
                    semanticContext);
        }

        /// <summary>Shares the environment's compiler status with reporting coverage.</summary>
        /// <param name="compilation">The exact reporting compilation.</param>
        /// <returns>Whether a compiler error other than CS5001 is present.</returns>
        internal bool HasBlockingCompilationErrors(Compilation compilation)
            => semanticContext.HasBlockingCompilationErrors(compilation);

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
            ExceptionFlowFiniteOrbitAdmission admission = ExceptionFlowFiniteOrbitAdmission.Discover(member, semanticContext, admissionFacts);
            if (!admission.Outcome.Complete) return Outside(admission.Outcome, admission.DiagnoseGroundFacts());
            bool completed = false;
            try
            {
            graph.BeginProduction(admission.Seal, semanticContext);
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
                return Outside(WithReason(admission.Outcome, new("OwnerBindingFailure", "Admitted root could not be registered.")));
            }

            BuildPendingSummaryNodes(
                graph);

            ExceptionFlowAnalysisResult result = evaluator.Evaluate(
                graph,
                rootKey,
                rootSemanticModel.Compilation);
            if (result.HasUncertainPaths)
                return Outside(WithReason(admission.Outcome, new("TargetOrAnalysisCoverageIncomplete", "Concrete evaluation has incomplete coverage.")));
            var resolver = new Canonical.RoslynCanonicalIdentityResolver(rootSemanticModel.Compilation);
            foreach (var type in result.ThrownExceptions.Concat(result.ExternalDocumentationEvidenceExceptions))
                if (resolver.ResolveType(Canonical.RoslynCanonicalIdentityFactory.CreateTypeIdentity(type)) is not Microsoft.CodeAnalysis.INamedTypeSymbol)
                    return Outside(WithReason(admission.Outcome, new("OwnerBindingFailure", "Required exact result projection has no unique destination binding: "
                        + Canonical.CanonicalIdentityKeyWriter.Write(Canonical.RoslynCanonicalIdentityFactory.CreateTypeIdentity(type)))));
            // Existing path limits are projection policy, not a termination bound.
            if (result.ThrownExceptions.Any(result.ArePathsTruncated)
                || result.ExternalDocumentationEvidenceExceptions.Any(result.AreExternalDocumentationEvidencePathsTruncated))
            {
                // Retain explicitly uncertified diagnostic paths; consumers must
                // use the typed outcome, never promote these to exact findings.
                result.ProfileOutcome = WithReason(admission.Outcome, new("ProjectionNotCertifiedComplete", "Required flow paths are truncated."));
                return result;
            }
            result.ProfileOutcome = admission.Outcome;
            completed = true;
            return result;
            }
            catch (ExceptionFlowProfileViolation violation)
            {
                return Outside(WithReason(admission.Outcome, violation.Reason));
            }
            finally { graph.EndProduction(completed); }
        }

        private static ExceptionFlowFiniteOrbitProfile.Certificate WithReason(
            ExceptionFlowFiniteOrbitProfile.Certificate certificate, ExceptionFlowFiniteOrbitProfile.Reason reason)
            => certificate with { Complete = false, Reasons = certificate.Reasons.Append(reason).Distinct()
                .OrderBy(item => item.Category, StringComparer.Ordinal).ThenBy(item => item.Witness, StringComparer.Ordinal).ToArray() };

        private static ExceptionFlowAnalysisResult Outside(ExceptionFlowFiniteOrbitProfile.Certificate certificate,
            ExceptionFlowProfileDiagnostics? diagnostics = null)
        {
            ExceptionFlowAnalysisResult result = new() { ProfileOutcome = certificate, ProfileDiagnostics = diagnostics };
            result.UncertainTargets.UnionWith(certificate.Details);
            return result;
        }
    }
}
