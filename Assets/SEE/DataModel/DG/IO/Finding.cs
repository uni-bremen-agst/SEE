using System.Collections.Generic;

/// <summary>
/// Contains types for parsing external tool reports and applying their metrics to SEE dependency graphs.
/// </summary>
namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Represents a single parsed report element together with its metrics and optional location data.
    /// </summary>
    public class Finding
    {
        /// <summary>
        /// Normalized identifier that can be mapped back to a node in a <see cref="Graph"/>.
        /// </summary>
        /// <remarks>Preconditions: Should not be null or whitespace if the finding is meant to be applied to a graph.</remarks>
        public string FullPath { get; set; }

        /// <summary>
        /// Name or path of the source file.
        /// </summary>
        /// <remarks>
        /// May be empty if <see cref="FullPath"/> already contains the file name or if the report format does not
        /// expose a file name separately.
        /// </remarks>
        public string FileName { get; set; }

        /// <summary>
        /// Context or tag name under which the finding was reported (for example, class, method, file).
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Optional location information such as lines and columns.
        /// </summary>
        public MetricLocation? Location { get; set; }

        /// <summary>
        /// Metric values emitted for this finding, keyed by a descriptive string.
        /// </summary>
        /// <remarks>Preconditions: Never null.</remarks>
        public Dictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();
    }
}
