using System.Collections.Generic;

namespace SEE.GraphProviders.NodeCounting
{
    /// <summary>
    /// Interface for counting nodes in report files of various formats.
    /// </summary>
    public interface ICountReportNodes
    {
        /// <summary>
        /// Counts the number of nodes for each specified tag name in the report.
        /// </summary>
        /// <param name="reportPath">Relative path to the report file (relative to StreamingAssets)</param>
        /// <param name="tagNames">List of tag/property names to count</param>
        /// <returns>Dictionary with tag names as keys and counts as values</returns>
        Dictionary<string, int> Count(string reportPath, IEnumerable<string> tagNames);
    }
}
