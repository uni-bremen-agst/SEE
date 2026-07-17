using System.Text.Json;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Reporting.Json;

namespace XMLDocNormalizerTests.Reporting.Json
{
    /// <summary>
    /// Tests JSON serialization of value-documentation mode comparison reports.
    /// </summary>
    public sealed class ValueDocumentationModeComparisonReportWriterTests
    {
        /// <summary>
        /// Ensures that the comparison JSON writes the expected top-level structure.
        /// </summary>
        [Fact]
        public void Write_WithComparisonReport_WritesExpectedTopLevelStructure()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                ValueDocumentationModeComparisonReportDto report = CreateReport();

                ValueDocumentationModeComparisonReportWriter.Write(outputPath, report);

                string json = File.ReadAllText(outputPath);
                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement root = doc.RootElement;

                Assert.Equal("XMLDocNormalizer", root.GetProperty("Tool").GetString());
                Assert.Equal("1.0.0-test", root.GetProperty("Version").GetString());
                Assert.Equal("TestTarget", root.GetProperty("TargetPath").GetString());
                Assert.Equal(1200, root.GetProperty("Sloc").GetInt32());

                JsonElement modes = root.GetProperty("Modes");

                Assert.Equal(JsonValueKind.Array, modes.ValueKind);
                Assert.Equal(4, modes.GetArrayLength());
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Ensures that value-documentation modes are serialized as strings.
        /// </summary>
        [Fact]
        public void Write_WithEnumModes_WritesModeNamesAsStrings()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                ValueDocumentationModeComparisonReportDto report = CreateReport();

                ValueDocumentationModeComparisonReportWriter.Write(outputPath, report);

                string json = File.ReadAllText(outputPath);
                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement modes = doc.RootElement.GetProperty("Modes");

                Assert.Equal("AllReadableProperties", modes[0].GetProperty("Mode").GetString());
                Assert.Equal("ExcludeDtoLikeTypes", modes[1].GetProperty("Mode").GetString());
                Assert.Equal("IndexersOnly", modes[2].GetProperty("Mode").GetString());
                Assert.Equal("None", modes[3].GetProperty("Mode").GetString());
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Ensures that value-mode-specific counts are serialized.
        /// </summary>
        [Fact]
        public void Write_WithModeCounts_WritesValueComparisonCounts()
        {
            string outputPath = CreateTempFilePath(".json");

            try
            {
                ValueDocumentationModeComparisonReportDto report = CreateReport();

                ValueDocumentationModeComparisonReportWriter.Write(outputPath, report);

                string json = File.ReadAllText(outputPath);
                using JsonDocument doc = JsonDocument.Parse(json);

                JsonElement excludeDtoLikeMode = doc.RootElement.GetProperty("Modes")[1];

                Assert.Equal(421, excludeDtoLikeMode.GetProperty("FindingCount").GetInt32());
                Assert.Equal(352, excludeDtoLikeMode.GetProperty("WarningCount").GetInt32());
                Assert.Equal(69, excludeDtoLikeMode.GetProperty("SuggestionCount").GetInt32());
                Assert.Equal(53, excludeDtoLikeMode.GetProperty("MissingValueTagCount").GetInt32());
                Assert.Equal(143, excludeDtoLikeMode.GetProperty("SuppressedMissingValueTagCount").GetInt32());
                Assert.Equal(350.83, excludeDtoLikeMode.GetProperty("FindingsPerKSloc").GetDouble(), 2);
                Assert.Equal(57.5, excludeDtoLikeMode.GetProperty("SuggestionsPerKSloc").GetDouble(), 2);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
            }
        }

        /// <summary>
        /// Ensures that parent directories are created for the comparison report.
        /// </summary>
        [Fact]
        public void Write_WithNestedOutputPath_CreatesParentDirectory()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString("N"));

            string outputPath = Path.Combine(directory, "value-comparison.json");

            try
            {
                ValueDocumentationModeComparisonReportDto report = CreateReport();

                ValueDocumentationModeComparisonReportWriter.Write(outputPath, report);

                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        /// <summary>
        /// Creates a representative value-documentation mode comparison report.
        /// </summary>
        /// <returns>
        /// A populated comparison report DTO.
        /// </returns>
        private static ValueDocumentationModeComparisonReportDto CreateReport()
        {
            return new ValueDocumentationModeComparisonReportDto
            {
                Tool = "XMLDocNormalizer",
                Version = "1.0.0-test",
                GeneratedAtUtc = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc),
                TargetPath = "TestTarget",
                Sloc = 1200,
                Modes = new List<ValueDocumentationModeRunDto>
                {
                    CreateModeRun(
                        ValueDocumentationMode.AllReadableProperties,
                        findingCount: 564,
                        warningCount: 352,
                        suggestionCount: 212,
                        missingValueTagCount: 196,
                        suppressedMissingValueTagCount: 0),
                    CreateModeRun(
                        ValueDocumentationMode.ExcludeDtoLikeTypes,
                        findingCount: 421,
                        warningCount: 352,
                        suggestionCount: 69,
                        missingValueTagCount: 53,
                        suppressedMissingValueTagCount: 143),
                    CreateModeRun(
                        ValueDocumentationMode.IndexersOnly,
                        findingCount: 368,
                        warningCount: 352,
                        suggestionCount: 16,
                        missingValueTagCount: 0,
                        suppressedMissingValueTagCount: 196),
                    CreateModeRun(
                        ValueDocumentationMode.None,
                        findingCount: 368,
                        warningCount: 352,
                        suggestionCount: 16,
                        missingValueTagCount: 0,
                        suppressedMissingValueTagCount: 196)
                }
            };
        }

        /// <summary>
        /// Creates one representative mode run DTO.
        /// </summary>
        /// <param name="mode">
        /// The value-documentation mode.
        /// </param>
        /// <param name="findingCount">
        /// The finding count.
        /// </param>
        /// <param name="warningCount">
        /// The warning count.
        /// </param>
        /// <param name="suggestionCount">
        /// The suggestion count.
        /// </param>
        /// <param name="missingValueTagCount">
        /// The missing value-tag count.
        /// </param>
        /// <param name="suppressedMissingValueTagCount">
        /// The suppressed missing value-tag count.
        /// </param>
        /// <returns>
        /// A populated mode run DTO.
        /// </returns>
        private static ValueDocumentationModeRunDto CreateModeRun(
            ValueDocumentationMode mode,
            int findingCount,
            int warningCount,
            int suggestionCount,
            int missingValueTagCount,
            int suppressedMissingValueTagCount)
        {
            const int sloc = 1200;

            return new ValueDocumentationModeRunDto
            {
                Mode = mode,
                FindingCount = findingCount,
                ErrorCount = 0,
                WarningCount = warningCount,
                SuggestionCount = suggestionCount,
                MissingValueTagCount = missingValueTagCount,
                SuppressedMissingValueTagCount = suppressedMissingValueTagCount,
                FindingsPerKSloc = findingCount / (sloc / 1000.0),
                SuggestionsPerKSloc = suggestionCount / (sloc / 1000.0)
            };
        }

        /// <summary>
        /// Creates a unique temporary file path.
        /// </summary>
        /// <param name="extension">
        /// The desired file extension including the dot.
        /// </param>
        /// <returns>
        /// A unique temporary file path.
        /// </returns>
        private static string CreateTempFilePath(string extension)
        {
            string fileName = Guid.NewGuid().ToString("N") + extension;
            return Path.Combine(Path.GetTempPath(), fileName);
        }

        /// <summary>
        /// Deletes the specified file if it exists.
        /// </summary>
        /// <param name="path">
        /// The file path to delete.
        /// </param>
        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
