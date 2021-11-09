using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// Result of querying the issue-list retrieval entry point mainly containing a list of issues.
    /// </summary>
    [Serializable]
    public class IssueTable<T> where T : Issue
    {
        /// <summary>
        /// The version of the removed issues.
        /// If the query was not an actual diff query this will be unset.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly AnalysisVersion startVersion;

        /// <summary>
        /// The version of the added issues for a diff query or simply the version of a normal issue list query (no startVersion)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly AnalysisVersion endVersion;

        /// <summary>
        /// The actual issue data objects.
        /// The issue object contents are dynamic and depend on the queried issue kind, hence the parameter
        /// <typeparamref name="T"/>. The values need to be interpreted according to their column type.
        /// This only contains a subset of the complete data if paging is enabled via <c>offset</c> and <c>limit</c>.
        /// </summary>
        /// <typeparam name="T">Type of the issue. If this is unknown, simply use <see cref="Issue"/>.</typeparam>
        [JsonProperty(Required = Required.Always)]
        public readonly IList<T> rows;

        /// <summary>
        /// The total number of issues.
        /// </summary>
        /// <remarks>
        /// Only available when <c>computeTotalRowCount</c> was specified as <c>true</c>.
        /// Mostly useful when doing paged queries using the query parameters <c>limit</c> and <c>offset</c>.
        /// </remarks>
        [JsonProperty(Required = Required.Default)]
        public readonly uint totalRowCount;

        /// <summary>
        /// The total number of issues existing in the current version and not in the baseline version. 
        /// </summary>
        /// <remarks>
        /// Only useful in diff queries and only calculated when <c>computeTotalRowCount</c> was specified as <c>true</c>.
        /// </remarks>
        [JsonProperty(Required = Required.Default)]
        public readonly uint totalAddedCount;

        /// <summary>
        /// The total number of issues existing in the baseline version and not in the current version. 
        /// </summary>
        /// <remarks>
        /// Only useful in diff queries and only calculated when <c>computeTotalRowCount</c> was specified as <c>true</c>.
        /// </remarks>
        [JsonProperty(Required = Required.Default)]
        public readonly uint totalRemovedCount;

        public IssueTable(AnalysisVersion startVersion, AnalysisVersion endVersion, IList<T> rows, 
                          uint totalRowCount, uint totalAddedCount, uint totalRemovedCount)
        {
            this.startVersion = startVersion;
            this.endVersion = endVersion;
            this.rows = rows;
            this.totalRowCount = totalRowCount;
            this.totalAddedCount = totalAddedCount;
            this.totalRemovedCount = totalRemovedCount;
        }
    }
}