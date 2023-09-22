using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
using Newtonsoft.Json;

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
        public readonly int CloneType;

        /// <summary>
        /// The filename of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string LeftPath;

        /// <summary>
        /// The start line number of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int LeftLine;

        /// <summary>
        /// The end line number of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int LeftEndLine;

        /// <summary>
        /// The number of lines of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int LeftLength;

        /// <summary>
        /// The weight of the left clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int LeftWeight;

        /// <summary>
        /// The filename of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string RightPath;

        /// <summary>
        /// The start line number of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int RightLine;

        /// <summary>
        /// The end line number of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int RightEndLine;

        /// <summary>
        /// The number of lines of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int RightLength;

        /// <summary>
        /// The weight of the right clone fragment
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int RightWeight;

        public CloneIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        public CloneIssue(int cloneType, string leftPath, int leftLine, int leftEndLine, int leftLength,
                          int leftWeight, string rightPath, int rightLine, int rightEndLine, int rightLength,
                          int rightWeight)
        {
            this.CloneType = cloneType;
            this.LeftPath = leftPath;
            this.LeftLine = leftLine;
            this.LeftEndLine = leftEndLine;
            this.LeftLength = leftLength;
            this.LeftWeight = leftWeight;
            this.RightPath = rightPath;
            this.RightLine = rightLine;
            this.RightEndLine = rightEndLine;
            this.RightLength = rightLength;
            this.RightWeight = rightWeight;
        }

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescription($"CL{ID}");
            return $"<style=\"H2\">Clone of type {CloneType}</style>"
                   + $"\nLeft: {LeftPath}, Lines {LeftLine}-{LeftEndLine}".WrapLines(WrapAt)
                   + $"\nRight: {RightPath}, Lines {RightLine}-{RightEndLine}\n".WrapLines(WrapAt)
                   + $"\n{explanation.WrapLines(WrapAt)}";
        }

        public override string IssueKind => "CL";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Clone;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(LeftPath, LeftLine, LeftEndLine),
            new SourceCodeEntity(RightPath, RightLine, RightEndLine)
        };
    }
}