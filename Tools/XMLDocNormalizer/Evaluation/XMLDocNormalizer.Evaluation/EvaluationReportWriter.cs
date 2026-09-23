using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Evaluation
{
    /// <summary>
    /// Writes stable machine-readable and human-readable E1 reports.
    /// </summary>
    public static class EvaluationReportWriter
    {
        /// <summary>
        /// Writes both reports to one explicit output directory.
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="report">The completed report.</param>
        public static void Write(string outputDirectory, EvaluationReport report)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
            ArgumentNullException.ThrowIfNull(report);
            Directory.CreateDirectory(outputDirectory);
            report.Candidates = report.Candidates
                .OrderBy(static candidate => candidate.Id, StringComparer.Ordinal)
                .ToList();
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };
            File.WriteAllText(
                Path.Combine(outputDirectory, "real-world-evaluation.json"),
                JsonSerializer.Serialize(report, options));
            File.WriteAllText(
                Path.Combine(outputDirectory, "real-world-evaluation.md"),
                CreateMarkdown(report));
        }

        /// <summary>
        /// Creates the stable Markdown summary used by tests and the file writer.
        /// </summary>
        /// <param name="report">The completed report.</param>
        /// <returns>The human-readable report.</returns>
        public static string CreateMarkdown(EvaluationReport report)
        {
            ArgumentNullException.ThrowIfNull(report);
            StringBuilder builder = new();
            builder.AppendLine("# XMLDocNormalizer real-world evaluation");
            builder.AppendLine();
            builder.AppendLine($"- Commit: `{report.GitCommit}`");
            builder.AppendLine($"- Generated: `{report.GeneratedAtUtc:O}`");
            builder.AppendLine($"- Runtime: `{report.Runtime}` ({report.ProcessArchitecture})");
            builder.AppendLine($"- Source Link enabled: `{report.SourceLinkEnabled}`");
            builder.AppendLine($"- Source reconstruction: `{report.SourceReconstructionPolicy}`");
            builder.AppendLine();
            builder.AppendLine("| Candidate | Version | PDB | Source | Reconstruction | Analysis | Result |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");

            foreach (EvaluationCandidateResult candidate in report.Candidates.OrderBy(
                         static candidate => candidate.Id,
                         StringComparer.Ordinal))
            {
                string origins = candidate.SourceOrigins.Count == 0
                    ? "none"
                    : string.Join("+", candidate.SourceOrigins.Keys);
                string reconstruction = candidate.ReconstructionSucceeded
                    ? "Success"
                    : $"Fallback ({candidate.FallbackStage})";
                string analysis = candidate.SourceBackedFindings.Count > 0
                    ? "source-backed"
                    : "metadata";
                string result = candidate.ExpectedOutcomeObserved ? "Expected" : "Unexpected";
                builder.AppendLine(
                    $"| {candidate.Package} | {candidate.Version} | {candidate.PdbType} | "
                    + $"{origins} | {reconstruction} | {analysis} | {result} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine($"- Candidates evaluated: {report.Summary.CandidatesEvaluated}");
            builder.AppendLine($"- Full reconstructions: {report.Summary.FullReconstructionSuccesses}");
            builder.AppendLine($"- Expected fail-closed: {report.Summary.ExpectedFailClosedCases}");
            builder.AppendLine($"- Unexpected failures: {report.Summary.UnexpectedFailures}");
            builder.AppendLine($"- Potential bugs: {report.Summary.PotentialBugs}");

            foreach (EvaluationCandidateResult candidate in report.Candidates.OrderBy(
                         static candidate => candidate.Id,
                         StringComparer.Ordinal))
            {
                builder.AppendLine();
                builder.AppendLine($"## {candidate.Package} {candidate.Version} ({candidate.TargetFramework})");
                builder.AppendLine();
                builder.AppendLine($"- Reconstruction: {candidate.ReconstructionSucceeded}");
                builder.AppendLine($"- Fallback: {candidate.FallbackStage ?? "none"} / {candidate.FallbackCategory?.ToString() ?? "none"}");
                builder.AppendLine($"- Reason: {candidate.FallbackReason ?? "none"}");
                builder.AppendLine($"- Findings: {candidate.BaselineFindings.Count} baseline, {candidate.SourceBackedFindings.Count} source-backed, {candidate.AddedFindings.Count} added, {candidate.RemovedFindings.Count} removed, {candidate.ChangedExceptionEvidence.Count} changed exception evidence");
                builder.AppendLine($"- Trees/references: {candidate.SourceTreeCount}/{candidate.ReferenceCount}");
                builder.AppendLine($"- Source exactness: {candidate.ExpectedSourceFileCount} expected, {candidate.DirectExactSourceCount} direct, {candidate.ReconstructedExactSourceCount} reconstructed, {candidate.UnavailableSourceCount} unavailable");
                builder.AppendLine($"- Source reconstruction work: {candidate.SourceAcquisition.ReconstructionAttempts} attempts, {candidate.SourceAcquisition.ReconstructionSuccesses} successes, {candidate.SourceAcquisition.ReconstructionFailures} failures, {candidate.SourceAcquisition.LfToCrlfSuccesses} LF→CRLF, {candidate.SourceAcquisition.CrlfToLfSuccesses} CRLF→LF, {candidate.SourceAcquisition.P5HValidationAttempts} P5H attempts, {candidate.SourceAcquisition.ReconstructionBytesProduced} candidate bytes, {candidate.SourceAcquisition.ReconstructionDurationTicks} timer ticks");
                builder.AppendLine($"- Source acquisition: {candidate.SourceAcquisition.SourceLinkRequests} requests, {candidate.SourceAcquisition.DownloadedSourceBytes} downloaded bytes, {candidate.SourceAcquisition.PositiveCacheHits} positive cache hits, {candidate.SourceAcquisition.NegativeCacheHits} negative cache hits");
                builder.AppendLine($"- PDB origin: {candidate.PdbOrigin ?? "none"}");
                builder.AppendLine($"- PDB acquisition: {candidate.PdbAcquisition.CandidatesConsidered} candidates, {candidate.PdbAcquisition.CandidatesOpened} opens, {candidate.PdbAcquisition.LocalSymbolPackagesInspected} packages, {candidate.PdbAcquisition.RemoteSymbolRequests} requests, {candidate.PdbAcquisition.DownloadedPdbBytes} downloaded bytes, {candidate.PdbAcquisition.ValidationAttempts} P4B attempts, {candidate.PdbAcquisition.PositiveCacheHits} positive cache hits, {candidate.PdbAcquisition.NegativeCacheHits} negative cache hits");
                string referenceSources = string.Join(
                    ", ",
                    candidate.References
                        .Where(static reference => reference.ExactMatchFound)
                        .GroupBy(static reference => reference.ArtifactSource ?? "Unknown")
                        .OrderBy(static group => group.Key, StringComparer.Ordinal)
                        .Select(static group => $"{group.Key}={group.Count()}"));
                builder.AppendLine($"- Reference sources: {(referenceSources.Length == 0 ? "none" : referenceSources)}");
                builder.AppendLine($"- Reference search: {candidate.ReferenceDiscovery.ArtifactRootsExamined} roots, {candidate.ReferenceDiscovery.DirectoriesEnumerated} directory enumerations, {candidate.ReferenceDiscovery.CandidateFilesConsidered} candidates, {candidate.ReferenceDiscovery.CandidateFilesOpened} opens, {candidate.ReferenceDiscovery.ValidationAttempts} P5 attempts");
                builder.AppendLine($"- Diagnostics: {candidate.CompilerErrorCount} errors, {candidate.CompilerWarningCount} warnings");
                builder.AppendLine($"- Manual flow: {candidate.ManualVerification ?? "not applicable"}");
                builder.AppendLine($"- Duration: {candidate.DurationMs} ms");
            }

            return builder.ToString();
        }
    }
}
