namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents aggregated results of a single tool execution run.
    /// </summary>
    /// <remarks>
    /// Aggregation is performed incrementally while files are processed. Use
    /// <see cref="AccumulateFindings(IReadOnlyList{Finding})"/> to update all counters
    /// consistently from a collection of findings.
    /// </remarks>
    internal sealed class RunResult
    {
        /// <summary>
        /// Gets the total SLOC (non-empty, non-comment source lines of code) for the analyzed input.
        /// The SLOC count is computed for included files only (generated/tests may be excluded by default).
        /// </summary>
        public int Sloc { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were changed in fix mode.
        /// </summary>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// Gets the total number of findings produced by the run.
        /// </summary>
        /// <remarks>
        /// This value is the sum of all findings across all processed files.
        /// </remarks>
        public int FindingCount { get; private set; }

        /// <summary>
        /// Gets the number of findings with severity <see cref="Severity.Error"/>.
        /// </summary>
        public int ErrorCount { get; private set; }

        /// <summary>
        /// Gets the number of findings with severity <see cref="Severity.Warning"/>.
        /// </summary>
        public int WarningCount { get; private set; }

        /// <summary>
        /// Gets the number of findings with severity <see cref="Severity.Suggestion"/>.
        /// </summary>
        public int SuggestionCount { get; private set; }

        /// <summary>
        /// Gets the number of occurrences per smell id (rule id), e.g. "DOC200" =&gt; 15.
        /// </summary>
        /// <remarks>
        /// The dictionary uses ordinal string comparison to ensure stable, culture-invariant keys.
        /// </remarks>
        public Dictionary<string, int> SmellCounts { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Gets the total number of occurrences per statistics key, e.g. "MethodsTotal" =&gt; 120.
        /// </summary>
        /// <remarks>
        /// These values represent denominators for coverage metrics and are collected independently of findings.
        /// </remarks>
        public Dictionary<string, int> Totals { get; } =
            new Dictionary<string, int>(StringComparer.Ordinal);

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

                SmellCounts[smell.Id] = SmellCounts.GetValueOrDefault(smell.Id) + 1;
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
    }
}
