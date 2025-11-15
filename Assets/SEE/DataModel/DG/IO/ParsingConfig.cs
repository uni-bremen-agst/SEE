using Assets.SEE.DataModel.DG.IO;
using System;
using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Base configuration that describes how a specific tool's report should be interpreted.
    /// </summary>
    public abstract class ParsingConfig
    {
        /// <summary>
        /// Identifier that ties parsed metrics to their origin (e.g., "JaCoCo").
        /// </summary>
        public string ToolId = "";

        /// <summary>
        /// Creates the concrete parser that can process this configuration.
        /// </summary>
        public abstract IReportParser CreateParser();


        /// <summary>
        /// Helper for callers that only need the textual tool identifier.
        /// </summary>
        public string GetToolId()
        {
            return ToolId;
        }

        /// <summary>
        /// Describes which XML nodes to visit and how to interpret them.
        /// </summary>
        public XPathMapping XPathMapping;

        /// <summary>
        /// Creates the concrete Index strategy to find node in SourceRangeIndex.
        /// </summary>
        public abstract IIndexNodeStrategy CreateIndexNodeStrategy();

    }

    /// <summary>
    /// Encapsulates the XPath expressions used to traverse and interpret a report.
    /// </summary>
    public class XPathMapping
    {
        /// <summary>
        /// XPath union expression that selects every node of interest.
        /// </summary>
        public string SearchedNodes { get; set; } = "";

        /// <summary>
        /// Maps XML element names to XPath expressions that produce the full path identifier.
        /// </summary>
        public Dictionary<string, string> PathBuilders { get; set; } = new();


        /// <summary>
        /// Searches for the filename of the xml node.
        /// </summary>
        public Dictionary<string, string> FileName { get; set; } = new() ;

        /// <summary>
        /// Optional mapping from location field names to XPath expressions.
        /// </summary>
        public Dictionary<string, string>? LocationMapping { get; set; }

        /// <summary>
        /// Metric definitions keyed by their output name, each pointing to an XPath expression.
        /// </summary>
        public Dictionary<string, string> Metrics { get; set; } = new();

        /// <summary>
        /// Optional namespace prefix/URI map for XPath evaluation.
        /// </summary>
        public Dictionary<string, string>? Namespaces { get; set; }  

        /// <summary>
        /// Optional template for location metadata, used by parsers that allocate upfront objects.
        /// </summary>
        public MetricLocation MetricLocation;

        /// <summary>
        /// Maps a xml tag to a Context (class, package, method, root)
        /// </summary>
        /// <param name="localName"></param>
        /// <returns></returns>
        public Dictionary<string, string> MapContext { get; set; } = new();
    }

}
