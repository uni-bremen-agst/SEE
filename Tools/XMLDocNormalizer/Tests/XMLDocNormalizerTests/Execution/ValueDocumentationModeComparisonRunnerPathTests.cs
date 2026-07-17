using System.Reflection;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution
{
    /// <summary>
    /// Tests output-path resolution for <see cref="ValueDocumentationModeComparisonRunner"/>.
    /// </summary>
    public sealed class ValueDocumentationModeComparisonRunnerPathTests
    {
        /// <summary>
        /// Ensures that an explicit comparison output path is used unchanged.
        /// </summary>
        [Fact]
        public void ResolveComparisonOutputPath_WithConfiguredOutputPath_UsesConfiguredPath()
        {
            string configuredPath = Path.Combine("artifacts", "value-mode-comparison.json");
            ToolOptions options = TestToolOptionsFactory.Create(
                OutputFormat.Json,
                outputPath: configuredPath);

            string outputPath = InvokeResolveComparisonOutputPath(options);

            Assert.Equal(configuredPath, outputPath);
        }

        /// <summary>
        /// Ensures that the default comparison output path is used when no explicit output path is configured.
        /// </summary>
        [Fact]
        public void ResolveComparisonOutputPath_WithoutConfiguredOutputPath_UsesDefaultComparisonPath()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Json, outputPath: null);

            string outputPath = InvokeResolveComparisonOutputPath(options);

            Assert.Equal("artifacts/value-documentation-mode-comparison.json", outputPath);
        }

        /// <summary>
        /// Ensures that a mode-specific report path is derived from the configured comparison output path.
        /// </summary>
        [Fact]
        public void ResolveModeReportPath_WithConfiguredOutputPath_AppendsModeSuffix()
        {
            ToolOptions options = TestToolOptionsFactory.Create(
                OutputFormat.Json,
                outputPath: Path.Combine("artifacts", "value-mode-comparison.json"));

            string outputPath = InvokeResolveModeReportPath(
                options,
                ValueDocumentationMode.ExcludeDtoLikeTypes);

            Assert.Equal(
                Path.Combine("artifacts", "value-mode-comparison_exclude-dto-like-types.json"),
                outputPath);
        }

        /// <summary>
        /// Ensures that the disabled value-documentation mode uses a stable file suffix.
        /// </summary>
        [Fact]
        public void ResolveModeReportPath_ForNoneMode_UsesDisabledSuffix()
        {
            ToolOptions options = TestToolOptionsFactory.Create(
                OutputFormat.Json,
                outputPath: Path.Combine("artifacts", "value-mode-comparison.json"));

            string outputPath = InvokeResolveModeReportPath(
                options,
                ValueDocumentationMode.None);

            Assert.Equal(
                Path.Combine("artifacts", "value-mode-comparison_disabled.json"),
                outputPath);
        }

        /// <summary>
        /// Invokes the private comparison-output-path resolver via reflection.
        /// </summary>
        /// <param name="options">
        /// The tool options to pass to the resolver.
        /// </param>
        /// <returns>
        /// The resolved comparison output path.
        /// </returns>
        private static string InvokeResolveComparisonOutputPath(ToolOptions options)
        {
            MethodInfo? method = typeof(ValueDocumentationModeComparisonRunner).GetMethod(
                "ResolveComparisonOutputPath",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            object? result = method.Invoke(null, new object[] { options });

            string path = Assert.IsType<string>(result);
            return path;
        }

        /// <summary>
        /// Invokes the private mode-report-path resolver via reflection.
        /// </summary>
        /// <param name="options">
        /// The tool options to pass to the resolver.
        /// </param>
        /// <param name="mode">
        /// The value-documentation mode to pass to the resolver.
        /// </param>
        /// <returns>
        /// The resolved mode report path.
        /// </returns>
        private static string InvokeResolveModeReportPath(
            ToolOptions options,
            ValueDocumentationMode mode)
        {
            MethodInfo? method = typeof(ValueDocumentationModeComparisonRunner).GetMethod(
                "ResolveModeReportPath",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            object? result = method.Invoke(null, new object[] { options, mode });

            string path = Assert.IsType<string>(result);
            return path;
        }
    }
}
