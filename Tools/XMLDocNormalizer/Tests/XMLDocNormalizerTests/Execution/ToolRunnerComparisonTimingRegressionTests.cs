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
        /// Ensures that comparison mode writes isolated child-process timing data and keeps
        /// the per-mode reported analysis duration aligned with the child JSON report.
        /// </summary>
        [Fact]
        public void RunComparison_WritesIsolatedProcessTimingData()
        {
            string rootDirectory = CreateTempDirectory();

            try
            {
                string projectPath = CreateSingleProject(rootDirectory);
                string outputPath = Path.Combine(rootDirectory, "artifacts", "xcompare-test.json");

                ToolOptions options = CreateCompareOptions(projectPath, outputPath, comparisonRuns: 1);

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

                Assert.Equal("Process", timings.GetProperty("ExecutionIsolation").GetString());
                Assert.Equal("Fixed", timings.GetProperty("ModeOrderStrategy").GetString());
                Assert.Equal(1, timings.GetProperty("RunCount").GetInt32());
                Assert.True(timings.GetProperty("IncludesProcessStartup").GetBoolean());
                Assert.Equal(0L, timings.GetProperty("SharedDetectorsDurationMs").GetInt64());

                long directWallClockDurationMs = timings.GetProperty("DirectWallClockDurationMs").GetInt64();
                long directReportedAnalysisDurationMs =
                    timings.GetProperty("DirectReportedAnalysisDurationMs").GetInt64();

                JsonElement directMetrics = directDoc.RootElement.GetProperty("Metrics");
                long directAnalysisDurationMs = directMetrics.GetProperty("AnalysisDurationMs").GetInt64();

                Assert.True(directWallClockDurationMs > 0, "The direct wall-clock duration should be greater than zero.");
                Assert.True(directReportedAnalysisDurationMs > 0, "The direct reported analysis duration should be greater than zero.");

                Assert.Equal(directAnalysisDurationMs, directReportedAnalysisDurationMs);
                Assert.Equal(directAnalysisDurationMs, timings.GetProperty("DirectExceptionDurationMs").GetInt64());

                JsonElement modes = comparisonDoc.RootElement.GetProperty("Modes");
                JsonElement directMode = modes.EnumerateArray().First(
                    mode => mode.GetProperty("Mode").GetString() == nameof(ExceptionAnalysisMode.Direct));

                Assert.Equal(1, directMode.GetProperty("RunCount").GetInt32());
                Assert.True(directMode.GetProperty("FindingCountsStableAcrossRuns").GetBoolean());
                Assert.Equal(directWallClockDurationMs, directMode.GetProperty("WallClockDurationMs").GetInt64());
                Assert.Equal(directAnalysisDurationMs, directMode.GetProperty("ReportedAnalysisDurationMs").GetInt64());
                Assert.Single(directMode.GetProperty("WallClockDurationsMs").EnumerateArray());
                Assert.Single(directMode.GetProperty("ReportedAnalysisDurationsMs").EnumerateArray());
            }
            finally
            {
                DeleteDirectoryIfExists(rootDirectory);
            }
        }

        /// <summary>
        /// Ensures that multiple comparison runs use rotating mode order and expose per-run timing arrays.
        /// </summary>
        [Fact]
        public void RunComparison_WithMultipleRuns_WritesRotatingTimingStatistics()
        {
            string rootDirectory = CreateTempDirectory();

            try
            {
                string projectPath = CreateSingleProject(rootDirectory);
                string outputPath = Path.Combine(rootDirectory, "artifacts", "xcompare-multi-test.json");

                ToolOptions options = CreateCompareOptions(projectPath, outputPath, comparisonRuns: 2);

                _ = ExceptionAnalysisModeComparisonRunner.Run(options);

                string comparisonPath =
                    Path.Combine(rootDirectory, "artifacts", "xcompare-multi-test_exception-analysis-mode-comparison.json");

                string directPath =
                    Path.Combine(rootDirectory, "artifacts", "xcompare-multi-test_direct.json");

                string secondDirectPath =
                    Path.Combine(rootDirectory, "artifacts", "xcompare-multi-test_direct_run-2.json");

                Assert.True(File.Exists(comparisonPath), "The comparison JSON report was not written.");
                Assert.True(File.Exists(directPath), "The first direct per-mode JSON report was not written.");
                Assert.True(File.Exists(secondDirectPath), "The second direct per-mode JSON report was not written.");

                using JsonDocument comparisonDoc = JsonDocument.Parse(File.ReadAllText(comparisonPath));

                JsonElement timings = comparisonDoc.RootElement.GetProperty("Timings");

                Assert.Equal("Process", timings.GetProperty("ExecutionIsolation").GetString());
                Assert.Equal("Rotating", timings.GetProperty("ModeOrderStrategy").GetString());
                Assert.Equal(2, timings.GetProperty("RunCount").GetInt32());

                Assert.Equal(2, timings.GetProperty("DirectWallClockDurationsMs").GetArrayLength());
                Assert.Equal(2, timings.GetProperty("DirectReportedAnalysisDurationsMs").GetArrayLength());

                JsonElement modes = comparisonDoc.RootElement.GetProperty("Modes");
                JsonElement directMode = modes.EnumerateArray().First(
                    mode => mode.GetProperty("Mode").GetString() == nameof(ExceptionAnalysisMode.Direct));

                Assert.Equal(2, directMode.GetProperty("RunCount").GetInt32());
                Assert.True(directMode.GetProperty("FindingCountsStableAcrossRuns").GetBoolean());
                Assert.Equal(2, directMode.GetProperty("ReportPaths").GetArrayLength());
                Assert.Equal(2, directMode.GetProperty("WallClockDurationsMs").GetArrayLength());
                Assert.Equal(2, directMode.GetProperty("ReportedAnalysisDurationsMs").GetArrayLength());
                Assert.True(directMode.GetProperty("MedianWallClockDurationMs").GetInt64() > 0);
                Assert.True(directMode.GetProperty("MeanWallClockDurationMs").GetDouble() > 0.0);
                Assert.True(directMode.GetProperty("MedianReportedAnalysisDurationMs").GetInt64() > 0);
                Assert.True(directMode.GetProperty("MeanReportedAnalysisDurationMs").GetDouble() > 0.0);
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
        /// <param name="comparisonRuns">The measured comparison run count per mode.</param>
        /// <returns>A configured <see cref="ToolOptions"/> instance.</returns>
        private static ToolOptions CreateCompareOptions(
            string projectPath,
            string outputPath,
            int comparisonRuns)
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
                compareExceptionAnalysisModes: true,
                exceptionAnalysisComparisonRuns: comparisonRuns);
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
