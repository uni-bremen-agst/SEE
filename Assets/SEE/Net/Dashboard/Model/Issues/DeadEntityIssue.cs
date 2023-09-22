using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
using Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string Entity;

        /// <summary>
        /// The type of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string EntityType;

        /// <summary>
        /// The filename of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string Path;

        /// <summary>
        /// The line number of the entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly int Line;

        /// <summary>
        /// The internal name of the corresponding entity
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string LinkName;

        public DeadEntityIssue()
        {
            // Necessary for generics shenanigans in IssueRetriever.
        }

        [JsonConstructor]
        protected DeadEntityIssue(string entity, string entityType, string path, int line, string linkName)
        {
            this.Entity = entity;
            this.EntityType = entityType;
            this.Path = path;
            this.Line = line;
            this.LinkName = linkName;
        }

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescription($"DE{ID}");
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