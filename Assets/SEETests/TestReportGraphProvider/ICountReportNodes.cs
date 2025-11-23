using System.Collections.Generic;

namespace SEE.GraphProviders.NodeCounting
{
    /// <summary>
    /// Interface for counting nodes in report files of various formats.
    /// Preconditions: <paramref name="reportPath"/> must not be null or empty and
    /// <paramref name="tagNames"/> must not be null.
    /// </summary>
    internal interface ICountReportNodes
    {
        /// <summary>
        /// Counts the number of nodes for each specified tag name in the report.
        /// </summary>
        /// <param name="reportPath">
        /// Relative path to the report file (relative to StreamingAssets). Must not be null or empty.
        /// </param>
        /// <param name="tagNames">List of tag or property names to count. Must not be null.</param>
        /// <returns>Dictionary with tag names as keys and counts as values.</returns>
        Dictionary<string, int> Count(string reportPath, IEnumerable<string> tagNames);
    }
}
