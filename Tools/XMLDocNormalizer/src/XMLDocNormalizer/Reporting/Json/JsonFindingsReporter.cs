using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Dto;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Reporting.Json
{
    /// <summary>
    /// Collects findings and writes them into a single JSON report file.
    /// </summary>
    /// <remarks>
    /// Intended for CI usage as an artifact. The reporter buffers findings in memory.
    /// </remarks>
    internal sealed class JsonFindingsReporter : IResultAwareFindingsReporter
    {
        private readonly string outputPath;
        private readonly string targetPath;
        private readonly List<JsonFindingDto> buffer = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFindingsReporter"/> class.
        /// </summary>
        /// <param name="outputPath">The JSON output path.</param>
        /// <param name="targetPath">The analyzed target path (metadata).</param>
        public JsonFindingsReporter(string outputPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Output path must not be null or whitespace.", nameof(outputPath));
            }

            this.outputPath = outputPath;
            this.targetPath = targetPath ?? string.Empty;
        }

        /// <summary>
        /// Adds findings for a single file to the internal buffer.
        /// </summary>
        /// <param name="filePath">The analyzed file path.</param>
        /// <param name="findings">The findings produced for the file.</param>
        public void ReportFile(string filePath, IReadOnlyList<Finding> findings)
        {
            if (findings == null || findings.Count == 0)
            {
                return;
            }

            foreach (Finding finding in findings)
            {
                buffer.Add(JsonFindingDto.FromFinding(finding));
            }
        }

        /// <summary>
        /// Writes the buffered findings to the configured JSON output path.
        /// </summary>
        /// <remarks>
        /// If the reporter is used via <see cref="IResultAwareFindingsReporter"/>, prefer
        /// <see cref="Complete(RunResult)"/> to include run metrics.
        /// </remarks>
        public void Complete()
        {
            Complete(new RunResult());
        }

        /// <summary>
        /// Writes the buffered findings and aggregated run metrics to the configured JSON output path.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        public void Complete(RunResult result)
        {
            RunMetricsDto metrics = RunMetricsCalculator.From(result);

            JsonReport report = new(
                Tool: ToolMetadata.Name,
                Version: ToolMetadata.Version,
                GeneratedAtUtc: DateTime.UtcNow,
                TargetPath: targetPath,
                FindingCount: buffer.Count,
                Findings: buffer,
                Metrics: metrics);

            JsonReportWriter.Write(outputPath, report);
        }
    }
}
