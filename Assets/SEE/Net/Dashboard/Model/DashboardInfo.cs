using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model
{
    /// <summary>
    /// Meta information about the Axivion Dashboard.
    /// Contains a list of visible projects.
    /// </summary>
    [Serializable]
    public class DashboardInfo
    {
        /// <summary>
        /// Axivion Dashboard version serving this API.
        /// </summary>
        [JsonProperty(PropertyName = "dashboardVersion", Required = Required.Always)]
        public readonly string DashboardVersion;

        /// <summary>
        /// Parseable Axivion Dashboard Version.
        /// </summary>
        [JsonProperty(PropertyName = "dashboardVersionNumber", Required = Required.Always)]
        public readonly string DashboardVersionNumber;

        /// <summary>
        /// Dashboard Server Build date.
        /// </summary>
        [JsonProperty(PropertyName = "dashboardBuildDate", Required = Required.Always)]
        public readonly string DashboardBuildDate;

        /// <summary>
        /// Name of the successfully authenticated user if a dashboard-user is associated with the request.
        /// </summary>
        [JsonProperty(PropertyName = "username", Required = Required.Default)]
        public readonly string Username;

        /// <summary>
        /// The HTTP-Request Header expected present for all HTTP requests that are not GET, HEAD, OPTIONS or TRACE.
        /// </summary>
        [JsonProperty(PropertyName = "csrfTokenHeader", Required = Required.Default)]
        public readonly string CsrfTokenHeader;

        /// <summary>
        /// The value expected to be sent along the <c>csrfTokenHeader</c> for all HTTP requests that are not
        /// GET, HEAD, OPTIONS or TRACE.
        /// </summary>
        /// <remarks>
        /// Note, that this does not replace authentication of subsequent requests.
        /// Also the token is combined with the authentication data,
        /// so will not work when authenticating as another user.
        /// Its lifetime is limited, so when creating a very long-running application you should consider refreshing
        /// this token from time to time.
        /// </remarks>
        [JsonProperty(PropertyName = "csrfToken", Required = Required.Default)]
        public readonly string CsrfToken;

        /// <summary>
        /// A URI that can be used to check credentials via GET. It returns "ok" in case of valid credentials.
        /// </summary>
        [JsonProperty(PropertyName = "checkCredentialsUrl", Required = Required.Always)]
        public readonly string CheckCredentialsUrl;

        /// <summary>
        /// List of references to the projects visible to the authenticated user.
        /// </summary>
        [JsonProperty(PropertyName = "projects", Required = Required.Always)]
        public readonly IList<ProjectReference> Projects;

        /// <summary>
        /// Entry point for creating, listing, deleting api tokens of the authenticated user.
        /// </summary>
        [JsonProperty(PropertyName = "userApiTokenUrl", Required = Required.Always)]
        public readonly string UserApiTokenUrl;

        public DashboardInfo(string dashboardVersion, string dashboardVersionNumber, string dashboardBuildDate,
                             string username, string csrfTokenHeader, string csrfToken, string checkCredentialsUrl,
                             IList<ProjectReference> projects, string userApiTokenUrl)
        {
            this.DashboardVersion = dashboardVersion;
            this.DashboardVersionNumber = dashboardVersionNumber;
            this.DashboardBuildDate = dashboardBuildDate;
            this.Username = username;
            this.CsrfTokenHeader = csrfTokenHeader;
            this.CsrfToken = csrfToken;
            this.CheckCredentialsUrl = checkCredentialsUrl;
            this.Projects = projects;
            this.UserApiTokenUrl = userApiTokenUrl;
        }

        /// <summary>
        /// A reference to a project.
        /// </summary>
        [Serializable]
        public class ProjectReference
        {
            /// <summary>
            /// The name of the project. Use this string to refer to the project.
            /// </summary>
            [JsonProperty(PropertyName = "name", Required = Required.Always)]
            public readonly string Name;

            /// <summary>
            /// URI to get further information about the project.
            /// </summary>
            [JsonProperty(PropertyName = "url", Required = Required.Always)]
            public readonly string URL;

            public ProjectReference(string name, string url)
            {
                this.Name = name;
                this.URL = url;
            }

            public override string ToString()
            {
                return $"{nameof(Name)}: {Name}, {nameof(URL)}: {URL}";
            }
        }

        public override string ToString()
        {
            return $"{nameof(DashboardVersion)}: {DashboardVersion}, "
                   + $"{nameof(DashboardVersionNumber)}: {DashboardVersionNumber}, "
                   + $"{nameof(DashboardBuildDate)}: {DashboardBuildDate}, {nameof(Username)}: {Username},"
                   + $" {nameof(CsrfTokenHeader)}: {CsrfTokenHeader}, {nameof(CsrfToken)}: {CsrfToken},"
                   + $" {nameof(CheckCredentialsUrl)}: {CheckCredentialsUrl}, {nameof(Projects)}: {Projects},"
                   + $" {nameof(UserApiTokenUrl)}: {UserApiTokenUrl}";
        }
    }
}