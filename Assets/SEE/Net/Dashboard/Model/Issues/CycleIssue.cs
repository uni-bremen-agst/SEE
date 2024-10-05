using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing cyclic dependencies.
    /// </summary>
    [Serializable]
    public class CycleIssue : Issue
    {
        /// <summary>
        /// The type of the relation between source and target
        /// </summary>
        [JsonProperty(PropertyName = "dependencyType", Required = Required.Always)]
        public readonly string DependencyType;

        /// <summary>
        /// The source entity
        /// </summary>
        [JsonProperty(PropertyName = "sourceEntity", Required = Required.Always)]
        public readonly string SourceEntity;

        /// <summary>
        /// The source entity type
        /// </summary>
        [JsonProperty(PropertyName = "sourceEntityType", Required = Required.Always)]
        public readonly string SourceEntityType;

        /// <summary>
        /// The source filename
        /// </summary>
        [JsonProperty(PropertyName = "sourcePath", Required = Required.Always)]
        public readonly string SourcePath;

        /// <summary>
        /// The source line number
        /// </summary>
        [JsonProperty(PropertyName = "sourceLine", Required = Required.Always)]
        public readonly int SourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "sourceLinkName", Required = Required.Always)]
        public readonly string SourceLinkName;

        /// <summary>
        /// The target entity
        /// </summary>
        [JsonProperty(PropertyName = "targetEntity", Required = Required.Always)]
        public readonly string TargetEntity;

        /// <summary>
        /// The target entity type
        /// </summary>
        [JsonProperty(PropertyName = "targetEntityType", Required = Required.Always)]
        public readonly string TargetEntityType;

        /// <summary>
        /// The target filename
        /// </summary>
        [JsonProperty(PropertyName = "targetPath", Required = Required.Always)]
        public readonly string TargetPath;

        /// <summary>
        /// The target line number
        /// </summary>
        [JsonProperty(PropertyName = "targetLine", Required = Required.Always)]
        public readonly int TargetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "targetLinkName", Required = Required.Always)]
        public readonly string TargetLinkName;

        public CycleIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected CycleIssue(string dependencyType, string sourceEntity, string sourceEntityType,
                             string sourcePath, int sourceLine, string sourceLinkName, string targetEntity,
                             string targetEntityType, string targetPath, int targetLine, string targetLinkName)
        {
            DependencyType = dependencyType;
            SourceEntity = sourceEntity;
            SourceEntityType = sourceEntityType;
            SourcePath = sourcePath;
            SourceLine = sourceLine;
            SourceLinkName = sourceLinkName;
            TargetEntity = targetEntity;
            TargetEntityType = targetEntityType;
            TargetPath = targetPath;
            TargetLine = targetLine;
            TargetLinkName = targetLinkName;
        }

        public override async UniTask<string> ToDisplayStringAsync()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescriptionAsync($"CY{ID}");
            return "<style=\"H2\">Cyclic dependency</style>"
                   + $"\nSource: {SourcePath} ({SourceEntityType}), Line {SourceLine}\n".WrapLines(WrapAt)
                   + $"\nTarget: {TargetPath} ({TargetEntityType}), Line {TargetLine}\n".WrapLines(WrapAt)
                   + $"\n{explanation.WrapLines(WrapAt)}";
        }

        public override string IssueKind => "CY";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Cycle;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(SourcePath, SourceLine, null, SourceEntity),
            new SourceCodeEntity(TargetPath, TargetLine, null, TargetEntity)
        };
    }
}
