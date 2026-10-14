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
        internal const int MaximumProjectSources = 16;
        internal const int MaximumSolutionProjects = 4;
        internal const long MaximumSolutionReferenceBytes = 64L * 1024 * 1024;
        internal const int MaximumSolutionRequestCharacters = 96 * 1024 * 1024;
        internal const string ExactReferenceProfile = "validated-pe-images-v1";
        internal const int MaximumReferenceCount = 64;
        internal const int MaximumReferenceBytes = 32 * 1024 * 1024;
        internal const int MaximumReferenceRequestCharacters = 48 * 1024 * 1024;

        internal static int RequestLimit(WorkerAnalysisInput input) => input.Context?.Solution != null ? MaximumSolutionRequestCharacters : input.Context?.ReferenceProfile == ExactReferenceProfile
            ? MaximumReferenceRequestCharacters : MaximumRequestCharacters;

        /// <summary>Bind ordered image hashes, identities/properties and target identity; exclude descriptive reference lookup names.</summary>
        internal static string? ReferenceWorldHash(WorkerAnalysisInput input)
        {
            if (input.Context?.ReferenceProfile != ExactReferenceProfile) { return null; }
            var context = StripReferences(input.Context);
            if (context.Solution is { } graph)
            {
                context = context with
                {
                    Solution = graph with
                    {
                        Projects = graph.Projects!.Select(project => project with
                        { Input = project.Input! with { Context = StripReferences(project.Input!.Context!) } }).ToArray()
                    }
                };
            }
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(context, JsonOptions))));
        }

        private static WorkerCompilationContext StripReferences(WorkerCompilationContext context)
            => context with { References = context.References!.Select(reference => reference with { Image = [], LogicalName = "" }).ToArray() };

        internal static string SourceHash(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));

        internal static WorkerAnalysisProvenance Provenance(WorkerAnalysisInput input) => new(
            SourceHash(input.Source!), input.TypeMetadataName!, input.MethodName!,
            WorkerInputValidation.GetLogicalPath(input), input.Context?.AnalysisMode ?? "solution-transitive", input.Context?.ReferenceProfile ?? "net8-runtime-bounded-v1",
            input.Context?.LanguageVersion ?? "12", "enable", ReferenceWorldHash(input));

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
        string? TargetModuleName = null,
        WorkerParseContext? ParseContext = null,
        string? ParseProfile = null,
        WorkerProjectScope? Scope = null,
        WorkerSolutionGraph? Solution = null,
        WorkerDeclaredPolicyRequest? DeclaredPolicy = null);

    /// <summary>Explicit detector-policy facts opt-in, not an Analyzer mode or a default.</summary>
    internal enum WorkerDeclaredPolicyMode { Unspecified, ProjectTransitiveDeclaredExceptions }
    internal sealed record WorkerDeclaredPolicyRequest(string? Profile, WorkerDeclaredPolicyMode? Mode,
        CanonicalTypeIdentity[]? ExpectedDeclarations = null, WorkerDeclaredDocumentationRequest? Documentation = null);
    /// <summary>Owned source definitions, including unused types; no Finding relevance computed here.</summary>
    internal sealed record WorkerExceptionDeclaration(CanonicalTypeIdentity? Type, string[]? DeclaringSourcePaths);
    internal sealed record WorkerDeclaredPolicyEvidence(string? Profile, WorkerDeclaredPolicyMode? Mode,
        string[]? ReportingSourcePaths, int? DeclarationCount, WorkerExceptionDeclaration[]? Declarations,
        CanonicalExceptionFlowAnalysisResult? DirectResult);

    /// <summary>Selected-root local XML facts, never an external XML contract or a Finding payload.</summary>
    internal sealed record WorkerDeclaredDocumentationRequest(string? Profile, WorkerDeclaredPolicyMode? Mode,
        CanonicalCallableIdentity? Method = null, WorkerDeclaredDocumentationSyntax? Syntax = null);
    internal sealed record WorkerDeclaredDocumentationSyntax(string? LogicalPath, int? MemberStart, int? MemberLength,
        int? Anchor, bool? HasDocumentation, bool? HasInheritdoc, WorkerExceptionTagSyntax[]? Tags);
    internal sealed record WorkerExceptionTagSyntax(int? Start, int? Length, int? CrefStart, int? CrefLength, string? RawCref);
    internal enum WorkerCrefBindingKind { Unspecified, Unresolved, NamedType, OtherSymbol }
    internal sealed record WorkerExceptionTagBinding(WorkerExceptionTagSyntax? Syntax, WorkerCrefBindingKind? Kind,
        CanonicalTypeIdentity? Type, bool? IsException);
    internal sealed record WorkerTypeDisplayFact(CanonicalTypeIdentity? Type, string? DisplayName);
    internal sealed record WorkerDeclaredDocumentationEvidence(string? Profile, WorkerDeclaredPolicyMode? Mode,
        CanonicalCallableIdentity? Method, WorkerDeclaredDocumentationSyntax? Syntax,
        WorkerExceptionTagBinding[]? Bindings, WorkerTypeDisplayFact[]? TypeDisplays,
        string? DirectSha256 = null, string? SummarySha256 = null);

    internal static class WorkerDeclaredDocumentationContract
    {
        internal const string Profile = "single-root-declared-documentation-v1";
        internal const int MaximumTags = 128;
        internal static bool HasSupportedRequest(WorkerAnalysisInput input)
        {
            if (input.Context?.DeclaredPolicy?.Documentation is not { } request) { return true; }
            var syntax = request.Syntax;
            return request.Profile == Profile && request.Mode == WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions
                && request.Method != null && request.Method.ContainingType?.Assembly?.Equals(input.Context.TargetIdentity) == true
                && syntax != null && syntax.LogicalPath == input.LogicalPath && syntax.MemberStart is >= 0 && syntax.MemberLength is > 0
                && (long)syntax.MemberStart + syntax.MemberLength <= input.Source!.Length
                && syntax.Anchor >= syntax.MemberStart && syntax.Anchor < syntax.MemberStart + syntax.MemberLength
                && syntax.HasDocumentation != null && syntax.HasInheritdoc != null && syntax.Tags != null
                && syntax.Tags.Length <= MaximumTags && (syntax.HasDocumentation == true || syntax.Tags.Length == 0 && syntax.HasInheritdoc == false)
                && syntax.Tags.All(tag => tag != null && tag.Start is >= 0 && tag.Length is > 0
                    && (long)tag.Start + tag.Length <= syntax.MemberStart && tag.CrefStart.HasValue == tag.CrefLength.HasValue
                    && (tag.CrefStart == null ? tag.RawCref == null : tag.CrefStart >= tag.Start && tag.CrefLength > 0
                        && (long)tag.CrefStart + tag.CrefLength <= tag.Start + tag.Length));
        }
    }

    internal static class WorkerDeclaredPolicyContract
    {
        internal const string Profile = "single-project-declared-policy-v1";
        internal const int MaximumDeclarations = 128;
        internal static bool HasSupportedRequest(WorkerAnalysisInput input)
            => input.Context?.DeclaredPolicy is not { } request || request.Profile == Profile
                && request.Mode == WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions
                && input.Context.AnalysisMode == "project-transitive" && input.Context.Solution == null
                && input.Context.Scope != null && input.Context.ParseContext != null
                && input.Context.ReferenceProfile == WorkerProtocol.ExactReferenceProfile
                && request.ExpectedDeclarations != null && request.ExpectedDeclarations.Length <= MaximumDeclarations
                && request.ExpectedDeclarations.All(type => type != null && type.Assembly != null && type.Module != null
                    && type.Assembly.Equals(input.Context.TargetIdentity) && type.Module.Assembly.Equals(type.Assembly)
                    && type.Module.Name == input.Context.TargetModuleName && type.Module.Ordinal == 0 && type.Module.ModuleVersionId == null)
                && request.ExpectedDeclarations.Distinct().Count() == request.ExpectedDeclarations.Length
                && WorkerDeclaredDocumentationContract.HasSupportedRequest(input);
    }

    /// <summary>Original binary-project identity, not a path, simple assembly name or Roslyn ProjectId.</summary>
    internal sealed record WorkerProjectIdentity(CanonicalAssemblyIdentity? Assembly, string? ModuleName, Guid? ModuleVersionId);
    /// <summary>One complete B14 project and ordered exact-reference-bound outgoing dependencies.</summary>
    internal sealed record WorkerProjectNode(WorkerProjectIdentity? Identity, WorkerAnalysisInput? Input, WorkerProjectIdentity[]? Dependencies);
    /// <summary>Explicit complete reachable external source-project DAG and its exact target.</summary>
    internal sealed record WorkerSolutionGraph(WorkerProjectIdentity? Target, WorkerProjectNode[]? Projects);

    /// <summary>Complete ordered source membership in exactly one assembly-bound project, not a solution closure.</summary>
    internal sealed record WorkerProjectScope(string? Kind, string? AssemblyName, string[]? OrderedSourcePaths, string? RootLogicalPath);

    /// <summary>Explicit logical parse input, not serialized Roslyn options. No missing-field defaults.</summary>
    internal sealed record WorkerParseContext(string? LanguageVersion, string[]? PreprocessorSymbols,
        string? DocumentationMode, string? SourceKind);

    /// <summary>Exact ordered compiler-option entries, like existing PDB option provenance; no polymorphic values.</summary>
    internal sealed record WorkerCompilerOption(string Key, string Value);

    /// <summary>Source bytes represented as text, a logical path and checksum provenance, not syntax.</summary>
    internal sealed record WorkerSourceDocument(string LogicalPath, string Text, string Sha256,
        byte[]? OriginalBytes = null, string? ChecksumAlgorithm = null,
        WorkerParseContext? ParseContext = null, string? AssemblyName = null);

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
            => IsSupportedLocal(input) && HasSupportedSolution(input!);

        private static bool IsSupportedLocal(WorkerAnalysisInput? input)
            => input != null && !string.IsNullOrWhiteSpace(input.Source)
                && input.Source.Length <= WorkerProtocol.MaximumSourceCharacters
                && !string.IsNullOrWhiteSpace(input.TypeMetadataName) && input.TypeMetadataName.Length <= 256
                && !string.IsNullOrWhiteSpace(input.MethodName) && input.MethodName.Length <= 256
                && (input.LogicalPath == null || (!string.IsNullOrWhiteSpace(input.LogicalPath)
                    && input.LogicalPath.Length <= WorkerProtocol.MaximumLogicalPathCharacters
                    && !input.LogicalPath.Any(char.IsControl)))
                && (input.Context?.ParseContext == null || input.LogicalPath != null)
                && (input.Context == null || (HasSupportedScope(input)
                    && HasSupportedParseContext(input.Context) && input.Context.NullableContext == "enable"
                    && input.Context.CompilerOptions == null && input.Context.PreprocessorSymbols == null
                    && input.Context.SupportingCompilations == null && input.Context.RootIdentity == null
                    && HasSupportedReferences(input.Context) && WorkerDeclaredPolicyContract.HasSupportedRequest(input)));

        internal static bool ReferenceMatchesProject(WorkerReferenceImage reference, WorkerProjectIdentity project)
            => reference.Identity != null && reference.Identity.Equals(project.Assembly)
                && reference.ModuleName == project.ModuleName && reference.ModuleVersionId == project.ModuleVersionId;

        private static bool HasSupportedSolution(WorkerAnalysisInput input)
        {
            if (input.Context?.Solution is not { } graph) { return true; }
            if (input.Context.AnalysisMode != "solution-transitive" || input.Context.Scope == null || graph.Target == null
                || graph.Projects == null || graph.Projects.Length is < 2 or > MaximumProjects)
            { return false; }
            var nodes = new Dictionary<WorkerProjectIdentity, WorkerProjectNode>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            long images = input.Context.References!.Sum(reference => (long)reference.Image.Length);
            long text = 0;
            foreach (var project in graph.Projects)
            {
                if (project?.Identity is not { } identity || project.Input?.Context is not { } context || context.Solution != null
                    || context.Scope == null || context.AnalysisMode != "solution-transitive" || !IsSupportedLocal(project.Input)
                    || !Equals(identity.Assembly, context.TargetIdentity) || identity.ModuleName != context.TargetModuleName
                    || identity.ModuleVersionId == null || identity.ModuleVersionId == Guid.Empty || project.Dependencies == null
                    || project.Dependencies.Length >= graph.Projects.Length || !nodes.TryAdd(identity, project)
                    || !names.Add(identity.Assembly!.Name))
                { return false; }
                foreach (var path in context.Scope.OrderedSourcePaths!) { if (!paths.Add(path)) { return false; } }
                text += project.Input.Source!.Length + context.AdditionalSources!.Sum(source => (long)source.Text.Length);
                images += context.References!.Sum(reference => (long)reference.Image.Length);
            }
            if (text > 65536 || images > WorkerProtocol.MaximumSolutionReferenceBytes || !nodes.TryGetValue(graph.Target, out var target)
                || input.Source != target.Input!.Source || input.LogicalPath != target.Input.LogicalPath
                || input.TypeMetadataName != target.Input.TypeMetadataName || input.MethodName != target.Input.MethodName
                || WorkerProtocol.ReferenceWorldHash(input with { Context = input.Context with { Solution = null } }) != WorkerProtocol.ReferenceWorldHash(target.Input))
            { return false; }
            foreach (var node in nodes.Values)
            {
                var expected = new List<WorkerProjectIdentity>();
                foreach (var reference in node.Input!.Context!.References!)
                {
                    var candidates = nodes.Keys.Where(identity => string.Equals(identity.Assembly!.Name, reference.Identity!.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (candidates.Length == 0) { continue; }
                    if (candidates.Length != 1 || !ReferenceMatchesProject(reference, candidates[0])) { return false; }
                    expected.Add(candidates[0]);
                }
                if (!expected.SequenceEqual(node.Dependencies!)) { return false; }
            }
            var visiting = new HashSet<WorkerProjectIdentity>(); var complete = new HashSet<WorkerProjectIdentity>();
            bool Visit(WorkerProjectIdentity identity)
            {
                if (complete.Contains(identity)) { return true; }
                if (!nodes.TryGetValue(identity, out var node) || !visiting.Add(identity)) { return false; }
                foreach (var dependency in node.Dependencies!) { if (!Visit(dependency)) { return false; } }
                visiting.Remove(identity); complete.Add(identity); return true;
            }
            return Visit(graph.Target) && complete.Count == nodes.Count;
        }

        private const int MaximumProjects = WorkerProtocol.MaximumSolutionProjects;

        /// <summary>Explicit membership, completeness and budgets; absent scope retains the old single-source policy.</summary>
        internal static bool HasSupportedScope(WorkerAnalysisInput input)
        {
            var context = input.Context!;
            if (context.Scope == null)
            { return context.AnalysisMode == "solution-transitive" && context.AdditionalSources == null; }
            var scope = context.Scope;
            if (scope.Kind != "single-project-v1" || context.AnalysisMode is not ("direct" or "project-transitive" or "solution-transitive")
                || context.ReferenceProfile != WorkerProtocol.ExactReferenceProfile || !HasSupportedParseContext(context)
                || context.ParseContext == null || scope.AssemblyName != context.AssemblyName
                || scope.RootLogicalPath != input.LogicalPath || input.LogicalPath == null
                || scope.OrderedSourcePaths == null || scope.OrderedSourcePaths.Length is < 1 or > WorkerProtocol.MaximumProjectSources
                || scope.OrderedSourcePaths.Any(path => !ValidText(path, WorkerProtocol.MaximumLogicalPathCharacters))
                || scope.OrderedSourcePaths.Distinct(StringComparer.Ordinal).Count() != scope.OrderedSourcePaths.Length
                || !scope.OrderedSourcePaths.Contains(input.LogicalPath, StringComparer.Ordinal) || context.AdditionalSources == null
                || context.AdditionalSources.Length != scope.OrderedSourcePaths.Length - 1)
            { return false; }
            long size = input.Source!.Length;
            var expected = scope.OrderedSourcePaths.Where(path => path != input.LogicalPath).ToArray();
            for (int index = 0; index < context.AdditionalSources.Length; index++)
            {
                var source = context.AdditionalSources[index];
                if (source == null || source.LogicalPath != expected[index] || source.AssemblyName != scope.AssemblyName
                    || string.IsNullOrWhiteSpace(source.Text) || source.Text.Length > WorkerProtocol.MaximumSourceCharacters
                    || (size += source.Text.Length) > WorkerProtocol.MaximumSourceCharacters
                    || source.Sha256 != WorkerProtocol.SourceHash(source.Text) || source.OriginalBytes != null || source.ChecksumAlgorithm != null
                    || source.ParseContext == null || !HasSupportedParseContext(context with
                    { LanguageVersion = source.ParseContext.LanguageVersion!, ParseContext = source.ParseContext }))
                { return false; }
            }
            return true;
        }

        internal static bool HasSupportedParseContext(WorkerCompilationContext context)
        {
            if (context.ParseContext == null) { return context.ParseProfile == null && context.LanguageVersion == "12"; }
            var parse = context.ParseContext;
            return context.ParseProfile == "validated-parse-v1" && context.ReferenceProfile == WorkerProtocol.ExactReferenceProfile
                && context.LanguageVersion == parse.LanguageVersion && IsConcreteLanguageVersion(parse.LanguageVersion)
                && parse.DocumentationMode == "parse" && parse.SourceKind == "regular"
                && parse.PreprocessorSymbols != null && parse.PreprocessorSymbols.Length <= 128
                && parse.PreprocessorSymbols.All(symbol => symbol != null && symbol is not "true" and not "false" && symbol.Length is > 0 and <= 256
                    && (symbol[0] == '_' || char.IsLetter(symbol[0]))
                    && symbol.All(character => character == '_' || char.IsLetterOrDigit(character)))
                && parse.PreprocessorSymbols.Distinct(StringComparer.Ordinal).Count() == parse.PreprocessorSymbols.Length;
        }

        // Neutral syntax only. Actual capability is checked against the loaded compiler in the child.
        private static bool IsConcreteLanguageVersion(string? version)
            => version != null && version.Length is > 0 and <= 8
                && version[0] is >= '1' and <= '9'
                && version.All(character => character is >= '0' and <= '9' or '.')
                && (version.Count(character => character == '.') == 0
                    || (version.Length == 3 && version[0] == '7' && version[1] == '.' && version[2] is >= '1' and <= '3'));

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
        WorkerAnalysisProvenance? Provenance = null,
        WorkerDeclaredPolicyEvidence? DeclaredPolicy = null, WorkerDeclaredDocumentationEvidence? DeclaredDocumentation = null);

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
        UnsupportedContext,
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
