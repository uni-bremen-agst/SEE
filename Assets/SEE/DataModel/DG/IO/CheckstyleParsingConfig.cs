using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Contains parser configuration and node-indexing helpers for importing external analysis reports.
    /// Parsing configuration for Checkstyle XML reports.
    /// </summary>
    /// <remarks>
    /// Supported nodes:
    /// <list type="bullet">
    /// <item><description><file>: File-level aggregation (counts by severity).</description></item>
    /// <item><description><error>: Individual violations (line/column + rendered message).</description></item>
    /// </list>
    ///
    /// Key idea:
    /// Metrics are configured per context (see <see cref="XPathMapping.MetricsByContext"/>),
    /// so the parser does not need XPath hacks that force irrelevant metrics to evaluate to NaN.
    /// </remarks>
    internal sealed class CheckstyleParsingConfig : XmlParsingConfig
    {
        /// <summary>
        /// Context name for individual Checkstyle violations (<error> nodes).
        /// </summary>
        private const string errorContext = "error";

        /// <summary>
        /// Context name for file-level aggregation (<file> nodes).
        /// </summary>
        private const string fileContext = "file";

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckstyleParsingConfig"/> class.
        /// </summary>
        public CheckstyleParsingConfig()
        {
            ToolId = "Checkstyle";

            // Used by CheckstyleIndexNodeStrategy to trim absolute paths down to package/class.
            SourceRootMarker = "src/main/java/";

            XPathMapping = new XPathMapping
            {
                // Parse both file-level and error-level nodes.
                SearchedNodes = $"//{fileContext}|//{errorContext}",

                // Build a stable "full path" identifier for each node.
                // For Checkstyle, the report contains absolute file paths in @name.
                PathBuilders = new Dictionary<string, string>
                {
                    [fileContext] = "string(@name)",
                    [errorContext] = "string(ancestor::file/@name)",
                },

                // File name extraction (optional for CheckstyleIndexNodeStrategy, but useful for other strategies).
                // We keep this context-first to avoid tag-name ambiguity.
                FileName = new Dictionary<string, string>
                {
                    // The file element has the file path in @name.
                    [fileContext] = "string(@name)",
                    // The error element inherits the file path from its ancestor <file>.
                    [errorContext] = "string(ancestor::file/@name)",
                },

                // Location is only meaningful for <error>, but parsing it globally is safe:
                // for <file> these attributes are missing and are simply ignored.
                LocationMapping = new Dictionary<string, string>
                {
                    ["StartLine"] = "string(@line)",
                    ["StartColumn"] = "string(@column)",
                },

                // Context-specific metrics.
                MetricsByContext = new Dictionary<string, Dictionary<string, string>>
                {
                    [fileContext] = new Dictionary<string, string>
                    {
                        // Count errors by severity within this file.
                        // count(error[@severity='warning']) counts all child <error> elements.
                        ["Aggregated.WarningCount"] = "string(count(error[@severity='warning']))",
                        ["Aggregated.ErrorCount"] = "string(count(error[@severity='error']))",
                        ["Aggregated.InfoCount"] = "string(count(error[@severity='info']))",

                        // Total violations for this file.
                        ["Aggregated.ViolationCount"] = "string(count(error))",
                    },

                    [errorContext] = new Dictionary<string, string>
                    {
                        // A short, human-friendly issue string.
                        // Using substring(...) with a large length keeps this robust even if message is empty.
                        ["ContextLevel.Issue"] =
                            "substring(concat('Line ', @line, ': [', @severity, '] ', @message), 1, 10000)",
                    },
                },
            };
        }

        /// <summary>
        /// Creates the index-node strategy that maps findings to the corresponding graph node identifiers.
        /// </summary>
        /// <returns>An index node strategy suitable for Checkstyle file paths.</returns>
        public override IIndexNodeStrategy CreateIndexNodeStrategy()
        {
            return new JavaIndexNodeStrategy(this);
        }
    }
}
