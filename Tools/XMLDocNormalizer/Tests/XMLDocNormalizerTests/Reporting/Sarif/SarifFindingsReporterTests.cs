using System.Text.Json;
using System.Text.Json.Nodes;
using XMLDocNormalizer.Models;
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
        /// Ensures that <see cref="SarifFindingsReporter.Complete"/> writes a SARIF file
        /// that contains the required SARIF version and the expected rule id and file path.
        /// </summary>
        [Fact]
        public void Complete_WithSingleFinding_WritesSarifFileContainingExpectedData()
        {
            string outputPath = CreateTempFilePath(".sarif");

            try
            {
                SarifFindingsReporter reporter = new SarifFindingsReporter(outputPath);

                List<Finding> findings = new List<Finding>
                {
                    TestFindingFactory.Create("DOC610", Severity.Error, filePath: "B.cs", tagName: "exception")
                };

                reporter.ReportFile("B.cs", findings);
                reporter.Complete();

                Assert.True(File.Exists(outputPath));

                string sarifJson = File.ReadAllText(outputPath);

                JsonNode? root = JsonNode.Parse(sarifJson);
                Assert.NotNull(root);

                // Required SARIF top-level fields
                Assert.Equal("2.1.0", (string?)root!["version"]);
                Assert.False(string.IsNullOrWhiteSpace((string?)root["$schema"]));

                // runs[0].results[0].ruleId
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

                // runs[0].results[0].locations[0].physicalLocation.artifactLocation.uri contains file path
                JsonArray? locations = firstResult["locations"] as JsonArray;
                Assert.NotNull(locations);
                Assert.True(locations!.Count > 0);

                JsonObject? firstLocation = locations[0] as JsonObject;
                Assert.NotNull(firstLocation);

                JsonObject? physicalLocation = firstLocation!["physicalLocation"] as JsonObject;
                Assert.NotNull(physicalLocation);

                JsonObject? artifactLocation = physicalLocation!["artifactLocation"] as JsonObject;
                Assert.NotNull(artifactLocation);

                string? uri = (string?)artifactLocation!["uri"];
                Assert.False(string.IsNullOrWhiteSpace(uri));
                Assert.Contains("B.cs", uri, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
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
