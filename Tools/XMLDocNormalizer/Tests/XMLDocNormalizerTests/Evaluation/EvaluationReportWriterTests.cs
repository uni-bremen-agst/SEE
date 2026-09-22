using System.Text.Json;
using XMLDocNormalizer.Evaluation;

namespace XMLDocNormalizerTests.Evaluation
{
    /// <summary>
    /// Tests deterministic offline evaluation reporting.
    /// </summary>
    public sealed class EvaluationReportWriterTests
    {
        /// <summary>
        /// Ensures JSON output sorts candidates and serializes failure enums by name.
        /// </summary>
        [Fact]
        public void Write_UnsortedCandidates_WritesStableJson()
        {
            string directory = CreateDirectory();

            try
            {
                EvaluationReport report = CreateReport();
                EvaluationReportWriter.Write(directory, report);
                using JsonDocument document = JsonDocument.Parse(
                    File.ReadAllText(Path.Combine(directory, "real-world-evaluation.json")));
                JsonElement candidates = document.RootElement.GetProperty("Candidates");

                Assert.Equal("a", candidates[0].GetProperty("Id").GetString());
                Assert.Equal("z", candidates[1].GetProperty("Id").GetString());
                Assert.Equal(
                    nameof(EvaluationFailureCategory.MissingArtifact),
                    candidates[0].GetProperty("FallbackCategory").GetString());
                Assert.Equal(
                    "LoadedReference",
                    candidates[0].GetProperty("References")[0]
                        .GetProperty("ArtifactSource").GetString());
                Assert.Equal(
                    1,
                    candidates[0].GetProperty("ReferenceDiscovery")
                        .GetProperty("ValidationAttempts").GetInt64());
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        /// <summary>
        /// Ensures Markdown contains the summary table and concise candidate detail.
        /// </summary>
        [Fact]
        public void CreateMarkdown_CompleteReport_ContainsStableSummary()
        {
            string markdown = EvaluationReportWriter.CreateMarkdown(CreateReport());

            Assert.Contains("| Candidate | Version | PDB | Source | Reconstruction | Analysis | Result |", markdown, StringComparison.Ordinal);
            Assert.Contains("- Candidates evaluated: 2", markdown, StringComparison.Ordinal);
            Assert.Contains("## PackageA 1.0.0 (net8.0)", markdown, StringComparison.Ordinal);
            Assert.Contains("MissingArtifact", markdown, StringComparison.Ordinal);
        }

        private static EvaluationReport CreateReport()
        {
            return new EvaluationReport
            {
                GeneratedAtUtc = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc),
                GitCommit = "0123456789abcdef",
                OperatingSystem = "TestOS",
                Runtime = ".NET Test",
                ProcessArchitecture = "X64",
                SourceLinkEnabled = true,
                Summary = new EvaluationSummary
                {
                    CandidatesEvaluated = 2,
                    FullReconstructionSuccesses = 1,
                    ExpectedFailClosedCases = 1
                },
                Candidates =
                [
                    new EvaluationCandidateResult
                    {
                        Id = "z",
                        Package = "PackageZ",
                        Version = "2.0.0",
                        TargetFramework = "net8.0",
                        PdbType = "Portable",
                        ReconstructionSucceeded = true,
                        ExpectedOutcomeObserved = true,
                        SourceOrigins = new SortedDictionary<string, int>(StringComparer.Ordinal)
                        {
                            ["SourceLink"] = 1
                        }
                    },
                    new EvaluationCandidateResult
                    {
                        Id = "a",
                        Package = "PackageA",
                        Version = "1.0.0",
                        TargetFramework = "net8.0",
                        FallbackStage = "PdbValidated",
                        FallbackCategory = EvaluationFailureCategory.MissingArtifact,
                        ExpectedOutcomeObserved = true,
                        References =
                        [
                            new EvaluationReferenceResult
                            {
                                Ordinal = 0,
                                Name = "PackageA.Dependency.dll",
                                ExactMatchFound = true,
                                ArtifactSource = "LoadedReference"
                            }
                        ],
                        ReferenceDiscovery = new EvaluationReferenceDiscoveryStatistics
                        {
                            CandidateFilesConsidered = 1,
                            CandidateFilesOpened = 1,
                            ValidationAttempts = 1
                        }
                    }
                ]
            };
        }

        private static string CreateDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
