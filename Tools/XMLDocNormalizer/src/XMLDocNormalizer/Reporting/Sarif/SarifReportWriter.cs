using System.Text.Json;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Reporting.Sarif.Contract;

namespace XMLDocNormalizer.Reporting.Sarif
{
    /// <summary>
    /// Writes a <see cref="SarifLog"/> instance to disk as a SARIF 2.1.0 JSON file.
    /// </summary>
    internal static class SarifReportWriter
    {
        /// <summary>
        /// Serializes the specified SARIF log and writes it to the given output path.
        /// </summary>
        /// <param name="outputPath">The target file path.</param>
        /// <param name="log">The SARIF log instance to serialize.</param>
        public static void Write(string outputPath, SarifLog log)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path must not be null or whitespace.", nameof(outputPath));
            }

            ArgumentNullException.ThrowIfNull(log);

            FileSystemUtils.EnsureParentDirectoryExists(outputPath);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(log, jsonOptions);

            File.WriteAllText(outputPath, json);
        }
    }
}
