using System;
using Newtonsoft.Json;

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
        public readonly string dependencyType;

        /// <summary>
        /// The source entity
        /// </summary>
        public readonly string sourceEntity;

        /// <summary>
        /// The source entity type
        /// </summary>
        public readonly string sourceEntityType;

        /// <summary>
        /// The source filename
        /// </summary>
        public readonly string sourcePath;

        /// <summary>
        /// The source line number
        /// </summary>
        public readonly uint sourceLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        public readonly string sourceLinkName;

        /// <summary>
        /// The target entity
        /// </summary>
        public readonly string targetEntity;

        /// <summary>
        /// The target entity type
        /// </summary>
        public readonly string targetEntityType;

        /// <summary>
        /// The target filename
        /// </summary>
        public readonly string targetPath;

        /// <summary>
        /// The target line number
        /// </summary>
        public readonly uint targetLine;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        public readonly string targetLinkName;

        public CycleIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected CycleIssue(string dependencyType, string sourceEntity, string sourceEntityType, 
                             string sourcePath, uint sourceLine, string sourceLinkName, string targetEntity, 
                             string targetEntityType, string targetPath, uint targetLine, string targetLinkName)
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
    }
}