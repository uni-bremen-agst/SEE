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
        /// <value>
        /// The execution isolation strategy used by the comparison run.
        /// </value>
        public string ExecutionIsolation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mode execution order strategy used by the comparison run.
        /// </summary>
        /// <value>
        /// The mode execution order strategy used by the comparison run.
        /// </value>
        public string ModeOrderStrategy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether wall-clock durations include child process startup.
        /// </summary>
        /// <value>
        /// True if wall-clock durations include child process startup; otherwise false.
        /// </value>
        public bool IncludesProcessStartup { get; set; }

        /// <summary>
        /// Gets or sets the measured run count per mode.
        /// </summary>
        /// <value>
        /// The measured run count per mode.
        /// </value>
        public int RunCount { get; set; }

        /// <summary>
        /// Gets or sets the warmup run count per mode.
        /// </summary>
        /// <value>
        /// The warmup run count per mode.
        /// </value>
        public int WarmupRunCount { get; set; }

        /// <summary>
        /// Gets or sets the duration of the shared detectors in milliseconds.
        /// This value is zero for isolated process comparisons because no shared detector phase is reused.
        /// </summary>
        /// <value>
        /// The duration of the shared detectors in milliseconds.
        /// </value>
        public long SharedDetectorsDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative Direct reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        /// <value>
        /// The representative Direct reported analysis duration in milliseconds.
        /// </value>
        public long DirectExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitive reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        /// <value>
        /// The representative ProjectTransitive reported analysis duration in milliseconds.
        /// </value>
        public long ProjectTransitiveExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitiveDeclaredExceptions reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        /// <value>
        /// The representative ProjectTransitiveDeclaredExceptions reported analysis duration in milliseconds.
        /// </value>
        public long ProjectTransitiveDeclaredExceptionsExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative SolutionTransitive reported analysis duration in milliseconds.
        /// Retained for compatibility with earlier comparison reports.
        /// </summary>
        /// <value>
        /// The representative SolutionTransitive reported analysis duration in milliseconds.
        /// </value>
        public long SolutionTransitiveExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative Direct wall-clock child process duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative Direct wall-clock child process duration in milliseconds.
        /// </value>
        public long DirectWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitive wall-clock child process duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative ProjectTransitive wall-clock child process duration in milliseconds.
        /// </value>
        public long ProjectTransitiveWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitiveDeclaredExceptions wall-clock child process duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative ProjectTransitiveDeclaredExceptions wall-clock child process duration in milliseconds.
        /// </value>
        public long ProjectTransitiveDeclaredExceptionsWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative SolutionTransitive wall-clock child process duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative SolutionTransitive wall-clock child process duration in milliseconds.
        /// </value>
        public long SolutionTransitiveWallClockDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative Direct reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative Direct reported analysis duration in milliseconds.
        /// </value>
        public long DirectReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitive reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative ProjectTransitive reported analysis duration in milliseconds.
        /// </value>
        public long ProjectTransitiveReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative ProjectTransitiveDeclaredExceptions reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative ProjectTransitiveDeclaredExceptions reported analysis duration in milliseconds.
        /// </value>
        public long ProjectTransitiveDeclaredExceptionsReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the representative SolutionTransitive reported analysis duration in milliseconds.
        /// </summary>
        /// <value>
        /// The representative SolutionTransitive reported analysis duration in milliseconds.
        /// </value>
        public long SolutionTransitiveReportedAnalysisDurationMs { get; set; }

        /// <summary>
        /// Gets or sets all Direct wall-clock durations in milliseconds.
        /// </summary>
        /// <value>
        /// All Direct wall-clock durations in milliseconds.
        /// </value>
        public List<long> DirectWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitive wall-clock durations in milliseconds.
        /// </summary>
        /// <value>
        /// All ProjectTransitive wall-clock durations in milliseconds.
        /// </value>
        public List<long> ProjectTransitiveWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitiveDeclaredExceptions wall-clock durations in milliseconds.
        /// </summary>
        /// <value>
        /// All ProjectTransitiveDeclaredExceptions wall-clock durations in milliseconds.
        /// </value>
        public List<long> ProjectTransitiveDeclaredExceptionsWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all SolutionTransitive wall-clock durations in milliseconds.
        /// </summary>
        /// <value>
        /// All SolutionTransitive wall-clock durations in milliseconds.
        /// </value>
        public List<long> SolutionTransitiveWallClockDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all Direct reported analysis durations in milliseconds.
        /// </summary>
        /// <value>
        /// All Direct reported analysis durations in milliseconds.
        /// </value>
        public List<long> DirectReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitive reported analysis durations in milliseconds.
        /// </summary>
        /// <value>
        /// All ProjectTransitive reported analysis durations in milliseconds.
        /// </value>
        public List<long> ProjectTransitiveReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all ProjectTransitiveDeclaredExceptions reported analysis durations in milliseconds.
        /// </summary>
        /// <value>
        /// All ProjectTransitiveDeclaredExceptions reported analysis durations in milliseconds.
        /// </value>
        public List<long> ProjectTransitiveDeclaredExceptionsReportedAnalysisDurationsMs { get; set; } = new();

        /// <summary>
        /// Gets or sets all SolutionTransitive reported analysis durations in milliseconds.
        /// </summary>
        /// <value>
        /// All SolutionTransitive reported analysis durations in milliseconds.
        /// </value>
        public List<long> SolutionTransitiveReportedAnalysisDurationsMs { get; set; } = new();
    }
}
