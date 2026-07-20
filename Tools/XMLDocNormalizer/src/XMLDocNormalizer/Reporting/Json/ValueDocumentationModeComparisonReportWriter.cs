using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Models.DTO;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Writes machine-readable comparison reports for value-documentation modes.
    /// </summary>
    internal static class ValueDocumentationModeComparisonReportWriter
    {
        /// <summary>
        /// Writes the given comparison report to the specified output path.
        /// </summary>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="report">The report to write.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the output path is null or whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the report is null.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when the parent directory cannot be created or the report file
        /// cannot be written because of an I/O error.
        /// </exception>
        public static void Write(string outputPath, ValueDocumentationModeComparisonReportDto report)
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
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };

            string json = JsonSerializer.Serialize(report, options);
            File.WriteAllText(outputPath, json);
        }
    }
}
