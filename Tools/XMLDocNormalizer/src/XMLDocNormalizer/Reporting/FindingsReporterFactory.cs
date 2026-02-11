using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Console;
using XMLDocNormalizer.Reporting.Json;
using XMLDocNormalizer.Reporting.Sarif;

namespace XMLDocNormalizer.Reporting
{
    /// <summary>
    /// Factory responsible for creating the appropriate findings reporter
    /// based on the selected output format.
    /// </summary>
    internal static class FindingsReporterFactory
    {
        /// <summary>
        /// Creates an <see cref="IFindingsReporter"/> instance
        /// according to the provided <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The parsed tool options.</param>
        /// <returns>An initialized reporter.</returns>
        public static IFindingsReporter Create(ToolOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            string outputPath = options.OutputPath ?? GetDefaultPath(options.OutputFormat);

            return options.OutputFormat switch
            {
                OutputFormat.Console =>
                    new ConsoleFindingsReporter(),

                OutputFormat.Json =>
                    new JsonFindingsReporter(outputPath, options.TargetPath),

                OutputFormat.Sarif =>
                    new SarifFindingsReporter(outputPath),

                _ =>
                    new ConsoleFindingsReporter()
            };
        }

        /// <summary>
        /// Returns a default output path for machine-readable formats.
        /// </summary>
        /// <param name="format">The selected output format.</param>
        /// <returns>A default file path.</returns>
        private static string GetDefaultPath(OutputFormat format)
        {
            return format switch
            {
                OutputFormat.Json => "artifacts/findings.json",
                OutputFormat.Sarif => "artifacts/findings.sarif",
                _ => "artifacts/findings.txt"
            };
        }
    }
}
