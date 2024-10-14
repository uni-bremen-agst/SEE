using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SEE.DataModel.DG;
using SEE.Utils;

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
        private const bool showExplanation = false;

        /// <summary>
        /// The severity of the violation
        /// </summary>
        [JsonProperty(PropertyName = "severity", Required = Required.Always)]
        public readonly string Severity;

        /// <summary>
        /// The entity
        /// </summary>
        [JsonProperty(PropertyName = "entity", Required = Required.Always)]
        public readonly string Entity;

        /// <summary>
        /// The entity type
        /// </summary>
        [JsonProperty(PropertyName = "entityType", Required = Required.Always)]
        public readonly string EntityType;

        /// <summary>
        /// The filename of the entity
        /// </summary>
        [JsonProperty(PropertyName = "path", Required = Required.AllowNull)]
        public readonly string Path;

        /// <summary>
        /// The line number of the entity
        /// </summary>
        [JsonProperty(PropertyName = "line", Required = Required.Always)]
        public readonly int Line;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "linkName", Required = Required.Always)]
        public readonly string LinkName;

        /// <summary>
        /// The internal name of the metric
        /// </summary>
        [JsonProperty(PropertyName = "metric", Required = Required.Always)]
        public readonly string Metric;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(PropertyName = "errorNumber", Required = Required.Always)]
        public readonly string ErrorNumber;

        /// <summary>
        /// The short description of the metric
        /// </summary>
        [JsonProperty(PropertyName = "description", Required = Required.Always)]
        public readonly string Description;

        /// <summary>
        /// The max value configured for the metric
        /// </summary>
        [JsonProperty(PropertyName = "max", Required = Required.AllowNull)]
        public readonly float? Max;

        /// <summary>
        /// The min value configured for the metric
        /// </summary>
        [JsonProperty(PropertyName = "min", Required = Required.AllowNull)]
        public readonly float? Min;

        /// <summary>
        /// The measured value
        /// </summary>
        [JsonProperty(PropertyName = "value", Required = Required.Always)]
        public readonly float Value;

        public MetricViolationIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public MetricViolationIssue(string severity, string entity, string entityType, string path, int line,
                                    string linkName, string metric, string errorNumber, string description,
                                    float? max, float? min, float value)
        {
            Severity = severity;
            Entity = entity;
            EntityType = entityType;
            Path = path;
            Line = line;
            LinkName = linkName;
            Metric = metric;
            ErrorNumber = errorNumber;
            Description = description;
            Max = max;
            Min = min;
            Value = value;
        }

        public override async UniTask<string> ToDisplayStringAsync()
        {
            string explanation = showExplanation ? await DashboardRetriever.Instance.GetIssueDescriptionAsync($"MV{ID}") : "";
            string minimum = Min.HasValue ? $"; Minimum: <b>{Min:0.##}</b>" : "";
            string maximum = Max.HasValue ? $"; Maximum: <b>{Max:0.##}</b>" : "";
            return $"<style=\"H2\">Metric: {Description.WrapLines(WrapAt / 2)}</style>"
                   + $"\nActual: <b>{Value}</b>{minimum}{maximum}"
                   + $"\n{explanation.WrapLines(WrapAt)}";
        }

        public override string IssueKind => "MV";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Metric;

        public override IEnumerable<SourceCodeEntity> Entities =>
            Path == null
                ? new SourceCodeEntity[] { }
                : new[]
                {
                    new SourceCodeEntity(Path, Line, null, Entity)
                };
    }
}
