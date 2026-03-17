using System.Text.Json;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Reporting.Json;

namespace XMLDocNormalizerTests.Reporting.Json
{
    /// <summary>
    /// Tests for JSON serialization of exception-analysis-mode comparison reports.
    /// </summary>
    public sealed class ExceptionAnalysisModeComparisonReportWriterTests
    {
        /// <summary>
        /// Ensures that the comparison JSON contains all expected timing fields for the shared phase
        /// and for each exception-analysis mode.
        /// </summary>
        [Fact]
        public void Write_WithTimingInformation_WritesAllExpectedTimingFields()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                ExceptionAnalysisModeComparisonReportDto report = CreateReport();

                ExceptionAnalysisModeComparisonReportWriter.Write(outputPath, report);

                string json = File.ReadAllText(outputPath);
                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement timings = doc.RootElement.GetProperty("Timings");

                Assert.Equal(13049L, timings.GetProperty("SharedDetectorsDurationMs").GetInt64());
                Assert.Equal(761L, timings.GetProperty("DirectExceptionDurationMs").GetInt64());
                Assert.Equal(2100L, timings.GetProperty("ProjectTransitiveExceptionDurationMs").GetInt64());
                Assert.Equal(2400L, timings.GetProperty("ProjectTransitiveProjectExceptionsExceptionDurationMs").GetInt64());
                Assert.Equal(2600L, timings.GetProperty("SolutionTransitiveExceptionDurationMs").GetInt64());
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Ensures that the comparison JSON keeps shared data and per-mode data in separate top-level sections.
        /// </summary>
        [Fact]
        public void Write_WithSharedAndModeData_WritesExpectedTopLevelStructure()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                ExceptionAnalysisModeComparisonReportDto report = CreateReport();

                ExceptionAnalysisModeComparisonReportWriter.Write(outputPath, report);

                string json = File.ReadAllText(outputPath);
                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement root = doc.RootElement;

                Assert.True(root.TryGetProperty("SharedMetrics", out JsonElement sharedMetrics));
                Assert.True(root.TryGetProperty("SharedFindingCounts", out JsonElement sharedFindingCounts));
                Assert.True(root.TryGetProperty("Timings", out JsonElement timings));
                Assert.True(root.TryGetProperty("Modes", out JsonElement modes));

                Assert.Equal(1200, sharedMetrics.GetProperty("Sloc").GetInt32());
                Assert.Equal(3, sharedFindingCounts.GetProperty("DOC200").GetInt32());
                Assert.Equal(JsonValueKind.Array, modes.ValueKind);
                Assert.Equal(4, modes.GetArrayLength());

                JsonElement firstMode = modes[0];
                Assert.Equal(nameof(ExceptionAnalysisMode.Direct), firstMode.GetProperty("Mode").GetString());
                Assert.Equal("artifacts/xcompare-test_direct.json", firstMode.GetProperty("ReportPath").GetString());
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Creates a representative comparison report DTO for serialization tests.
        /// </summary>
        /// <returns>A populated comparison report DTO.</returns>
        private static ExceptionAnalysisModeComparisonReportDto CreateReport()
        {
            return new ExceptionAnalysisModeComparisonReportDto
            {
                Tool = "XMLDocNormalizer",
                Version = "1.0.0-test",
                GeneratedAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc),
                TargetPath = "TestTarget",
                SharedMetrics = new ExceptionAnalysisModeSharedMetricsDto
                {
                    Sloc = 1200,
                    Totals = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        ["MethodsTotal"] = 12
                    },
                    Coverage = new Dictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["ReturnsMissingRate"] = 0.25
                    }
                },
                SharedFindingCounts = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    ["DOC200"] = 3
                },
                Timings = new ExceptionAnalysisModeTimingDto
                {
                    SharedDetectorsDurationMs = 13049,
                    DirectExceptionDurationMs = 761,
                    ProjectTransitiveExceptionDurationMs = 2100,
                    ProjectTransitiveProjectExceptionsExceptionDurationMs = 2400,
                    SolutionTransitiveExceptionDurationMs = 2600
                },
                Modes = new List<ExceptionAnalysisModeRunDto>
                {
                    CreateModeRun(ExceptionAnalysisMode.Direct, "artifacts/xcompare-test_direct.json"),
                    CreateModeRun(ExceptionAnalysisMode.ProjectTransitive, "artifacts/xcompare-test_project-transitive.json"),
                    CreateModeRun(ExceptionAnalysisMode.ProjectTransitiveProjectExceptions, "artifacts/xcompare-test_project-transitive-project-exceptions.json"),
                    CreateModeRun(ExceptionAnalysisMode.SolutionTransitive, "artifacts/xcompare-test_solution-transitive.json")
                }
            };
        }

        /// <summary>
        /// Creates one representative per-mode DTO.
        /// </summary>
        /// <param name="mode">The exception-analysis mode.</param>
        /// <param name="reportPath">The per-mode report path.</param>
        /// <returns>A populated mode DTO.</returns>
        private static ExceptionAnalysisModeRunDto CreateModeRun(ExceptionAnalysisMode mode, string reportPath)
        {
            return new ExceptionAnalysisModeRunDto
            {
                Mode = mode,
                ReportPath = reportPath,
                FindingCount = 10,
                ErrorCount = 1,
                WarningCount = 8,
                SuggestionCount = 1,
                FindingsPerKLoc = 8.33,
                ErrorsPerKLoc = 0.83,
                WarningsPerKLoc = 6.67,
                SuggestionsPerKLoc = 0.83,
                ExceptionFindingCount = 4,
                ExceptionFindingsPerKLoc = 3.33,
                Doc610Count = 1,
                Doc611Count = 1,
                Doc620Count = 0,
                Doc630Count = 0,
                Doc631Count = 1,
                Doc632Count = 1,
                Doc640Count = 0,
                Doc660Count = 0,
                Doc670Count = 0,
                Doc680Count = 0
            };
        }

        /// <summary>
        /// Creates a unique temporary file path.
        /// </summary>
        /// <param name="extension">The desired file extension including the dot.</param>
        /// <returns>A unique temporary file path.</returns>
        private static string CreateTempFilePath(string extension)
        {
            string fileName = Guid.NewGuid().ToString("N") + extension;
            return Path.Combine(Path.GetTempPath(), fileName);
        }

        /// <summary>
        /// Deletes the specified file if it exists.
        /// </summary>
        /// <param name="path">The file path to delete.</param>
        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
