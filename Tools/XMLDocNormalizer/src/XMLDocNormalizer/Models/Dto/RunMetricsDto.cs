namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents aggregated run metrics that are suitable for machine-readable outputs.
    /// </summary>
    internal sealed class RunMetricsDto
    {
        /// <summary>
        /// Gets or sets the total SLOC counted during the run.
        /// </summary>
        /// <value>
        /// The total source lines of code counted during the run.
        /// </value>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the total analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The total analysis duration in milliseconds.
        /// </value>
        public long AnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the analysis duration per 1000 SLOC.
        /// </summary>
        /// <value>
        /// The analysis duration per 1000 SLOC.
        /// </value>
        public double AnalysisDurationMsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the total number of findings.
        /// </summary>
        /// <value>
        /// The total number of findings.
        /// </value>
        public int FindingCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of errors.
        /// </summary>
        /// <value>
        /// The total number of error findings.
        /// </value>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of warnings.
        /// </summary>
        /// <value>
        /// The total number of warning findings.
        /// </value>
        public int WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of suggestions.
        /// </summary>
        /// <value>
        /// The total number of suggestion findings.
        /// </value>
        public int SuggestionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of changed files in fix mode.
        /// </summary>
        /// <value>
        /// The number of changed files in fix mode.
        /// </value>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// Gets or sets the findings density per 1000 SLOC.
        /// </summary>
        /// <value>
        /// The findings density per 1000 SLOC.
        /// </value>
        public double FindingsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the errors density per 1000 SLOC.
        /// </summary>
        /// <value>
        /// The errors density per 1000 SLOC.
        /// </value>
        public double ErrorsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the warnings density per 1000 SLOC.
        /// </summary>
        /// <value>
        /// The warnings density per 1000 SLOC.
        /// </value>
        public double WarningsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the suggestions density per 1000 SLOC.
        /// </summary>
        /// <value>
        /// The suggestions density per 1000 SLOC.
        /// </value>
        public double SuggestionsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets raw totals collected across the run.
        /// </summary>
        /// <value>
        /// The raw denominator totals collected across the run.
        /// </value>
        /// <remarks>
        /// Keys are stable statistics identifiers. Values represent absolute counts across all included files.
        /// </remarks>
        public IReadOnlyDictionary<string, int> Totals { get; init; } =
            new Dictionary<string, int>();

        /// <summary>
        /// Gets the number of occurrences per finding identifier.
        /// </summary>
        /// <value>
        /// The number of occurrences per finding identifier.
        /// </value>
        public SortedDictionary<string, int> TotalFindingCounts { get; init; }
            = new SortedDictionary<string, int>();

        /// <summary>
        /// Gets or sets derived coverage ratios in the range from zero to one.
        /// </summary>
        /// <value>
        /// The derived coverage ratios keyed by coverage metric name.
        /// </value>
        /// <remarks>
        /// Coverage values are ratios computed from a numerator and a denominator.
        /// Values are only emitted when the denominator is greater than zero.
        /// </remarks>
        public IReadOnlyDictionary<string, double> Coverage { get; init; } =
            new Dictionary<string, double>();
    }
}
