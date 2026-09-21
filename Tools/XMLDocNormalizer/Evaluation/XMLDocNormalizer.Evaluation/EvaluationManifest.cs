using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Describes pinned real-world artifacts and candidate expectations.
    /// </summary>
    public sealed class EvaluationManifest
    {
        /// <summary>Gets or sets the manifest schema version.</summary>
        public int SchemaVersion { get; set; }

        /// <summary>Gets or sets the pinned downloadable artifacts.</summary>
        public List<EvaluationArtifact> Artifacts { get; set; } = new();

        /// <summary>Gets or sets the deterministically ordered candidates.</summary>
        public List<EvaluationCandidate> Candidates { get; set; } = new();
    }

    /// <summary>
    /// Describes one downloaded package without authorizing execution of its build logic.
    /// </summary>
    public sealed class EvaluationArtifact
    {
        /// <summary>Gets or sets the stable artifact identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the exact HTTPS download URL.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Gets or sets the expected SHA-256 hash.</summary>
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>Gets or sets the workspace-relative extraction directory.</summary>
        public string ExtractTo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Describes one immutable package/TFM evaluation case.
    /// </summary>
    public sealed class EvaluationCandidate
    {
        /// <summary>Gets or sets the stable candidate identifier.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets the NuGet package identifier.</summary>
        public string Package { get; set; } = string.Empty;

        /// <summary>Gets or sets the exact package version.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Gets or sets the selected target framework.</summary>
        public string TargetFramework { get; set; } = string.Empty;

        /// <summary>Gets or sets the workspace-relative assembly path.</summary>
        public string AssemblyPath { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional workspace-relative PDB path.</summary>
        public string? PdbPath { get; set; }

        /// <summary>Gets or sets non-recursive P7A search roots.</summary>
        public List<string> ReferenceRoots { get; set; } = new();

        /// <summary>Gets or sets the expected terminal classification.</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EvaluationExpectedOutcome ExpectedOutcome { get; set; }

        /// <summary>Gets or sets the selection rationale.</summary>
        public string SelectionReason { get; set; } = string.Empty;

        /// <summary>Gets or sets covered evaluation categories.</summary>
        public List<string> Categories { get; set; } = new();

        /// <summary>Gets or sets an optional source-backed analysis probe.</summary>
        public EvaluationProbe? Probe { get; set; }
    }

    /// <summary>
    /// Describes one small consumer used by the existing exception-flow analyzer.
    /// </summary>
    public sealed class EvaluationProbe
    {
        /// <summary>Gets or sets the complete consumer source.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>Gets or sets the consumer method name.</summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>Gets or sets the manually expected exception metadata name.</summary>
        public string ExpectedExceptionType { get; set; } = string.Empty;

        /// <summary>Gets or sets the concise manually verified call path.</summary>
        public string VerifiedFlow { get; set; } = string.Empty;
    }

    /// <summary>
    /// Defines whether full reconstruction or a known fail-closed result is expected.
    /// </summary>
    public enum EvaluationExpectedOutcome
    {
        /// <summary>Every P3-P7 stage and the analysis probe should succeed.</summary>
        Success,

        /// <summary>The candidate should fail closed at a documented boundary.</summary>
        ExpectedFailClosed
    }
}
