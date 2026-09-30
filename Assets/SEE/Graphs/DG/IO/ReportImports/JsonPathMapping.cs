using System.Collections.Generic;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Encapsulates the JSONPath expressions used to traverse and interpret a JSON report.
    /// Structured analogously to <see cref="XPathMapping"/>.
    /// </summary>
    public class JsonPathMapping
    {
        /// <summary>
        /// Maps context names (e.g. "class", "method") to JSONPath expressions that select the list of elements.
        /// Analogous to <see cref="XPathMapping.SearchedNodes"/> but split by context for JSON.
        /// </summary>
        public Dictionary<string, string> SelectElements { get; set; } = new();

        /// <summary>
        /// Maps context names to JSONPath expressions that produce the full path identifier (ID).
        /// The result of this expression is stored in <see cref="Finding.FullPath"/>.
        /// </summary>
        public Dictionary<string, string> PathBuilders { get; set; } = new();

        /// <summary>
        /// Maps context names to JSONPath expressions that select the file name.
        /// </summary>
        public Dictionary<string, string> FileName { get; set; } = new();

        /// <summary>
        /// Optional mapping from location field names to JSONPath expressions.
        /// </summary>
        public Dictionary<string, string>? LocationMapping { get; set; }

        /// <summary>
        /// Metric definitions keyed by context, pointing to JSONPath expressions relative to the selected element.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> MetricsByContext { get; set; } = new();
    }
}
