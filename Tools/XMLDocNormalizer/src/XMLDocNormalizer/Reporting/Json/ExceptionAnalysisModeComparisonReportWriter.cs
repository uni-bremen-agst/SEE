using System.Text.Json;
using System.Text.Json.Serialization;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Writes machine-readable comparison reports for exception analysis modes.
    /// </summary>
    internal static class ExceptionAnalysisModeComparisonReportWriter
    {
        /// <summary>
        /// Writes the given comparison report to the specified output path.
        /// </summary>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="report">The report to write.</param>
        public static void Write(string outputPath, ExceptionAnalysisModeComparisonReportDto report)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path must not be null or whitespace.", nameof(outputPath));
            }

            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            FileSystemUtils.EnsureParentDirectoryExists(outputPath);

            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };

            string json = JsonSerializer.Serialize(report, options);
            File.WriteAllText(outputPath, json);
        }
    }
}
