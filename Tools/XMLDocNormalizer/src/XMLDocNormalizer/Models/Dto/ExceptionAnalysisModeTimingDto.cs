namespace XMLDocNormalizer.Models.DTO
{
    /// <summary>
    /// Represents timing information for a comparison run across all exception analysis modes.
    /// </summary>
    internal sealed class ExceptionAnalysisModeTimingDto
    {
        /// <summary>
        /// Gets or sets the duration of the shared detectors in milliseconds.
        /// </summary>
        public long SharedDetectorsDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the Direct exception detector duration in milliseconds.
        /// </summary>
        public long DirectExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the ProjectTransitive exception detector duration in milliseconds.
        /// </summary>
        public long ProjectTransitiveExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the ProjectTransitiveProjectExceptions exception detector duration in milliseconds.
        /// </summary>
        public long ProjectTransitiveProjectExceptionsExceptionDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the SolutionTransitive exception detector duration in milliseconds.
        /// </summary>
        public long SolutionTransitiveExceptionDurationMs { get; set; }
    }
}
