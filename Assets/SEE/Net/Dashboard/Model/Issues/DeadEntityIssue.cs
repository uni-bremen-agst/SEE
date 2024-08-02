using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SEE.DataModel.DG;
using SEE.Utils;

namespace SEE.Net.Dashboard.Model.Issues
{
    /// <summary>
    /// An issue representing a dead entity.
    /// </summary>
    [Serializable]
    public class DeadEntityIssue : Issue
    {
        /// <summary>
        /// The dead entity
        /// </summary>
        [JsonProperty(PropertyName = "entity", Required = Required.Always)]
        public readonly string Entity;

        /// <summary>
        /// The type of the entity
        /// </summary>
        [JsonProperty(PropertyName = "entityType", Required = Required.Always)]
        public readonly string EntityType;

        /// <summary>
        /// The filename of the entity
        /// </summary>
        [JsonProperty(PropertyName = "path", Required = Required.Always)]
        public readonly string Path;

        /// <summary>
        /// The line number of the entity
        /// </summary>
        [JsonProperty(PropertyName = "line", Required = Required.Always)]
        public readonly int Line;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(PropertyName = "linkName", Required = Required.Always)]
        public readonly string LinkName;

        public DeadEntityIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected DeadEntityIssue(string entity, string entityType, string path, int line, string linkName)
        {
            Entity = entity;
            EntityType = entityType;
            Path = path;
            Line = line;
            LinkName = linkName;
        }

        public override async UniTask<string> ToDisplayStringAsync()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescriptionAsync($"DE{ID}");
            return "<style=\"H2\">Dead Entity</style>"
                   + $"\nThe entity '{Entity.WrapLines(WrapAt)}' ({EntityType.WrapLines(WrapAt)}) is dead."
                   + $"\nMay it rest in peace.\n{explanation.WrapLines(WrapAt)}";
        }

        public override string IssueKind => "DE";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.DeadCode;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(Path, Line, null, Entity)
        };
    }
}
