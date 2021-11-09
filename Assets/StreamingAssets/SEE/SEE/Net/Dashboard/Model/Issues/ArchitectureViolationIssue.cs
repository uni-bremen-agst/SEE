using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
using Valve.Newtonsoft.Json;

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
        public readonly string architectureSource;

        /// <summary>
        /// The type of the architecture source entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string architectureSourceType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string architectureSourceLinkName;

        /// <summary>
        /// The architecture target entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string architectureTarget;

        /// <summary>
        /// The type of the architecture target entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string architectureTargetType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string architectureTargetLinkName;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string errorNumber;

        /// <summary>
        /// The type of the architecture violation
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string violationType;

        /// <summary>
        /// The type of the relation between source and target
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string dependencyType;

        /// <summary>
        /// The source code source entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string sourceEntity;

        /// <summary>
        /// The type of the source code source entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string sourceEntityType;

        /// <summary>
        /// The file of the source code target location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string sourcePath;

        /// <summary>
        /// The line of the source code target location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly int sourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string sourceLinkName;

        /// <summary>
        /// The source code target entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string targetEntity;

        /// <summary>
        /// The source code target entity type
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string targetEntityType;

        /// <summary>
        /// The file of the source code source location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string targetPath;

        /// <summary>
        /// The line of the source code source location
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly int targetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public readonly string targetLinkName;

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
            this.architectureSource = architectureSource;
            this.architectureSourceType = architectureSourceType;
            this.architectureSourceLinkName = architectureSourceLinkName;
            this.architectureTarget = architectureTarget;
            this.architectureTargetType = architectureTargetType;
            this.architectureTargetLinkName = architectureTargetLinkName;
            this.errorNumber = errorNumber;
            this.violationType = violationType;
            this.dependencyType = dependencyType;
            this.sourceEntity = sourceEntity;
            this.sourceEntityType = sourceEntityType;
            this.sourcePath = sourcePath;
            this.sourceLine = sourceLine;
            this.sourceLinkName = sourceLinkName;
            this.targetEntity = targetEntity;
            this.targetEntityType = targetEntityType;
            this.targetPath = targetPath;
            this.targetLine = targetLine;
            this.targetLinkName = targetLinkName;
        }

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescription($"AV{id}");
            return "<style=\"H2\">"
                   + $"{violationType} ({architectureSource} to {architectureTarget})".WrapLines(WRAP_AT / 2)
                   + $"</style>\nSource: {sourcePath} ({sourceEntityType}), Line {sourceLine}".WrapLines(WRAP_AT)
                   + $"\nTarget: {targetPath} ({targetEntityType}), Line {targetLine}".WrapLines(WRAP_AT)
                   + $"\n{explanation.WrapLines(WRAP_AT)}";
        }

        public override string IssueKind => "AV";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Architecture_Violations;

        public override IEnumerable<SourceCodeEntity> Entities =>
            // Return source or target only if their path is not null
            new (string path, int line, string entity)[]
                {
                    (sourcePath, sourceLine, sourceEntity),
                    (targetPath, targetLine, targetEntity)
                }.Where(x => x.path != null)
                 .Select(x => new SourceCodeEntity(x.path, x.line, null, x.entity));
    }
}