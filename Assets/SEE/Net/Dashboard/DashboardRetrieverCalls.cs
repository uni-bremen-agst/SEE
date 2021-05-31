using System;
using Cysharp.Threading.Tasks;
using SEE.Net.Dashboard.Model;
using SEE.Net.Dashboard.Model.Issues;

namespace SEE.Net.Dashboard
{
    /// <summary>
    /// The part of the <see cref="DashboardRetriever"/> which contains the actual API calls.
    /// Basically a wrapper around the Axivion Dashboard API as described in its documentation.
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
        public async UniTask<DashboardInfo> GetDashboardInfo()
        {
            DashboardResult result = await GetAtPath("/../../");
            DashboardInfo info = result.RetrieveObject<DashboardInfo>(StrictMode);
            return info;
        }
        
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
        /// <param name="kind">The issue kind.
        /// Will be inferred from <typeparamref name="T"/>, so specifying it isn't necessary.</param>
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
        /// <param name="limit">Limit the number of returned issues to the given number.
        /// Returns a deterministic result. Useful for paging.</param>
        /// <param name="offset">Omit the issues before the given 0 based index. Returns a deterministic result.
        /// Useful for paging.</param>
        /// <param name="computeTotalRowCount">Whether to include total issue counts in the result. Useful for paging.
        /// Be aware that calculating this might involve additional database accesses.</param>
        /// <typeparam name="T">The issue kind that shall be queried.</typeparam>
        /// <returns>The list of queried issues, as represented by the <see cref="IssueTable{T}"/>.</returns>
        /// <remarks>
        /// TODO: Note that sorting and filtering issues is not yet supported.
        /// </remarks>
        public async UniTask<IssueTable<T>> GetIssues<T>(Issue.IssueKind kind = Issue.IssueKind.Unknown, 
                                                         string start = null, string end = null,
                                                         Issue.IssueState state = Issue.IssueState.changed,
                                                         string user = null, int limit = int.MaxValue,
                                                         int offset = 0, bool computeTotalRowCount = false) 
            where T : Issue, new()
        {
            Issue.IssueKind actualKind = new T().kind;
            if (kind != Issue.IssueKind.Unknown && actualKind != kind)
            {
                throw new ArgumentException($"Given {nameof(kind)} doesn't correspond to expected return type!");
            }
            string[] values = { actualKind.ToString(), start, end, state.ToString(), user, limit.ToString(), offset.ToString(), 
                computeTotalRowCount.ToString().ToLower() };
            return await QueryDashboard<IssueTable<T>>("/issues/", values);
        }

        #endregion

        #region Metrics API

        

        #endregion
    }
}