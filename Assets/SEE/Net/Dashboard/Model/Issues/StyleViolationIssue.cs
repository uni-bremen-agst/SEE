using System;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing a style violation.
    /// </summary>
    [Serializable]
    public class StyleViolationIssue : Issue
    {
        /// <summary>
        /// The severity of the violation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string severity;

        /// <summary>
        /// The tool/manufacturer that reported this violation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string provider;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string errorNumber;

        /// <summary>
        /// The message describing the violation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string message;

        /// <summary>
        /// The source code entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entity;

        /// <summary>
        /// The filename
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string path;

        /// <summary>
        /// The line number
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint line;

        public StyleViolationIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public StyleViolationIssue(string severity, string provider, string errorNumber, string message, string entity, 
                                   string path, uint line)
        {
            this.severity = severity;
            this.provider = provider;
            this.errorNumber = errorNumber;
            this.message = message;
            this.entity = entity;
            this.path = path;
            this.line = line;
        }
    }
}