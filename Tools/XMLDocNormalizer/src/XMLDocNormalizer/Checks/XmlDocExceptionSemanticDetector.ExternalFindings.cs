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
        /// <summary>Main-only product status; never participates in Finding admission.</summary>
        internal AnalysisCoverage? Coverage { get; init; }

        internal bool Succeeded => Failure == null && Direct.Succeeded && (!SummaryRequested || Summary?.Succeeded == true);
    }

    internal static partial class XmlDocExceptionSemanticDetector
    {
        /// <summary>Bounded productive root Finding entry; uses the same B9 projection/selection/routing seam.</summary>
        internal static async Task<ExternalExceptionFindingAnalysisResult> FindExternalExceptionSmellsAsync(
            HistoricalWorkerPayloadProjectionInput? input, ExceptionFlowAnalysisDispatch dispatch,
            CancellationToken cancellationToken = default)
        {
            if (input?.Finding != null) { return await FindIndependentExternalExceptionSmellsAsync(input, dispatch, cancellationToken); }
            if (input?.AnalysisMode == ExceptionAnalysisMode.SolutionTransitive && input.DeclaredPolicy == null)
            { return await FindSolutionExternalExceptionSmellsAsync(input, dispatch, cancellationToken); }
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
            => RenderBoundedFindingEvidence(selected.Documentation!, selected.DirectResult, selected.FlowResult, selected.Declarations.Contains, mode);

        private static ImmutableArray<Finding> RenderBoundedFindingEvidence(DeclaredFindingDocumentationFacts document,
            CanonicalExceptionFlowAnalysisResult direct, CanonicalExceptionFlowAnalysisResult flow,
            Func<CanonicalTypeIdentity, bool> isLocal, ExceptionAnalysisMode mode)
        {
            var findings = new List<Finding>();
            if (!document.HasDocumentation) { return []; }
            var displays = document.TypeDisplays.ToDictionary(item => item.Type!, item => item.DisplayName!);
            var exceptions = direct.Entries.Concat(flow.Entries).Select(entry => entry.ExceptionType)
                .Concat(document.Tags.Where(tag => tag.IsException == true).Select(tag => tag.Type!)).ToHashSet();
            var tags = document.Tags.Select(tag => new FindingTag<CanonicalTypeIdentity>(tag.Syntax.RawCref,
                tag.Syntax.CrefStart != null, tag.Kind != WorkerCrefBindingKind.Unresolved, tag.Type, tag.Anchor, tag.Snippet)).ToArray();
            AddExceptionFindings(findings, tags, () => document.Member,
                direct.Entries.Where(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven).Select(entry => entry.ExceptionType),
                flow.Entries.Where(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.Proven).Select(entry => entry.ExceptionType),
                EqualityComparer<CanonicalTypeIdentity>.Default, exceptions.Contains, isLocal,
                type => displays[type], mode, document.HasInheritdoc,
                flow.Uncertainties.Length != 0,
                flow.Uncertainties.Select(item => item.DisplayText).ToHashSet(StringComparer.Ordinal),
                flow.Entries.Where(entry => entry.EvidenceKind == CanonicalExceptionFlowEvidenceKind.ExternalDocumentation).Select(entry => entry.ExceptionType));
            return findings.ToImmutableArray();
        }

        private static async Task<ExternalExceptionFindingAnalysisResult> FindIndependentExternalExceptionSmellsAsync(
            HistoricalWorkerPayloadProjectionInput input, ExceptionFlowAnalysisDispatch dispatch, CancellationToken cancellationToken)
        {
            var firstInput = input with { Finding = input.Finding! with { Execution = WorkerDeclaredExecution.DirectEvidence } };
            var direct = await AnalyzeFindingEvidenceAsync(firstInput, dispatch, WorkerDeclaredExecution.DirectEvidence, cancellationToken);
            bool Admit(ExternalExceptionFlowAnalysisResult run, WorkerDeclaredExecution purpose, out HistoricalFindingFacts? facts)
            {
                facts = null;
                if (!run.Succeeded) { return false; }
                if (!HistoricalWorkerResponseValidation.TryGetFindingFacts(run.Dispatch?.Routing?.HistoricalAnalysis, out facts)) { return false; }
                return facts!.Execution == purpose && facts.Mode == (input.AnalysisMode == ExceptionAnalysisMode.Direct
                    ? WorkerDeclaredPolicyMode.Direct : WorkerDeclaredPolicyMode.ProjectTransitive);
            }
            if (!Admit(direct, WorkerDeclaredExecution.DirectEvidence, out var first))
            { return new(direct, null, [], false, false, "Independent Finding admission requires original B21-attested mode/root/document/Direct evidence."); }
            if (!first!.Documentation.HasDocumentation) { return new(direct, null, [], true, false, null); }
            ExternalExceptionFlowAnalysisResult? summary = null;
            var selected = first;
            bool summaryRequired = RequiresSummary(input.AnalysisMode, false);
            if (summaryRequired)
            {
                summary = await AnalyzeFindingEvidenceAsync(input with { Finding = input.Finding! with { Execution = WorkerDeclaredExecution.SummaryEvidence } },
                    dispatch, WorkerDeclaredExecution.SummaryEvidence, cancellationToken);
                if (!Admit(summary, WorkerDeclaredExecution.SummaryEvidence, out selected)
                    || !first.DirectResult.Equals(selected!.DirectResult) || first.OwnershipSnapshot != selected.OwnershipSnapshot
                    || !first.Documentation.Method.Equals(selected.Documentation.Method) || first.Documentation.Member != selected.Documentation.Member
                    || first.Documentation.HasDocumentation != selected.Documentation.HasDocumentation
                    || first.Documentation.HasInheritdoc != selected.Documentation.HasInheritdoc || !first.Documentation.Tags.SequenceEqual(selected.Documentation.Tags))
                { return new(direct, summary, [], true, true, "Independent Direct and Summary executions must agree on the immutable root contract."); }
            }
            // These modes have no local-definition relevance policy. Do not obtain or transport an inventory.
            return new(direct, summary, RenderBoundedFindingEvidence(selected!.Documentation, selected.DirectResult, selected.FlowResult,
                _ => throw new InvalidOperationException("Direct/Project must not ask Declared relevance."), input.AnalysisMode), true, summaryRequired, null);
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

        private static async Task<ExternalExceptionFindingAnalysisResult> FindSolutionExternalExceptionSmellsAsync(
            HistoricalWorkerPayloadProjectionInput input, ExceptionFlowAnalysisDispatch dispatch, CancellationToken cancellationToken)
        {
            if (!HistoricalSolutionFindingOperation.TryPrepare(input, out var operation, out var projected))
            { return new(new(projected, null), null, [], false, false, "Solution Finding input is not admitted by the bounded graph contract."); }
            async Task<ExternalExceptionFlowAnalysisResult> Execute(HistoricalWorkerPayloadProjectionResult stage)
            {
                var value = stage.Value!;
                var request = new ExceptionFlowAnalysisDispatchRequest.PortablePdb(value.Provenance,
                    new ExceptionFlowAnalysisDispatchRequest.ExecutionPayload.Worker(value.Payload));
                return new(stage, await AnalyzeConfiguredExceptionFlowAsync(request, dispatch, cancellationToken));
            }
            var direct = await Execute(operation!.Direct);
            if (!direct.Succeeded || !operation.TryAdmitDirect(direct.Dispatch?.Routing?.HistoricalAnalysis, out var first))
            { return new(direct, null, [], false, false, "Solution Direct/document receipt requires its original validated execution."); }
            if (!first!.Documentation.HasDocumentation) { return new(direct, null, [], true, false, null); }
            var summary = await Execute(operation.Summary);
            if (!summary.Succeeded || !operation.TryComplete(direct.Dispatch?.Routing?.HistoricalAnalysis,
                summary.Dispatch?.Routing?.HistoricalAnalysis, out first, out var document))
            { return new(direct, summary, [], true, true, "Solution Summary requires an exact complete graph/root/scope/execution-bound receipt pair."); }
            return new(direct, summary, RenderBoundedFindingEvidence(document!, first!.DirectResult, summary.Result!,
                _ => throw new InvalidOperationException("Solution must not ask Declared relevance."), ExceptionAnalysisMode.SolutionTransitive), true, true, null);
        }
    }
}
