using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Original staged outcomes; unreached stages stay absent and no failed stage exposes an analysis result.</summary>
    internal sealed record ExceptionFlowAnalysisDispatchResult(
        ExceptionFlowAnalyzerSelectionProjectionResult Projection,
        ExceptionFlowAnalyzerSelectionDecision? Decision,
        ExceptionFlowAnalysisRoutingResult? Routing)
    {
        internal bool Succeeded => Projection.Succeeded && Decision?.Succeeded == true && Routing?.Succeeded == true;
        internal CanonicalExceptionFlowAnalysisResult? Result => Succeeded ? Routing!.Result : null;
    }

    /// <summary>Main-local orchestration only: existing B7 projection, B6 decision and B5 execution in that order.</summary>
    internal sealed class ExceptionFlowAnalysisDispatch(ExceptionFlowAnalysisRouter router)
    {
        /// <summary>Automatically dispatches the supplied typed input without discovery, engine rules or execution duplication.</summary>
        internal async Task<ExceptionFlowAnalysisDispatchResult> AnalyzeAsync(
            ExceptionFlowAnalysisDispatchRequest? request, CancellationToken cancellationToken = default)
        {
            ExceptionFlowAnalyzerSelectionProjectionResult projection = ExceptionFlowAnalyzerSelectionContext.Projector.Project(request?.ProjectionInput);
            if (!projection.Succeeded) { return new(projection, null, null); }

            ExceptionFlowAnalyzerSelectionDecision decision = ExceptionFlowAnalyzerSelectionPolicy.Select(projection.Context);
            if (!decision.Succeeded) { return new(projection, decision, null); }

            return new(projection, decision, await router.AnalyzeAsync(request!.Route(decision.Selection), cancellationToken));
        }
    }
}
