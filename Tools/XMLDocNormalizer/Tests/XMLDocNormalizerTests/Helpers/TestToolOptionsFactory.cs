using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;

namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// Provides helper methods to create <see cref="ToolOptions"/> instances for tests.
    /// </summary>
    internal static class TestToolOptionsFactory
    {
        /// <summary>
        /// Creates a minimal <see cref="ToolOptions"/> for reporter factory tests.
        /// </summary>
        /// <param name="format">The desired output format.</param>
        /// <param name="outputPath">Optional output path.</param>
        /// <returns>A tool options instance.</returns>
        public static ToolOptions Create(OutputFormat format, string? outputPath = null)
        {
            XmlDocOptions xmlDocOptions = new();

            return new ToolOptions(
                targetPath: "TestTarget",
                checkOnly: true,
                cleanBackups: false,
                useTest: false,
                xmlDocOptions: xmlDocOptions,
                outputFormat: format,
                outputPath: outputPath);
        }
    }
}
