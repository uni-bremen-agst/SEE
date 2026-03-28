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
}
