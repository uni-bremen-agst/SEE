using System.Text.Json;
using System.Text.Json.Serialization;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Versioned, bounded, Roslyn-free one-request process contract.</summary>
    internal static class WorkerProtocol
    {
        internal const int Version = 1;
        internal const int MaximumRequestCharacters = 65536;
        internal const int MaximumSourceCharacters = 32768;

        /// <summary>Strict options shared by request parsing and canonical result serialization.</summary>
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 64,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
        };
    }

    /// <summary>One command; no file paths, compiler objects or reference injection.</summary>
    internal sealed record WorkerRequest(int? ProtocolVersion, string? Operation, WorkerAnalysisInput? Payload = null);

    /// <summary>One source compilation and an exact, parameterless static method selector.</summary>
    internal sealed record WorkerAnalysisInput(string? Source, string? TypeMetadataName, string? MethodName);

    /// <summary>A completed result or a structured failure, never an optimistic partial success.</summary>
    internal sealed record WorkerResponse(
        int ProtocolVersion,
        string Operation,
        bool Success,
        WorkerIdentity? Identity = null,
        CanonicalExceptionFlowAnalysisResult? Result = null,
        WorkerFailure? Failure = null);

    /// <summary>Stable failure categories, distinct from logs and stack traces.</summary>
    internal enum WorkerFailureCode
    {
        InvalidRequest,
        UnsupportedProtocolVersion,
        MalformedInput,
        CompilationFailure,
        AnalysisFailure,
        UnexpectedWorkerFailure
    }

    /// <summary>Structured fail-closed diagnostics without Roslyn diagnostic objects.</summary>
    internal sealed record WorkerFailure(WorkerFailureCode Code, string Message, string[] Details);

    /// <summary>Actual loaded assembly metadata; no local paths, PIDs or timing in the response.</summary>
    internal sealed record WorkerAssemblyIdentity(
        string Name, string AssemblyVersion, string InformationalVersion, string Mvid, string Sha256);

    /// <summary>Runtime proof for one isolated process and its bounded input profile.</summary>
    internal sealed record WorkerIdentity(
        string WorkerVersion,
        int ProtocolVersion,
        WorkerAssemblyIdentity Worker,
        WorkerAssemblyIdentity Analyzer,
        WorkerAssemblyIdentity[] LoadedRoslyn,
        bool RuntimeAwaitInformationAvailable,
        string ReferenceProfile);
}
