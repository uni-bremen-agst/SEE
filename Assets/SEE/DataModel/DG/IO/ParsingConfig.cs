using System;
using System.Collections.Generic;


/// <summary>
/// Contains data model types for parsing and interpreting external tool reports in the SEE dependency graph.
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
        /// Windows path separator (<c>\</c>), used to detect and normalize Windows-style paths.
        /// </summary>
        private const char WindowsPathSeparator = '\\';

        /// <summary>
        /// Linux/Unix path separator (<c>/</c>), used as the normalized internal separator.
        /// </summary>
        private const char LinuxPathSeparator = '/';

        /// <summary>
        /// Identifier that ties parsed metrics to their origin (for example, "JaCoCo").
        /// This value must not be null when a parser uses this configuration.
        /// </summary>
        public string ToolId = string.Empty;

        /// <summary>
        /// Source root marker of the project
        /// </summary>
        public string SourceRootMarker = string.Empty;

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

        public string SourceRootRelativePath(string fullPath)
        {
            // 1) Normalize path separators to a single canonical separator.
            string normalized = fullPath.Replace(WindowsPathSeparator, LinuxPathSeparator);

            // 2) Cut off everything before the configured source root marker, if available.
            if (!string.IsNullOrWhiteSpace(SourceRootMarker))
            {
                // Normalize the marker itself to use '/' and no leading/trailing slashes.
                string marker = SourceRootMarker.Replace('\\', '/').Trim('/');
                string needle = "/" + marker + "/";

                // Prefer a clean boundary match ("/marker/") from the end of the path (LastIndexOf).
                int idx = normalized.LastIndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    normalized = normalized.Substring(idx + needle.Length);
                else
                {
                    // Fallback: fuzzy match on the marker substring.
                    // This is less precise but may still yield acceptable results for unusual path layouts.
                    idx = normalized.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        normalized = normalized.Substring(idx + marker.Length).TrimStart('/');
                }
            }
            return normalized;
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
        /// Maps XML element names to XPath expressions that select the file name of a node.
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
        public Dictionary<string, string> Metrics { get; set; } =
            new Dictionary<string, string>();

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

        /// <summary>
        /// Maps an XML tag name to a context designation such as class, package, method or root.
        /// Dictionary keys and values must not be null.
        /// </summary>
        public Dictionary<string, string> MapContext { get; set; } =
            new Dictionary<string, string>();
    }
}
