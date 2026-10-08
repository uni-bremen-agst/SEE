using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Historical
{
    /// <summary>Local transport failures are separate from structured Worker analysis failures.</summary>
    internal enum HistoricalWorkerClientFailureCode
    {
        UnsupportedInput, StartFailure, ProtocolMismatch, MalformedResponse, IdentityMismatch,
        WorkerCrash, NonZeroExit, StructuredFailure, IncompleteResult, TransportFailure,
        OutputLimit, Timeout, Cancelled
    }

    /// <summary>Never carries a successful or partial result on failure.</summary>
    internal sealed record HistoricalWorkerClientFailure(HistoricalWorkerClientFailureCode Code,
        string Message, WorkerFailure? WorkerFailure = null);

    /// <summary>Explicit local outcome; process diagnostics are not canonical finding data.</summary>
    internal sealed record HistoricalWorkerCallResult(
        CanonicalExceptionFlowAnalysisResult? Result,
        WorkerIdentity? Identity,
        HistoricalWorkerClientFailure? Failure,
        string Diagnostics = "",
        int? ProcessId = null,
        WorkerAnalysisProvenance? Provenance = null)
    {
        internal bool Success => Result != null && Identity != null && Provenance != null && Failure == null;
    }
}
