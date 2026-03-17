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
        public RunResult SharedBaselineResult { get; set; } = new();

        /// <summary>
        /// Gets or sets the duration of the shared detectors in milliseconds.
        /// </summary>
        public long SharedDetectorsDurationMs { get; set; }

        /// <summary>
        /// Gets the per-mode execution results.
        /// </summary>
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
        public ExceptionAnalysisMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the aggregated run result for the mode.
        /// </summary>
        public RunResult Result { get; set; } = new();

        /// <summary>
        /// Gets or sets the written report path for the mode, if any.
        /// </summary>
        public string? ReportPath { get; set; }

        /// <summary>
        /// Gets or sets the exception detector duration in milliseconds.
        /// </summary>
        public long ExceptionDetectorDurationMs { get; set; }
    }
}
