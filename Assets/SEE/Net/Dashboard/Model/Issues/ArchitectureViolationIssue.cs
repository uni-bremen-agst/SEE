using System;
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
        public readonly string architectureSource;

        /// <summary>
        /// The type of the architecture source entity
        /// </summary>
        public readonly string architectureSourceType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        public readonly string architectureSourceLinkName;

        /// <summary>
        /// The architecture target entity
        /// </summary>
        public readonly string architectureTarget;

        /// <summary>
        /// The type of the architecture target entity
        /// </summary>
        public readonly string architectureTargetType;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        public readonly string architectureTargetLinkName;

        /// <summary>
        /// The error number / error code / rule name
        /// </summary>
        public readonly string errorNumber;

        /// <summary>
        /// The type of the architecture violation
        /// </summary>
        public readonly string violationType;

        /// <summary>
        /// The type of the relation between source and target
        /// </summary>
        public readonly string dependencyType;

        /// <summary>
        /// The source code source entity
        /// </summary>
        public readonly string sourceEntity;

        /// <summary>
        /// The type of the source code source entity
        /// </summary>
        public readonly string sourceEntityType;

        /// <summary>
        /// The file of the source code target location
        /// </summary>
        public readonly string sourcePath;

        /// <summary>
        /// The line of the source code target location
        /// </summary>
        public readonly uint sourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        public readonly string sourceLinkName;

        /// <summary>
        /// The source code target entity
        /// </summary>
        public readonly string targetEntity;

        /// <summary>
        /// The source code target entity type
        /// </summary>
        public readonly string targetEntityType;

        /// <summary>
        /// The file of the source code source location
        /// </summary>
        public readonly string targetPath;

        /// <summary>
        /// The line of the source code source location
        /// </summary>
        public readonly uint targetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
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
                                             uint sourceLine, string sourceLinkName, string targetEntity,
                                             string targetEntityType, string targetPath, uint targetLine,
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

        public override string ToString()
        {
            return $"{nameof(architectureSource)}: {architectureSource}, "
                   + $"{nameof(architectureSourceType)}: {architectureSourceType},"
                   + $" {nameof(architectureSourceLinkName)}: {architectureSourceLinkName},"
                   + $" {nameof(architectureTarget)}: {architectureTarget},"
                   + $" {nameof(architectureTargetType)}: {architectureTargetType},"
                   + $" {nameof(architectureTargetLinkName)}: {architectureTargetLinkName},"
                   + $" {nameof(errorNumber)}: {errorNumber}, {nameof(violationType)}: {violationType},"
                   + $" {nameof(dependencyType)}: {dependencyType}, {nameof(sourceEntity)}: {sourceEntity},"
                   + $" {nameof(sourceEntityType)}: {sourceEntityType}, {nameof(sourcePath)}: {sourcePath},"
                   + $" {nameof(sourceLine)}: {sourceLine}, {nameof(sourceLinkName)}: {sourceLinkName},"
                   + $" {nameof(targetEntity)}: {targetEntity}, {nameof(targetEntityType)}: {targetEntityType},"
                   + $" {nameof(targetPath)}: {targetPath}, {nameof(targetLine)}: {targetLine},"
                   + $" {nameof(targetLinkName)}: {targetLinkName}";
        }
    }
}