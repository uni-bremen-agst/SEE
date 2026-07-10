namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents timing information for a comparison run across all exception analysis modes.
    /// </summary>
    internal sealed class ExceptionAnalysisModeTimingDto
    {
        /// <summary>
        /// Gets or sets the execution isolation strategy used by the comparison run.
        /// </summary>
        public string ExecutionIsolation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mode execution order strategy used by the comparison run.
        /// </summary>
        public string ModeOrderStrategy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether wall-clock durations include child process startup.
        /// </summary>
        public bool IncludesProcessStartup { get; set; }

        /// <summary>
        /// Gets or sets the measured run count per mode.
        /// </summary>
        public int RunCount { get; set; }

        /// <summary>
        /// Gets or sets the warmup run count per mode.
        /// </summary>
        public int WarmupRunCount { get; set; }

        /// <summary>
        /// Gets or sets the duration of the shared detectors in milliseconds.
        /// This value is zero for isolated process comparisons because no shared detector phase is reused.
        /// </summary>
        public long SharedDetectorsDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative Direct reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        public long DirectExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitive reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        public long ProjectTransitiveExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitiveDeclaredExceptions reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        public long ProjectTransitiveDeclaredExceptionsExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative SolutionTransitive reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        public long SolutionTransitiveExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative Direct wall-clock child process duration in milliseconds.
        /// </summary>
        public long DirectWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitive wall-clock child process duration in milliseconds.
        /// </summary>
        public long ProjectTransitiveWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitiveDeclaredExceptions wall-clock child process duration in milliseconds.
        /// </summary>
        public long ProjectTransitiveDeclaredExceptionsWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative SolutionTransitive wall-clock child process duration in milliseconds.
        /// </summary>
        public long SolutionTransitiveWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative Direct reported analysis duration in milliseconds.
        /// </summary>
        public long DirectReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitive reported analysis duration in milliseconds.
        /// </summary>
        public long ProjectTransitiveReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitiveDeclaredExceptions reported analysis duration in milliseconds.
        /// </summary>
        public long ProjectTransitiveDeclaredExceptionsReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative SolutionTransitive reported analysis duration in milliseconds.
        /// </summary>
        public long SolutionTransitiveReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets all Direct wall-clock durations in milliseconds.
        /// </summary>
        public List<long> DirectWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitive wall-clock durations in milliseconds.
        /// </summary>
        public List<long> ProjectTransitiveWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitiveDeclaredExceptions wall-clock durations in milliseconds.
        /// </summary>
        public List<long> ProjectTransitiveDeclaredExceptionsWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all SolutionTransitive wall-clock durations in milliseconds.
        /// </summary>
        public List<long> SolutionTransitiveWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all Direct reported analysis durations in milliseconds.
        /// </summary>
        public List<long> DirectReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitive reported analysis durations in milliseconds.
        /// </summary>
        public List<long> ProjectTransitiveReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitiveDeclaredExceptions reported analysis durations in milliseconds.
        /// </summary>
        public List<long> ProjectTransitiveDeclaredExceptionsReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all SolutionTransitive reported analysis durations in milliseconds.
        /// </summary>
        public List<long> SolutionTransitiveReportedAnalysisDurationsMs { get; set; } = new();
    }
}
