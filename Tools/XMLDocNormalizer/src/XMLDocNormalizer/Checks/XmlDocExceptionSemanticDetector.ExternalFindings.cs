using System.Collections.Immutable;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks
{
    internal sealed record ExternalExceptionFindingAnalysisResult(ExternalExceptionFlowAnalysisResult Direct,
        ExternalExceptionFlowAnalysisResult? Summary, ImmutableArray<Finding> Findings,
        bool GuardReached, bool SummaryRequested, string? Failure)
    {
        internal bool Succeeded => Failure == null && Direct.Succeeded && (!SummaryRequested || Summary?.Succeeded == true);
    }

    internal static partial class XmlDocExceptionSemanticDetector
    {
        /// <summary>Bounded productive root Finding entry; uses the same B9 projection/selection/routing seam.</summary>
        internal static async Task<ExternalExceptionFindingAnalysisResult> FindExternalExceptionSmellsAsync(
            HistoricalWorkerPayloadProjectionInput? input, ExceptionFlowAnalysisDispatch dispatch,
            CancellationToken cancellationToken = default)
        {
            var direct = await AnalyzeFindingEvidenceAsync(input, dispatch, WorkerDeclaredExecution.DirectEvidence, cancellationToken);
            ExternalExceptionFindingAnalysisResult Fail(string message, bool guarded = false, ExternalExceptionFlowAnalysisResult? summary = null)
                => new(direct, summary, [], guarded, summary != null, message);
            if (!TryAdmitFindingEvidence(direct.Dispatch?.Routing?.HistoricalAnalysis, WorkerDeclaredExecution.DirectEvidence, out var first))
            { return Fail("Declared Finding admission requires the original B21-attested complete Direct/document/declaration execution."); }
            var document = first!.Documentation!;
            // The same skip/summary guard as Current, AFTER the B21 receipt, not a Worker decision.
            if (!document.HasDocumentation) { return new(direct, null, [], true, false, null); }
            bool summaryRequired = RequiresSummary(input!.AnalysisMode, first.Declarations.Count != 0);
            ExternalExceptionFlowAnalysisResult? summary = null;
            var selected = first;
            if (summaryRequired)
            {
                summary = await AnalyzeFindingEvidenceAsync(input, dispatch, WorkerDeclaredExecution.SummaryEvidence, cancellationToken);
                if (!TryAdmitFindingEvidence(summary.Dispatch?.Routing?.HistoricalAnalysis, WorkerDeclaredExecution.SummaryEvidence, out selected))
                { return Fail("Selected Summary must complete its own original B21-attested execution before Finding policy.", true, summary); }
                if (!first.DirectResult.Equals(selected!.DirectResult) || first.OwnershipSnapshot != selected.OwnershipSnapshot
                    || !first.Documentation!.Method.Equals(selected.Documentation!.Method)
                    || first.Documentation.Member != selected.Documentation.Member
                    || first.Documentation.HasDocumentation != selected.Documentation.HasDocumentation
                    || first.Documentation.HasInheritdoc != selected.Documentation.HasInheritdoc
                    || !first.Documentation.Tags.SequenceEqual(selected.Documentation.Tags))
                { return Fail("Direct and selected Summary executions disagree on the same immutable root contract.", true, summary); }
                document = selected.Documentation;
            }
            var findings = RenderFindingEvidence(selected!, input.AnalysisMode);
            return new(direct, summary, findings, true, summaryRequired, null);
        }

        // One bounded adapter into the shared policy. It accepts the validation-owned
        // immutable receipt, never caller-authored worker DTOs or native rebound source.
        private static ImmutableArray<Finding> RenderFindingEvidence(DeclaredPolicyFacts selected, ExceptionAnalysisMode mode)
        {
            var document = selected.Documentation!;
            var findings = new List<Finding>();
            if (!document.HasDocumentation) { return []; }
            var displays = document.TypeDisplays.ToDictionary(item => item.Type!, item => item.DisplayName!);
            var exceptions = selected!.Direct.Concat(selected.Summary).Concat(selected.Documented).ToHashSet();
            var tags = document.Tags.Select(tag => new FindingTag<CanonicalTypeIdentity>(tag.Syntax.RawCref,
                tag.Syntax.CrefStart != null, tag.Kind != WorkerCrefBindingKind.Unresolved, tag.Type, tag.Anchor, tag.Snippet)).ToArray();
            AddExceptionFindings(findings, tags, () => document.Member,
                selected.DirectResult.Entries.Where(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven).Select(entry => entry.ExceptionType),
                selected.FlowResult.Entries.Where(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven).Select(entry => entry.ExceptionType),
                EqualityComparer<CanonicalTypeIdentity>.Default, exceptions.Contains, selected.Declarations.Contains,
                type => displays[type], mode, document.HasInheritdoc,
                selected.FlowResult.Uncertainties.Length != 0,
                selected.FlowResult.Uncertainties.Select(item => item.DisplayText).ToHashSet(StringComparer.Ordinal),
                selected.FlowResult.Entries.Where(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.ExternalDocumentation).Select(entry => entry.ExceptionType));
            return findings.ToImmutableArray();
        }

        // Admission deliberately precedes every guard/render invocation. A cloned call or
        // fabricated contract cannot use this seam, even when its fields look identical.
        internal static bool TryAdmitFindingEvidence(HistoricalWorkerCallResult? call, WorkerDeclaredExecution execution,
            out DeclaredPolicyFacts? facts)
        {
            facts = null;
            if (!HistoricalWorkerResponseValidation.TryGetDeclaredPolicyFacts(call, out var original)
                || original!.DocumentationExecution == null || original.Documentation == null || original.Execution != execution
                || execution is not (WorkerDeclaredExecution.DirectEvidence or WorkerDeclaredExecution.SummaryEvidence)) { return false; }
            facts = original; return true;
        }

        private static async Task<ExternalExceptionFlowAnalysisResult> AnalyzeFindingEvidenceAsync(
            HistoricalWorkerPayloadProjectionInput? input, ExceptionFlowAnalysisDispatch dispatch,
            WorkerDeclaredExecution execution, CancellationToken cancellationToken)
        {
            var projected = HistoricalWorkerPayloadProjection.ProjectForFindings(input, execution);
            if (!projected.Succeeded) { return new(projected, null); }
            var request = new ExceptionFlowAnalysisDispatchRequest.PortablePdb(projected.Value!.Provenance,
                new ExceptionFlowAnalysisDispatchRequest.ExecutionPayload.Worker(projected.Value.Payload));
            return new(projected, await AnalyzeConfiguredExceptionFlowAsync(request, dispatch, cancellationToken));
        }
    }
}
