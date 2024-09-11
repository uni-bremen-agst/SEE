using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SEE.DataModel.DG;
using SEE.Utils;

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
        [JsonProperty(PropertyName = "severity", Required = Required.Always)]
        public readonly string Severity;

        /// <summary>
        /// The tool/manufacturer that reported this violation
        /// </summary>
        [JsonProperty(PropertyName = "provider", Required = Required.Always)]
        public readonly string Provider;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(PropertyName = "errorNumber", Required = Required.Always)]
        public readonly string ErrorNumber;

        /// <summary>
        /// The message describing the violation
        /// </summary>
        [JsonProperty(PropertyName = "message", Required = Required.Always)]
        public readonly string Message;

        /// <summary>
        /// The source code entity
        /// </summary>
        [JsonProperty(PropertyName = "entity", Required = Required.Always)]
        public readonly string Entity;

        /// <summary>
        /// The filename
        /// </summary>
        [JsonProperty(PropertyName = "path", Required = Required.Always)]
        public readonly string Path;

        /// <summary>
        /// The line number
        /// </summary>
        [JsonProperty(PropertyName = "line", Required = Required.Always)]
        public readonly int Line;

        public StyleViolationIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public StyleViolationIssue(string severity, string provider, string errorNumber, string message, string entity,
                                   string path, int line)
        {
            Severity = severity;
            Provider = provider;
            ErrorNumber = errorNumber;
            Message = message;
            Entity = entity;
            Path = path;
            Line = line;
        }

        public override async UniTask<string> ToDisplayStringAsync()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescriptionAsync($"SV{ID}");
            return $"<style=\"H2\">{Message.WrapLines(WrapAt / 2)}</style>\n{explanation.WrapLines(WrapAt)}";
        }

        public override string IssueKind => "SV";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Style;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(Path, Line, null, Entity)
        };
    }
}
