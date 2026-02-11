using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Reporting;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Console;
using XMLDocNormalizer.Reporting.Json;
using XMLDocNormalizer.Reporting.Sarif;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Reporting
{
    /// <summary>
    /// Tests for <see cref="FindingsReporterFactory"/>.
    /// </summary>
    public sealed class FindingsReporterFactoryTests
    {
        /// <summary>
        /// Ensures that JSON format produces a <see cref="JsonFindingsReporter"/>.
        /// </summary>
        [Fact]
        public void Create_WithJsonFormat_ReturnsJsonReporter()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Json);

            IFindingsReporter reporter = FindingsReporterFactory.Create(options);

            Assert.IsType<JsonFindingsReporter>(reporter);
        }

        /// <summary>
        /// Ensures that SARIF format produces a <see cref="SarifFindingsReporter"/>.
        /// </summary>
        [Fact]
        public void Create_WithSarifFormat_ReturnsSarifReporter()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Sarif);

            IFindingsReporter reporter = FindingsReporterFactory.Create(options);

            Assert.IsType<SarifFindingsReporter>(reporter);
        }

        /// <summary>
        /// Ensures that console format produces a <see cref="ConsoleFindingsReporter"/>.
        /// </summary>
        [Fact]
        public void Create_WithConsoleFormat_ReturnsConsoleReporter()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Console);

            IFindingsReporter reporter = FindingsReporterFactory.Create(options);

            Assert.IsType<ConsoleFindingsReporter>(reporter);
        }

        /// <summary>
        /// Ensures that JSON format uses the default output path when <see cref="ToolOptions.OutputPath"/> is not set.
        /// </summary>
        [Fact]
        public void Create_WithJsonFormat_AndNoOutputPath_UsesDefaultPath()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Json, outputPath: null);

            IFindingsReporter reporter = FindingsReporterFactory.Create(options);

            Assert.IsType<JsonFindingsReporter>(reporter);
        }

        /// <summary>
        /// Ensures that SARIF format uses the default output path when <see cref="ToolOptions.OutputPath"/> is not set.
        /// </summary>
        [Fact]
        public void Create_WithSarifFormat_AndNoOutputPath_UsesDefaultPath()
        {
            ToolOptions options = TestToolOptionsFactory.Create(OutputFormat.Sarif, outputPath: null);

            IFindingsReporter reporter = FindingsReporterFactory.Create(options);

            Assert.IsType<SarifFindingsReporter>(reporter);
        }
    }
}
