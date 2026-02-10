using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Console;
using XMLDocNormalizer.Reporting.Json;

namespace XMLDocNormalizer.Reporting
{
    /// <summary>
    /// Creates findings reporters based on tool options.
    /// </summary>
    internal static class FindingsReporterFactory
    {
        /// <summary>
        /// Creates the reporter requested by the options.
        /// </summary>
        /// <param name="options">The tool options.</param>
        /// <returns>The reporter to use.</returns>
        public static IFindingsReporter Create(ToolOptions options)
        {
            if (options.OutputFormat == OutputFormat.Json)
            {
                string outputPath = string.IsNullOrWhiteSpace(options.OutputPath)
                    ? "artifacts/findings.json"
                    : options.OutputPath;

                return new JsonFindingsReporter(outputPath, options.TargetPath);
            }

            return new ConsoleFindingsReporter();
        }
    }
}
