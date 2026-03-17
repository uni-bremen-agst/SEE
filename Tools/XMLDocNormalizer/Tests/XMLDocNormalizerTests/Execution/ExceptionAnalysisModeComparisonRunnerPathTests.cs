using System.Reflection;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Execution;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution
{
    /// <summary>
    /// Tests for output-path normalization in <see cref="ExceptionAnalysisModeComparisonRunner"/>.
    /// </summary>
    public sealed class ExceptionAnalysisModeComparisonRunnerPathTests
    {
        /// <summary>
        /// Ensures that a mode-specific suffix is removed before the comparison suffix is appended.
        /// </summary>
        [Fact]
        public void ResolveComparisonOutputPath_WithKnownModeSuffix_RemovesSuffixBeforeAppendingComparisonSuffix()
        {
            ToolOptions options = TestToolOptionsFactory.Create(
                OutputFormat.Json,
                outputPath: Path.Combine("artifacts", "xcompare-test_direct.json"));

            string outputPath = InvokeResolveComparisonOutputPath(options);

            Assert.Equal(
                Path.Combine("artifacts", "xcompare-test_exception-analysis-mode-comparison.json"),
                outputPath);
        }

        /// <summary>
        /// Ensures that a plain JSON output path receives the comparison suffix unchanged except for the filename transformation.
        /// </summary>
        [Fact]
        public void ResolveComparisonOutputPath_WithoutModeSuffix_AppendsComparisonSuffix()
        {
            ToolOptions options = TestToolOptionsFactory.Create(
                OutputFormat.Json,
                outputPath: Path.Combine("artifacts", "xcompare-test.json"));

            string outputPath = InvokeResolveComparisonOutputPath(options);

            Assert.Equal(
                Path.Combine("artifacts", "xcompare-test_exception-analysis-mode-comparison.json"),
                outputPath);
        }

        /// <summary>
        /// Ensures that the default comparison output path is used when no explicit output path is configured.
        /// </summary>
        [Fact]
        public void ResolveComparisonOutputPath_WithoutConfiguredOutputPath_UsesDefaultComparisonPath()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Json, outputPath: null);

            string outputPath = InvokeResolveComparisonOutputPath(options);

            Assert.Equal("artifacts/exception-analysis-mode-comparison.json", outputPath);
        }

        /// <summary>
        /// Invokes the private comparison-output-path resolver via reflection.
        /// </summary>
        /// <param name="options">The tool options to pass to the resolver.</param>
        /// <returns>The resolved comparison output path.</returns>
        private static string InvokeResolveComparisonOutputPath(ToolOptions options)
        {
            MethodInfo? method = typeof(ExceptionAnalysisModeComparisonRunner).GetMethod(
                "ResolveComparisonOutputPath",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            object? result = method.Invoke(null, new object[] { options });

            string path = Assert.IsType<string>(result);
            return path;
        }
    }
}
