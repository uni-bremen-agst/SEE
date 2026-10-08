using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Explicit engine choice; the default value never silently selects Current.</summary>
    internal enum ExceptionFlowAnalyzerSelection
    {
        Unspecified = 0,
        Current = 1,
        Historical = 2
    }

    /// <summary>Main-local Current input; its existing sequential session and Roslyn objects never enter IPC.</summary>
    internal sealed record CurrentExceptionFlowAnalysisInput(
        MemberDeclarationSyntax Member, ExceptionFlowSummaryAnalysisSession Session);

    /// <summary>Main-local route request, not a process DTO; exactly one selected branch input is required.</summary>
    internal sealed record ExceptionFlowAnalysisRoutingRequest(
        ExceptionFlowAnalyzerSelection Selection,
        CurrentExceptionFlowAnalysisInput? Current = null,
        WorkerAnalysisInput? Historical = null);

    /// <summary>Routing failures are distinct from the unchanged B4 Worker failure contract.</summary>
    internal enum ExceptionFlowAnalysisRoutingFailureCode
    {
        InvalidSelection, InvalidInput, HistoricalUnavailable, CurrentExecutionFailure, Cancelled
    }

    /// <summary>A local routing failure, never an empty successful analysis.</summary>
    internal sealed record ExceptionFlowAnalysisRoutingFailure(ExceptionFlowAnalysisRoutingFailureCode Code, string Message);

    /// <summary>Main-local outcome preserving the native Current result or the complete original B4 outcome.</summary>
    /// <remarks>Execution success is not proof that Current analysis has no uncertainty; inspect the existing result.</remarks>
    internal sealed class ExceptionFlowAnalysisRoutingResult
    {
        private ExceptionFlowAnalysisRoutingResult(ExceptionFlowAnalyzerSelection selection,
            CanonicalExceptionFlowAnalysisResult? result, ExceptionFlowAnalysisResult? current,
            HistoricalWorkerCallResult? historical, ExceptionFlowAnalysisRoutingFailure? failure)
        {
            Selection = selection;
            Result = result;
            CurrentAnalysis = current;
            HistoricalAnalysis = historical;
            RoutingFailure = failure;
        }

        internal ExceptionFlowAnalyzerSelection Selection { get; }
        internal CanonicalExceptionFlowAnalysisResult? Result { get; }
        internal ExceptionFlowAnalysisResult? CurrentAnalysis { get; }
        internal HistoricalWorkerCallResult? HistoricalAnalysis { get; }
        internal ExceptionFlowAnalysisRoutingFailure? RoutingFailure { get; }
        internal HistoricalWorkerClientFailure? HistoricalFailure => HistoricalAnalysis?.Failure;
        internal bool Succeeded => RoutingFailure == null && Result != null && (Selection switch
        {
            ExceptionFlowAnalyzerSelection.Current => CurrentAnalysis != null && HistoricalAnalysis == null,
            ExceptionFlowAnalyzerSelection.Historical => HistoricalAnalysis?.Success == true && CurrentAnalysis == null,
            _ => false
        });

        internal static ExceptionFlowAnalysisRoutingResult FromCurrent(ExceptionFlowAnalysisResult native,
            CanonicalExceptionFlowAnalysisResult canonical)
            => new(ExceptionFlowAnalyzerSelection.Current, canonical, native, null, null);

        internal static ExceptionFlowAnalysisRoutingResult FromHistorical(HistoricalWorkerCallResult outcome)
            => new(ExceptionFlowAnalyzerSelection.Historical, outcome.Success ? outcome.Result : null, null, outcome, null);

        internal static ExceptionFlowAnalysisRoutingResult Failed(ExceptionFlowAnalyzerSelection selection,
            ExceptionFlowAnalysisRoutingFailureCode code, string message)
            => new(selection, null, null, null, new(code, message));
    }
}
