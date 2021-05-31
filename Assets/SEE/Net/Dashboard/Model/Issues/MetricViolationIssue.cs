using System;
using Newtonsoft.Json;

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
        public readonly string severity;

        /// <summary>
        /// The entity
        /// </summary>
        public readonly string entity;

        /// <summary>
        /// The entity type
        /// </summary>
        public readonly string entityType;

        /// <summary>
        /// The filename of the entity
        /// </summary>
        public readonly string path;

        /// <summary>
        /// The line number of the entity
        /// </summary>
        public readonly uint line;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        public readonly string linkName;

        /// <summary>
        /// The internal name of the metric
        /// </summary>
        public readonly string metric;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        public readonly string errorNumber;

        /// <summary>
        /// The short description of the metric
        /// </summary>
        public readonly string description;

        /// <summary>
        /// The max value configured for the metric
        /// </summary>
        public readonly int max;

        /// <summary>
        /// The min value configured for the metric
        /// </summary>
        public readonly int min;

        /// <summary>
        /// The measured value
        /// </summary>
        public readonly int value;

        public MetricViolationIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public MetricViolationIssue(string severity, string entity, string entityType, string path, uint line, 
                                    string linkName, string metric, string errorNumber, string description, 
                                    int max, int min, int value)
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