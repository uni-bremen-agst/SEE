using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Container for all findings extracted from a single metrics report.
    /// </summary>
    public class MetricSchema
    {
        /// <summary>
        /// Identifier of the originating tool (for example, JaCoCo, Checkstyle).
        /// </summary>
        public string ToolId { get; set; } = string.Empty;

        /// <summary>
        /// Flat collection of parsed findings, each representing one node/context in the report.
        /// </summary>
        public List<Finding> Findings { get; } = new List<Finding>();
    }

    /// <summary>
    /// Represents a single parsed element together with its metrics and optional location data.
    /// </summary>
    public class Finding
    {
        /// <summary>
        /// Normalized path that can be mapped back to a node in the SEE graph.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Name of the source file. Can be empty if FullPath contains the file name.
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
        public Dictionary<string, string?> Metrics { get; set; } =
            new Dictionary<string, string?>();
    }

    /// <summary>
    /// Optional source-code location for a finding.
    /// </summary>
    public class MetricLocation
    {
        /// <summary>
        /// 1-based starting line within the file, if reported.
        /// </summary>
        public int? StartLine { get; set; }

        /// <summary>
        /// 1-based ending line within the file, if reported.
        /// </summary>
        public int? EndLine { get; set; }

        /// <summary>
        /// 1-based starting column within the line, if reported.
        /// </summary>
        public int? StartColumn { get; set; }

        /// <summary>
        /// 1-based ending column within the line, if reported.
        /// </summary>
        public int? EndColumn { get; set; }
    }
}
