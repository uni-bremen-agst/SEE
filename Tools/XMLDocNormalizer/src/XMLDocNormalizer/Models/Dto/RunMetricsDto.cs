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

        /// <summary>
        /// Gets or sets raw totals (denominators) collected across the run.
        /// </summary>
        /// <remarks>
        /// Keys are stable identifiers (e.g. "MethodsTotal") and are defined by <c>StatisticsKeys</c>.
        /// Values represent absolute counts across all included files (respecting include/exclude options).
        /// </remarks>
        public IReadOnlyDictionary<string, int> Totals { get; init; } =
            new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets derived coverage ratios in the range [0..1].
        /// </summary>
        /// <remarks>
        /// Coverage values are ratios computed from a numerator (typically a smell count) and a denominator
        /// (a total from <see cref="Totals"/>). Values are only emitted when the denominator is &gt; 0.
        /// </remarks>
        public IReadOnlyDictionary<string, double> Coverage { get; init; } =
            new Dictionary<string, double>();
    }
}