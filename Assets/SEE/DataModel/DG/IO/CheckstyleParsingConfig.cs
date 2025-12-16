using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Parsing configuration for Checkstyle XML reports.
    ///
    /// This class provides:
    /// - XPath expressions that identify relevant nodes in a Checkstyle report.
    /// - Path builders that map a Checkstyle <c>&lt;file&gt;</c> or <c>&lt;error&gt;</c> node to a stable file path.
    /// - Metric expressions that aggregate violations at file level and (optionally) attach limited error-context data.
    ///
    /// Design:
    /// Checkstyle reports are primarily file-oriented. Here, each <c>&lt;file&gt;</c> element effectively forms
    /// a "file summary" finding by aggregating counts of its child <c>&lt;error&gt;</c> nodes. Additionally,
    /// <c>&lt;error&gt;</c> nodes may be searched as individual nodes, but the strategy intentionally focuses on
    /// aggregation for file-level metrics and a minimal per-error context metric.
    ///
    /// Trade-off:
    /// Aggregation loses most individual error detail (e.g., distinct rule ids, multiple messages, etc.) but yields
    /// stable, file-level metrics that are easier to attach to file or type nodes in the graph.
    ///
    /// Preconditions:
    /// - This configuration is intended only for Checkstyle-compatible XML input.
    /// - Consumers are expected to use the parser created by <see cref="CreateParser"/> together with the index
    ///   strategy created by <see cref="CreateIndexNodeStrategy"/>.
    /// </summary>
    internal class CheckstyleParsingConfig : ParsingConfig
    {
        /// <summary>
        /// XPath snippet used to ensure that aggregation expressions are only evaluated in a file context.
        ///
        /// Motivation:
        /// The parser searches both <c>&lt;file&gt;</c> and <c>&lt;error&gt;</c> nodes (<see cref="XPathMapping.SearchedNodes"/>).
        /// Aggregation expressions like <c>count(error)</c> only make sense on a <c>&lt;file&gt;</c> node.
        ///
        /// Technique:
        /// - <c>boolean(self::file)</c> returns 1 for <c>&lt;file&gt;</c> nodes, 0 otherwise.
        /// - By dividing the aggregation result by that boolean, expressions evaluated on <c>&lt;error&gt;</c>
        ///   nodes will divide by 0 and produce <c>NaN</c> (or an equivalent non-number).
        /// - The <c>XmlReportParser</c> is expected to ignore such non-numeric results (per the inline comment).
        ///
        /// Important:
        /// - This is an intentional "guard" pattern. If the parser behavior changes (e.g., starts treating NaN as 0),
        ///   the semantics of these metrics would change.
        /// </summary>
        private const string onlyIfFile = " div boolean(self::file)";

        /// <summary>
        /// Creates a new parsing configuration for Checkstyle.
        ///
        /// This constructor initializes:
        /// - <see cref="ParsingConfig.ToolId"/> so that metrics can be namespaced consistently.
        /// - <see cref="ParsingConfig.SourceRootMarker"/> which is later used by the index strategy
        ///   to normalize paths to fully qualified class names.
        /// - <see cref="ParsingConfig.XPathMapping"/> containing all XPath selectors and metric expressions.
        ///
        /// Preconditions:
        /// - After construction, the instance is ready to be passed to <see cref="XmlReportParser"/> via <see cref="CreateParser"/>.
        /// </summary>
        public CheckstyleParsingConfig()
        {
            // Used for metric namespacing and tool identification throughout the pipeline.
            ToolId = "Checkstyle";

            // Used by CheckstyleIndexNodeStrategy to cut absolute paths down to a relative package path.
            // Note: trailing slash is included here; the strategy also normalizes/trim-handles it.
            SourceRootMarker = "src/main/java/";

            XPathMapping = new XPathMapping
            {
                /// <summary>
                /// Node selection for parsing.
                ///
                /// We search both:
                /// - <c>//file</c>: each file node becomes a finding that can carry aggregated metrics.
                /// - <c>//error</c>: allows attaching limited per-error context (e.g., message snippet).
                ///
                /// The mapping uses <see cref="PathBuilders"/> to ensure both node types can be traced back to a file path.
                /// </summary>
                SearchedNodes = "//file|//error",

                /// <summary>
                /// For each searched node type, build the "path" that identifies the affected file.
                ///
                /// - For <c>file</c>: the @name attribute is the file path.
                /// - For <c>error</c>: use its ancestor file's @name attribute (since errors are nested inside file).
                ///
                /// This ensures that even when parsing an <c>&lt;error&gt;</c> node, we can resolve and aggregate
                /// metrics back to the file/type node in the graph.
                /// </summary>
                PathBuilders = new Dictionary<string, string>
                {
                    ["file"] = "string(@name)",
                    ["error"] = "string(ancestor::file/@name)"
                },

                /// <summary>
                /// Optional mapping for a "FileName" field.
                ///
                /// For Checkstyle, the index strategy works with paths and derives a qualified name from that.
                /// Therefore, we do not require a separate file name extraction here.
                /// </summary>
                FileName = new Dictionary<string, string>
                {
                    // Not needed for indexing MetricFindings via CheckstyleIndexNodeStrategy
                },

                /// <summary>
                /// Location mapping for line/column information.
                ///
                /// Checkstyle errors often carry <c>@line</c> and <c>@column</c>. For file nodes these may not exist,
                /// but for error nodes they often do. The parser will interpret missing values accordingly.
                /// </summary>
                LocationMapping = new Dictionary<string, string>
                {
                    ["StartLine"] = "string(@line)",
                    ["StartColumn"] = "string(@column)"
                },

                /// <summary>
                /// Metric extraction definitions.
                ///
                /// File context:
                /// - Aggregated counts per severity (warning/error/info) within a single file.
                /// - Total count of all errors within the file.
                ///
                /// Error context:
                /// - A compact string that includes line, severity, and message.
                ///
                /// Guarding:
                /// Aggregated expressions append <see cref="onlyIfFile"/> so that they produce NaN when evaluated
                /// on an <c>&lt;error&gt;</c> node and can be ignored by the parser.
                /// </summary>
                Metrics = new Dictionary<string, string>
                {
                    // ---------- File context ----------
                    // Count errors by severity within this file:
                    // count(error[@severity='warning']) counts all child <error> elements with severity='warning'.
                    ["Aggregated.WarningCount"] = "string(count(error[@severity='warning'])" + onlyIfFile + ")",
                    ["Aggregated.ErrorCount"] = "string(count(error[@severity='error'])" + onlyIfFile + ")",
                    ["Aggregated.InfoCount"] = "string(count(error[@severity='info'])" + onlyIfFile + ")",

                    // Total violation count for this file.
                    ["Aggregated.ViolationCount"] = "string(count(error)" + onlyIfFile + ")",

                    // ---------- Error context ----------
                    // Produces a human-readable issue summary such as:
                    // "Line 42: [warning] Something happened"
                    //
                    // The substring(...) trick with number(boolean(@message)) ensures that if @message is missing,
                    // the max length becomes 0 and the result is an empty string.
                    //
                    // Note: this does not aggregate multiple messages; it describes a single error node.
                    ["Context-Level.Issue"] = "substring(concat('Line ', @line, ': [', @severity, '] ', @message), 1, number(boolean(@message)) * 10000)"
                },

                /// <summary>
                /// Context mapping for node name -> context label.
                ///
                /// This tells the parsing pipeline how to label findings by the node type they originated from.
                /// </summary>
                MapContext = new Dictionary<string, string>
                {
                    ["file"] = "file",
                    ["error"] = "error"
                }
            };
        }

        /// <summary>
        /// Creates the report parser instance used to parse Checkstyle XML with this configuration.
        ///
        /// Preconditions:
        /// - The current instance must have a valid <see cref="ParsingConfig.XPathMapping"/> (initialized in the constructor).
        /// </summary>
        /// <returns>An <see cref="IReportParser"/> that parses XML reports according to this configuration.</returns>
        internal override IReportParser CreateParser()
        {
            return new XmlReportParser(this);
        }

        /// <summary>
        /// Creates the node indexing strategy used to resolve findings to graph node identifiers.
        ///
        /// The returned strategy converts file paths found in Checkstyle reports into the fully qualified
        /// class names used by the graph (typically derived by stripping <see cref="SourceRootMarker"/>
        /// and converting slashes to dots).
        /// </summary>
        /// <returns>An <see cref="IIndexNodeStrategy"/> tailored to Checkstyle path conventions.</returns>
        public override IIndexNodeStrategy CreateIndexNodeStrategy()
        {
            return new CheckstyleIndexNodeStrategy(this);
        }
    }
}
