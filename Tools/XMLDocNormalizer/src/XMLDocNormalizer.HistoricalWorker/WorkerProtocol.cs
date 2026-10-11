using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Versioned, bounded, Roslyn-free one-request process contract.</summary>
    internal static class WorkerProtocol
    {
        internal const int Version = 2;
        internal const int MaximumRequestCharacters = 65536;
        internal const int MaximumSourceCharacters = 32768;
        internal const int MaximumLogicalPathCharacters = 4096;
        internal const string ExactReferenceProfile = "validated-pe-images-v1";
        internal const int MaximumReferenceCount = 64;
        internal const int MaximumReferenceBytes = 32 * 1024 * 1024;
        internal const int MaximumReferenceRequestCharacters = 48 * 1024 * 1024;

        internal static int RequestLimit(WorkerAnalysisInput input) => input.Context?.ReferenceProfile == ExactReferenceProfile
            ? MaximumReferenceRequestCharacters : MaximumRequestCharacters;

        /// <summary>Bind ordered image hashes, identities/properties and target identity; exclude descriptive reference lookup names.</summary>
        internal static string? ReferenceWorldHash(WorkerAnalysisInput input)
        {
            if (input.Context?.ReferenceProfile != ExactReferenceProfile) { return null; }
            var context = input.Context with { References = input.Context.References!.Select(reference => reference with { Image = [], LogicalName = "" }).ToArray() };
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context, JsonOptions))));
        }

        internal static WorkerAnalysisProvenance Provenance(WorkerAnalysisInput input) => new(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Source!))), input.TypeMetadataName!, input.MethodName!,
            WorkerInputValidation.GetLogicalPath(input), "solution-transitive", input.Context?.ReferenceProfile ?? "net8-runtime-bounded-v1",
            "12", "enable", ReferenceWorldHash(input));

        /// <summary>Reject duplicate fields recursively rather than accepting last-property-wins input.</summary>
        internal static void RejectDuplicateProperties(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                HashSet<string> names = new(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name)) { throw new JsonException("Duplicate protocol field."); }
                    RejectDuplicateProperties(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            { foreach (var item in element.EnumerateArray()) { RejectDuplicateProperties(item); } }
        }

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

    /// <summary>One command; no file access, compiler objects or reference injection.</summary>
    internal sealed record WorkerRequest(int? ProtocolVersion, string? Operation, WorkerAnalysisInput? Payload = null);

    /// <summary>One source compilation and an exact, parameterless static method selector.</summary>
    internal sealed record WorkerAnalysisInput(string? Source, string? TypeMetadataName, string? MethodName,
        WorkerCompilationContext? Context = null, string? LogicalPath = null);

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
        string[]? PreprocessorSymbols = null,
        CanonicalAssemblyIdentity? TargetIdentity = null,
        string? TargetModuleName = null);

    /// <summary>Exact ordered compiler-option entries, like existing PDB option provenance; no polymorphic values.</summary>
    internal sealed record WorkerCompilerOption(string Key, string Value);

    /// <summary>Source bytes represented as text, a logical path and checksum provenance, not syntax.</summary>
    internal sealed record WorkerSourceDocument(string LogicalPath, string Text, string Sha256,
        byte[]? OriginalBytes = null, string? ChecksumAlgorithm = null);

    /// <summary>Metadata image and provenance; no caller-local paths or symbol instances.</summary>
    internal sealed record WorkerReferenceImage(string LogicalName, byte[] Image, string Sha256,
        CanonicalAssemblyIdentity? Identity = null, string? ModuleName = null, Guid? ModuleVersionId = null,
        string[]? Aliases = null, bool EmbedInteropTypes = false, string Kind = "assembly");

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
                && (input.LogicalPath == null || (!string.IsNullOrWhiteSpace(input.LogicalPath)
                    && input.LogicalPath.Length <= WorkerProtocol.MaximumLogicalPathCharacters
                    && !input.LogicalPath.Any(char.IsControl)))
                && (input.Context == null || (input.Context.AnalysisMode == "solution-transitive"
                    && input.Context.LanguageVersion == "12" && input.Context.NullableContext == "enable"
                    && input.Context.CompilerOptions == null && input.Context.PreprocessorSymbols == null
                    && input.Context.AdditionalSources == null
                    && input.Context.SupportingCompilations == null && input.Context.RootIdentity == null
                    && HasSupportedReferences(input.Context)));

        internal static bool HasSupportedReferences(WorkerCompilationContext context)
        {
            if (context.ReferenceProfile == "net8-runtime-bounded-v1")
            { return context.AssemblyName == "HistoricalWorkerInput" && context.References == null && context.TargetIdentity == null && context.TargetModuleName == null; }
            if (context.ReferenceProfile != WorkerProtocol.ExactReferenceProfile || !ValidIdentity(context.TargetIdentity)
                || context.TargetIdentity!.Name != context.AssemblyName || context.TargetIdentity.HasPublicKey
                || context.TargetIdentity.PublicKeyOrToken.Length != 0 || !ValidText(context.TargetModuleName, 4096)
                || context.References == null || context.References.Length > WorkerProtocol.MaximumReferenceCount)
            { return false; }
            long size = 0;
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in context.References)
            {
                if (reference == null || !ValidText(reference.LogicalName, 4096) || reference.Image == null || reference.Image.Length == 0
                    || (size += reference.Image.Length) > WorkerProtocol.MaximumReferenceBytes || reference.Sha256?.Length != 64
                    || reference.Sha256 != Convert.ToHexString(SHA256.HashData(reference.Image)) || !ValidIdentity(reference.Identity)
                    || !names.Add(reference.Identity!.Name) || !ValidText(reference.ModuleName, 4096)
                    || reference.ModuleVersionId == null || reference.ModuleVersionId == Guid.Empty || reference.Kind != "assembly"
                    || reference.EmbedInteropTypes || reference.Aliases == null || reference.Aliases.Length > 64
                    || reference.Aliases.Any(alias => !ValidText(alias, 256)) || reference.Aliases.Distinct(StringComparer.Ordinal).Count() != reference.Aliases.Length)
                { return false; }
            }
            return true;
        }

        private static bool ValidText(string? text, int bound) => !string.IsNullOrWhiteSpace(text) && text.Length <= bound && !text.Any(char.IsControl);
        private static bool ValidIdentity(CanonicalAssemblyIdentity? identity) => identity != null && ValidText(identity.Name, 256)
            && identity.VersionMajor is >= 0 and <= 65535 && identity.VersionMinor is >= 0 and <= 65535
            && identity.VersionBuild is >= 0 and <= 65535 && identity.VersionRevision is >= 0 and <= 65535
            && identity.CultureName.Length <= 256 && !identity.CultureName.Any(char.IsControl)
            && identity.PublicKeyOrToken.Length <= 8192 && identity.PublicKeyOrToken.Length % 2 == 0
            && identity.PublicKeyOrToken.All(Uri.IsHexDigit)
            && (identity.HasPublicKey ? identity.PublicKeyOrToken.Length > 0 : identity.PublicKeyOrToken.Length is 0 or 16)
            && Enum.IsDefined(identity.ContentType);

        /// <summary>Absent path retains the original explicit bounded policy; supplied data is never normalized or opened.</summary>
        internal static string GetLogicalPath(WorkerAnalysisInput input) => input.LogicalPath ?? "worker-input.cs";
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
        string LanguageVersion, string NullableContext, string? ReferenceWorldSha256 = null);

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
