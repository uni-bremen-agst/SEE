using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Explicit Main composition owner for existing Current sessions and the unchanged Historical client.</summary>
    internal sealed class ExceptionFlowAnalysisRouter
    {
        private readonly HistoricalWorkerClient? historicalClient;

        /// <summary>Configures the optional existing Historical endpoint; Current never needs a Worker deployment.</summary>
        internal ExceptionFlowAnalysisRouter(HistoricalWorkerClient? historicalClient = null)
        {
            this.historicalClient = historicalClient;
        }

        /// <summary>Routes only the explicit choice, without version detection, fallback or a new Analyzer lifetime.</summary>
        /// <remarks>Current retains sequential caller-owned session execution; cancellation is checked before its synchronous call.</remarks>
        internal async Task<ExceptionFlowAnalysisRoutingResult> AnalyzeAsync(
            ExceptionFlowAnalysisRoutingRequest? request, CancellationToken cancellationToken = default)
        {
            ExceptionFlowAnalyzerSelection selection = request?.Selection ?? ExceptionFlowAnalyzerSelection.Unspecified;
            ExceptionFlowAnalysisRoutingResult Fail(ExceptionFlowAnalysisRoutingFailureCode code, string message)
                => ExceptionFlowAnalysisRoutingResult.Failed(selection, code, message);

            if (selection != ExceptionFlowAnalyzerSelection.Current && selection != ExceptionFlowAnalyzerSelection.Historical)
            {
                return Fail(ExceptionFlowAnalysisRoutingFailureCode.InvalidSelection, "An explicit supported Analyzer selection is required.");
            }
            if (selection == ExceptionFlowAnalyzerSelection.Current)
            {
                if (request!.Historical != null || request.Current?.Member == null || request.Current.Session == null)
                {
                    return Fail(ExceptionFlowAnalysisRoutingFailureCode.InvalidInput, "Current requires only a member and its existing session.");
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    return Fail(ExceptionFlowAnalysisRoutingFailureCode.Cancelled, "Current request was cancelled before execution.");
                }
                try
                {
                    // Same existing in-process entry as the detector, preserving caller-owned graph/model/cache lifetime.
                    ExceptionFlowAnalysisResult native = request.Current.Session.Analyze(request.Current.Member);
                    return ExceptionFlowAnalysisRoutingResult.FromCurrent(native,
                        RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(native));
                }
                catch (Exception exception)
                {
                    return Fail(ExceptionFlowAnalysisRoutingFailureCode.CurrentExecutionFailure,
                        "Current analysis could not complete: " + exception.Message);
                }
            }

            if (request!.Current != null || request.Historical == null)
            {
                return Fail(ExceptionFlowAnalysisRoutingFailureCode.InvalidInput, "Historical requires only the existing neutral Worker input.");
            }
            if (historicalClient == null)
            {
                return Fail(ExceptionFlowAnalysisRoutingFailureCode.HistoricalUnavailable, "No explicit Historical endpoint is configured.");
            }
            return ExceptionFlowAnalysisRoutingResult.FromHistorical(
                await historicalClient.AnalyzeAsync(request.Historical, cancellationToken));
        }
    }
}
