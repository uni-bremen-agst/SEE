using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing a dead entity.
    /// </summary>
    [Serializable]
    public class DeadEntityIssue: Issue
    {
        /// <summary>
        /// The dead entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entity;

        /// <summary>
        /// The type of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entityType;

        /// <summary>
        /// The filename of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string path;

        /// <summary>
        /// The line number of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int line;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string linkName;
        
        public DeadEntityIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected DeadEntityIssue(string entity, string entityType, string path, int line, string linkName)
        {
            this.entity = entity;
            this.entityType = entityType;
            this.path = path;
            this.line = line;
            this.linkName = linkName;
        }

        public override string ToString()
        {
            return $"{nameof(entity)}: {entity}, {nameof(entityType)}: {entityType}, {nameof(path)}: {path}, "
                   + $"{nameof(line)}: {line}, {nameof(linkName)}: {linkName}";
        }

        public override IssueKind kind => IssueKind.DE;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(path, line, null, entity)
        };
    }
}