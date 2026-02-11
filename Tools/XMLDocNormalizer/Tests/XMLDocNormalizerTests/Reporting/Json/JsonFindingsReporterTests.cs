using System.Text.Json;
using XMLDocNormalizer.Models;
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
        /// Ensures that <see cref="JsonFindingsReporter.Complete"/> writes a JSON file
        /// that contains the expected smell id and file path.
        /// </summary>
        [Fact]
        public void Complete_WithSingleFinding_WritesJsonFileContainingExpectedData()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                JsonFindingsReporter reporter = new JsonFindingsReporter(
                    outputPath: outputPath,
                    targetPath: "TestTarget");

                List<Finding> findings = new List<Finding>
                {
                    TestFindingFactory.Create("DOC100", Severity.Error, filePath: "A.cs", tagName: "documentation")
                };

                reporter.ReportFile("A.cs", findings);
                reporter.Complete();

                Assert.True(File.Exists(outputPath));

                string json = File.ReadAllText(outputPath);

                Assert.Contains("DOC100", json, StringComparison.Ordinal);
                Assert.Contains("A.cs", json, StringComparison.Ordinal);

                EnsureJsonIsValid(json);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Ensures that JSON output is syntactically valid.
        /// </summary>
        /// <param name="json">The JSON string to validate.</param>
        private static void EnsureJsonIsValid(string json)
        {
            JsonDocument? _ = JsonDocument.Parse(json);
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
