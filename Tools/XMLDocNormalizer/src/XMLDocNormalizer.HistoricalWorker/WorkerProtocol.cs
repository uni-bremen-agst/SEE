using System.Text.Json;
using System.Text.Json.Serialization;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Versioned, bounded, Roslyn-free one-request process contract.</summary>
    internal static class WorkerProtocol
    {
        internal const int Version = 2;
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
    internal sealed record WorkerAnalysisInput(string? Source, string? TypeMetadataName, string? MethodName,
        WorkerCompilationContext? Context = null);

    /// <summary>Explicit bounded profile, with typed future inputs that are rejected until supported.</summary>
    internal sealed record WorkerCompilationContext(
        string AnalysisMode = "solution-transitive",
        string ReferenceProfile = "net8-runtime-bounded-v1",
        string LanguageVersion = "12",
        string NullableContext = "enable",
        WorkerSourceDocument[]? AdditionalSources = null,
        WorkerReferenceImage[]? References = null,
        WorkerSupportingCompilation[]? SupportingCompilations = null,
        CanonicalCallableIdentity? RootIdentity = null,
        string AssemblyName = "HistoricalWorkerInput",
        WorkerCompilerOption[]? CompilerOptions = null,
        string[]? PreprocessorSymbols = null);

    /// <summary>Exact ordered compiler-option entries, like existing PDB option provenance; no polymorphic values.</summary>
    internal sealed record WorkerCompilerOption(string Key, string Value);

    /// <summary>Source bytes represented as text, a logical path and checksum provenance, not syntax.</summary>
    internal sealed record WorkerSourceDocument(string LogicalPath, string Text, string Sha256,
        byte[]? OriginalBytes = null, string? ChecksumAlgorithm = null);

    /// <summary>Metadata image and provenance; no caller-local paths or symbol instances.</summary>
    internal sealed record WorkerReferenceImage(string LogicalName, byte[] Image, string Sha256);

    /// <summary>Explicit supporting/project input envelope, reserved and currently rejected.</summary>
    internal sealed record WorkerSupportingCompilation(string Id, WorkerSourceDocument[] Sources,
        WorkerReferenceImage[] References, WorkerCompilationContext Options, string Provenance,
        string[]? DependencyIds = null);

    /// <summary>Shared validation of the supported profile; null means the original B3 implicit profile.</summary>
    internal static class WorkerInputValidation
    {
        internal static bool IsSupported(WorkerAnalysisInput? input)
            => input != null && !string.IsNullOrWhiteSpace(input.Source)
                && input.Source.Length <= WorkerProtocol.MaximumSourceCharacters
                && !string.IsNullOrWhiteSpace(input.TypeMetadataName) && input.TypeMetadataName.Length <= 256
                && !string.IsNullOrWhiteSpace(input.MethodName) && input.MethodName.Length <= 256
                && (input.Context == null || (input.Context.AnalysisMode == "solution-transitive"
                    && input.Context.ReferenceProfile == "net8-runtime-bounded-v1"
                    && input.Context.LanguageVersion == "12" && input.Context.NullableContext == "enable"
                    && input.Context.AssemblyName == "HistoricalWorkerInput"
                    && input.Context.CompilerOptions == null && input.Context.PreprocessorSymbols == null
                    && input.Context.AdditionalSources == null && input.Context.References == null
                    && input.Context.SupportingCompilations == null && input.Context.RootIdentity == null));
    }

    /// <summary>A completed result or a structured failure, never an optimistic partial success.</summary>
    internal sealed record WorkerResponse(
        int ProtocolVersion,
        string Operation,
        bool Success,
        WorkerIdentity? Identity = null,
        CanonicalExceptionFlowAnalysisResult? Result = null,
        WorkerFailure? Failure = null,
        WorkerAnalysisProvenance? Provenance = null);

    /// <summary>Completed analysis input binding; separate from existing canonical exception/path identities.</summary>
    internal sealed record WorkerAnalysisProvenance(string SourceSha256, string TypeMetadataName,
        string MethodName, string LogicalPath, string AnalysisMode, string ReferenceProfile,
        string LanguageVersion, string NullableContext);

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
