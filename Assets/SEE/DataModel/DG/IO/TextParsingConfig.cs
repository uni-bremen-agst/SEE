using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Configuration for parsing line-oriented text reports using regular expressions.
    /// </summary>
    /// <remarks>
    /// This configuration uses a pattern-matching approach where each line context
    /// has its own regex patterns for extracting structured data.
    ///
    /// Example usage (PMD text report):
    /// <code>
    /// LinePatterns = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["violation"] = @"^(?&lt;file&gt;.+?):(?&lt;line&gt;\d+):\s+(?&lt;message&gt;.+)$"
    /// }
    ///
    /// PathBuilders = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["violation"] = "${file}"
    /// }
    ///
    /// MetricsByContext = new Dictionary&lt;string, Dictionary&lt;string, string&gt;&gt;
    /// {
    ///     ["violation"] = new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["Issue"] = "${message}"
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class TextParsingConfig : ParsingConfig
    {
        /// <summary>
        /// Maps context names to regex patterns that identify and capture data from matching lines.
        /// </summary>
        /// <remarks>
        /// Each pattern should use named capture groups that can be referenced in other mappings.
        /// Preconditions: Must not be null when a text parser uses this configuration.
        /// </remarks>
        public Dictionary<string, string> LinePatterns { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Maps context names to template strings that build the full path identifier.
        /// Templates can reference named groups from <see cref="LinePatterns"/> using ${groupName} syntax.
        /// </summary>
        /// <remarks>Preconditions: Dictionary keys must match contexts defined in <see cref="LinePatterns"/>.</remarks>
        public Dictionary<string, string> PathBuilders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Maps context names to template strings that extract the file name.
        /// Templates can reference named groups from <see cref="LinePatterns"/> using ${groupName} syntax.
        /// </summary>
        /// <remarks>May be empty if file names are already captured in PathBuilders.</remarks>
        public Dictionary<string, string> FileNameTemplates { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Maps standard location field names to capture group names from the line patterns.
        /// </summary>
        /// <remarks>
        /// Supported keys: StartLine, EndLine, StartColumn, EndColumn.
        /// Values should be capture group names (without ${} syntax).
        /// May be null if the report format does not provide explicit locations.
        /// </remarks>
        public Dictionary<string, string>? LocationMapping { get; set; }

        /// <summary>
        /// Metric definitions keyed by context, each containing metric names mapped to template strings.
        /// Templates can reference named groups from <see cref="LinePatterns"/> using ${groupName} syntax.
        /// </summary>
        /// <remarks>Preconditions: Outer dictionary keys must match contexts defined in <see cref="LinePatterns"/>.</remarks>
        public Dictionary<string, Dictionary<string, string>> MetricsByContext { get; set; } =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Optional regex options applied to all patterns in <see cref="LinePatterns"/>.
        /// </summary>
        /// <remarks>Defaults to RegexOptions.None if not specified.</remarks>
        public RegexOptions RegexOptions { get; set; } = RegexOptions.None;

        /// <summary>
        /// Optional filter that determines which lines should be processed.
        /// If null, all lines are processed. If set, only lines matching this pattern are considered.
        /// </summary>
        /// <remarks>Useful for skipping header lines or filtering out noise in verbose reports.</remarks>
        public string? LineFilter { get; set; }

        /// <summary>
        /// Creates a <see cref="TextReportParser"/> configured for text input.
        /// </summary>
        /// <remarks>
        /// Preconditions: <see cref="LinePatterns"/> and <see cref="ParsingConfig.ToolId"/> must be initialized.
        /// </remarks>
        /// <returns>An <see cref="IReportParser"/> instance for text reports.</returns>
        internal override IReportParser CreateParser()
        {
            return new TextReportParser(this);
        }
    }
}
