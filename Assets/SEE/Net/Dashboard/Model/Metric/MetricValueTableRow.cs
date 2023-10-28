using System;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// An entity table row.
    /// </summary>
    [Serializable]
    public class MetricValueTableRow
    {
        /// <summary>
        /// The Metric ID.
        /// </summary>
        [JsonProperty(PropertyName = "metric", Required = Required.Always)]
        public readonly string Metric;

        /// <summary>
        /// The source file of the entity definition if available.
        /// </summary>
        [JsonProperty(PropertyName = "path", Required = Required.Default)]
        public readonly string Path;

        /// <summary>
        /// The source file line number of the entity definition if available.
        /// </summary>
        [JsonProperty(PropertyName = "line", Required = Required.Default)]
        public readonly uint? Line;

        /// <summary>
        /// The measured or aggregated metric value.
        /// </summary>
        [JsonProperty(PropertyName = "value", Required = Required.Always)]
        public readonly float Value;

        /// <summary>
        /// The non-unique entity name.
        /// </summary>
        [JsonProperty(PropertyName = "entity", Required = Required.Always)]
        public readonly string Entity;

        /// <summary>
        /// The entity type.
        /// </summary>
        [JsonProperty(PropertyName = "entityType", Required = Required.Always)]
        public readonly string EntityType;

        /// <summary>
        /// The project-wide entity ID.
        /// </summary>
        [JsonProperty(PropertyName = "entityId", Required = Required.Always)]
        public readonly string EntityId;

        public MetricValueTableRow(string metric, string path, uint? line, float value, string entity, string entityType, string entityId)
        {
            this.Metric = metric;
            this.Path = path;
            this.Line = line;
            this.Value = value;
            this.Entity = entity;
            this.EntityType = entityType;
            this.EntityId = entityId;
        }
    }
}