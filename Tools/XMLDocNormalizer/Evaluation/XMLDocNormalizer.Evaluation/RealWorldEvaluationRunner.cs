using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Evaluates prepared real package artifacts through the existing P3-P7 gates.
    /// </summary>
    internal sealed class RealWorldEvaluationRunner
    {
        private const string ConsumerPath = "/evaluation/Consumer.cs";
        private readonly EvaluationManifest manifest;
        private readonly EvaluationArguments options;
        private readonly ImmutableArray<MetadataReference> platformReferences;

        /// <summary>
        /// Initializes one local, opt-in evaluation run.
        /// </summary>
        /// <param name="manifest">The validated pinned manifest.</param>
        /// <param name="options">The validated run arguments.</param>
        public RealWorldEvaluationRunner(
            EvaluationManifest manifest,
            EvaluationArguments options)
        {
            this.manifest = manifest;
            this.options = options;
            platformReferences = CreatePlatformReferences();
        }

        /// <summary>
        /// Evaluates every candidate in stable order and creates aggregate counts.
        /// </summary>
        /// <returns>The complete report.</returns>
        public EvaluationReport Run()
        {
            List<EvaluationCandidateResult> results = manifest.Candidates
                .OrderBy(static candidate => candidate.Id, StringComparer.Ordinal)
                .Select(EvaluateCandidate)
                .ToList();
            EvaluationSummary summary = new()
            {
                CandidatesEvaluated = results.Count,
                FullReconstructionSuccesses = results.Count(static result => result.ReconstructionSucceeded),
                ExpectedFailClosedCases = results.Count(static result => !result.ReconstructionSucceeded && result.ExpectedOutcomeObserved),
                UnexpectedFailures = results.Count(static result => !result.ExpectedOutcomeObserved),
                PotentialBugs = results.Count(static result => result.PotentialBug),
                EmbeddedSourceCandidates = results.Count(static result => result.SourceOrigins.ContainsKey(nameof(ExternalSourceMaterialOrigin.Embedded))),
                SourceLinkCandidates = results.Count(static result => result.SourceOrigins.ContainsKey(nameof(ExternalSourceMaterialOrigin.SourceLink))),
                FindingDifferenceCandidates = results.Count(static result => result.AddedFindings.Count != 0 || result.RemovedFindings.Count != 0)
            };

            return new EvaluationReport
            {
                GeneratedAtUtc = DateTime.UtcNow,
                GitCommit = ReadGitCommit(),
                OperatingSystem = RuntimeInformation.OSDescription,
                Runtime = RuntimeInformation.FrameworkDescription,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                SourceLinkEnabled = options.SourceLinkEnabled,
                SourceReconstructionPolicy = options.SourceReconstructionPolicy.ToString(),
                ReferenceAcquisitionPolicy = options.ReferenceAcquisitionPolicy.ToString(),
                Summary = summary,
                Candidates = results
            };
        }

        private EvaluationCandidateResult EvaluateCandidate(EvaluationCandidate candidate)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            EvaluationCandidateResult result = CreateResult(candidate);

            try
            {
                string assemblyPath = Resolve(candidate.AssemblyPath);
                if (!File.Exists(assemblyPath))
                {
                    return Fail(result, candidate, "BinaryLocated", EvaluationFailureCategory.MissingArtifact, "Candidate assembly is absent.", stopwatch);
                }

                result.AssemblySha256 = HashFile(assemblyPath);
                AddStage(result, "BinaryLocated", true, "Pinned package assembly found.");
                ConsumerFixture? fixture = CreateConsumerFixture(candidate, assemblyPath);
                if (fixture == null)
                {
                    return Fail(result, candidate, "BinaryValidated", EvaluationFailureCategory.ProvenanceMismatch, "Roslyn could not bind the candidate assembly.", stopwatch);
                }

                AddStage(result, "BinaryValidated", true, fixture.Target.AssemblyIdentity.ToString());
                result.BaselineFindings = Analyze(fixture, supportingSource: null);

                if (!ExternalPeDebugDirectoryDescriptorFactory.TryCreateFromFile(
                        fixture.Target,
                        assemblyPath,
                        out ExternalPeDebugDirectoryDescriptor debugDirectory))
                {
                    return Fail(result, candidate, "PdbValidated", EvaluationFailureCategory.ProvenanceMismatch, "PE debug provenance validation failed.", stopwatch);
                }

                ExternalCompilationProvenanceDescriptor provenance;
                if (candidate.PdbPath != null)
                {
                    string pdbPath = Resolve(candidate.PdbPath);
                    result.PdbAvailable = File.Exists(pdbPath);
                    if (!result.PdbAvailable)
                    {
                        return Fail(result, candidate, "PdbValidated", EvaluationFailureCategory.MissingArtifact, "Portable PDB is absent.", stopwatch);
                    }

                    result.PdbSha256 = HashFile(pdbPath);
                    result.PdbType = DetectPdbType(pdbPath);
                    result.PdbOrigin = ExternalPortablePdbOrigin.Explicit.ToString();
                    result.PdbAcquisition.ValidationAttempts = 1;
                    if (!ExternalCompilationProvenanceDescriptorFactory.TryCreateFromFile(
                            debugDirectory,
                            pdbPath,
                            out provenance))
                    {
                        return Fail(result, candidate, "PdbValidated", EvaluationFailureCategory.ProvenanceMismatch, "PE/PDB identity or Portable-PDB provenance validation failed.", stopwatch);
                    }
                }
                else if (!TryAcquirePortablePdb(
                        debugDirectory,
                        assemblyPath,
                        result,
                        out provenance))
                {
                    return Fail(result, candidate, "PdbValidated", EvaluationFailureCategory.MissingArtifact, "No exact Portable PDB was available from permitted local sources.", stopwatch);
                }

                result.PdbAvailable = true;
                result.PdbType = "Portable";
                result.SourceLinkAvailable = provenance.PortablePdb.SourceLink != null;
                result.EmbeddedSourceCount = provenance.PortablePdb.Documents.Count(static document => document.EmbeddedSource != null);
                AddStage(result, "PdbValidated", true, $"{result.PdbOrigin}; {provenance.PortablePdb.ValidationKind}; {provenance.PortablePdb.Documents.Length} documents.");
                if (provenance.CompilationOptions == null || provenance.MetadataReferences == null)
                {
                    return Fail(result, candidate, "CompilationProvenanceRead", EvaluationFailureCategory.ProvenanceMismatch, "Compilation options or metadata-reference provenance is absent.", stopwatch);
                }

                result.ExpectedReferenceCount = provenance.MetadataReferences.References.Length;
                AddStage(result, "CompilationProvenanceRead", true, $"{result.ExpectedReferenceCount} exact references.");
                if (!ExternalCSharpCompilationConfigurationFactory.TryCreate(
                    provenance,
                    out ExternalCSharpCompilationConfiguration configuration))
                {
                    return Fail(result, candidate, "ConfigurationReconstructed", EvaluationFailureCategory.ConfigurationUnsupported, "Recorded compiler configuration is outside the supported P5G shape: " + FormatCompilationOptions(provenance.CompilationOptions), stopwatch);
                }

                result.ExpectedSourceFileCount = configuration.SourceFileCount;
                AddStage(result, "ConfigurationReconstructed", true, $"Compiler {configuration.CompilerVersion}; {configuration.SourceFileCount} sources.");
                ExternalSourceAcquisition acquisition = new();
                if (!acquisition.TryConfigureReconstructionPolicy(
                        options.SourceReconstructionPolicy))
                {
                    return Fail(result, candidate, "SourcesAcquired", EvaluationFailureCategory.PotentialBug, "Source reconstruction policy configuration failed.", stopwatch, potentialBug: true);
                }

                if (options.SourceLinkEnabled)
                {
                    _ = acquisition.TryConfigureSourceLink(ExternalSourceLinkClient.CreateDefault());
                }

                if (!TryCreateSyntaxTrees(
                    provenance,
                    configuration,
                    acquisition,
                    result,
                    out ExternalCSharpSyntaxTreeSet syntaxTreeSet))
                {
                    EvaluationFailureCategory category = options.SourceLinkEnabled
                        ? EvaluationFailureCategory.SourceUnavailable
                        : EvaluationFailureCategory.ExpectedUnsupported;
                    return Fail(result, candidate, "SourcesAcquired", category, options.SourceLinkEnabled
                        ? $"{result.DirectExactSourceCount} direct exact, {result.ReconstructedExactSourceCount} reconstructed exact, and {result.UnavailableSourceCount} unavailable source documents."
                        : "Source Link is disabled and non-embedded sources remain unavailable.", stopwatch);
                }

                AddStage(result, "SourcesAcquired", true, FormatOrigins(result.SourceOrigins));
                AddStage(result, "SourcesValidated", true, "Every source passed P5H checksum validation.");
                AddStage(result, "SyntaxTreesCreated", true, $"{syntaxTreeSet.Trees.Length} trees passed P5I/P5J.");
                result.SourceTreeCount = syntaxTreeSet.Trees.Length;

                if (!TryCreateReferences(
                    candidate,
                    provenance,
                    result,
                    out ExternalMetadataReferenceSet referenceSet))
                {
                    return Fail(result, candidate, "ReferencesValidated", EvaluationFailureCategory.ReferenceUnsupported, "P7A could not locate and validate every exact P5A reference.", stopwatch);
                }

                result.ReferenceCount = referenceSet.References.Length;
                AddStage(result, "ReferencesValidated", true, $"{result.ReferenceCount} references passed P5B-P5E.");
                if (!ExternalCSharpCompilationFactory.TryCreate(
                    fixture.Target,
                    provenance,
                    configuration,
                    syntaxTreeSet,
                    referenceSet,
                    out CSharpCompilation compilation))
                {
                    return Fail(result, candidate, "CompilationCreated", EvaluationFailureCategory.ExpectedUnsupported, "P5K rejected the final composition; signed targets are intentionally unsupported.", stopwatch);
                }

                result.CompilerErrorCount = compilation.GetDiagnostics().Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
                result.CompilerWarningCount = compilation.GetDiagnostics().Count(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
                AddStage(result, "CompilationCreated", true, compilation.Assembly.Identity.ToString());
                if (!ExternalSupportingSourceCompilation.TryCreate(
                    fixture.Target,
                    provenance,
                    configuration,
                    syntaxTreeSet,
                    referenceSet,
                    compilation,
                    out ExternalSupportingSourceCompilation supportingSource))
                {
                    return Fail(result, candidate, "SupportingSourceRegistered", EvaluationFailureCategory.PotentialBug, "P6A handoff validation rejected a successful P5K result.", stopwatch, potentialBug: true);
                }

                result.SourceBackedFindings = Analyze(fixture, supportingSource);
                result.AddedFindings = result.SourceBackedFindings.Except(result.BaselineFindings, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
                result.RemovedFindings = result.BaselineFindings.Except(result.SourceBackedFindings, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
                result.ChangedExceptionEvidence = FindChangedExceptionEvidence(result.RemovedFindings, result.AddedFindings);
                result.ReconstructionSucceeded = true;
                result.ExpectedOutcomeObserved = candidate.ExpectedOutcome == EvaluationExpectedOutcome.Success;
                result.SemanticModelProbeSucceeded = fixture.SourceMethodResolved;
                result.ExceptionProbeSucceeded = candidate.Probe == null
                    || result.SourceBackedFindings.Any(finding => finding.Contains(candidate.Probe.ExpectedExceptionType, StringComparison.Ordinal));
                result.ManualVerification = candidate.Probe?.VerifiedFlow;
                AddStage(result, "SupportingSourceRegistered", true, "Exact P6A external scope registered.");
                AddStage(result, "SourceBodyUsed", fixture.SourceMethodResolved, fixture.SourceMethodResolved
                    ? "P6B resolved the invoked metadata method to a source declaration."
                    : "No source-backed invoked method was resolved.");
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException
                or BadImageFormatException
                or ArgumentException
                or InvalidOperationException)
            {
                return Fail(result, candidate, "UnhandledCandidateBoundary", EvaluationFailureCategory.PotentialBug, exception.Message, stopwatch, potentialBug: true);
            }
        }

        private ConsumerFixture? CreateConsumerFixture(EvaluationCandidate candidate, string assemblyPath)
        {
            string source = candidate.Probe?.Source
                ?? "public static class Consumer { /** <summary>Runs.</summary> */ public static void M() { } }";
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: ConsumerPath);
            PortableExecutableReference targetReference = MetadataReference.CreateFromFile(assemblyPath);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "E1.Consumer." + candidate.Id.Replace('-', '_'),
                [tree],
                platformReferences.Append(targetReference),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            IAssemblySymbol? targetSymbol = compilation.GetAssemblyOrModuleSymbol(targetReference) as IAssemblySymbol;

            if (targetSymbol == null
                || !ExternalAssemblyReferenceDescriptorFactory.TryCreate(
                    compilation,
                    targetSymbol,
                    out ExternalAssemblyReferenceDescriptor target))
            {
                return null;
            }

            return new ConsumerFixture(tree, compilation, targetReference, target);
        }

        private bool TryCreateSyntaxTrees(
            ExternalCompilationProvenanceDescriptor provenance,
            ExternalCSharpCompilationConfiguration configuration,
            ExternalSourceAcquisition acquisition,
            EvaluationCandidateResult result,
            out ExternalCSharpSyntaxTreeSet syntaxTreeSet)
        {
            List<ExternalSourceDocumentDescriptor> documents = new();
            List<ExternalCSharpSyntaxTree> trees = new();
            bool complete = true;

            foreach (ExternalSourceDocumentDescriptor document in provenance.PortablePdb.Documents)
            {
                bool created = ValidatedExternalSourceMaterialFactory.TryCreateFromEmbeddedSource(document, out ValidatedExternalSourceMaterial material)
                    || acquisition.TryAcquire(
                        document,
                        provenance.PortablePdb.SourceLink,
                        configuration,
                        out material);

                if (!created)
                {
                    result.UnavailableSourceCount++;
                    complete = false;
                    continue;
                }

                string origin = material.Origin.ToString();
                result.SourceOrigins[origin] = result.SourceOrigins.GetValueOrDefault(origin) + 1;
                if (material.Exactness == ExternalSourceMaterialExactness.DirectExact)
                {
                    result.DirectExactSourceCount++;
                }
                else
                {
                    result.ReconstructedExactSourceCount++;
                }

                if (!ExternalCSharpSyntaxTreeFactory.TryCreate(material, configuration, out ExternalCSharpSyntaxTree tree))
                {
                    complete = false;
                    continue;
                }

                documents.Add(document);
                trees.Add(tree);
            }

            ExternalSourceAcquisitionStatistics statistics = acquisition.GetStatistics();
            result.SourceAcquisition = new EvaluationSourceAcquisitionStatistics
            {
                DirectExactSources = statistics.DirectExactSourceCount,
                ReconstructionAttempts = statistics.ReconstructionAttemptCount,
                ReconstructionSuccesses = statistics.ReconstructionSuccessCount,
                ReconstructionFailures = statistics.ReconstructionFailureCount,
                LfToCrlfSuccesses = statistics.LfToCrlfSuccessCount,
                CrlfToLfSuccesses = statistics.CrlfToLfSuccessCount,
                P5HValidationAttempts = statistics.P5HValidationAttempts,
                SourceLinkRequests = statistics.SourceLinkRequests,
                DownloadedSourceBytes = statistics.DownloadedSourceBytes,
                ReconstructionBytesProduced = statistics.ReconstructionBytesProduced,
                ReconstructionDurationTicks = statistics.ReconstructionDurationTicks,
                PositiveCacheHits = statistics.PositiveCacheHits,
                NegativeCacheHits = statistics.NegativeCacheHits
            };

            if (!complete)
            {
                syntaxTreeSet = null!;
                return false;
            }

            return ExternalCSharpSyntaxTreeSetFactory.TryCreate(
                configuration,
                documents,
                trees,
                out syntaxTreeSet);
        }

        private bool TryCreateReferences(
            EvaluationCandidate candidate,
            ExternalCompilationProvenanceDescriptor provenance,
            EvaluationCandidateResult result,
            out ExternalMetadataReferenceSet referenceSet)
        {
            ExternalCompilationMetadataReferencesDescriptor references = provenance.MetadataReferences!;
            ExternalBinaryCandidateDiscovery discovery = new();
            string[] roots = candidate.ReferenceRoots.Select(Resolve).ToArray();
            result.BinaryDiscoveryUsed = true;

            if (!discovery.TryConfigure(roots))
            {
                referenceSet = null!;
                return false;
            }

            IEnumerable<string> loadedReferences = platformReferences
                .OfType<PortableExecutableReference>()
                .Select(static reference => reference.FilePath)
                .Where(static path => path != null)
                .Select(static path => path!);
            if (!ExternalReferenceArtifactSourceConfiguration.TryCreateForCurrentProcess(
                    loadedReferences,
                    Environment.CurrentDirectory,
                    out ExternalReferenceArtifactSourceConfiguration artifactSources)
                || !discovery.TryConfigureStandardArtifactSources(artifactSources))
            {
                referenceSet = null!;
                return false;
            }

            ImmutableArray<ValidatedExternalMetadataReferenceMaterial>.Builder materials =
                ImmutableArray.CreateBuilder<ValidatedExternalMetadataReferenceMaterial>(
                    references.References.Length);
            ExternalRemoteReferenceAcquisition remote = CreateRemoteReferenceAcquisition(candidate);
            bool complete = true;

            for (int ordinal = 0; ordinal < references.References.Length; ordinal++)
            {
                ExternalCompilationMetadataReferenceDescriptor reference = references.References[ordinal];
                bool found = discovery.TryAcquireReferenceMaterial(
                    reference,
                    ordinal,
                    remote,
                    out ValidatedExternalMetadataReferenceMaterial material,
                    out ExternalReferenceArtifactSourceKind sourceKind,
                    out ExternalRemoteReferenceProvenance? remoteProvenance);
                result.References.Add(new EvaluationReferenceResult
                {
                    Ordinal = ordinal,
                    Name = reference.Name,
                    MetadataImageKind = reference.Kind.ToString(),
                    ModuleVersionId = reference.ModuleVersionId,
                    Timestamp = reference.Timestamp,
                    ImageSize = reference.ImageSize,
                    Aliases = reference.Aliases.ToList(),
                    EmbedInteropTypes = reference.EmbedInteropTypes,
                    ExactMatchFound = found,
                    CandidatePath = found && material.Candidate.FilePath != null
                        ? Path.GetRelativePath(options.WorkspacePath, material.Candidate.FilePath).Replace('\\', '/')
                        : null,
                    ArtifactSource = found ? sourceKind.ToString() : null,
                    AcquisitionResult = !found
                        ? "Unavailable"
                        : remoteProvenance == null ? "LocalExact" : "RemoteExact",
                    RemoteDiscoveryHint = remoteProvenance?.DiscoveryHint,
                    RemoteArtifactIdentity = remoteProvenance?.ArtifactIdentity,
                    RemoteArtifactVersion = remoteProvenance?.ArtifactVersion,
                    RemoteArtifactSha512 = remoteProvenance?.ArtifactSha512,
                    RemoteArchiveEntry = remoteProvenance?.ArchiveEntry
                });

                if (found)
                {
                    materials.Add(material);
                }
                else
                {
                    complete = false;
                }
            }

            ExternalBinaryCandidateDiscoveryStatistics statistics = discovery.GetStatistics();
            result.ReferenceDiscovery = new EvaluationReferenceDiscoveryStatistics
            {
                ArtifactRootsExamined = statistics.ArtifactRootsExamined,
                DirectoriesEnumerated = statistics.DirectoriesEnumerated,
                CandidateFilesConsidered = statistics.CandidateFilesConsidered,
                CandidateFilesOpened = statistics.CandidateFilesOpened,
                ValidationAttempts = statistics.ValidationAttempts
            };
            ExternalRemoteReferenceAcquisitionStatistics remoteStatistics = remote.GetStatistics();
            result.RemoteReferenceAcquisition = new EvaluationRemoteReferenceAcquisitionStatistics
            {
                Searches = remoteStatistics.RemoteSearchCount,
                Requests = remoteStatistics.RemoteRequestCount,
                ArtifactRequests = remoteStatistics.RemoteArtifactRequestCount,
                DownloadedBytes = remoteStatistics.RemoteDownloadedBytes,
                BinaryCandidates = remoteStatistics.RemoteBinaryCandidateCount,
                P5ValidationAttempts = remoteStatistics.RemoteP5ValidationAttemptCount,
                RemoteExact = remoteStatistics.RemoteExactCount,
                Unavailable = remoteStatistics.UnavailableCount,
                LimitHits = remoteStatistics.LimitHitCount,
                CacheHits = remoteStatistics.CacheHits,
                DurationTicks = remoteStatistics.DurationTicks,
                Providers = remoteStatistics.Providers.Select(static provider =>
                    new EvaluationRemoteReferenceProviderStatistics
                    {
                        Provider = provider.ProviderKind.ToString(),
                        Searches = provider.SearchCount,
                        MetadataRequests = provider.MetadataRequestCount,
                        ArtifactRequests = provider.ArtifactRequestCount,
                        DownloadedBytes = provider.DownloadedBytes,
                        BinaryCandidates = provider.CandidateBinaryCount,
                        P5ValidationAttempts = provider.P5ValidationAttemptCount,
                        RemoteExact = provider.RemoteExactCount,
                        Rejected = provider.RejectedCount,
                        Unavailable = provider.UnavailableCount,
                        CacheHits = provider.CacheHits,
                        LimitHits = provider.LimitHits,
                        DurationTicks = provider.DurationTicks
                    }).ToList()
            };

            result.BinaryDiscoverySucceeded = complete;
            if (!complete)
            {
                referenceSet = null!;
                return false;
            }

            return ExternalMetadataReferenceSetFactory.TryCreate(
                provenance,
                materials.MoveToImmutable(),
                out referenceSet);
        }

        private ExternalRemoteReferenceAcquisition CreateRemoteReferenceAcquisition(
            EvaluationCandidate candidate)
        {
            ExternalRemoteReferenceAcquisition acquisition = new();
            if (options.ReferenceAcquisitionPolicy != ExternalReferenceAcquisitionPolicy.BoundedRemoteArtifacts)
            {
                return acquisition;
            }

            ExternalRemoteReferencePackageHint[] hints = candidate.ReferencePackages
                .Select(static hint => new ExternalRemoteReferencePackageHint(
                    hint.AssemblySimpleName,
                    hint.PackageId,
                    hint.Versions,
                    Enum.Parse<ExternalRemoteArtifactProviderKind>(hint.Provider),
                    hint.Evidence))
                .ToArray();
            ExternalRemoteReferenceAcquisitionLimits limits = ExternalRemoteReferenceAcquisitionLimits.Default;
            if (!ExternalRemoteReferenceAcquisitionConfiguration.TryCreate(
                    options.ReferenceAcquisitionPolicy,
                    new Uri("https://api.nuget.org/v3/index.json"),
                    hints,
                    enablePackageSearch: true,
                    limits,
                    out ExternalRemoteReferenceAcquisitionConfiguration configuration)
                || !acquisition.TryConfigure(
                    configuration,
                    ExternalSourceLinkClient.CreateDefault(
                        limits.Timeout,
                        limits.MaxArtifactBytes)))
            {
                throw new InvalidOperationException("Bounded remote reference acquisition could not be configured.");
            }

            return acquisition;
        }

        /// <summary>
        /// Acquires an exact embedded or local sibling Portable PDB and records G2 work.
        /// </summary>
        /// <param name="debugDirectory">The authoritative P4A provenance.</param>
        /// <param name="assemblyPath">The exact target PE path.</param>
        /// <param name="result">The mutable local evaluation result.</param>
        /// <param name="provenance">The P4B/P5A result when successful.</param>
        /// <returns><see langword="true"/> only for an exact local candidate.</returns>
        private static bool TryAcquirePortablePdb(
            ExternalPeDebugDirectoryDescriptor debugDirectory,
            string assemblyPath,
            EvaluationCandidateResult result,
            out ExternalCompilationProvenanceDescriptor provenance)
        {
            ExternalPortablePdbAcquisition acquisition = new();
            if (!ExternalPortablePdbAcquisitionConfiguration.TryCreate(
                    [],
                    [],
                    [],
                    out ExternalPortablePdbAcquisitionConfiguration configuration)
                || !acquisition.TryConfigure(configuration))
            {
                provenance = null!;
                return false;
            }

            bool acquired = acquisition.TryAcquire(
                debugDirectory,
                assemblyPath,
                out provenance,
                out ExternalPortablePdbOrigin origin);
            ExternalPortablePdbAcquisitionStatistics statistics = acquisition.GetStatistics();
            result.PdbAcquisition = new EvaluationPdbAcquisitionStatistics
            {
                CandidatesConsidered = statistics.CandidatesConsidered,
                CandidatesOpened = statistics.CandidatesOpened,
                LocalSymbolPackagesInspected = statistics.LocalSymbolPackagesInspected,
                RemoteSymbolRequests = statistics.RemoteSymbolRequests,
                DownloadedPdbBytes = statistics.DownloadedPdbBytes,
                ValidationAttempts = statistics.ValidationAttempts,
                PositiveCacheHits = statistics.PositiveCacheHits,
                NegativeCacheHits = statistics.NegativeCacheHits
            };
            result.PdbOrigin = acquired ? origin.ToString() : null;
            return acquired;
        }

        /// <summary>Formats exact P5A compilation options for fail-closed diagnosis.</summary>
        /// <param name="options">The validated compilation options.</param>
        /// <returns>The deterministic key/value summary.</returns>
        private static string FormatCompilationOptions(
            ExternalCompilationOptionsDescriptor? options)
        {
            return options == null
                ? "absent"
                : string.Join(
                    ", ",
                    options.Options.Select(static option => $"{option.Key}={option.Value}"));
        }

        private List<string> Analyze(
            ConsumerFixture fixture,
            ExternalSupportingSourceCompilation? supportingSource)
        {
            ProjectClosureSemanticContext context = ProjectClosureSemanticContext.CreateSingleCompilationContext(
                fixture.Tree,
                fixture.Compilation);

            if (supportingSource != null)
            {
                _ = context.TryRegisterExternalSupportingSource(supportingSource, out _);
                InvocationExpressionSyntax? invocation = fixture.Tree.GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault();
                IMethodSymbol? method = invocation == null
                    ? null
                    : fixture.Compilation.GetSemanticModel(fixture.Tree).GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                fixture.SourceMethodResolved = method != null
                    && SupportingSourceSymbolResolver.TryResolveMethod(
                        method,
                        fixture.Compilation,
                        context,
                        out IMethodSymbol sourceMethod,
                        out _)
                    && sourceMethod.DeclaringSyntaxReferences.Length != 0;
            }

            XmlDocOptions analyzerOptions = new()
            {
                ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive
            };
            List<Finding> findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(
                fixture.Tree,
                ConsumerPath,
                fixture.Compilation.GetSemanticModel(fixture.Tree),
                context,
                analyzerOptions);
            return findings
                .Select(static finding => $"{finding.Smell.ID}|{finding.Line}|{finding.Column}|{finding.Message}")
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        private EvaluationCandidateResult CreateResult(EvaluationCandidate candidate)
        {
            return new EvaluationCandidateResult
            {
                Id = candidate.Id,
                Package = candidate.Package,
                Version = candidate.Version,
                TargetFramework = candidate.TargetFramework,
                SelectionReason = candidate.SelectionReason,
                Categories = candidate.Categories.Order(StringComparer.Ordinal).ToList(),
                AnalyzerMode = nameof(ExceptionAnalysisMode.SolutionTransitive),
                AssemblyPath = candidate.AssemblyPath.Replace('\\', '/'),
                PdbPath = candidate.PdbPath?.Replace('\\', '/'),
                ManualVerification = candidate.Probe?.VerifiedFlow
            };
        }

        private static List<string> FindChangedExceptionEvidence(
            IReadOnlyList<string> removedFindings,
            IReadOnlyList<string> addedFindings)
        {
            List<string> changes = new();

            foreach (string removedFinding in removedFindings)
            {
                string removedLocation = GetFindingLocation(removedFinding);

                foreach (string addedFinding in addedFindings)
                {
                    string addedLocation = GetFindingLocation(addedFinding);
                    if (string.Equals(removedLocation, addedLocation, StringComparison.Ordinal))
                    {
                        changes.Add($"{removedFinding} => {addedFinding}");
                    }
                }
            }

            return changes.Order(StringComparer.Ordinal).ToList();
        }

        private static string GetFindingLocation(string finding)
        {
            int separator = -1;

            for (int index = 0; index < 3; index++)
            {
                separator = finding.IndexOf('|', separator + 1);
                if (separator < 0)
                {
                    return finding;
                }
            }

            return finding[..separator];
        }

        private static EvaluationCandidateResult Fail(
            EvaluationCandidateResult result,
            EvaluationCandidate candidate,
            string stage,
            EvaluationFailureCategory category,
            string reason,
            Stopwatch stopwatch,
            bool potentialBug = false)
        {
            AddStage(result, stage, false, reason);
            result.FallbackStage = stage;
            result.FallbackCategory = category;
            result.FallbackReason = reason;
            result.PotentialBug = potentialBug;
            result.ExpectedOutcomeObserved = !potentialBug
                && (candidate.ExpectedOutcome == EvaluationExpectedOutcome.ExpectedFailClosed
                    || category == EvaluationFailureCategory.ExpectedUnsupported);
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private static void AddStage(
            EvaluationCandidateResult result,
            string stage,
            bool succeeded,
            string detail)
        {
            result.Stages.Add(new EvaluationStageResult
            {
                Stage = stage,
                Succeeded = succeeded,
                Detail = detail
            });
        }

        private string Resolve(string relativePath)
        {
            return EvaluationManifestLoader.ResolveWorkspacePath(options.WorkspacePath, relativePath);
        }

        private static string HashFile(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        private static string DetectPdbType(string path)
        {
            byte[] header = new byte[4];
            using FileStream stream = File.OpenRead(path);
            int count = stream.Read(header, 0, header.Length);
            return count == header.Length && header.AsSpan().SequenceEqual("BSJB"u8)
                ? "Portable"
                : "Unsupported";
        }

        private static string FormatOrigins(IReadOnlyDictionary<string, int> origins)
        {
            return string.Join(
                ", ",
                origins.OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                    .Select(static pair => $"{pair.Key}={pair.Value}"));
        }

        private static ImmutableArray<MetadataReference> CreatePlatformReferences()
        {
            string value = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
                ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
            return value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(static path => MetadataReference.CreateFromFile(path))
                .Cast<MetadataReference>()
                .ToImmutableArray();
        }

        private static string ReadGitCommit()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "git",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("rev-parse");
            startInfo.ArgumentList.Add("HEAD");
            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Git could not be started.");
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0
                ? output.Trim()
                : "unknown";
        }

        private sealed class ConsumerFixture
        {
            public ConsumerFixture(
                SyntaxTree tree,
                CSharpCompilation compilation,
                PortableExecutableReference targetReference,
                ExternalAssemblyReferenceDescriptor target)
            {
                Tree = tree;
                Compilation = compilation;
                TargetReference = targetReference;
                Target = target;
            }

            public SyntaxTree Tree { get; }

            public CSharpCompilation Compilation { get; }

            public PortableExecutableReference TargetReference { get; }

            public ExternalAssemblyReferenceDescriptor Target { get; }

            public bool SourceMethodResolved { get; set; }
        }
    }
}
