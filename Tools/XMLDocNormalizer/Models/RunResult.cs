namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Accumulates results across a single tool run.
    /// </summary>
    internal sealed class RunResult
    {
        /// <summary>
        /// Gets or sets the total number of malformed XML documentation findings.
        /// </summary>
        public int MalformedCount { get; set; }

        /// <summary>
        /// Gets or sets the number of files that were changed in fix mode.
        /// </summary>
        public int ChangedFiles { get; set; }
    }
}
