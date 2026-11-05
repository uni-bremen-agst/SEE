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
            => AnalyzeWithProgress(member, null);

        /// <summary>Observes only this query; the session retains no observer reference.</summary>
        internal ExceptionFlowAnalysisResult AnalyzeWithProgress(
            MemberDeclarationSyntax member, Action<ExceptionFlowProgressEvent>? progress)
        {
            ExceptionFlowProgressEvent.Send(progress, ExceptionFlowProgressKind.Admission);
            ExceptionFlowFiniteOrbitAdmission admission = ExceptionFlowFiniteOrbitAdmission.Discover(member, semanticContext, admissionFacts, progress);
            if (!admission.Outcome.Complete) return Outside(admission.Outcome, admission.DiagnoseGroundFacts(),
                admission.DiagnoseBoundedLowerWitnesses());
            bool completed = false;
            try
            {
            graph.BeginProduction(admission.Seal, semanticContext);
            ExceptionFlowProgressEvent.Send(progress, ExceptionFlowProgressKind.Graph);
            int registeredBefore = progress is null ? 0 : graph.Count;
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

            ReportDiscoveredContexts(graph, registeredBefore, progress);
            BuildPendingSummaryNodesWithProgress(graph, progress);

            ExceptionFlowProgressEvent.Send(progress, ExceptionFlowProgressKind.Evaluation, symbol: rootKey.Symbol);
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
            ExceptionFlowProfileDiagnostics? diagnostics = null,
            ExceptionFlowBoundedLowerWitness.Witness[]? qualifiedLower = null)
        {
            ExceptionFlowAnalysisResult result = new() { ProfileOutcome = certificate, ProfileDiagnostics = diagnostics };
            result.UncertainTargets.UnionWith(certificate.Details);
            if (qualifiedLower is { Length: > 0 } && diagnostics != null)
            {
                foreach (var witness in qualifiedLower) result.AddExceptionPath(witness.Source.ExceptionType, witness.Source.LocalPath);
                result.ProfileDiagnostics = diagnostics with
                {
                    ProvenLowerWitnesses = diagnostics.ProvenLowerWitnesses.Concat(qualifiedLower.Select(witness => witness.Source.ExceptionType))
                        .Distinct<Microsoft.CodeAnalysis.INamedTypeSymbol>(SymbolEqualityComparer.Default).ToArray(),
                    QualifiedLowerWitnesses = qualifiedLower,
                    Proof = diagnostics.Proof.Concat(qualifiedLower.Select(witness =>
                        "Qualified finite normal-initialization/default-return/argument/escape composition: "
                        + witness.Schema.ReceiverField.ToDisplayString() + " -> " + witness.Schema.Getter.ToDisplayString()
                        + " -> default(" + witness.ClosedReturn.ToDisplayString() + ") -> "
                        + witness.Schema.Validator.ToDisplayString() + "; coverage remains incomplete.")).ToArray()
                };
            }
            return result;
        }
    }

    internal enum ExceptionFlowProgressKind
    {
        RootStarted, RootCompleted, RootFailed, Admission, AdmissionSealing,
        DefinitionDiscovered, DefinitionStarted, DefinitionProcessed, Graph, ContextDiscovered, ContextStarted, ContextProcessed, Evaluation
    }

    /// <summary>Diagnostic notifications only; no profile, cache, path or protocol fields.</summary>
    internal readonly record struct ExceptionFlowProgressEvent(ExceptionFlowProgressKind Kind, int Count = 1,
        ISymbol? Symbol = null, MemberDeclarationSyntax? Member = null)
    {
        internal static void Send(Action<ExceptionFlowProgressEvent>? observer, ExceptionFlowProgressKind kind,
            int count = 1, ISymbol? symbol = null, MemberDeclarationSyntax? member = null)
        {
            if (observer is null) return;
            // A diagnostic consumer cannot turn an analyzer outcome into a technical failure.
            try { observer(new(kind, count, symbol, member)); } catch (System.Exception) { }
        }
    }
}
