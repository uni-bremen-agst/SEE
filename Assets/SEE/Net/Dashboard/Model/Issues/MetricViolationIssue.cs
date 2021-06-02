using System;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing a metric violation.
    /// </summary>
    [Serializable]
    public class MetricViolationIssue : Issue
    {
        /// <summary>
        /// The severity of the violation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string severity;

        /// <summary>
        /// The entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entity;

        /// <summary>
        /// The entity type
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entityType;

        /// <summary>
        /// The filename of the entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string path;

        /// <summary>
        /// The line number of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly uint line;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string linkName;

        /// <summary>
        /// The internal name of the metric
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string metric;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string errorNumber;

        /// <summary>
        /// The short description of the metric
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string description;

        /// <summary>
        /// The max value configured for the metric
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly float? max;

        /// <summary>
        /// The min value configured for the metric
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly float? min;

        /// <summary>
        /// The measured value
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly float value;

        public MetricViolationIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public MetricViolationIssue(string severity, string entity, string entityType, string path, uint line, 
                                    string linkName, string metric, string errorNumber, string description, 
                                    float? max, float? min, float value)
        {
            this.severity = severity;
            this.entity = entity;
            this.entityType = entityType;
            this.path = path;
            this.line = line;
            this.linkName = linkName;
            this.metric = metric;
            this.errorNumber = errorNumber;
            this.description = description;
            this.max = max;
            this.min = min;
            this.value = value;
        }
    }
}