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
        /// This string must not be null when used for report traversal.
        /// </summary>
        public string SearchedNodes { get; set; } = string.Empty;

        /// <summary>
        /// Maps XML element names to XPath expressions that produce the full path identifier.
        /// Dictionary keys and values must not be null.
        /// </summary>
        public Dictionary<string, string> PathBuilders { get; set; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Maps XML element names (context) to XPath expressions that select the file name of a node.
        /// Dictionary keys and values must not be null.
        /// </summary>
        public Dictionary<string, string> FileName { get; set; } =
            new Dictionary<string, string>();

        /// <summary>
        /// Optional mapping from location field names to XPath expressions.
        /// May be null if the report format does not provide explicit locations.
        /// </summary>
        public Dictionary<string, string>? LocationMapping { get; set; }

        /// <summary>
        /// Metric definitions keyed by their output name, each pointing to an XPath expression.
        /// Dictionary keys and values must not be null.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> MetricsByContext { get; set; } =
            new Dictionary<string, Dictionary<string,string>>();

        /// <summary>
        /// Optional namespace prefix or URI map for XPath evaluation.
        /// May be null if the report does not use XML namespaces.
        /// </summary>
        public Dictionary<string, string>? Namespaces { get; set; }

        /// <summary>
        /// Optional template for location metadata, used by parsers that allocate location objects upfront.
        /// May be null if no location template is required.
        /// </summary>
        public MetricLocation MetricLocation;
    }
}
