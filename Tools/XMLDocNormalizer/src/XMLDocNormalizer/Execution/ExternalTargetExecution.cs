using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Execution.Analysis;
using XMLDocNormalizer.Execution.Historical;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution
{
    /// <summary>A visible failed external operation, never an empty successful analysis.</summary>
    internal sealed class ExternalTargetExecutionException(string message) : Exception(message);

    /// <summary>Untrusted local file names and a bounded selector, not trusted execution DTOs.</summary>
    internal sealed record ExternalTargetJob([property: JsonRequired] int Version, string? TargetPe, string? PortablePdb,
        ExternalTargetSource[]? Sources, string[]? References, ExternalTargetRoot? Root,
        string? WorkerAssembly, string? DotnetHost = null, string? AnalysisMode = null,
        ExternalTargetProject[]? SupportingProjects = null);
    internal sealed record ExternalTargetProject(string? TargetPe, string? PortablePdb,
        ExternalTargetSource[]? Sources, string[]? References, ExternalTargetRoot? Root);
    internal sealed record ExternalTargetSource([property: JsonRequired] int DocumentOrdinal, string? Path);
    internal sealed record ExternalTargetRoot(string? TypeMetadataName, string? MethodName, [property: JsonRequired] int DocumentOrdinal);

    /// <summary>Input composition only; all authority and execution remain in existing P4/P5 and B7–B22 owners.</summary>
    internal static class ExternalTargetExecution
    {
        private static readonly JsonSerializerOptions JobJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 16
        };

        internal static async Task<ExternalExceptionFindingAnalysisResult> AnalyzeAsync(string jobPath, ExceptionAnalysisMode? assertedMode = null)
        {
            static ExternalTargetExecutionException Failure(string message) => new(message);
            try
            {
                string fullJobPath = Path.GetFullPath(jobPath);
                if (new FileInfo(fullJobPath).Length > 65536) { throw Failure("Input: job exceeds 64 KiB."); }
                string json = File.ReadAllText(fullJobPath, new UTF8Encoding(false, true));
                using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
                WorkerProtocol.RejectDuplicateProperties(document.RootElement);
                var job = JsonSerializer.Deserialize<ExternalTargetJob>(json, JobJson);
                if (job == null || job.Version != 1 || job.Sources == null || job.References == null || job.Root == null
                    || job.Sources.Length is < 1 or > WorkerProtocol.MaximumProjectSources
                    || job.References.Length is < 1 or > WorkerProtocol.MaximumReferenceCount
                    || job.Sources.Any(source => source == null || source.DocumentOrdinal < 0)
                    || job.Sources.Select(source => source.DocumentOrdinal).Distinct().Count() != job.Sources.Length
                    || !job.Sources.Any(source => source.DocumentOrdinal == job.Root.DocumentOrdinal)
                    || string.IsNullOrWhiteSpace(job.Root.TypeMetadataName) || string.IsNullOrWhiteSpace(job.Root.MethodName))
                { throw Failure("Input: version-1 complete local sources/references and one explicit root are required."); }
                // Additive job opt-in: old v1 jobs and their CLI admission remain unchanged.
                var mode = job.AnalysisMode switch
                {
                    null or "project-transitive-declared-exceptions" => ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions,
                    "direct" => ExceptionAnalysisMode.Direct,
                    "project-transitive" => ExceptionAnalysisMode.ProjectTransitive,
                    "solution-transitive" when job.SupportingProjects?.Length is 1 or 2 => ExceptionAnalysisMode.SolutionTransitive,
                    _ => throw Failure("Input: bounded external Findings support only direct, project-transitive or project-transitive-declared-exceptions; Solution is not admitted.")
                };
                if (mode != ExceptionAnalysisMode.SolutionTransitive && job.SupportingProjects != null)
                { throw Failure("Input: supporting projects require the explicit bounded Solution profile."); }
                if (assertedMode != null && assertedMode != mode) { throw Failure("Input: explicit CLI analysis mode conflicts with the external job analysisMode."); }
                string Local(string? path)
                {
                    if (string.IsNullOrWhiteSpace(path)) { throw Failure("Input: an explicit local file path is required."); }
                    return Path.GetFullPath(path, Path.GetDirectoryName(fullJobPath)!);
                }
                string worker = Local(job.WorkerAssembly);
                if (!File.Exists(worker)) { throw Failure("Worker: configured local assembly is missing."); }
                HistoricalWorkerPayloadProjectionInput Load(ExternalTargetProject project)
                {
                if (project.Sources == null || project.References == null || project.Root == null
                    || project.Sources.Length is < 1 or > WorkerProtocol.MaximumProjectSources
                    || project.References.Length is < 1 or > WorkerProtocol.MaximumReferenceCount
                    || project.Sources.Any(source => source == null || source.DocumentOrdinal < 0)
                    || project.Sources.Select(source => source.DocumentOrdinal).Distinct().Count() != project.Sources.Length
                    || !project.Sources.Any(source => source.DocumentOrdinal == project.Root.DocumentOrdinal)
                    || string.IsNullOrWhiteSpace(project.Root.TypeMetadataName) || string.IsNullOrWhiteSpace(project.Root.MethodName))
                { throw Failure("Input: each project requires complete local sources/references and one metadata selector."); }
                byte[] peBytes = File.ReadAllBytes(Local(project.TargetPe));
                string[] referencePaths = project.References.Select(Local).ToArray();
                if (referencePaths.Sum(path => new FileInfo(path).Length) > WorkerProtocol.MaximumReferenceBytes)
                { throw Failure("P5C: reference materials exceed the bounded Worker image budget."); }
                byte[][] referenceImages = referencePaths.Select(File.ReadAllBytes).ToArray();
                var rawReferences = referenceImages.Select(image => MetadataReference.CreateFromImage(image)).ToArray();
                var targetReference = MetadataReference.CreateFromImage(peBytes);
                // Metadata binding only, not active-compiler reconstruction of the submitted source.
                var metadataContext = CSharpCompilation.Create("ExternalTargetMetadata", references: rawReferences.Append(targetReference),
                    options: new(OutputKind.DynamicallyLinkedLibrary));
                if (metadataContext.GetAssemblyOrModuleSymbol(targetReference) is not IAssemblySymbol assembly
                    || !ExternalAssemblyReferenceDescriptorFactory.TryCreate(metadataContext, assembly, out var target))
                { throw Failure("P3: target is not a bound assembly PE."); }
                using var pe = new MemoryStream(peBytes, writable: false);
                if (!ExternalPeDebugDirectoryDescriptorFactory.TryCreate(target, pe, out var debug)
                    || !ExternalCompilationProvenanceDescriptorFactory.TryCreateFromFile(debug, Local(project.PortablePdb), out var provenance))
                { throw Failure("P4/P5A: missing, invalid or non-matching PE/PDB provenance."); }
                if (!ExternalCSharpCompilationConfigurationFactory.TryCreate(provenance, out var configuration))
                { throw Failure("P5G: unsupported compilation/parse/signing configuration."); }
                var sources = new List<ExternalCSharpSyntaxTree>();
                foreach (var source in project.Sources)
                {
                    if (source.DocumentOrdinal >= provenance.PortablePdb.Documents.Length)
                    { throw Failure("P5H: source document ordinal is outside the validated PDB."); }
                    var sourceDocument = provenance.PortablePdb.Documents[source.DocumentOrdinal];
                    if (!ValidatedExternalSourceMaterialFactory.TryCreateFromFile(sourceDocument, Local(source.Path), out var material)
                        || !ExternalCSharpSyntaxTreeFactory.TryCreate(material, configuration, out var tree))
                    { throw Failure("P5H/P5I: missing source or checksum/decoding failure."); }
                    sources.Add(tree);
                }
                if (!ExternalCSharpSyntaxTreeSetFactory.TryCreate(configuration, sources.Select(source => source.Document).ToArray(), sources, out var sourceSet))
                { throw Failure("P5J: source set is incomplete or inconsistent."); }
                var expectedReferences = provenance.MetadataReferences?.References;
                if (expectedReferences == null || expectedReferences.Value.Length != referenceImages.Length)
                { throw Failure("P5C: exact reference ordinal count is required."); }
                var materials = new List<ValidatedExternalMetadataReferenceMaterial>();
                for (int index = 0; index < referenceImages.Length; index++)
                {
                    using var image = new MemoryStream(referenceImages[index], writable: false);
                    if (!ValidatedExternalMetadataReferenceMaterialFactory.TryCreate(expectedReferences.Value[index], image, out var material))
                    { throw Failure("P5C: exact reference identity/checksum validation failed."); }
                    materials.Add(material);
                }
                if (!ExternalMetadataReferenceSetFactory.TryCreate(provenance, materials, out var referenceSet))
                { throw Failure("P5C/P5E: ordered exact reference materials are incomplete or invalid."); }
                var methods = assembly.GetTypeByMetadataName(project.Root.TypeMetadataName)?.GetMembers(project.Root.MethodName).OfType<IMethodSymbol>().ToArray();
                if (methods == null || methods.Length != 1) { throw Failure("Root: exactly one metadata method is required; ambiguous selectors are unsupported."); }
                var rootSource = sources[project.Sources.ToList().FindIndex(source => source.DocumentOrdinal == project.Root.DocumentOrdinal)];
                var policy = new WorkerDeclaredPolicyRequest(WorkerDeclaredPolicyContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions,
                    Documentation: new(WorkerDeclaredDocumentationContract.Profile, WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions));
                return new HistoricalWorkerPayloadProjectionInput(provenance, rootSource, methods[0],
                    mode, referenceSet, configuration, sourceSet,
                    DeclaredPolicy: mode == ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions ? policy : null,
                    Finding: mode is ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions or ExceptionAnalysisMode.SolutionTransitive ? null : new(WorkerDeclaredExecution.DirectEvidence,
                        new(WorkerFindingContract.Profile, mode == ExceptionAnalysisMode.Direct ? WorkerDeclaredPolicyMode.Direct : WorkerDeclaredPolicyMode.ProjectTransitive)));
                }
                var input = Load(new(job.TargetPe, job.PortablePdb, job.Sources, job.References, job.Root));
                if (mode == ExceptionAnalysisMode.SolutionTransitive)
                { input = input with { SupportingProjects = job.SupportingProjects!.Select(Load).ToImmutableArray() }; }
                var dispatch = new ExceptionFlowAnalysisDispatch(new(new HistoricalWorkerClient(job.DotnetHost ?? "dotnet", worker, TimeSpan.FromSeconds(60))));
                var result = await XmlDocExceptionSemanticDetector.FindExternalExceptionSmellsAsync(input, dispatch);
                if (!result.Succeeded)
                {
                    var failed = result.Summary ?? result.Direct;
                    if (failed.Dispatch?.Decision is { Succeeded: true, Selection: ExceptionFlowAnalyzerSelection.Current })
                    { throw Failure("Selection: validated provenance selects Current; this bounded external input requires the supported Historical compiler, without fallback."); }
                    throw Failure("B21/B22: " + (failed.PayloadProjection.Failure?.Message ?? failed.Dispatch?.Projection.Failure?.Message
                        ?? failed.Dispatch?.Decision?.Failure?.Message ?? failed.Dispatch?.Routing?.RoutingFailure?.Message
                        ?? failed.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message ?? result.Failure));
                }
                return result;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException
                or ArgumentException or BadImageFormatException or InvalidOperationException or DecoderFallbackException)
            { throw Failure("Local input/execution: " + exception.Message); }
        }
    }
}
