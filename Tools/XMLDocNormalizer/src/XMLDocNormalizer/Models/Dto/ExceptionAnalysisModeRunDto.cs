using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents one exception analysis mode entry in the comparison report.
    /// </summary>
    internal sealed class ExceptionAnalysisModeRunDto
    {
        /// <summary>
        /// Gets or sets the analyzed exception mode.
        /// </summary>
        public ExceptionAnalysisMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the output path of the per-mode report, if one was written.
        /// </summary>
        public string? ReportPath { get; set; }

        /// <summary>
        /// Gets or sets the total number of findings for this mode.
        /// </summary>
        public int FindingCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of errors for this mode.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of warnings for this mode.
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of suggestions for this mode.
        /// </summary>
        public int SuggestionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of changed files.
        /// </summary>
        public int ChangedFiles { get; set; }

        /// <summary>
        /// Gets or sets findings per KSLOC.
        /// </summary>
        public double FindingsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets errors per KSLOC.
        /// </summary>
        public double ErrorsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets warnings per KSLOC.
        /// </summary>
        public double WarningsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets suggestions per KSLOC.
        /// </summary>
        public double SuggestionsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets smell counts that differ from the shared finding counts.
        /// </summary>
        public Dictionary<string, int> ModeFindingCounts { get; set; } =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the total number of exception-related findings.
        /// </summary>
        public int ExceptionFindingCount { get; set; }

        /// <summary>
        /// Gets or sets the exception-related findings density per 1000 SLOC.
        /// </summary>
        public double ExceptionFindingsPerKSloc { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC610 findings.
        /// </summary>
        public int Doc610Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC611 findings.
        /// </summary>
        public int Doc611Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC620 findings.
        /// </summary>
        public int Doc620Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC630 findings.
        /// </summary>
        public int Doc630Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC631 findings.
        /// </summary>
        public int Doc631Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC632 findings.
        /// </summary>
        public int Doc632Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC640 findings.
        /// </summary>
        public int Doc640Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC660 findings.
        /// </summary>
        public int Doc660Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC670 findings.
        /// </summary>
        public int Doc670Count { get; set; }

        /// <summary>
        /// Gets or sets the number of DOC680 findings.
        /// </summary>
        public int Doc680Count { get; set; }

        /// <summary>
        /// Gets or sets the share of DOC631 among all exception-related findings.
        /// </summary>
        public double Doc631Share { get; set; }

        /// <summary>
        /// Gets or sets the delta in total findings compared to Direct.
        /// </summary>
        public int FindingCountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in findings per KSLOC compared to Direct.
        /// </summary>
        public double FindingsPerKSlocDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in exception findings compared to Direct.
        /// </summary>
        public int ExceptionFindingCountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in exception findings per KSLOC compared to Direct.
        /// </summary>
        public double ExceptionFindingsPerKSlocDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in DOC611 compared to Direct.
        /// </summary>
        public int Doc611CountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in DOC631 compared to Direct.
        /// </summary>
        public int Doc631CountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the delta in DOC632 compared to Direct.
        /// </summary>
        public int Doc632CountDeltaToDirect { get; set; }

        /// <summary>
        /// Gets or sets the amplification factor of all findings compared to Direct.
        /// </summary>
        public double FindingAmplificationFactorToDirect { get; set; }

        /// <summary>
        /// Gets or sets the amplification factor of exception findings compared to Direct.
        /// </summary>
        public double ExceptionFindingAmplificationFactorToDirect { get; set; }
    }
}
