namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents a comparison report for value-documentation modes.
    /// </summary>
    internal sealed class ValueDocumentationModeComparisonReportDto
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
        /// Gets or sets the shared SLOC value used by all compared modes.
        /// </summary>
        /// <value>
        /// The shared SLOC value used by all compared modes.
        /// </value>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the compared value-documentation mode runs.
        /// </summary>
        /// <value>
        /// The compared value-documentation mode runs.
        /// </value>
        public List<ValueDocumentationModeRunDto> Modes { get; set; } = new();
    }
}
