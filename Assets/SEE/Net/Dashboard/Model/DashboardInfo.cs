using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string dashboardVersion;
        
        /// <summary>
        /// Parseable Axivion Dashboard Version.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string dashboardVersionNumber;
        
        /// <summary>
        /// Dashboard Server Build date.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string dashboardBuildDate;
        
        /// <summary>
        /// Name of the successfully authenticated user if a dashboard-user is associated with the request.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly string username;
        
        /// <summary>
        /// The HTTP-Request Header expected present for all HTTP requests that are not GET, HEAD, OPTIONS or TRACE.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly string csrfTokenHeader;
        
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
        [JsonProperty(Required = Required.Default)]
        public readonly string csrfToken;
        
        /// <summary>
        /// A URI that can be used to check credentials via GET. It returns "ok" in case of valid credentials.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string checkCredentialsUrl;

        /// <summary>
        /// List of references to the projects visible to the authenticated user.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly IList<ProjectReference> projects;

        /// <summary>
        /// Entry point for creating, listing, deleting api tokens of the authenticated user.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string userApiTokenUrl;

        public DashboardInfo(string dashboardVersion, string dashboardVersionNumber, string dashboardBuildDate, 
                             string username, string csrfTokenHeader, string csrfToken, string checkCredentialsUrl,
                             IList<ProjectReference> projects, string userApiTokenUrl)
        {
            this.dashboardVersion = dashboardVersion;
            this.dashboardVersionNumber = dashboardVersionNumber;
            this.dashboardBuildDate = dashboardBuildDate;
            this.username = username;
            this.csrfTokenHeader = csrfTokenHeader;
            this.csrfToken = csrfToken;
            this.checkCredentialsUrl = checkCredentialsUrl;
            this.projects = projects;
            this.userApiTokenUrl = userApiTokenUrl;
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
            [JsonProperty(Required = Required.Always)]
            public readonly string name;

            /// <summary>
            /// URI to get further information about the project.
            /// </summary>
            [JsonProperty(Required = Required.Always)]
            public readonly string url;

            public ProjectReference(string name, string url)
            {
                this.name = name;
                this.url = url;
            }

            public override string ToString()
            {
                return $"{nameof(name)}: {name}, {nameof(url)}: {url}";
            }
        }

        public override string ToString()
        {
            return $"{nameof(dashboardVersion)}: {dashboardVersion}, "
                   + $"{nameof(dashboardVersionNumber)}: {dashboardVersionNumber}, "
                   + $"{nameof(dashboardBuildDate)}: {dashboardBuildDate}, {nameof(username)}: {username},"
                   + $" {nameof(csrfTokenHeader)}: {csrfTokenHeader}, {nameof(csrfToken)}: {csrfToken},"
                   + $" {nameof(checkCredentialsUrl)}: {checkCredentialsUrl}, {nameof(projects)}: {projects},"
                   + $" {nameof(userApiTokenUrl)}: {userApiTokenUrl}";
        }
    }
}