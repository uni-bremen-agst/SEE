namespace XMLDocNormalizer.Models.Dto
{
    /// <summary>
    /// Represents aggregated run metrics that are suitable for machine-readable outputs.
    /// </summary>
    internal sealed class RunMetricsDto
    {
        /// <summary>
        /// Gets or sets the total SLOC counted during the run.
        /// </summary>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the total number of findings.
        /// </summary>
        public int FindingCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of errors.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of warnings.
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of suggestions.
        /// </summary>
        public int SuggestionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of changed files (fix mode).
        /// </summary>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// Gets or sets the findings density per 1000 SLOC.
        /// </summary>
        public double FindingsPerKLoc { get; set; }

        /// <summary>
        /// Gets or sets the errors density per 1000 SLOC.
        /// </summary>
        public double ErrorsPerKLoc { get; set; }

        /// <summary>
        /// Gets or sets the warnings density per 1000 SLOC.
        /// </summary>
        public double WarningsPerKLoc { get; set; }

        /// <summary>
        /// Gets or sets the suggestions density per 1000 SLOC.
        /// </summary>
        public double SuggestionsPerKLoc { get; set; }
    }
}