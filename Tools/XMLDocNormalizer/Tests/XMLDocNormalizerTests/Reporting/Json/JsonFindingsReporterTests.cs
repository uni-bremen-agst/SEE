using System.Text.Json;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Json;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Reporting.Json
{
    /// <summary>
    /// Integration-style tests for <see cref="JsonFindingsReporter"/>.
    /// </summary>
    public sealed class JsonFindingsReporterTests
    {
        /// <summary>
        /// Ensures that JSON output is written and contains the aggregated run metrics block.
        /// </summary>
        /// <remarks>
        /// Counts in <see cref="RunResult"/> are derived via <see cref="RunResult.AccumulateFindings"/>
        /// and are not directly settable. Therefore this test constructs the result counters by accumulating
        /// findings of different severities.
        /// </remarks>
        [Fact]
        public void Complete_WithAggregatedRunResult_WritesJsonContainingMetrics()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                JsonFindingsReporter reporter = new JsonFindingsReporter(
                    outputPath: outputPath,
                    targetPath: "TestTarget");

                List<Finding> reportedFindings = new List<Finding>
                {
                    TestFindingFactory.Create(
                        smellId: "DOC100",
                        severity: Severity.Warning,
                        filePath: "A.cs",
                        tagName: "documentation")
                };

                reporter.ReportFile("A.cs", reportedFindings);

                RunResult result = CreateRunResult(
                    sloc: 2000,
                    errorCount: 2,
                    warningCount: 7,
                    suggestionCount: 1);

                IResultAwareFindingsReporter resultAware = (IResultAwareFindingsReporter)reporter;
                resultAware.Complete(result);

                Assert.True(File.Exists(outputPath));

                string json = File.ReadAllText(outputPath);

                Assert.Contains("DOC100", json, StringComparison.Ordinal);
                Assert.Contains("A.cs", json, StringComparison.Ordinal);

                EnsureJsonIsValid(json);
                AssertJsonContainsMetrics(json, expectedSloc: 2000, expectedFindings: 10, expectedErrors: 2, expectedWarnings: 7, expectedSuggestions: 1);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Creates a <see cref="RunResult"/> whose counters are derived by accumulating findings.
        /// </summary>
        /// <param name="sloc">The SLOC to set on the result.</param>
        /// <param name="errorCount">The number of error findings to accumulate.</param>
        /// <param name="warningCount">The number of warning findings to accumulate.</param>
        /// <param name="suggestionCount">The number of suggestion findings to accumulate.</param>
        /// <returns>A <see cref="RunResult"/> populated with the specified counters and SLOC.</returns>
        private static RunResult CreateRunResult(int sloc, int errorCount, int warningCount, int suggestionCount)
        {
            RunResult result = new RunResult();
            result.Sloc = sloc;

            List<Finding> all = new List<Finding>();
            all.AddRange(CreateFindings(errorCount, Severity.Error));
            all.AddRange(CreateFindings(warningCount, Severity.Warning));
            all.AddRange(CreateFindings(suggestionCount, Severity.Suggestion));

            result.AccumulateFindings(all);
            return result;
        }

        /// <summary>
        /// Creates a list of findings with the specified severity.
        /// </summary>
        /// <param name="count">The number of findings to create.</param>
        /// <param name="severity">The severity of the findings.</param>
        /// <returns>A list of findings.</returns>
        private static List<Finding> CreateFindings(int count, Severity severity)
        {
            List<Finding> list = new List<Finding>();

            for (int i = 0; i < count; i++)
            {
                list.Add(TestFindingFactory.Create(
                    smellId: "DOCX",
                    severity: severity,
                    filePath: "X.cs",
                    tagName: "summary"));
            }

            return list;
        }

        /// <summary>
        /// Ensures that the JSON is syntactically valid.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        private static void EnsureJsonIsValid(string json)
        {
            using JsonDocument _ = JsonDocument.Parse(json);
        }

        /// <summary>
        /// Asserts that the JSON report contains the expected metrics block and values.
        /// </summary>
        /// <param name="json">The report JSON.</param>
        /// <param name="expectedSloc">Expected SLOC value.</param>
        /// <param name="expectedFindings">Expected total findings.</param>
        /// <param name="expectedErrors">Expected error count.</param>
        /// <param name="expectedWarnings">Expected warning count.</param>
        /// <param name="expectedSuggestions">Expected suggestion count.</param>
        private static void AssertJsonContainsMetrics(
            string json,
            int expectedSloc,
            int expectedFindings,
            int expectedErrors,
            int expectedWarnings,
            int expectedSuggestions)
        {
            using JsonDocument doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;

            Assert.True(root.TryGetProperty("Metrics", out JsonElement metrics));

            Assert.Equal(expectedSloc, metrics.GetProperty("Sloc").GetInt32());
            Assert.Equal(expectedFindings, metrics.GetProperty("FindingCount").GetInt32());
            Assert.Equal(expectedErrors, metrics.GetProperty("ErrorCount").GetInt32());
            Assert.Equal(expectedWarnings, metrics.GetProperty("WarningCount").GetInt32());
            Assert.Equal(expectedSuggestions, metrics.GetProperty("SuggestionCount").GetInt32());

            // For 2000 SLOC the divisor is 2.0 KLOC.
            double expectedFindingsPerKLoc = expectedFindings / 2.0;
            double expectedErrorsPerKLoc = expectedErrors / 2.0;
            double expectedWarningsPerKLoc = expectedWarnings / 2.0;
            double expectedSuggestionsPerKLoc = expectedSuggestions / 2.0;

            Assert.Equal(expectedFindingsPerKLoc, metrics.GetProperty("FindingsPerKLoc").GetDouble(), precision: 6);
            Assert.Equal(expectedErrorsPerKLoc, metrics.GetProperty("ErrorsPerKLoc").GetDouble(), precision: 6);
            Assert.Equal(expectedWarningsPerKLoc, metrics.GetProperty("WarningsPerKLoc").GetDouble(), precision: 6);
            Assert.Equal(expectedSuggestionsPerKLoc, metrics.GetProperty("SuggestionsPerKLoc").GetDouble(), precision: 6);
        }

        /// <summary>
        /// Creates a unique temp file path with the specified extension.
        /// </summary>
        /// <param name="extension">File extension including dot.</param>
        /// <returns>A unique file path.</returns>
        private static string CreateTempFilePath(string extension)
        {
            string fileName = Guid.NewGuid().ToString("N") + extension;
            return Path.Combine(Path.GetTempPath(), fileName);
        }

        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="path">File path.</param>
        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}