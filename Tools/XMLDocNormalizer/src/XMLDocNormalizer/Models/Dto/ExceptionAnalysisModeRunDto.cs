using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents the comparison metrics for one exception analysis mode.
    /// </summary>
    internal sealed class ExceptionAnalysisModeRunDto
    {
        /// <summary>
        /// Gets or sets the analyzed exception analysis mode.
        /// </summary>
        /// <value>
        /// The analyzed exception analysis mode.
        /// </value>
        public ExceptionAnalysisMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the representative output path of the per-mode report, if one was written.
        /// </summary>
        /// <value>
        /// The representative output path of the per-mode report, or null when no report was written.
        /// </value>
        public string? ReportPath { get; set; }

        /// <summary>
        /// Gets or sets all per-run output paths for this mode.
        /// </summary>
        /// <value>
        /// All per-run output paths for this mode.
        /// </value>
        public List<string> ReportPaths { get; set; } = new();

        /// <summary>
        /// Gets or sets the measured run count for this mode.
        /// </summary>
        /// <value>
        /// The measured run count for this mode.
        /// </value>
        public int RunCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether repeated runs produced identical finding counts.
        /// </summary>
        /// <value>
        /// True if repeated runs produced identical finding counts; otherwise false.
        /// </value>
        public bool FindingCountsStableAcrossRuns { get; set; } = true;

        /// <summary>
        /// Gets or sets the representative reported analysis duration in milliseconds.
        /// For multiple runs this is the median.
        /// </summary>
        /// <value>
        /// The representative reported analysis duration in milliseconds.
        /// </value>
        public long ReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative isolated child process wall-clock duration in milliseconds.
        /// For multiple runs this is the median.
        /// </summary>
        /// <value>
        /// The representative isolated child process wall-clock duration in milliseconds.
        /// </value>
        public long WallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets all reported analysis durations in milliseconds.
        /// </summary>
        /// <value>
        /// All reported analysis durations in milliseconds.
        /// </value>
        public List<long> ReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all isolated child process wall-clock durations in milliseconds.
        /// </summary>
        /// <value>
        /// All isolated child process wall-clock durations in milliseconds.
        /// </value>
        public List<long> WallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets the minimum reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The minimum reported analysis duration in milliseconds.
        /// </value>
        public long MinReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The maximum reported analysis duration in milliseconds.
        /// </value>
        public long MaxReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the mean reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The mean reported analysis duration in milliseconds.
        /// </value>
        public double MeanReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the median reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The median reported analysis duration in milliseconds.
        /// </value>
        public long MedianReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the sample standard deviation of reported analysis durations in milliseconds.
        /// </summary>
        /// <value>
        /// The sample standard deviation of reported analysis durations in milliseconds.
        /// </value>
        public double StandardDeviationReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum wall-clock duration in milliseconds.
        /// </summary>
        /// <value>
        /// The minimum wall-clock duration in milliseconds.
        /// </value>
        public long MinWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum wall-clock duration in milliseconds.
        /// </summary>
        /// <value>
        /// The maximum wall-clock duration in milliseconds.
        /// </value>
        public long MaxWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the mean wall-clock duration in milliseconds.
        /// </summary>
        /// <value>
        /// The mean wall-clock duration in milliseconds.
        /// </value>
        public double MeanWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the median wall-clock duration in milliseconds.
        /// </summary>
        /// <value>
        /// The median wall-clock duration in milliseconds.
        /// </value>
        public long MedianWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the sample standard deviation of wall-clock durations in milliseconds.
        /// </summary>
        /// <value>
        /// The sample standard deviation of wall-clock durations in milliseconds.
        /// </value>
        public double StandardDeviationWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the total number of findings for this mode.
        /// </summary>
        /// <value>
        /// The total number of findings for this mode.
        /// </value>
        public int FindingCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of errors for this mode.
        /// </summary>
        /// <value>
        /// The total number of error findings for this mode.
        /// </value>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of warnings for this mode.
        /// </summary>
        /// <value>
        /// The total number of warning findings for this mode.
        /// </value>
        public int WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of suggestions for this mode.
        /// </summary>
        /// <value>
        /// The total number of suggestion findings for this mode.
        /// </value>
        public int SuggestionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of changed files.
        /// </summary>
        /// <value>
        /// The number of changed files.
        /// </value>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// Gets or sets findings per KSLOC.
        /// </summary>
        /// <value>
        /// The findings density per 1000 SLOC.
        /// </value>
        public double FindingsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets errors per KSLOC.
        /// </summary>
        /// <value>
        /// The error finding density per 1000 SLOC.
        /// </value>
        public double ErrorsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets warnings per KSLOC.
        /// </summary>
        /// <value>
        /// The warning finding density per 1000 SLOC.
        /// </value>
        public double WarningsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets suggestions per KSLOC.
        /// </summary>
        /// <value>
        /// The suggestion finding density per 1000 SLOC.
        /// </value>
        public double SuggestionsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets smell counts that differ from the shared finding counts.
        /// </summary>
        /// <value>
        /// The smell counts that differ from the shared finding counts.
        /// </value>
        public Dictionary<string, int> ModeFindingCounts { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the total number of exception-related findings.
        /// </summary>
        /// <value>
        /// The total number of exception-related findings.
        /// </value>
        public int ExceptionFindingCount { get; set; }

        /// <summary>
        /// Gets or sets the exception-related findings density per 1000 SLOC.
        /// </summary>
        /// <value>
        /// The exception-related findings density per 1000 SLOC.
        /// </value>
        public double ExceptionFindingsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC610 findings.
        /// </summary>
        /// <value>
        /// The number of DOC610 findings.
        /// </value>
        public int Doc610Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC611 findings.
        /// </summary>
        /// <value>
        /// The number of DOC611 findings.
        /// </value>
        public int Doc611Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC620 findings.
        /// </summary>
        /// <value>
        /// The number of DOC620 findings.
        /// </value>
        public int Doc620Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC630 findings.
        /// </summary>
        /// <value>
        /// The number of DOC630 findings.
        /// </value>
        public int Doc630Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC631 findings.
        /// </summary>
        /// <value>
        /// The number of DOC631 findings.
        /// </value>
        public int Doc631Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC632 findings.
        /// </summary>
        /// <value>
        /// The number of DOC632 findings.
        /// </value>
        public int Doc632Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC640 findings.
        /// </summary>
        /// <value>
        /// The number of DOC640 findings.
        /// </value>
        public int Doc640Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC660 findings.
        /// </summary>
        /// <value>
        /// The number of DOC660 findings.
        /// </value>
        public int Doc660Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC670 findings.
        /// </summary>
        /// <value>
        /// The number of DOC670 findings.
        /// </value>
        public int Doc670Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC680 findings.
        /// </summary>
        /// <value>
        /// The number of DOC680 findings.
        /// </value>
        public int Doc680Count { get; set; }

        /// <summary>
        /// Gets or sets the share of DOC631 among all exception-related findings.
        /// </summary>
        /// <value>
        /// The share of DOC631 among all exception-related findings.
        /// </value>
        public double Doc631Share { get; set; }

        /// <summary>
        /// Gets or sets the delta in total findings compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in total findings compared to Direct.
        /// </value>
        public int FindingCountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in findings per KSLOC compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in findings per KSLOC compared to Direct.
        /// </value>
        public double FindingsPerKSlocDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in exception findings compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in exception findings compared to Direct.
        /// </value>
        public int ExceptionFindingCountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in exception findings per KSLOC compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in exception findings per KSLOC compared to Direct.
        /// </value>
        public double ExceptionFindingsPerKSlocDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in DOC611 compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in DOC611 compared to Direct.
        /// </value>
        public int Doc611CountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in DOC631 compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in DOC631 compared to Direct.
        /// </value>
        public int Doc631CountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in DOC632 compared to Direct.
        /// </summary>
        /// <value>
        /// The delta in DOC632 compared to Direct.
        /// </value>
        public int Doc632CountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the amplification factor of all findings compared to Direct.
        /// </summary>
        /// <value>
        /// The amplification factor of all findings compared to Direct.
        /// </value>
        public double FindingAmplificationFactorToDirect { get; set; }

        /// <summary>
        /// Gets or sets the amplification factor of exception findings compared to Direct.
        /// </summary>
        /// <value>
        /// The amplification factor of exception findings compared to Direct.
        /// </value>
        public double ExceptionFindingAmplificationFactorToDirect { get; set; }
    }
}
