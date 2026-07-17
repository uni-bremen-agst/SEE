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
        /// Gets or sets the shared SLOC value used by all compared modes.
        /// </summary>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the compared value-documentation mode runs.
        /// </summary>
        public List<ValueDocumentationModeRunDto> Modes { get; set; } = new();
    }
}
