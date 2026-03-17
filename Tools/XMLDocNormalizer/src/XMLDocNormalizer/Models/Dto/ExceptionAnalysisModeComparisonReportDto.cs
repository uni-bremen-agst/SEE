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
        public string Tool { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tool version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC generation timestamp.
        /// </summary>
        public DateTime GeneratedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the analyzed target path.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metrics shared by all modes.
        /// </summary>
        public ExceptionAnalysisModeSharedMetricsDto SharedMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets smell counts that are identical across all compared modes.
        /// </summary>
        public Dictionary<string, int> SharedFindingCounts { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the timing information for the comparison run.
        /// </summary>
        public ExceptionAnalysisModeTimingDto Timings { get; set; } = new();

        /// <summary>
        /// Gets or sets the compared mode runs.
        /// </summary>
        public List<ExceptionAnalysisModeRunDto> Modes { get; set; } = new();
    }
}
