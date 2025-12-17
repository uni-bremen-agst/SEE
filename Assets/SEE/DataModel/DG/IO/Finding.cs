using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Represents a single parsed element together with its metrics and optional location data.
    /// </summary>
    public class Finding
    {
        /// <summary>
        /// Normalized path that can be mapped back to a node in a <see cref="Graph"/>.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Name of the source file. Can be empty if <see cref="FullPath"/> contains the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Context or tag name under which the finding was reported (for example, class, method).
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Optional location information such as lines and columns.
        /// </summary>
        public MetricLocation? Location { get; set; }

        /// <summary>
        /// Metric values emitted for this finding, keyed by a descriptive string.
        /// </summary>
        public Dictionary<string, string> Metrics { get; set; }
            = new Dictionary<string, string>();
    }
}
