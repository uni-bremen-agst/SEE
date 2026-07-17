namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents the complete machine-readable comparison report across all exception analysis modes.
    /// </summary>
    internal sealed class ExceptionAnalysisModeComparisonReportDto
    {
        /// <summary>
        /// Gets or sets the tool name.
        /// </summary>
        /// <value>
        /// The tool name written to the comparison report.
        /// </value>
        public string Tool { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool version.
        /// </summary>
        /// <value>
        /// The tool version written to the comparison report.
        /// </value>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC generation timestamp.
        /// </summary>
        /// <value>
        /// The UTC generation timestamp of the comparison report.
        /// </value>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the analyzed target path.
        /// </summary>
        /// <value>
        /// The analyzed target path.
        /// </value>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metrics shared by all modes.
        /// </summary>
        /// <value>
        /// The metrics shared by all compared modes.
        /// </value>
        public ExceptionAnalysisModeSharedMetricsDto SharedMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets smell counts that are identical across all compared modes.
        /// </summary>
        /// <value>
        /// The smell counts that are identical across all compared modes.
        /// </value>
        public Dictionary<string, int> SharedFindingCounts { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the timing information for the comparison run.
        /// </summary>
        /// <value>
        /// The timing information for the comparison run.
        /// </value>
        public ExceptionAnalysisModeTimingDto Timings { get; set; } = new();

        /// <summary>
        /// Gets or sets the compared mode runs.
        /// </summary>
        /// <value>
        /// The compared mode runs.
        /// </value>
        public List<ExceptionAnalysisModeRunDto> Modes { get; set; } = new();
    }
}
