namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents metrics that are shared across all exception analysis modes.
    /// </summary>
    internal sealed class ExceptionAnalysisModeSharedMetricsDto
    {
        /// <summary>
        /// Gets or sets the source lines of code.
        /// </summary>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the declaration totals.
        /// </summary>
        public Dictionary<string, int> Totals { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the coverage metrics.
        /// </summary>
        public Dictionary<string, double> Coverage { get; set; } =
            new(StringComparer.Ordinal);
    }
}
