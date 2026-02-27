using System.Text.Json.Nodes;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Sarif;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Reporting.Sarif
{
    /// <summary>
    /// Integration-style tests for <see cref="SarifFindingsReporter"/>.
    /// </summary>
    public sealed class SarifFindingsReporterTests
    {
        /// <summary>
        /// Ensures that SARIF output is written and contains aggregated run metrics under runs[0].properties.metrics.
        /// </summary>
        /// <remarks>
        /// Counts in <see cref="RunResult"/> are derived via <see cref="RunResult.AccumulateFindings"/>.
        /// This test constructs the result by accumulating findings of different severities.
        /// </remarks>
        [Fact]
        public void Complete_WithAggregatedRunResult_WritesSarifContainingMetrics()
        {
            string outputPath = CreateTempFilePath(".sarif");

            try
            {
                SarifFindingsReporter reporter = new SarifFindingsReporter(outputPath);

                List<Finding> reportedFindings = new List<Finding>
                {
                    TestFindingFactory.Create(
                        smellId: "DOC610",
                        severity: Severity.Error,
                        filePath: "B.cs",
                        tagName: "exception")
                };

                reporter.ReportFile("B.cs", reportedFindings);

                RunResult result = CreateRunResult(
                    sloc: 1000,
                    errorCount: 1,
                    warningCount: 2,
                    suggestionCount: 1);

                IResultAwareFindingsReporter resultAware = (IResultAwareFindingsReporter)reporter;
                resultAware.Complete(result);

                Assert.True(File.Exists(outputPath));

                string sarifJson = File.ReadAllText(outputPath);

                JsonNode? root = JsonNode.Parse(sarifJson);
                Assert.NotNull(root);

                Assert.Equal("2.1.0", (string?)root!["version"]);
                Assert.False(string.IsNullOrWhiteSpace((string?)root["$schema"]));

                JsonArray? runs = root["runs"] as JsonArray;
                Assert.NotNull(runs);
                Assert.True(runs!.Count > 0);

                JsonObject? firstRun = runs[0] as JsonObject;
                Assert.NotNull(firstRun);

                JsonArray? results = firstRun!["results"] as JsonArray;
                Assert.NotNull(results);
                Assert.True(results!.Count > 0);

                JsonObject? firstResult = results[0] as JsonObject;
                Assert.NotNull(firstResult);
                Assert.Equal("DOC610", (string?)firstResult!["ruleId"]);

                AssertSarifContainsMetrics(firstRun, expectedSloc: 1000, expectedFindings: 4, expectedErrors: 1, expectedWarnings: 2, expectedSuggestions: 1);
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
        /// Asserts that the SARIF run contains the expected metrics under properties.metrics.
        /// </summary>
        /// <param name="run">The SARIF run JSON object.</param>
        /// <param name="expectedSloc">Expected SLOC value.</param>
        /// <param name="expectedFindings">Expected total findings.</param>
        /// <param name="expectedErrors">Expected error count.</param>
        /// <param name="expectedWarnings">Expected warning count.</param>
        /// <param name="expectedSuggestions">Expected suggestion count.</param>
        private static void AssertSarifContainsMetrics(
            JsonObject run,
            int expectedSloc,
            int expectedFindings,
            int expectedErrors,
            int expectedWarnings,
            int expectedSuggestions)
        {
            JsonObject? properties = run["properties"] as JsonObject;
            Assert.NotNull(properties);

            JsonObject? metrics = properties!["metrics"] as JsonObject;
            Assert.NotNull(metrics);

            // SARIF writer typically uses camelCase naming.
            Assert.Equal(expectedSloc, (int?)metrics!["sloc"]);
            Assert.Equal(expectedFindings, (int?)metrics["findingCount"]);
            Assert.Equal(expectedErrors, (int?)metrics["errorCount"]);
            Assert.Equal(expectedWarnings, (int?)metrics["warningCount"]);
            Assert.Equal(expectedSuggestions, (int?)metrics["suggestionCount"]);

            // For 1000 SLOC, per-KLOC equals the absolute counts.
            Assert.Equal((double)expectedFindings, (double?)metrics["findingsPerKLoc"]);
            Assert.Equal((double)expectedErrors, (double?)metrics["errorsPerKLoc"]);
            Assert.Equal((double)expectedWarnings, (double?)metrics["warningsPerKLoc"]);
            Assert.Equal((double)expectedSuggestions, (double?)metrics["suggestionsPerKLoc"]);
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