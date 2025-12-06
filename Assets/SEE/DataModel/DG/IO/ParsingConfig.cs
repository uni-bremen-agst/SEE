/// <summary>
/// Contains data model types for parsing and interpreting external tool reports in a <see cref="Graph"/>.
/// </summary>
namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Base configuration that describes how a specific tool's report should be interpreted.
    /// Implementations must provide a non-null tool identifier and a valid XPath mapping before use.
    /// </summary>
    public abstract class ParsingConfig
    {
        /// <summary>
        /// Identifier that ties parsed metrics to their origin (for example, "JaCoCo").
        /// This value must not be null when a parser uses this configuration.
        /// </summary>
        public string ToolId = string.Empty;

        /// <summary>
        /// Describes which XML nodes to visit and how to interpret them.
        /// This value must not be null when a parser uses this configuration.
        /// </summary>
        public XPathMapping XPathMapping;

        /// <summary>
        /// Creates the concrete parser that can process this configuration.
        /// The returned parser instance must not be null.
        /// </summary>
        /// <returns>
        /// A concrete <see cref="IReportParser"/> that can interpret reports described by this configuration.
        /// </returns>
        internal abstract IReportParser CreateParser();

        /// <summary>
        /// Helper for callers that only need the textual tool identifier.
        /// </summary>
        /// <returns>
        /// The identifier of the tool. The returned string is never null.
        /// </returns>
        public string GetToolId()
        {
            return ToolId;
        }

        /// <summary>
        /// Creates the concrete index strategy that is used to find nodes in a <c>SourceRangeIndex</c>.
        /// The returned strategy instance must not be null.
        /// </summary>
        /// <returns>
        /// An <see cref="IIndexNodeStrategy"/> used to locate nodes in a <c>SourceRangeIndex</c>.
        /// </returns>
        public abstract IIndexNodeStrategy CreateIndexNodeStrategy();
    }
}
