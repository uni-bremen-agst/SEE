using System;
using Newtonsoft.Json;

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
        public readonly string DashboardVersionNumber;

        /// <summary>
        /// The name of the error kind.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string Type;

        /// <summary>
        /// A human readable english message describing the error.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string Message;

        /// <summary>
        /// Use this instead of message in order to display a message translated according to your language preferences.
        /// Will contain exactly the same contents as message in case no translation is available.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string LocalizedMessage;

        /// <summary>
        /// Optional field containing additional error information meant for automatic processing.
        /// This data is meant for helping software that uses the API to better
        /// understand and communicate certain types of error to the user.
        /// Always inspect the type so you know what keys you can expect.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly DashboardErrorData Data;

        public DashboardError(string dashboardVersionNumber, string type, string message, string localizedMessage, DashboardErrorData data)
        {
            this.DashboardVersionNumber = dashboardVersionNumber;
            this.Type = type;
            this.Message = message;
            this.LocalizedMessage = localizedMessage;
            this.Data = data;
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
            public readonly string Column;

            /// <summary>
            /// Provides an ASCII-encoded URL pointing to human-readable help that might help a user understand
            /// and resolve the error. If the URL is relative, then it is meant relative to the Dashboard the error originated from.
            /// </summary>
            [JsonProperty(Required = Required.Default)]
            public readonly string Help;

            /// <summary>
            /// Indicates that the provided password may be used as API token with the respective API.
            /// E.g. use 'Authorization: AxToken …' header instead of HTTP basic auth.
            /// </summary>
            [JsonProperty(Required = Required.Default)]
            public readonly bool? PasswordMayBeUsedAsApiToken;

            public DashboardErrorData(string column, string help, bool? passwordMayBeUsedAsApiToken)
            {
                this.Column = column;
                this.Help = help;
                this.PasswordMayBeUsedAsApiToken = passwordMayBeUsedAsApiToken;
            }

            public override string ToString()
            {
                return $"{nameof(Column)}: {Column}, {nameof(Help)}: {Help}, "
                       + $"{nameof(PasswordMayBeUsedAsApiToken)}: {PasswordMayBeUsedAsApiToken}";
            }
        }

        public override string ToString()
        {
            return $"{nameof(DashboardVersionNumber)}: {DashboardVersionNumber}, "
                   + $"{nameof(Type)}: {Type}, {nameof(Message)}: {Message}, "
                   + $"{nameof(LocalizedMessage)}: {LocalizedMessage}, {nameof(Data)}: {Data}";
        }
    }
}