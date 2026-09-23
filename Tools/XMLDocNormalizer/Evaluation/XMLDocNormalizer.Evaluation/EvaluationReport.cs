using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Captures one reproducible E1 run and its stable candidate results.
    /// </summary>
    public sealed class EvaluationReport
    {
        /// <summary>Gets or sets the UTC evaluation timestamp.</summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>Gets or sets the evaluated Git commit.</summary>
        public string GitCommit { get; set; } = string.Empty;

        /// <summary>Gets or sets the operating-system description.</summary>
        public string OperatingSystem { get; set; } = string.Empty;

        /// <summary>Gets or sets the .NET runtime description.</summary>
        public string Runtime { get; set; } = string.Empty;

        /// <summary>Gets or sets the process architecture.</summary>
        public string ProcessArchitecture { get; set; } = string.Empty;

        /// <summary>Gets or sets whether network Source Link was enabled.</summary>
        public bool SourceLinkEnabled { get; set; }

        /// <summary>Gets or sets the external-source reconstruction policy.</summary>
        public string SourceReconstructionPolicy { get; set; } = string.Empty;

        /// <summary>Gets or sets the external-reference acquisition policy.</summary>
        public string ReferenceAcquisitionPolicy { get; set; } = string.Empty;

        /// <summary>Gets or sets aggregate counts.</summary>
        public EvaluationSummary Summary { get; set; } = new();

        /// <summary>Gets or sets candidate results in stable identifier order.</summary>
        public List<EvaluationCandidateResult> Candidates { get; set; } = new();
    }

    /// <summary>
    /// Captures aggregate E1 result counts without claiming statistical accuracy.
    /// </summary>
    public sealed class EvaluationSummary
    {
        /// <summary>Gets or sets the evaluated candidate count.</summary>
        public int CandidatesEvaluated { get; set; }

        /// <summary>Gets or sets full reconstruction successes.</summary>
        public int FullReconstructionSuccesses { get; set; }

        /// <summary>Gets or sets expected fail-closed cases.</summary>
        public int ExpectedFailClosedCases { get; set; }

        /// <summary>Gets or sets unexpected failures.</summary>
        public int UnexpectedFailures { get; set; }

        /// <summary>Gets or sets potential implementation bugs.</summary>
        public int PotentialBugs { get; set; }

        /// <summary>Gets or sets candidates using embedded source.</summary>
        public int EmbeddedSourceCandidates { get; set; }

        /// <summary>Gets or sets candidates using Source Link.</summary>
        public int SourceLinkCandidates { get; set; }

        /// <summary>Gets or sets candidates with finding-set differences.</summary>
        public int FindingDifferenceCandidates { get; set; }
    }

    /// <summary>
    /// Captures all observable gates for one real published library case.
    /// </summary>
    public sealed class EvaluationCandidateResult
    {
        /// <summary>Gets or sets the stable candidate identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the package identifier.</summary>
        public string Package { get; set; } = string.Empty;

        /// <summary>Gets or sets the exact version.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Gets or sets the target framework.</summary>
        public string TargetFramework { get; set; } = string.Empty;

        /// <summary>Gets or sets the documented selection rationale.</summary>
        public string SelectionReason { get; set; } = string.Empty;

        /// <summary>Gets or sets the covered candidate categories.</summary>
        public List<string> Categories { get; set; } = new();

        /// <summary>Gets or sets the source-backed analyzer mode.</summary>
        public string AnalyzerMode { get; set; } = string.Empty;

        /// <summary>Gets or sets the normalized workspace-relative assembly path.</summary>
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>Gets or sets the normalized workspace-relative PDB path.</summary>
        public string? PdbPath { get; set; }

        /// <summary>Gets or sets the assembly SHA-256.</summary>
        public string? AssemblySha256 { get; set; }

        /// <summary>Gets or sets the PDB SHA-256.</summary>
        public string? PdbSha256 { get; set; }

        /// <summary>Gets or sets whether a PDB file was available.</summary>
        public bool PdbAvailable { get; set; }

        /// <summary>Gets or sets the observed PDB type.</summary>
        public string PdbType { get; set; } = "None";

        /// <summary>Gets or sets the exact validated PDB candidate origin.</summary>
        public string? PdbOrigin { get; set; }

        /// <summary>Gets or sets bounded G2 Portable PDB acquisition counters.</summary>
        public EvaluationPdbAcquisitionStatistics PdbAcquisition { get; set; } = new();

        /// <summary>Gets or sets whether Source Link provenance was present.</summary>
        public bool SourceLinkAvailable { get; set; }

        /// <summary>Gets or sets the embedded-source document count.</summary>
        public int EmbeddedSourceCount { get; set; }

        /// <summary>Gets or sets whether P7A was exercised.</summary>
        public bool BinaryDiscoveryUsed { get; set; }

        /// <summary>Gets or sets whether every P7A lookup succeeded.</summary>
        public bool BinaryDiscoverySucceeded { get; set; }

        /// <summary>Gets or sets the exact P5A reference count.</summary>
        public int ExpectedReferenceCount { get; set; }

        /// <summary>Gets or sets the validated P5E reference count.</summary>
        public int ReferenceCount { get; set; }

        /// <summary>Gets or sets every expected reference and its explicit-root result.</summary>
        public List<EvaluationReferenceResult> References { get; set; } = new();

        /// <summary>Gets or sets bounded local reference-discovery cost counters.</summary>
        public EvaluationReferenceDiscoveryStatistics ReferenceDiscovery { get; set; } = new();

        /// <summary>Gets or sets bounded remote reference acquisition work.</summary>
        public EvaluationRemoteReferenceAcquisitionStatistics RemoteReferenceAcquisition { get; set; } = new();

        /// <summary>Gets or sets the P5A source-file count.</summary>
        public int ExpectedSourceFileCount { get; set; }

        /// <summary>Gets or sets the P5J source-tree count.</summary>
        public int SourceTreeCount { get; set; }

        /// <summary>Gets or sets source documents validated without transformation.</summary>
        public int DirectExactSourceCount { get; set; }

        /// <summary>Gets or sets source documents validated after reconstruction.</summary>
        public int ReconstructedExactSourceCount { get; set; }

        /// <summary>Gets or sets source documents unavailable after permitted attempts.</summary>
        public int UnavailableSourceCount { get; set; }

        /// <summary>Gets or sets bounded G3A source acquisition work.</summary>
        public EvaluationSourceAcquisitionStatistics SourceAcquisition { get; set; } = new();

        /// <summary>Gets or sets compiler error count.</summary>
        public int CompilerErrorCount { get; set; }

        /// <summary>Gets or sets compiler warning count.</summary>
        public int CompilerWarningCount { get; set; }

        /// <summary>Gets or sets exact target and reconstructed signing evidence.</summary>
        public EvaluationSigningResult Signing { get; set; } = new();

        /// <summary>Gets or sets validated source-origin counts.</summary>
        public SortedDictionary<string, int> SourceOrigins { get; set; } = new(StringComparer.Ordinal);

        /// <summary>Gets or sets standard pipeline stage results.</summary>
        public List<EvaluationStageResult> Stages { get; set; } = new();

        /// <summary>Gets or sets the terminal reconstruction result.</summary>
        public bool ReconstructionSucceeded { get; set; }

        /// <summary>Gets or sets the first failed stage.</summary>
        public string? FallbackStage { get; set; }

        /// <summary>Gets or sets the stable failure category.</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EvaluationFailureCategory? FallbackCategory { get; set; }

        /// <summary>Gets or sets the concise failure reason.</summary>
        public string? FallbackReason { get; set; }

        /// <summary>Gets or sets whether the observed outcome matched the manifest.</summary>
        public bool ExpectedOutcomeObserved { get; set; }

        /// <summary>Gets or sets whether a potential supported-shape bug was found.</summary>
        public bool PotentialBug { get; set; }

        /// <summary>Gets or sets baseline canonical findings.</summary>
        public List<string> BaselineFindings { get; set; } = new();

        /// <summary>Gets or sets source-backed canonical findings.</summary>
        public List<string> SourceBackedFindings { get; set; } = new();

        /// <summary>Gets or sets added canonical findings.</summary>
        public List<string> AddedFindings { get; set; } = new();

        /// <summary>Gets or sets removed canonical findings.</summary>
        public List<string> RemovedFindings { get; set; } = new();

        /// <summary>Gets or sets changed exception evidence at an unchanged finding location.</summary>
        public List<string> ChangedExceptionEvidence { get; set; } = new();

        /// <summary>Gets or sets the semantic-model probe result.</summary>
        public bool SemanticModelProbeSucceeded { get; set; }

        /// <summary>Gets or sets whether the manifest configured a callable probe.</summary>
        public bool SourceBodyProbeConfigured { get; set; }

        /// <summary>Gets or sets the exception probe result.</summary>
        public bool ExceptionProbeSucceeded { get; set; }

        /// <summary>Gets or sets the manually verified flow note.</summary>
        public string? ManualVerification { get; set; }

        /// <summary>Gets or sets elapsed candidate milliseconds.</summary>
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// Captures observable target-PE signing provenance and the semantic-only
    /// Roslyn reconstruction without claiming emit or signature fidelity.
    /// </summary>
    public sealed class EvaluationSigningResult
    {
        /// <summary>Gets or sets the P4A signing-state classification.</summary>
        public string TargetState { get; set; } = string.Empty;

        /// <summary>Gets or sets the complete original target identity.</summary>
        public string OriginalAssemblyIdentity { get; set; } = string.Empty;

        /// <summary>Gets or sets the assembly simple name.</summary>
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>Gets or sets the assembly version.</summary>
        public string AssemblyVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets the neutral or explicit assembly culture.</summary>
        public string Culture { get; set; } = string.Empty;

        /// <summary>Gets or sets the complete manifest public key as hexadecimal.</summary>
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>Gets or sets the SHA-256 digest of the complete public key.</summary>
        public string PublicKeySha256 { get; set; } = string.Empty;

        /// <summary>Gets or sets the public-key token as hexadecimal.</summary>
        public string PublicKeyToken { get; set; } = string.Empty;

        /// <summary>Gets or sets exact manifest AssemblyFlags.</summary>
        public string AssemblyFlags { get; set; } = string.Empty;

        /// <summary>Gets or sets exact PE CLI CorFlags.</summary>
        public string CorFlags { get; set; } = string.Empty;

        /// <summary>Gets or sets the original signature-directory size.</summary>
        public int StrongNameSignatureSize { get; set; }

        /// <summary>Gets or sets the SHA-256 digest of original signature bytes.</summary>
        public string StrongNameSignatureSha256 { get; set; } = string.Empty;

        /// <summary>Gets or sets whether the signature directory contains nonzero bytes.</summary>
        public bool HasStrongNameSignature { get; set; }

        /// <summary>Gets or sets whether the semantic reconstruction supports the PE shape.</summary>
        public bool SemanticReconstructionSupported { get; set; }

        /// <summary>Gets or sets the reconstructed CryptoPublicKey as hexadecimal.</summary>
        public string? ReconstructedCryptoPublicKey { get; set; }

        /// <summary>Gets or sets the reconstructed CryptoKeyFile.</summary>
        public string? CryptoKeyFile { get; set; }

        /// <summary>Gets or sets the reconstructed CryptoKeyContainer.</summary>
        public string? CryptoKeyContainer { get; set; }

        /// <summary>Gets or sets the reconstructed nullable DelaySign option.</summary>
        public bool? DelaySign { get; set; }

        /// <summary>Gets or sets the reconstructed PublicSign option.</summary>
        public bool PublicSign { get; set; }

        /// <summary>Gets or sets whether a StrongNameProvider was configured.</summary>
        public bool StrongNameProviderConfigured { get; set; }

        /// <summary>Gets or sets the reconstructed compilation identity.</summary>
        public string? ReconstructedAssemblyIdentity { get; set; }

        /// <summary>Gets or sets whether original and reconstructed identities match.</summary>
        public bool? AssemblyIdentityMatches { get; set; }

        /// <summary>Gets or sets the explicit semantic/emit fidelity classification.</summary>
        public string FidelityResult { get; set; } = "NotReached";
    }

    /// <summary>
    /// Captures one authoritative P5A reference and the existing P7A result.
    /// </summary>
    public sealed class EvaluationReferenceResult
    {
        /// <summary>Gets or sets the P5A ordinal.</summary>
        public int Ordinal { get; set; }

        /// <summary>Gets or sets the serialized expected-name hint.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the expected metadata image kind.</summary>
        public string MetadataImageKind { get; set; } = string.Empty;

        /// <summary>Gets or sets the expected module version identifier.</summary>
        public Guid ModuleVersionId { get; set; }

        /// <summary>Gets or sets the expected PE timestamp.</summary>
        public int Timestamp { get; set; }

        /// <summary>Gets or sets the expected PE SizeOfImage.</summary>
        public int ImageSize { get; set; }

        /// <summary>Gets or sets the exact serialized aliases.</summary>
        public List<string> Aliases { get; set; } = new();

        /// <summary>Gets or sets the exact EmbedInteropTypes property.</summary>
        public bool EmbedInteropTypes { get; set; }

        /// <summary>Gets or sets whether existing explicit-root P7A found an exact match.</summary>
        public bool ExactMatchFound { get; set; }

        /// <summary>Gets or sets the normalized workspace-relative exact path.</summary>
        public string? CandidatePath { get; set; }

        /// <summary>Gets or sets the local source of the exact candidate.</summary>
        public string? ArtifactSource { get; set; }

        /// <summary>Gets or sets LocalExact, RemoteExact, or Unavailable.</summary>
        public string AcquisitionResult { get; set; } = "Unavailable";

        /// <summary>Gets or sets the remote package discovery hint.</summary>
        public string? RemoteDiscoveryHint { get; set; }

        /// <summary>Gets or sets the remote artifact identity.</summary>
        public string? RemoteArtifactIdentity { get; set; }

        /// <summary>Gets or sets the remote artifact version.</summary>
        public string? RemoteArtifactVersion { get; set; }

        /// <summary>Gets or sets the verified transport SHA-512.</summary>
        public string? RemoteArtifactSha512 { get; set; }

        /// <summary>Gets or sets the exact archive entry that passed P5.</summary>
        public string? RemoteArchiveEntry { get; set; }
    }

    /// <summary>Captures aggregate bounded remote reference-provider work.</summary>
    public sealed class EvaluationRemoteReferenceAcquisitionStatistics
    {
        /// <summary>Gets or sets bounded package searches.</summary>
        public long Searches { get; set; }
        /// <summary>Gets or sets all remote metadata and artifact requests.</summary>
        public long Requests { get; set; }
        /// <summary>Gets or sets package artifact requests.</summary>
        public long ArtifactRequests { get; set; }
        /// <summary>Gets or sets downloaded metadata and artifact bytes.</summary>
        public long DownloadedBytes { get; set; }
        /// <summary>Gets or sets inspected archive binary candidates.</summary>
        public long BinaryCandidates { get; set; }
        /// <summary>Gets or sets unchanged P5 validation attempts.</summary>
        public long P5ValidationAttempts { get; set; }
        /// <summary>Gets or sets exact remote reference successes.</summary>
        public long RemoteExact { get; set; }
        /// <summary>Gets or sets references unavailable after bounded acquisition.</summary>
        public long Unavailable { get; set; }
        /// <summary>Gets or sets hard-limit stops.</summary>
        public long LimitHits { get; set; }
        /// <summary>Gets or sets positive and negative context-cache hits.</summary>
        public long CacheHits { get; set; }
        /// <summary>Gets or sets acquisition stopwatch ticks.</summary>
        public long DurationTicks { get; set; }
        /// <summary>Gets or sets provider-specific counters.</summary>
        public List<EvaluationRemoteReferenceProviderStatistics> Providers { get; set; } = new();
    }

    /// <summary>Captures one bounded remote provider's work.</summary>
    public sealed class EvaluationRemoteReferenceProviderStatistics
    {
        /// <summary>Gets or sets the provider kind.</summary>
        public string Provider { get; set; } = string.Empty;
        /// <summary>Gets or sets bounded searches.</summary>
        public long Searches { get; set; }
        /// <summary>Gets or sets service-index, registration, catalog, and search requests.</summary>
        public long MetadataRequests { get; set; }
        /// <summary>Gets or sets package artifact requests.</summary>
        public long ArtifactRequests { get; set; }
        /// <summary>Gets or sets downloaded bytes.</summary>
        public long DownloadedBytes { get; set; }
        /// <summary>Gets or sets candidate binary entries inspected.</summary>
        public long BinaryCandidates { get; set; }
        /// <summary>Gets or sets unchanged P5 attempts.</summary>
        public long P5ValidationAttempts { get; set; }
        /// <summary>Gets or sets exact remote successes.</summary>
        public long RemoteExact { get; set; }
        /// <summary>Gets or sets rejected package or binary candidates.</summary>
        public long Rejected { get; set; }
        /// <summary>Gets or sets unavailable references.</summary>
        public long Unavailable { get; set; }
        /// <summary>Gets or sets provider cache hits.</summary>
        public long CacheHits { get; set; }
        /// <summary>Gets or sets hard-limit stops.</summary>
        public long LimitHits { get; set; }
        /// <summary>Gets or sets provider stopwatch ticks.</summary>
        public long DurationTicks { get; set; }
    }

    /// <summary>
    /// Captures descriptive P7A/G1 local search work without performance claims.
    /// </summary>
    public sealed class EvaluationReferenceDiscoveryStatistics
    {
        /// <summary>Gets or sets the number of source roots examined.</summary>
        public long ArtifactRootsExamined { get; set; }

        /// <summary>Gets or sets the number of bounded directory enumerations.</summary>
        public long DirectoriesEnumerated { get; set; }

        /// <summary>Gets or sets the number of deduplicated candidate files considered.</summary>
        public long CandidateFilesConsidered { get; set; }

        /// <summary>Gets or sets the number of candidate-open attempts.</summary>
        public long CandidateFilesOpened { get; set; }

        /// <summary>Gets or sets the number of existing P5 validation attempts.</summary>
        public long ValidationAttempts { get; set; }
    }

    /// <summary>Captures bounded G2 Portable PDB acquisition work.</summary>
    public sealed class EvaluationPdbAcquisitionStatistics
    {
        /// <summary>Gets or sets the number of candidate images considered.</summary>
        public long CandidatesConsidered { get; set; }

        /// <summary>Gets or sets the number of candidate open attempts.</summary>
        public long CandidatesOpened { get; set; }

        /// <summary>Gets or sets the number of local package archives inspected.</summary>
        public long LocalSymbolPackagesInspected { get; set; }

        /// <summary>Gets or sets the number of remote symbol requests.</summary>
        public long RemoteSymbolRequests { get; set; }

        /// <summary>Gets or sets the number of remotely downloaded PDB bytes.</summary>
        public long DownloadedPdbBytes { get; set; }

        /// <summary>Gets or sets the number of P4B validation attempts.</summary>
        public long ValidationAttempts { get; set; }

        /// <summary>Gets or sets the number of positive cache hits.</summary>
        public long PositiveCacheHits { get; set; }

        /// <summary>Gets or sets the number of negative cache hits.</summary>
        public long NegativeCacheHits { get; set; }
    }

    /// <summary>Captures bounded G3A source acquisition and reconstruction work.</summary>
    public sealed class EvaluationSourceAcquisitionStatistics
    {
        /// <summary>Gets or sets direct exact acquired-source successes.</summary>
        public long DirectExactSources { get; set; }

        /// <summary>Gets or sets reconstruction attempts after direct P5H failure.</summary>
        public long ReconstructionAttempts { get; set; }

        /// <summary>Gets or sets successful reconstruction attempts.</summary>
        public long ReconstructionSuccesses { get; set; }

        /// <summary>Gets or sets unsuccessful reconstruction attempts.</summary>
        public long ReconstructionFailures { get; set; }

        /// <summary>Gets or sets successful LF-to-CRLF candidates.</summary>
        public long LfToCrlfSuccesses { get; set; }

        /// <summary>Gets or sets successful CRLF-to-LF candidates.</summary>
        public long CrlfToLfSuccesses { get; set; }

        /// <summary>Gets or sets direct and reconstructed P5H validation attempts.</summary>
        public long P5HValidationAttempts { get; set; }

        /// <summary>Gets or sets Source Link requests.</summary>
        public long SourceLinkRequests { get; set; }

        /// <summary>Gets or sets downloaded source bytes.</summary>
        public long DownloadedSourceBytes { get; set; }

        /// <summary>Gets or sets bytes produced across reconstruction candidates.</summary>
        public long ReconstructionBytesProduced { get; set; }

        /// <summary>Gets or sets reconstruction elapsed timer ticks.</summary>
        public long ReconstructionDurationTicks { get; set; }

        /// <summary>Gets or sets positive source-cache hits.</summary>
        public long PositiveCacheHits { get; set; }

        /// <summary>Gets or sets negative source-cache hits.</summary>
        public long NegativeCacheHits { get; set; }
    }

    /// <summary>
    /// Captures one normalized pipeline gate.
    /// </summary>
    public sealed class EvaluationStageResult
    {
        /// <summary>Gets or sets the stable stage name.</summary>
        public string Stage { get; set; } = string.Empty;

        /// <summary>Gets or sets whether the stage succeeded.</summary>
        public bool Succeeded { get; set; }

        /// <summary>Gets or sets a concise observation.</summary>
        public string Detail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Classifies real-world fail-closed boundaries.
    /// </summary>
    public enum EvaluationFailureCategory
    {
        /// <summary>The manifest expected the unsupported shape.</summary>
        ExpectedUnsupported,

        /// <summary>A required DLL or PDB was absent.</summary>
        MissingArtifact,

        /// <summary>PE, PDB, source, or reference provenance did not match.</summary>
        ProvenanceMismatch,

        /// <summary>Validated source bytes were unavailable.</summary>
        SourceUnavailable,

        /// <summary>Downloaded or local source failed its PDB checksum.</summary>
        SourceChecksumMismatch,

        /// <summary>Network policy rejected the acquisition target.</summary>
        NetworkPolicyRejected,

        /// <summary>P5G rejected the recorded compiler configuration.</summary>
        ConfigurationUnsupported,

        /// <summary>P5B-P5E could not reconstruct all exact references.</summary>
        ReferenceUnsupported,

        /// <summary>The existing supported shape failed unexpectedly.</summary>
        PotentialBug
    }
}
