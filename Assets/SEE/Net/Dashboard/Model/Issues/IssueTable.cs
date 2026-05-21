using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        [JsonProperty(PropertyName = "startVersion", Required = Required.Default)]
        public readonly AnalysisVersion StartVersion;

        /// <summary>
        /// The version of the added issues for a diff query or simply the version of a normal issue list query (no startVersion)
        /// </summary>
        [JsonProperty(PropertyName = "endVersion", Required = Required.Always)]
        public readonly AnalysisVersion EndVersion;

        /// <summary>
        /// The actual issue data objects.
        /// The issue object contents are dynamic and depend on the queried issue kind, hence the parameter
        /// <typeparamref name="T"/>. The values need to be interpreted according to their column type.
        /// This only contains a subset of the complete data if paging is enabled via offset and limit.
        /// </summary>
        /// <typeparam name="T">Type of the issue. If this is unknown, simply use <see cref="Issue"/>.</typeparam>
        [JsonProperty(PropertyName = "rows", Required = Required.Always)]
        public readonly IList<T> Rows;

        /// <summary>
        /// The total number of issues.
        /// </summary>
        /// <remarks>
        /// Only available when computeTotalRowCount was specified as true.
        /// Mostly useful when doing paged queries using the query parameters limit and offset.
        /// </remarks>
        [JsonProperty(PropertyName = "totalRowCount", Required = Required.Default)]
        public readonly uint TotalRowCount;

        /// <summary>
        /// The total number of issues existing in the current version and not in the baseline version.
        /// </summary>
        /// <remarks>
        /// Only useful in diff queries and only calculated when computeTotalRowCount was specified as true.
        /// </remarks>
        [JsonProperty(PropertyName = "totalAddedCount", Required = Required.Default)]
        public readonly uint TotalAddedCount;

        /// <summary>
        /// The total number of issues existing in the baseline version and not in the current version.
        /// </summary>
        /// <remarks>
        /// Only useful in diff queries and only calculated when computeTotalRowCount was specified as true.
        /// </remarks>
        [JsonProperty(PropertyName = "totalRemovedCount", Required = Required.Default)]
        public readonly uint TotalRemovedCount;

        public IssueTable(AnalysisVersion startVersion, AnalysisVersion endVersion, IList<T> rows,
                          uint totalRowCount, uint totalAddedCount, uint totalRemovedCount)
        {
            this.StartVersion = startVersion;
            this.EndVersion = endVersion;
            this.Rows = rows;
            this.TotalRowCount = totalRowCount;
            this.TotalAddedCount = totalAddedCount;
            this.TotalRemovedCount = totalRemovedCount;
        }
    }
}