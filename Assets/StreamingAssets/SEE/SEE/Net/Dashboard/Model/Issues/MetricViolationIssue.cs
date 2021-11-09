using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
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
        /// Whether the explanation shall be shown for these issues.
        /// This is relevant because explanation for metrics are often very long, so disabling it may be of use.
        /// </summary>
        private const bool SHOW_EXPLANATION = false;

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
        public readonly int line;

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
        public MetricViolationIssue(string severity, string entity, string entityType, string path, int line,
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

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = SHOW_EXPLANATION ? await DashboardRetriever.Instance.GetIssueDescription($"MV{id}") : "";
            string minimum = min.HasValue ? $"; Minimum: <b>{min:0.##}</b>" : "";
            string maximum = max.HasValue ? $"; Maximum: <b>{max:0.##}</b>" : "";
            return $"<style=\"H2\">Metric: {description.WrapLines(WRAP_AT / 2)}</style>"
                   + $"\nActual: <b>{value}</b>{minimum}{maximum}"
                   + $"\n{explanation.WrapLines(WRAP_AT)}";
        }

        public override string IssueKind => "MV";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Metric;

        public override IEnumerable<SourceCodeEntity> Entities =>
            path == null
                ? new SourceCodeEntity[] { }
                : new[]
                {
                    new SourceCodeEntity(path, line, null, entity)
                };
    }
}