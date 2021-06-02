using System;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model
{
    /// <summary>
    /// Describes an error returned from the server.
    /// Usually they are caused by wrong API usage but they may also happen in case of bugs in the server.
    /// </summary>
    [Serializable]
    public class DashboardError
    {
        /// <summary>
        /// A parseable version number indicating the server version.
        /// </summary>
        /// <remarks>This can be parsed using <see cref="DashboardVersion"/>.</remarks>
        [JsonProperty(Required = Required.Always)]
        public readonly string dashboardVersionNumber;
        
        /// <summary>
        /// The name of the error kind.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string type;
        
        /// <summary>
        /// A human readable english message describing the error.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string message;
        
        /// <summary>
        /// Use this instead of message in order to display a message translated according to your language preferences.
        /// Will contain exactly the same contents as message in case no translation is available.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string localizedMessage;
        
        /// <summary>
        /// Optional field containing additional error information meant for automatic processing.
        /// This data is meant for helping software that uses the API to better
        /// understand and communicate certain types of error to the user.
        /// Always inspect the type so you know what keys you can expect.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly DashboardErrorData data;

        public DashboardError(string dashboardVersionNumber, string type, string message, string localizedMessage, DashboardErrorData data)
        {
            this.dashboardVersionNumber = dashboardVersionNumber;
            this.type = type;
            this.message = message;
            this.localizedMessage = localizedMessage;
            this.data = data;
        }

        /// <summary>
        /// Contains additional error information meant for automatic processing.
        /// </summary>
        [Serializable]
        public class DashboardErrorData
        {
            /// <summary>
            /// References the column that has the invalid filter value.
            /// The file filter is referred to by the string "any path"
            /// </summary>
            [JsonProperty(Required = Required.Default)]
            public readonly string column;
            
            /// <summary>
            /// Provides an ASCII-encoded URL pointing to human-readable help that might help a user understand
            /// and resolve the error. If the URL is relative, then it is meant relative to the Dashboard the error originated from.
            /// </summary>
            [JsonProperty(Required = Required.Default)]
            public readonly string help;
            
            /// <summary>
            /// Indicates that the provided password may be used as API token with the respective API.
            /// E.g. use 'Authorization: AxToken …' header instead of HTTP basic auth.
            /// </summary>
            [JsonProperty(Required = Required.Default)]
            public readonly bool? passwordMayBeUsedAsApiToken;

            public DashboardErrorData(string column, string help, bool? passwordMayBeUsedAsApiToken)
            {
                this.column = column;
                this.help = help;
                this.passwordMayBeUsedAsApiToken = passwordMayBeUsedAsApiToken;
            }

            public override string ToString()
            {
                return $"{nameof(column)}: {column}, {nameof(help)}: {help}, "
                       + $"{nameof(passwordMayBeUsedAsApiToken)}: {passwordMayBeUsedAsApiToken}";
            }
        }

        public override string ToString()
        {
            return $"{nameof(dashboardVersionNumber)}: {dashboardVersionNumber}, "
                   + $"{nameof(type)}: {type}, {nameof(message)}: {message}, "
                   + $"{nameof(localizedMessage)}: {localizedMessage}, {nameof(data)}: {data}";
        }
    }
}