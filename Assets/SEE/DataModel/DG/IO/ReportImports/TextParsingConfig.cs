using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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
    [Serializable]
    public abstract class TextParsingConfig : ParsingConfig
    {
        /// <summary>
        /// Maps context names to regex patterns that identify and capture data from matching lines.
        /// </summary>
        /// <remarks>
        /// Each pattern should use named capture groups that can be referenced in other mappings.
        /// Preconditions: Must not be null when a text parser uses this configuration.
        /// </remarks>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public Dictionary<string, string> LinePatterns { get; set; } = new();

        /// <summary>
        /// Maps context names to template strings that build the full path identifier.
        /// Templates can reference named groups from <see cref="LinePatterns"/> using ${groupName} syntax.
        ///
        /// Preconditions: Dictionary keys must match contexts defined in <see cref="LinePatterns"/>.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public Dictionary<string, string> PathBuilders { get; set; } = new();

        /// <summary>
        /// Maps context names to template strings that extract the file name.
        /// Templates can reference named groups from <see cref="LinePatterns"/> using ${groupName} syntax.
        ///
        /// May be empty if file names are already captured in PathBuilders.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public Dictionary<string, string> FileNameTemplates { get; set; } = new();

        /// <summary>
        /// Maps standard location field names to capture group names from the line patterns.
        /// Supported keys: StartLine, EndLine, StartColumn, EndColumn.
        /// Values should be capture group names (without ${} syntax).
        /// May be null if the report format does not provide explicit locations.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public Dictionary<string, string>? LocationMapping { get; set; }

        /// <summary>
        /// Metric definitions keyed by context, each containing metric names mapped to template strings.
        /// Templates can reference named groups from <see cref="LinePatterns"/> using ${groupName} syntax.
        ///
        /// Preconditions: Outer dictionary keys must match contexts defined in <see cref="LinePatterns"/>.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public Dictionary<string, Dictionary<string, string>> MetricsByContext { get; set; } = new();

        /// <summary>
        /// Optional regex options applied to all patterns in <see cref="LinePatterns"/>.
        ///
        /// Defaults to RegexOptions.None if not specified.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public RegexOptions RegexOptions { get; set; } = RegexOptions.None;

        /// <summary>
        /// Optional filter that determines which lines should be processed.
        /// If null, all lines are processed. If set, only lines matching this pattern are considered.
        /// Useful for skipping header lines or filtering out noise in verbose reports.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public string? LineFilter { get; set; }

        /// <summary>
        /// Creates a <see cref="TextReportParser"/> configured for text input.
        /// </summary>
        /// <returns>An <see cref="IReportParser"/> instance for text reports.</returns>
        /// <remarks>
        /// Preconditions: <see cref="LinePatterns"/> and <see cref="ParsingConfig.ToolId"/> must be initialized.
        /// </remarks>
        internal override IReportParser CreateParser()
        {
            return new TextReportParser(this);
        }
    }
}
