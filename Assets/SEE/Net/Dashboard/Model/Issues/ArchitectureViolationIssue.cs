using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing architecture violations.
    /// </summary>
    [Serializable]
    public class ArchitectureViolationIssue : Issue
    {
        /// <summary>
        /// The architecture source entity
        /// </summary>
        [JsonProperty(PropertyName = "architectureSource", Required = Required.Always)]
        public readonly string ArchitectureSource;

        /// <summary>
        /// The type of the architecture source entity
        /// </summary>
        [JsonProperty(PropertyName = "architectureSourceType", Required = Required.Always)]
        public readonly string ArchitectureSourceType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "architectureSourceLinkName", Required = Required.Always)]
        public readonly string ArchitectureSourceLinkName;

        /// <summary>
        /// The architecture target entity
        /// </summary>
        [JsonProperty(PropertyName = "architectureTarget", Required = Required.Always)]
        public readonly string ArchitectureTarget;

        /// <summary>
        /// The type of the architecture target entity
        /// </summary>
        [JsonProperty(PropertyName = "architectureTargetType", Required = Required.Always)]
        public readonly string ArchitectureTargetType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "architectureTargetLinkName", Required = Required.Always)]
        public readonly string ArchitectureTargetLinkName;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(PropertyName = "errorNumber", Required = Required.Always)]
        public readonly string ErrorNumber;

        /// <summary>
        /// The type of the architecture violation
        /// </summary>
        [JsonProperty(PropertyName = "violationType", Required = Required.Always)]
        public readonly string ViolationType;

        /// <summary>
        /// The type of the relation between source and target
        /// </summary>
        [JsonProperty(PropertyName = "dependencyType", Required = Required.Always)]
        public readonly string DependencyType;

        /// <summary>
        /// The source code source entity
        /// </summary>
        [JsonProperty(PropertyName = "sourceEntity", Required = Required.AllowNull)]
        public readonly string SourceEntity;

        /// <summary>
        /// The type of the source code source entity
        /// </summary>
        [JsonProperty(PropertyName = "sourceEntityType", Required = Required.AllowNull)]
        public readonly string SourceEntityType;

        /// <summary>
        /// The file of the source code target location
        /// </summary>
        [JsonProperty(PropertyName = "sourcePath", Required = Required.AllowNull)]
        public readonly string SourcePath;

        /// <summary>
        /// The line of the source code target location
        /// </summary>
        [JsonProperty(PropertyName = "sourceLine", Required = Required.AllowNull)]
        public readonly int SourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "sourceLinkName", Required = Required.AllowNull)]
        public readonly string SourceLinkName;

        /// <summary>
        /// The source code target entity
        /// </summary>
        [JsonProperty(PropertyName = "targetEntity", Required = Required.AllowNull)]
        public readonly string TargetEntity;

        /// <summary>
        /// The source code target entity type
        /// </summary>
        [JsonProperty(PropertyName = "targetEntityType", Required = Required.AllowNull)]
        public readonly string TargetEntityType;

        /// <summary>
        /// The file of the source code source location
        /// </summary>
        [JsonProperty(PropertyName = "targetPath", Required = Required.AllowNull)]
        public readonly string TargetPath;

        /// <summary>
        /// The line of the source code source location
        /// </summary>
        [JsonProperty(PropertyName = "targetLine", Required = Required.AllowNull)]
        public readonly int TargetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "targetLinkName", Required = Required.AllowNull)]
        public readonly string TargetLinkName;

        public ArchitectureViolationIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected ArchitectureViolationIssue(string architectureSource, string architectureSourceType,
                                             string architectureSourceLinkName, string architectureTarget,
                                             string architectureTargetType, string architectureTargetLinkName,
                                             string errorNumber, string violationType, string dependencyType,
                                             string sourceEntity, string sourceEntityType, string sourcePath,
                                             int sourceLine, string sourceLinkName, string targetEntity,
                                             string targetEntityType, string targetPath, int targetLine,
                                             string targetLinkName)
        {
            ArchitectureSource = architectureSource;
            ArchitectureSourceType = architectureSourceType;
            ArchitectureSourceLinkName = architectureSourceLinkName;
            ArchitectureTarget = architectureTarget;
            ArchitectureTargetType = architectureTargetType;
            ArchitectureTargetLinkName = architectureTargetLinkName;
            ErrorNumber = errorNumber;
            ViolationType = violationType;
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
            string explanation = await DashboardRetriever.Instance.GetIssueDescriptionAsync($"AV{ID}");
            return "<style=\"H2\">"
                   + $"{ViolationType} ({ArchitectureSource} to {ArchitectureTarget})".WrapLines(WrapAt / 2)
                   + $"</style>\nSource: {SourcePath} ({SourceEntityType}), Line {SourceLine}".WrapLines(WrapAt)
                   + $"\nTarget: {TargetPath} ({TargetEntityType}), Line {TargetLine}".WrapLines(WrapAt)
                   + $"\n{explanation.WrapLines(WrapAt)}";
        }

        public override string IssueKind => "AV";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.ArchitectureViolations;

        public override IEnumerable<SourceCodeEntity> Entities =>
            // Return source or target only if their path is not null
            new (string path, int line, string entity)[]
                {
                    (SourcePath, SourceLine, SourceEntity),
                    (TargetPath, TargetLine, TargetEntity)
                }.Where(x => x.path != null)
                 .Select(x => new SourceCodeEntity(x.path, x.line, null, x.entity));
    }
}
