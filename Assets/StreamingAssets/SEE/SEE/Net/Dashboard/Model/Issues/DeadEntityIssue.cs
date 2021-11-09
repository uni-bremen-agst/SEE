using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Utils;
using Valve.Newtonsoft.Json;

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

        public override async UniTask<string> ToDisplayString()
        {
            string explanation = await DashboardRetriever.Instance.GetIssueDescription($"DE{id}");
            return "<style=\"H2\">Dead Entity</style>"
                   + $"\nThe entity '{entity.WrapLines(WRAP_AT)}' ({entityType.WrapLines(WRAP_AT)}) is dead."
                   + $"\nMay it rest in peace.\n{explanation.WrapLines(WRAP_AT)}";
        }

        public override string IssueKind => "DE";

        public override NumericAttributeNames AttributeName => NumericAttributeNames.Dead_Code;

        public override IEnumerable<SourceCodeEntity> Entities => new[]
        {
            new SourceCodeEntity(path, line, null, entity)
        };
    }
}