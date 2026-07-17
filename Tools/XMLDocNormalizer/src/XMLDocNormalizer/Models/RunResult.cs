namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents aggregated results of a single tool execution run.
    /// </summary>
    /// <remarks>
    /// Aggregation is performed incrementally while files are processed.
    /// </remarks>
    internal sealed class RunResult
    {
        /// <summary>
        /// Gets the total SLOC for the analyzed input.
        /// </summary>
        /// <value>
        /// The total non-empty, non-comment source lines of code for the analyzed input.
        /// </value>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the total analysis duration in milliseconds for this run.
        /// </summary>
        /// <value>
        /// The total analysis duration in milliseconds for this run.
        /// </value>
        public long AnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were changed in fix mode.
        /// </summary>
        /// <value>
        /// The number of files that were changed in fix mode.
        /// </value>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// Gets the total number of findings produced by the run.
        /// </summary>
        /// <value>
        /// The total number of findings produced by the run.
        /// </value>
        /// <remarks>
        /// This value is the sum of all findings across all processed files.
        /// </remarks>
        public int FindingCount { get; private set; }

        /// <summary>
        /// Gets the number of findings with error severity.
        /// </summary>
        /// <value>
        /// The number of findings with error severity.
        /// </value>
        public int ErrorCount { get; private set; }

        /// <summary>
        /// Gets the number of findings with warning severity.
        /// </summary>
        /// <value>
        /// The number of findings with warning severity.
        /// </value>
        public int WarningCount { get; private set; }

        /// <summary>
        /// Gets the number of findings with suggestion severity.
        /// </summary>
        /// <value>
        /// The number of findings with suggestion severity.
        /// </value>
        public int SuggestionCount { get; private set; }

        /// <summary>
        /// Gets the number of occurrences per smell ID.
        /// </summary>
        /// <value>
        /// The number of occurrences per smell ID.
        /// </value>
        /// <remarks>
        /// The dictionary uses ordinal string comparison to ensure stable, culture-invariant keys.
        /// </remarks>
        public Dictionary<string, int> SmellCounts { get; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the total number of occurrences per statistics key.
        /// </summary>
        /// <value>
        /// The total number of occurrences per statistics key.
        /// </value>
        /// <remarks>
        /// These values represent denominators for coverage metrics and are collected independently of findings.
        /// </remarks>
        public Dictionary<string, int> Totals { get; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Updates all aggregated counters using the provided findings.
        /// </summary>
        /// <param name="findings">The findings to add to the run statistics.</param>
        /// <remarks>
        /// This method is the single entry point for updating counters. Call it whenever
        /// a new set of findings has been produced for a file or a processing stage.
        /// </remarks>
        public void AccumulateFindings(IReadOnlyList<Finding> findings)
        {
            if (findings == null || findings.Count == 0)
            {
                return;
            }

            foreach (Finding finding in findings)
            {
                FindingCount++;
                XmlDocSmell smell = finding.Smell;

                switch (smell.Severity)
                {
                    case Severity.Error:
                        ErrorCount++;
                        break;
                    case Severity.Warning:
                        WarningCount++;
                        break;
                    case Severity.Suggestion:
                        SuggestionCount++;
                        break;
                }

                SmellCounts[smell.ID] = SmellCounts.GetValueOrDefault(smell.ID) + 1;
            }
        }

        /// <summary>
        /// Accumulates totals from a per-file totals dictionary.
        /// </summary>
        /// <param name="fileTotals">The totals to add.</param>
        public void AccumulateTotals(IReadOnlyDictionary<string, int> fileTotals)
        {
            if (fileTotals == null || fileTotals.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, int> pair in fileTotals)
            {
                Totals[pair.Key] = Totals.GetValueOrDefault(pair.Key) + pair.Value;
            }
        }

        /// <summary>
        /// Creates a deep copy of the current aggregated run result.
        /// </summary>
        /// <returns>A cloned <see cref="RunResult"/> instance.</returns>
        public RunResult Clone()
        {
            RunResult clone = new()
            {
                Sloc = Sloc,
                AnalysisDurationMs = AnalysisDurationMs,
                ChangedFiles = ChangedFiles,
                FindingCount = FindingCount,
                ErrorCount = ErrorCount,
                WarningCount = WarningCount,
                SuggestionCount = SuggestionCount
            };

            foreach (KeyValuePair<string, int> pair in SmellCounts)
            {
                clone.SmellCounts[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<string, int> pair in Totals)
            {
                clone.Totals[pair.Key] = pair.Value;
            }

            return clone;
        }
    }
}
