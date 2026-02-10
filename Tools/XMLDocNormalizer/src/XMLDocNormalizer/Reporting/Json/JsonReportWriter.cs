using System.Text.Json;
using System.Text.Json.Serialization;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Writes JSON reports to disk.
    /// </summary>
    internal static class JsonReportWriter
    {
        /// <summary>
        /// Writes the given report to the specified output path.
        /// </summary>
        /// <param name="outputPath">The file path to write to.</param>
        /// <param name="report">The report to write.</param>
        public static void Write(string outputPath, JsonReport report)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path must not be null or whitespace.", nameof(outputPath));
            }

            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            EnsureParentDirectoryExists(outputPath);

            JsonSerializerOptions options = CreateSerializerOptions();
            string json = JsonSerializer.Serialize(report, options);

            File.WriteAllText(outputPath, json);
        }

        /// <summary>
        /// Creates serializer options used for the report.
        /// </summary>
        /// <returns>The configured serializer options.</returns>
        private static JsonSerializerOptions CreateSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Ensures the parent directory for <paramref name="outputPath"/> exists.
        /// </summary>
        /// <param name="outputPath">The output file path.</param>
        private static void EnsureParentDirectoryExists(string outputPath)
        {
            string? dir = Path.GetDirectoryName(outputPath);

            if (string.IsNullOrWhiteSpace(dir))
            {
                return;
            }

            Directory.CreateDirectory(dir);
        }
    }
}
