using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
using Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string ArchitectureSource;

        /// <summary>
        /// The type of the architecture source entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ArchitectureSourceType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ArchitectureSourceLinkName;

        /// <summary>
        /// The architecture target entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ArchitectureTarget;

        /// <summary>
        /// The type of the architecture target entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ArchitectureTargetType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ArchitectureTargetLinkName;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ErrorNumber;

        /// <summary>
        /// The type of the architecture violation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string ViolationType;

        /// <summary>
        /// The type of the relation between source and target
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string DependencyType;

        /// <summary>
        /// The source code source entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string SourceEntity;

        /// <summary>
        /// The type of the source code source entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string SourceEntityType;

        /// <summary>
        /// The file of the source code target location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string SourcePath;

        /// <summary>
        /// The line of the source code target location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly int SourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string SourceLinkName;

        /// <summary>
        /// The source code target entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string TargetEntity;

        /// <summary>
        /// The source code target entity type
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string TargetEntityType;

        /// <summary>
        /// The file of the source code source location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string TargetPath;

        /// <summary>
        /// The line of the source code source location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly int TargetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
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
            this.ArchitectureSource = architectureSource;
            this.ArchitectureSourceType = architectureSourceType;
            this.ArchitectureSourceLinkName = architectureSourceLinkName;
            this.ArchitectureTarget = architectureTarget;
            this.ArchitectureTargetType = architectureTargetType;
            this.ArchitectureTargetLinkName = architectureTargetLinkName;
            this.ErrorNumber = errorNumber;
            this.ViolationType = violationType;
            this.DependencyType = dependencyType;
            this.SourceEntity = sourceEntity;
            this.SourceEntityType = sourceEntityType;
            this.SourcePath = sourcePath;
            this.SourceLine = sourceLine;
            this.SourceLinkName = sourceLinkName;
            this.TargetEntity = targetEntity;
            this.TargetEntityType = targetEntityType;
            this.TargetPath = targetPath;
            this.TargetLine = targetLine;
            this.TargetLinkName = targetLinkName;
        }

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescription($"AV{ID}");
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