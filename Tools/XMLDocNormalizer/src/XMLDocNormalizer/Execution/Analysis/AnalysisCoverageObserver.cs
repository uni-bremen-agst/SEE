using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Execution.Analysis;

/// <summary>Main-only projection; observes outcomes and cannot authorize findings or receipts.</summary>
internal static class AnalysisCoverageObserver
{
    /// <summary>Uses existing compiler diagnostics once per reporting compilation.</summary>
    internal static void ObserveCompilation(AnalysisCoverage coverage, Compilation compilation)
    {
        if (compilation.GetDiagnostics().Any(item => item.Severity == DiagnosticSeverity.Error && item.Id != "CS5001"))
            coverage.Incomplete("SourceBindingIncomplete");
    }

    /// <summary>Observes a native result without a second analysis or graph traversal.</summary>
    internal static void Observe(AnalysisCoverage coverage, ExceptionFlowAnalysisResult result, bool summary)
    {
        if (result.ProfileOutcome is { } profile)
        {
            if (!profile.Complete)
            {
                coverage.Unsupported("OutsideProfile");
                foreach (XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile.Reason? reason in profile.Reasons) ObserveReason(coverage, reason.Category);
            }
        }
        else if (summary) coverage.Incomplete("SummaryCertificationUnavailable");
        // Certificate Details also inhabit UncertainTargets; do not invent a
        // second coverage interpretation of the same certificate.
        if (result.ProfileOutcome == null && result.HasUncertainPaths)
            coverage.Incomplete("ExceptionFlowUnresolved");
        if (result.ThrownExceptions.Any(result.ArePathsTruncated)
            || result.ExternalDocumentationEvidenceExceptions.Any(result.AreExternalDocumentationEvidencePathsTruncated))
            coverage.Incomplete("ResultProjectionIncomplete");
    }

    private static void ObserveReason(AnalysisCoverage coverage, string category)
    {
        switch (category)
        {
            case "TargetOrAnalysisCoverageIncomplete": coverage.Incomplete("TargetCoverageIncomplete"); break;
            case "OwnerBindingFailure": coverage.Incomplete("TypeBindingIncomplete"); break;
            case "ScopeMapNotClosed": case "ScopeMapNotClosedOrNotTotal": coverage.Incomplete("ScopeCoverageIncomplete"); break;
            case "ProjectionNotCertifiedComplete": coverage.Incomplete("ResultProjectionIncomplete"); break;
        }
    }

    /// <summary>Projects already retained structured failure data, never a partial result.</summary>
    internal static AnalysisCoverage ExternalFailure(ExternalExceptionFindingAnalysisResult result, ExceptionAnalysisMode mode)
    {
        AnalysisCoverage coverage = new(); coverage.Begin("ExternalSelectedRoot", mode);
        ExternalExceptionFlowAnalysisResult? stage = result.Summary ?? result.Direct;
        HistoricalWorkerCallResult? call = stage.Dispatch?.Routing?.HistoricalAnalysis;
        if (call?.Identity != null && call.Failure is
            { Code: HistoricalWorkerClientFailureCode.StructuredFailure, WorkerFailure: { Code: WorkerFailureCode.UnsupportedContext } failure })
        {
            coverage.Unsupported("UnsupportedContext");
            if (failure.Details.Contains("Profile=A9-FiniteOrbit-v1", StringComparer.Ordinal))
            {
                coverage.Unsupported("OutsideProfile");
                foreach (string detail in failure.Details)
                {
                    int separator = detail.IndexOf(':');
                    if (separator > 0) ObserveReason(coverage, detail[..separator]);
                }
            }
        }
        // The pinned existing envelope uses AnalysisFailure for both technical
        // failures and these two explicit incomplete-result paths. Preserve the
        // distinction using its existing exact message, never arbitrary text heuristics.
        else if (call?.Identity != null && call.Failure is
            { Code: HistoricalWorkerClientFailureCode.StructuredFailure, WorkerFailure: { Code: WorkerFailureCode.AnalysisFailure } analysis }
            && analysis.Message is "Exception-flow analysis is incomplete." or "Independent Direct evidence is incomplete.")
            coverage.Incomplete("ResultCoverageIncomplete");
        else if (call?.Identity != null && call.Failure is
            { Code: HistoricalWorkerClientFailureCode.StructuredFailure, WorkerFailure: { Code: WorkerFailureCode.CompilationFailure, Message: "Historical compilation failed." } })
            coverage.Incomplete("SourceBindingIncomplete");
        else if (call?.Failure is { Code: HistoricalWorkerClientFailureCode.UnsupportedInput }
            || call?.Identity != null && call.Failure is
                { Code: HistoricalWorkerClientFailureCode.StructuredFailure, WorkerFailure: { Code: WorkerFailureCode.InvalidRequest } })
            coverage.Unsupported("ExternalInputUnsupported");
        else if (call?.Failure is { } client)
            coverage.Fail(client.Code is HistoricalWorkerClientFailureCode.IdentityMismatch
                or HistoricalWorkerClientFailureCode.MalformedResponse or HistoricalWorkerClientFailureCode.ProtocolMismatch
                or HistoricalWorkerClientFailureCode.IncompleteResult ? "WorkerValidationFailed" : "WorkerExecutionFailed");
        else if (stage.Dispatch?.Routing?.RoutingFailure != null) coverage.Fail("AnalysisExecutionFailed");
        else if (stage.PayloadProjection.Failure != null || stage.Dispatch?.Projection.Failure != null
            || stage.Dispatch?.Decision?.Failure != null) coverage.Unsupported("ExternalInputUnsupported");
        else coverage.Fail("FindingEvidenceValidationFailed");
        return coverage;
    }
}
