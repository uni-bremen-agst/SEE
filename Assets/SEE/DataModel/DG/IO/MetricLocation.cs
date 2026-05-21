namespace SEE.DataModel.DG.IO
{
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
