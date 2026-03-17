using System.Text.Json;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Execution
{
    /// <summary>
    /// Regression tests for compare-mode timing propagation in <see cref="ToolRunner"/>.
    /// </summary>
    [Collection("Console-dependent tests")]
    public sealed class ToolRunnerComparisonTimingRegressionTests
    {
        /// <summary>
        /// Ensures that a per-mode compare report writes the combined duration of the shared baseline
        /// and the mode-specific exception analysis before the JSON report is completed.
        /// </summary>
        [Fact]
        public void RunComparison_WritesPerModeDurationAsSharedPlusModeDuration()
        {
            string rootDirectory = CreateTempDirectory();

            try
            {
                string projectPath = CreateSingleProject(rootDirectory);
                string outputPath = Path.Combine(rootDirectory, "artifacts", "xcompare-test.json");

                ToolOptions options = CreateCompareOptions(projectPath, outputPath);

                _ = ExceptionAnalysisModeComparisonRunner.Run(options);

                string comparisonPath =
                    Path.Combine(rootDirectory, "artifacts", "xcompare-test_exception-analysis-mode-comparison.json");

                string directPath =
                    Path.Combine(rootDirectory, "artifacts", "xcompare-test_direct.json");

                Assert.True(File.Exists(comparisonPath), "The comparison JSON report was not written.");
                Assert.True(File.Exists(directPath), "The direct per-mode JSON report was not written.");

                using JsonDocument comparisonDoc = JsonDocument.Parse(File.ReadAllText(comparisonPath));
                using JsonDocument directDoc = JsonDocument.Parse(File.ReadAllText(directPath));

                JsonElement timings = comparisonDoc.RootElement.GetProperty("Timings");
                long sharedDurationMs = timings.GetProperty("SharedDetectorsDurationMs").GetInt64();
                long directExceptionDurationMs = timings.GetProperty("DirectExceptionDurationMs").GetInt64();

                JsonElement directMetrics = directDoc.RootElement.GetProperty("Metrics");
                long directAnalysisDurationMs = directMetrics.GetProperty("AnalysisDurationMs").GetInt64();
                double directAnalysisDurationPerKSloc =
                    directMetrics.GetProperty("AnalysisDurationMsPerKSloc").GetDouble();
                int sloc = directMetrics.GetProperty("Sloc").GetInt32();

                Assert.True(sharedDurationMs > 0, "The shared duration should be greater than zero.");
                Assert.True(directExceptionDurationMs > 0, "The direct-mode exception duration should be greater than zero.");

                Assert.Equal(
                    sharedDurationMs + directExceptionDurationMs,
                    directAnalysisDurationMs);

                double expectedPerKSloc = sloc > 0
                    ? directAnalysisDurationMs / (sloc / 1000.0)
                    : 0.0;

                Assert.Equal(expectedPerKSloc, directAnalysisDurationPerKSloc, precision: 6);
            }
            finally
            {
                DeleteDirectoryIfExists(rootDirectory);
            }
        }

        /// <summary>
        /// Creates compare-mode tool options for a JSON comparison run.
        /// </summary>
        /// <param name="projectPath">The project file to analyze.</param>
        /// <param name="outputPath">The base JSON output path.</param>
        /// <returns>A configured <see cref="ToolOptions"/> instance.</returns>
        private static ToolOptions CreateCompareOptions(string projectPath, string outputPath)
        {
            XmlDocOptions xmlDocOptions = new()
            {
                ExceptionAnalysisMode = ExceptionAnalysisMode.Direct
            };

            return new ToolOptions(
                targetPath: projectPath,
                checkOnly: true,
                cleanBackups: false,
                useTest: false,
                xmlDocOptions: xmlDocOptions,
                outputFormat: OutputFormat.Json,
                outputPath: outputPath,
                verbose: false,
                fullAnalysis: false,
                compareExceptionAnalysisModes: true);
        }

        /// <summary>
        /// Creates a temporary directory for the regression test.
        /// </summary>
        /// <returns>The created directory path.</returns>
        private static string CreateTempDirectory()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "XMLDocNormalizerTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Creates a minimal SDK-style C# project with one documented class whose method throws an exception.
        /// </summary>
        /// <param name="rootDirectory">The root directory where the project should be created.</param>
        /// <returns>The created project file path.</returns>
        private static string CreateSingleProject(string rootDirectory)
        {
            string projectDirectory = Path.Combine(rootDirectory, "SampleProject");
            Directory.CreateDirectory(projectDirectory);

            string projectPath = Path.Combine(projectDirectory, "SampleProject.csproj");
            string sourcePath = Path.Combine(projectDirectory, "SampleClass.cs");

            File.WriteAllText(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """);

            File.WriteAllText(
                sourcePath,
                """
                namespace SampleProject
                {
                    /// <summary>
                    /// Provides a simple throwing member for compare-mode timing tests.
                    /// </summary>
                    public sealed class SampleClass
                    {
                        /// <summary>
                        /// Throws an exception.
                        /// </summary>
                        public void DoWork()
                        {
                            throw new global::System.InvalidOperationException();
                        }
                    }
                }
                """);

            return projectPath;
        }

        /// <summary>
        /// Deletes the specified directory if it exists.
        /// </summary>
        /// <param name="path">The directory path to delete.</param>
        private static void DeleteDirectoryIfExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
