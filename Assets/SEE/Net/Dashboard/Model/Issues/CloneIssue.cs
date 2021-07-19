using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing a clone.
    /// </summary>
    [Serializable]
    public class CloneIssue : Issue
    {
        /// <summary>
        /// The clone type
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int cloneType;

        /// <summary>
        /// The filename of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string leftPath;

        /// <summary>
        /// The start line number of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int leftLine;

        /// <summary>
        /// The end line number of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int leftEndLine;

        /// <summary>
        /// The number of lines of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int leftLength;

        /// <summary>
        /// The weight of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int leftWeight;

        /// <summary>
        /// The filename of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string rightPath;

        /// <summary>
        /// The start line number of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int rightLine;

        /// <summary>
        /// The end line number of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int rightEndLine;

        /// <summary>
        /// The number of lines of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int rightLength;

        /// <summary>
        /// The weight of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int rightWeight;

        public CloneIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public CloneIssue(int cloneType, string leftPath, int leftLine, int leftEndLine, int leftLength,
                          int leftWeight, string rightPath, int rightLine, int rightEndLine, int rightLength,
                          int rightWeight)
        {
            this.cloneType = cloneType;
            this.leftPath = leftPath;
            this.leftLine = leftLine;
            this.leftEndLine = leftEndLine;
            this.leftLength = leftLength;
            this.leftWeight = leftWeight;
            this.rightPath = rightPath;
            this.rightLine = rightLine;
            this.rightEndLine = rightEndLine;
            this.rightLength = rightLength;
            this.rightWeight = rightWeight;
        }

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescription($"CL{id}");
            return $"<style=\"H2\">Clone of type {cloneType}</style>"
                   + $"\nLeft: {leftPath}, Lines {leftLine}-{leftEndLine}".WrapLines(WRAP_AT)
                   + $"\nRight: {rightPath}, Lines {rightLine}-{rightEndLine}\n".WrapLines(WRAP_AT)
                   + $"\n{explanation.WrapLines(WRAP_AT)}";
        }

        public override string IssueKind => "CL";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Clone;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(leftPath, leftLine, leftEndLine),
            new SourceCodeEntity(rightPath, rightLine, rightEndLine)
        };
    }
}