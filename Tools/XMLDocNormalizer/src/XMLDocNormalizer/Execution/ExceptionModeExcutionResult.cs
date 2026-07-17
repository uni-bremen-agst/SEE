using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Represents the complete internal execution result of a comparison run.
    /// </summary>
    internal sealed class ExceptionComparisonExecutionResult
    {
        /// <summary>
        /// Gets or sets the baseline result that contains all findings shared across modes.
        /// </summary>
        /// <value>
        /// The baseline result that contains all findings shared across modes.
        /// </value>
        public RunResult SharedBaselineResult { get; set; } = new();

        /// <summary>
        /// Gets or sets the duration of the shared detectors in milliseconds.
        /// </summary>
        /// <value>
        /// The duration of the shared detectors in milliseconds.
        /// </value>
        public long SharedDetectorsDurationMs { get; set; }

        /// <summary>
        /// Gets the per-mode execution results.
        /// </summary>
        /// <value>
        /// The per-mode execution results.
        /// </value>
        public List<ExceptionModeExecutionResult> Modes { get; } = new();
    }

    /// <summary>
    /// Represents one executed exception analysis mode together with its aggregated result and timing.
    /// </summary>
    internal sealed class ExceptionModeExecutionResult
    {
        /// <summary>
        /// Gets or sets the executed exception analysis mode.
        /// </summary>
        /// <value>
        /// The executed exception analysis mode.
        /// </value>
        public ExceptionAnalysisMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the aggregated run result for the mode.
        /// </summary>
        /// <value>
        /// The aggregated run result for the mode.
        /// </value>
        public RunResult Result { get; set; } = new();

        /// <summary>
        /// Gets or sets the written report path for the mode, if any.
        /// </summary>
        /// <value>
        /// The written report path for the mode, or null when no report was written.
        /// </value>
        public string? ReportPath { get; set; }

        /// <summary>
        /// Gets or sets the exception detector duration in milliseconds.
        /// </summary>
        /// <value>
        /// The exception detector duration in milliseconds.
        /// </value>
        public long ExceptionDetectorDurationMs { get; set; }
    }
}
