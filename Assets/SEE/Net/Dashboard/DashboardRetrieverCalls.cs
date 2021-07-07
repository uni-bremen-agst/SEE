using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using HtmlAgilityPack;
using SEE.Net.Dashboard.Model;
using SEE.Net.Dashboard.Model.Issues;
using SEE.Net.Dashboard.Model.Metric;
using UnityEngine;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// The part of the <see cref="DashboardRetriever"/> which contains the actual API calls.
    /// Basically a wrapper around the Axivion Dashboard API as described in its documentation.
    /// In fact, most of the documentation here has been copied and modified from the dashboard itself.
    /// </summary>
    public partial class DashboardRetriever
    {
        #region Dashboard Info API

        /// <summary>
        /// Retrieves dashboard information from the dashboard configured for this <see cref="DashboardRetriever"/>.
        /// IMPORTANT NOTE: This will only work if your token has full permissions, i.e., when it's not just
        /// an IDE token. If you simply want to retrieve the dashboard version, use
        /// </summary>
        /// <returns>Dashboard information about the queried dashboard.</returns>
        public async UniTask<DashboardInfo> GetDashboardInfo() => await QueryDashboard<DashboardInfo>("/../../", apiPath: false);

        /// <summary>
        /// Returns the version number of the dashboard that's being queried.
        /// </summary>
        /// <returns>version number of the dashboard that's being queried.</returns>
        /// <remarks>We first try to get this using <see cref="GetDashboardInfo"/>, but typical IDE tokens don't have
        /// enough permissions to access that API endpoint. In that case, we instead deliberately cause an error by
        /// trying to access it, because the version number is supplied in the <see cref="DashboardError"/> object.
        /// </remarks>
        public async UniTask<DashboardVersion> GetDashboardVersion()
        {
            DashboardVersion version;
            try
            {
                version = new DashboardVersion((await GetDashboardInfo()).dashboardVersionNumber);
            }
            catch (DashboardException e)
            {
                if (e.Error == null)
                {
                    throw;
                }

                version = new DashboardVersion(e.Error.dashboardVersionNumber);
            }

            return version;
        }

        #endregion

        #region Issue Lists API

        /// <summary>
        /// Queries the issue lists.
        /// </summary>
        /// <param name="start">The diff start version as gotten by the version’s date property.
        /// Defaults to the <c>EMPTY</c> version if omitted, i.e. no diff will be displayed</param>
        /// <param name="end">The diff end version as gotten by the version’s date property.
        /// Defaults to the newest version if omitted.</param>
        /// <param name="state">Especially relevant when querying an actual diff (defaults to changed):
        /// <ul>
        /// <li><b>added</b>: Show only issues that did not exist in <paramref name="start"/> but exist in
        /// <paramref name="end"/>.</li>
        /// <li><b>removed</b>: Show only issues that existed in <paramref name="start"/> but do not exist in
        /// <paramref name="end"/> any more.</li>
        /// <li><b>changed</b>: Show added and removed issues.</li>
        /// </ul>
        /// When not querying an actual diff all issues are considered added.
        ///</param>
        /// <param name="user">Only show issues of the given user referenced by the name attribute of the project user.
        /// Defaults to <c>ANYBODY</c>, i.e. the result is not filtered by owner at all.</param>
        /// <param name="columnFilters">A dictionary where the key is the field name that's being filtered for
        /// and the value the filter string. It's highly recommended to use <c>nameof</c> for the key,
        /// e.g. if you want to filter suppressed issues use <c>nameof(Issue.suppressed)</c>.</param>
        /// <param name="fileFilter">Returns issues where the file matches the given path.
        /// Substring matching is used and wildcards (*) are supported. If you want to use whole string matching,
        /// enclose the search query in "double quotes".In the case of issues which have more than one file
        /// (e.g. <see cref="CloneIssue"/>), all filenames will be queried. </param>
        /// <param name="limit">Limit the number of returned issues to the given number.
        /// Returns a deterministic result. Useful for paging.</param>
        /// <param name="offset">Omit the issues before the given 0 based index. Returns a deterministic result.
        /// Useful for paging.</param>
        /// <param name="computeTotalRowCount">Whether to include total issue counts in the result. Useful for paging.
        /// Be aware that calculating this might involve additional database accesses.</param>
        /// <typeparam name="T">The issue kind that shall be queried.</typeparam>
        /// <returns>The list of queried issues, as represented by the <see cref="IssueTable{T}"/>.</returns>
        /// <exception cref="ArgumentException">If the given <paramref name="columnFilters"/> contain a field
        /// not present on the given type <typeparamref name="T"/>.</exception>
        /// <remarks>
        /// TODO: Note that sorting issues is not yet supported.
        /// </remarks>
        public async UniTask<IssueTable<T>> GetIssues<T>(string start = null, string end = null,
                                                         Issue.IssueState state = Issue.IssueState.changed,
                                                         string user = null, string fileFilter = null,
                                                         IReadOnlyDictionary<string, string> columnFilters = null,
                                                         int limit = int.MaxValue,
                                                         int offset = 0, bool computeTotalRowCount = false)
            where T : Issue, new()
        {
            const string ANY_PATH = "filter_any path";
            // add non-nullable parameters
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                ["kind"] = new T().IssueKind,
                ["start"] = start,
                ["end"] = end,
                ["state"] = state.ToString(),
                ["user"] = user,
                ["limit"] = limit.ToString(),
                ["offset"] = offset.ToString(),
                ["computeTotalRowCount"] = computeTotalRowCount.ToString().ToLower()
            };
            // remove null values
            parameters = parameters.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);
            if (columnFilters != null)
            {
                if (columnFilters.Any(x => x.Key == null || x.Value == null))
                {
                    throw new ArgumentNullException();
                }

                if (columnFilters.ContainsKey(ANY_PATH))
                {
                    throw new ArgumentException($"When filtering for file paths, use the {nameof(fileFilter)} parameter!");
                }

                if (columnFilters.Any(x => !typeof(T).GetFields().Select(f => f.Name).Contains(x.Key)))
                {
                    throw new ArgumentException($"The given {nameof(columnFilters)} may only contain field names from"
                                                + $"the queried class (in this case, {typeof(T).Name})!");
                }

                // Add "filter_{column}" parameters
                parameters = parameters.Concat(columnFilters.Select(x => new KeyValuePair<string, string>($"filter_{x.Key}", x.Value)))
                                       .ToDictionary(x => x.Key, x => x.Value);
            }

            if (fileFilter != null)
            {
                parameters[ANY_PATH] = fileFilter;
            }

            return await QueryDashboard<IssueTable<T>>("/issues/", parameters, false);
        }

        #endregion

        #region Metrics API

        /// <summary>
        /// Use this to get the project's System Entity.
        /// The version as well as versioned <see cref="Entity"/> properties will only be returned
        /// if a version is specified. The versioned attributes are <c>path</c> and <c>line</c>.
        /// </summary>
        /// <param name="projectName">The name of the Project</param>
        /// <param name="version">The optional version query string for the entity properties.</param>
        /// <returns>An <see cref="EntityList"/> containing only the System Entity.</returns>
        public async UniTask<EntityList> GetSystemEntity(string version = null) =>
            await QueryDashboard<EntityList>("/getSystemEntity", new[] {version});

        /// <summary>
        /// Returns a list of Entities available in the Project.
        /// Note, that this list is not necessarily complete.
        /// By default only entities associated with Issues are stored in the database.
        /// Storing all Metrics (and their associated Entities) has to be enabled explicitly
        /// in your project configuration. The version as well as versioned Entity properties will only be returned,
        /// if a version is specified. The versioned attributes are path and line.
        /// </summary>
        /// <param name="version">The optional version query string for the entity properties.
        /// If not specified, versioned entity attributes will not be included in the result.
        /// If it is specified, entities not available in that version will not be included in the result.</param>
        /// <param name="metric">If a Metric ID is given, only Entities having associated the given Metric will be
        /// returned.</param>
        /// <returns>List of Entities available in the given version.</returns>
        public async UniTask<EntityList> GetEntities(string version = null, string metric = null) =>
            await QueryDashboard<EntityList>("/getEntities", new[] {version, metric});

        /// <summary>
        /// Returns a list of Metrics available for the database that can be used to create nice charts over time.
        /// The Version and versioned Metric properties will only be returned if a version is specified.
        /// The versioned properties are <c>minValue<c> and <c>maxValue</c>.
        /// </summary>
        /// <param name="version">The optional version query string for the metric properties.
        /// If not specified, versioned metric attributes will not be included in the result.
        /// If it is specified metrics not available in that version will not be included in the result.
        /// </param>
        /// <param name="entity">If an Entity ID is given, only Metrics associated with the given Entity will be
        /// returned.</param>
        /// <returns>The metrics available in the given version.</returns>
        public async UniTask<MetricList> GetMetrics(string version = null, string entity = null) =>
            await QueryDashboard<MetricList>("/getMetrics", new[] {version, entity});

        /// <summary>
        /// Queries a <paramref name="metric"/> for a particular <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The Entity ID to fetch the values for.</param>
        /// <param name="metric">The Metric ID to fetch the values for.</param>
        /// <param name="start">The result Version range start specifier.</param>
        /// <param name="end">The result Version range end (inclusive) specifier.</param>
        /// <returns></returns>
        public async UniTask<MetricValueRange> GetMetricValueRange(string entity, string metric, string start = null,
                                                                   string end = null) =>
            await QueryDashboard<MetricValueRange>("/queryMetricValueRange", new[] {entity, metric, start, end});

        /// <summary>
        /// This allows querying metric values of a specific version with properties flattened out in a tabular format
        /// similar to the Issue List API. In contrast to the issue list entry point,
        /// this entry point does not support sorting or filtering.
        /// </summary>
        /// <param name="version">The Version of the table data.</param>
        /// <returns>The metric value table data.</returns>
        public async UniTask<MetricValueTable> GetMetricValueTable(string version = null) =>
            await QueryDashboard<MetricValueTable>("/queryMetricValueTable", new[] {version});

        #endregion

        #region Unofficial APIs

        /// <summary>
        /// Retrieves the issue description for the given <paramref name="issueName"/>.
        /// This will return an empty string if the retrieved issue description contains HTML tags.
        /// Note that this implementation is very hacky and may easily break for more complex descriptions
        /// or for older/more recent versions of the Axivion Dashboard. 
        /// </summary>
        /// <param name="issueName">The ID of the issue whose rule text shall be displayed</param>
        /// <param name="version">The optional analysis version of the issue.</param>
        /// <returns>The description/explanation of the issue's rule, or an empty string if it would otherwise
        /// contain HTML tags.</returns>
        public async UniTask<string> GetIssueDescription(string issueName, string version = null)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string> {["version"] = version};
            DashboardResult result = await GetAtPath($"/issues/{issueName}/rule", version == null ? null : parameters,
                                                     false, "text/html");

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(result.JSON);
            if (HideCopyrightedTexts)
            {
                // We only know for sure (at least, for now) that rules starting with Generic-* are safe.
                string ruleId = htmlDocument.DocumentNode.SelectSingleNode("//span[@class=\"ruleid\"]").InnerText;
                if (!ruleId.StartsWith("Rule Generic-"))
                {
                    Debug.LogWarning("Retrieved rule is protected by copyright and can't be displayed.");
                    return "";
                }
            }

            HtmlNode ruleInfo = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(concat(' ',normalize-space(@class),' '),' errorinfo ')]");
            // Remove configuration table and heading
            ruleInfo.SelectNodes("//h4[position()=1]")?.ToList().ForEach(x => x?.Remove());
            ruleInfo.SelectNodes("//h5[text()='Configuration']")?.ToList().ForEach(x => x?.Remove());
            ruleInfo.SelectNodes("//table[contains(concat(' ',normalize-space(@class),' '),' rule-config ')]")
                    ?.ToList().ForEach(x => x?.Remove());

            string ruleText = string.Join("\n", ruleInfo.InnerText.Split('\n').Select(x => x.Trim(' ', '\t')));
            return HtmlEntity.DeEntitize(ruleText).Trim(' ', '\t', '\n', '\r');
        }

        #endregion

        #region Aggregate Calls

        /// <summary>
        /// A dictionary mapping from path and entity name to the corresponding metrics.
        /// </summary>
        private IDictionary<(string path, string entity), List<MetricValueTableRow>> metrics;

        /// <summary>
        /// Returns a list of <see cref="MetricValueTableRow"/>s whose <paramref name="path"/> and
        /// <paramref name="entityName"/> match the given parameters.
        /// If no such rows exist, an empty list will be returned.
        ///
        /// NOTE: The first time this method is called, an expensive network call is made, the result of which
        /// will be cached for later accesses. This means that the first call will take a lot longer compared
        /// to any subsequent calls.
        /// </summary>
        /// <param name="path">The path of the queried entity</param>
        /// <param name="entityName">The name of the queried entity</param>
        /// <returns>A list of <see cref="MetricValueTableRow"/>s which matches the given parameters.</returns>
        public async UniTask<List<MetricValueTableRow>> GetSpecificMetricRows(string path, string entityName)
        {
            metrics ??= (await GetMetricValueTable()).rows.GroupBy(x => (x.path, x.entity))
                                                     .ToDictionary(x => x.Key, x => x.ToList());

            return metrics.ContainsKey((path, entityName)) ? metrics[(path, entityName)] : new List<MetricValueTableRow>();
        }

        /// <summary>
        /// TODO: Documentation
        /// </summary>
        /// <returns></returns>
        public async UniTask<IDictionary<(string path, string entity), List<MetricValueTableRow>>> GetAllMetricRows()
        {
            metrics ??= (await GetMetricValueTable()).rows.GroupBy(x => (x.path, x.entity))
                                                     .ToDictionary(x => x.Key, x => x.ToList());
            return metrics;
        }

        /// <summary>
        /// This method returns a list of all issues which are configured to be retrieved by
        /// the instance fields <see cref="ArchitectureViolationIssues"/>, <see cref="CloneIssues"/>,
        /// <see cref="CycleIssues"/>, <see cref="DeadEntityIssues"/>, <see cref="MetricViolationIssues"/> and
        /// <see cref="StyleViolationIssues"/>. For a documentation of parameters, see <see cref="GetIssues{T}"/>.
        /// </summary>
        /// <returns>A list of all retrieved issues.</returns>
        public async UniTask<IList<Issue>> GetConfiguredIssues(string start = null, string end = null,
                                                               Issue.IssueState state = Issue.IssueState.changed,
                                                               string user = null, string fileFilter = null,
                                                               IReadOnlyDictionary<string, string> columnFilters = null,
                                                               int limit = int.MaxValue,
                                                               int offset = 0, bool computeTotalRowCount = false)
        {
            List<Issue> issues = new List<Issue>();
            if (ArchitectureViolationIssues)
            {
                issues.AddRange((await GetIssues<ArchitectureViolationIssue>(start, end, state, user, fileFilter,
                                                                             columnFilters, limit, offset, computeTotalRowCount)).rows);
            }

            if (CloneIssues)
            {
                issues.AddRange((await GetIssues<CloneIssue>(start, end, state, user, fileFilter,
                                                             columnFilters, limit, offset, computeTotalRowCount)).rows);
            }

            if (CycleIssues)
            {
                issues.AddRange((await GetIssues<CycleIssue>(start, end, state, user, fileFilter,
                                                             columnFilters, limit, offset, computeTotalRowCount)).rows);
            }

            if (DeadEntityIssues)
            {
                issues.AddRange((await GetIssues<DeadEntityIssue>(start, end, state, user, fileFilter,
                                                                  columnFilters, limit, offset, computeTotalRowCount)).rows);
            }

            if (MetricViolationIssues)
            {
                issues.AddRange((await GetIssues<MetricViolationIssue>(start, end, state, user, fileFilter,
                                                                       columnFilters, limit, offset, computeTotalRowCount)).rows);
            }

            if (StyleViolationIssues)
            {
                issues.AddRange((await GetIssues<StyleViolationIssue>(start, end, state, user, fileFilter,
                                                                      columnFilters, limit, offset, computeTotalRowCount)).rows);
            }

            return issues;
        }

        #endregion
    }
}