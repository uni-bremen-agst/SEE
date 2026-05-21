using System.Collections.Generic;

/// <summary>
/// Contains data model types for parsing and interpreting external tool reports in a <see cref="Graph"/>.
/// </summary>
namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Encapsulates the XPath expressions used to traverse and interpret a report.
    /// All XPath expressions must be valid for the corresponding report format.
    /// </summary>
    public class XPathMapping
    {
        /// <summary>
        /// XPath union expression that selects every XML node of interest.
        /// </summary>
        /// <remarks>Preconditions: Must not be null when used for report traversal.</remarks>
        public string SearchedNodes { get; set; } = string.Empty;

        /// <summary>
        /// Maps XML element names to XPath expressions that produce the full path identifier.
        /// </summary>
        /// <remarks>Preconditions: Dictionary keys and values must not be null.</remarks>
        public Dictionary<string, string> PathBuilders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Maps XML element names (context) to XPath expressions that select the file name of a node.
        /// </summary>
        /// <remarks>Preconditions: Dictionary keys and values must not be null.</remarks>
        public Dictionary<string, string> FileName { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Optional mapping from location field names to XPath expressions.
        /// </summary>
        /// <remarks>May be null if the report format does not provide explicit locations.</remarks>
        public Dictionary<string, string>? LocationMapping { get; set; }

        /// <summary>
        /// Metric definitions keyed by their output name, each pointing to a context-specific XPath expression.
        /// </summary>
        /// <remarks>Preconditions: Dictionary keys and values must not be null.</remarks>
        public Dictionary<string, Dictionary<string, string>> MetricsByContext { get; set; } =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Optional namespace prefix or URI map for XPath evaluation.
        /// </summary>
        /// <remarks>May be null if the report does not use XML namespaces.</remarks>
        public Dictionary<string, string>? Namespaces { get; set; }

        /// <summary>
        /// Optional template for location metadata, used by parsers that allocate location objects upfront.
        /// </summary>
        /// <remarks>May be null if no location template is required.</remarks>
        public MetricLocation MetricLocation = null!;
    }
}
