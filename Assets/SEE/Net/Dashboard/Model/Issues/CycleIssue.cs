using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string dependencyType;

        /// <summary>
        /// The source entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string sourceEntity;

        /// <summary>
        /// The source entity type
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string sourceEntityType;

        /// <summary>
        /// The source filename
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string sourcePath;

        /// <summary>
        /// The source line number
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int sourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string sourceLinkName;

        /// <summary>
        /// The target entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string targetEntity;

        /// <summary>
        /// The target entity type
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string targetEntityType;

        /// <summary>
        /// The target filename
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string targetPath;

        /// <summary>
        /// The target line number
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int targetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string targetLinkName;

        public CycleIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected CycleIssue(string dependencyType, string sourceEntity, string sourceEntityType, 
                             string sourcePath, int sourceLine, string sourceLinkName, string targetEntity, 
                             string targetEntityType, string targetPath, int targetLine, string targetLinkName)
        {
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

        public override string ToString()
        {
            return $"{nameof(dependencyType)}: {dependencyType}, {nameof(sourceEntity)}: {sourceEntity},"
                   + $" {nameof(sourceEntityType)}: {sourceEntityType}, {nameof(sourcePath)}: {sourcePath},"
                   + $" {nameof(sourceLine)}: {sourceLine}, {nameof(sourceLinkName)}: {sourceLinkName},"
                   + $" {nameof(targetEntity)}: {targetEntity}, {nameof(targetEntityType)}: {targetEntityType},"
                   + $" {nameof(targetPath)}: {targetPath}, {nameof(targetLine)}: {targetLine},"
                   + $" {nameof(targetLinkName)}: {targetLinkName}";
        }

        public override IssueKind kind => IssueKind.CY;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(sourcePath, sourceLine, null, sourceEntity),
            new SourceCodeEntity(targetPath, targetLine, null, targetEntity)
        };
    }
}