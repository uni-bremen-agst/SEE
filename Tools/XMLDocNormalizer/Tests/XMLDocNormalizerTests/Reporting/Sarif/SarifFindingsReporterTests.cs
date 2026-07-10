using System.Text.Json.Nodes;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Sarif;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Reporting.Sarif
{
    /// <summary>
    /// Integration-style tests for SarifFindingsReporter.
    /// </summary>
    public sealed class SarifFindingsReporterTests
    {
        /// <summary>
        /// Ensures that SARIF output is written and contains aggregated run metrics under the first run properties.
        /// </summary>
        /// <remarks>
        /// Counts in RunResult are derived via RunResult.AccumulateFindings.
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
                    CreateFindingWithSarifRuleMetadata()
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

                AssertSarifRuleMetadataIsPlaceholderFree(firstRun!);

                JsonArray? results = firstRun!["results"] as JsonArray;
                Assert.NotNull(results);
                Assert.True(results!.Count > 0);

                JsonObject? firstResult = results[0] as JsonObject;
                Assert.NotNull(firstResult);
                Assert.Equal("DOC610", (string?)firstResult!["ruleId"]);

                AssertResultMessageIsConcreteAndPrefixFree(firstResult);

                AssertSarifContainsMetrics(
                    firstRun,
                    expectedSloc: 1000,
                    expectedFindings: 4,
                    expectedErrors: 1,
                    expectedWarnings: 2,
                    expectedSuggestions: 1);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Creates a finding whose smell has separate message template and SARIF rule metadata.
        /// </summary>
        /// <returns>A deterministic finding.</returns>
        private static Finding CreateFindingWithSarifRuleMetadata()
        {
            XmlDocSmell smell = new XmlDocSmell(
                id: "DOC610",
                messageTemplate: "Missing <exception> documentation for exception '{0}'.",
                severity: Severity.Error,
                ruleTitle: "Missing exception documentation",
                ruleDescription: "Reports thrown exceptions that are not documented with exception documentation.");

            return new Finding(
                smell: smell,
                filePath: "B.cs",
                tagName: "exception",
                line: 10,
                column: 5,
                snippet: "<exception cref=\"System.InvalidOperationException\">Thrown on invalid operation.</exception>",
                context: FindingContext.Unknown,
                "System.InvalidOperationException");
        }

        /// <summary>
        /// Creates a RunResult whose counters are derived by accumulating findings.
        /// </summary>
        /// <param name="sloc">The SLOC to set on the result.</param>
        /// <param name="errorCount">The number of error findings to accumulate.</param>
        /// <param name="warningCount">The number of warning findings to accumulate.</param>
        /// <param name="suggestionCount">The number of suggestion findings to accumulate.</param>
        /// <returns>A RunResult populated with the specified counters and SLOC.</returns>
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
        /// Asserts that SARIF rule metadata uses placeholder-free rule text instead of finding message templates.
        /// </summary>
        /// <param name="run">The SARIF run JSON object.</param>
        private static void AssertSarifRuleMetadataIsPlaceholderFree(JsonObject run)
        {
            JsonObject? tool = run["tool"] as JsonObject;
            Assert.NotNull(tool);

            JsonObject? driver = tool!["driver"] as JsonObject;
            Assert.NotNull(driver);

            JsonArray? rules = driver!["rules"] as JsonArray;
            Assert.NotNull(rules);
            Assert.True(rules!.Count > 0);

            JsonObject rule = FindRule(rules, "DOC610");

            JsonObject? shortDescription = rule["shortDescription"] as JsonObject;
            Assert.NotNull(shortDescription);
            Assert.Equal("Missing exception documentation", (string?)shortDescription!["text"]);

            JsonObject? fullDescription = rule["fullDescription"] as JsonObject;
            Assert.NotNull(fullDescription);
            Assert.Equal(
                "Reports thrown exceptions that are not documented with exception documentation.",
                (string?)fullDescription!["text"]);

            string ruleJson = rule.ToJsonString();

            Assert.False(
                ruleJson.Contains("{0}", StringComparison.Ordinal),
                "SARIF rule metadata should not contain message template placeholder {0}.");

            Assert.False(
                ruleJson.Contains("{1}", StringComparison.Ordinal),
                "SARIF rule metadata should not contain message template placeholder {1}.");

            Assert.False(
                ruleJson.Contains("{2}", StringComparison.Ordinal),
                "SARIF rule metadata should not contain message template placeholder {2}.");
        }

        /// <summary>
        /// Finds a SARIF rule by id.
        /// </summary>
        /// <param name="rules">The SARIF rules array.</param>
        /// <param name="ruleId">The rule id to find.</param>
        /// <returns>The matching SARIF rule object.</returns>
        private static JsonObject FindRule(JsonArray rules, string ruleId)
        {
            foreach (JsonNode? ruleNode in rules)
            {
                JsonObject? rule = ruleNode as JsonObject;

                if (rule == null)
                {
                    continue;
                }

                if ((string?)rule["id"] == ruleId)
                {
                    return rule;
                }
            }

            throw new InvalidOperationException("Expected SARIF rule was not found: " + ruleId);
        }

        /// <summary>
        /// Asserts that a SARIF result message contains the concrete finding message without an artificial tag prefix.
        /// </summary>
        /// <param name="result">The SARIF result JSON object.</param>
        private static void AssertResultMessageIsConcreteAndPrefixFree(JsonObject result)
        {
            JsonObject? message = result["message"] as JsonObject;
            Assert.NotNull(message);

            string? messageText = (string?)message!["text"];
            Assert.NotNull(messageText);

            Assert.Equal("Missing <exception> documentation for exception 'System.InvalidOperationException'.", messageText);

            Assert.False(
                messageText!.StartsWith("<exception> ", StringComparison.Ordinal),
                "SARIF result messages should not contain an artificial XML tag prefix.");

            Assert.False(
                messageText.Contains("{0}", StringComparison.Ordinal),
                "SARIF result messages should be concrete and must not contain message template placeholders.");
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

            Assert.Equal(expectedSloc, (int?)metrics!["sloc"]);
            Assert.Equal(expectedFindings, (int?)metrics["findingCount"]);
            Assert.Equal(expectedErrors, (int?)metrics["errorCount"]);
            Assert.Equal(expectedWarnings, (int?)metrics["warningCount"]);
            Assert.Equal(expectedSuggestions, (int?)metrics["suggestionCount"]);

            Assert.Equal((double)expectedFindings, (double?)metrics["findingsPerKSloc"]);
            Assert.Equal((double)expectedErrors, (double?)metrics["errorsPerKSloc"]);
            Assert.Equal((double)expectedWarnings, (double?)metrics["warningsPerKSloc"]);
            Assert.Equal((double)expectedSuggestions, (double?)metrics["suggestionsPerKSloc"]);
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
