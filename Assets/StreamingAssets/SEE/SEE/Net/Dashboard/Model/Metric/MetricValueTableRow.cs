using System;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string metric;
        
        /// <summary>
        /// The source file of the entity definition if available.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly string path;
        
        /// <summary>
        /// The source file line number of the entity definition if available.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly uint? line;
        
        /// <summary>
        /// The measured or aggregated metric value.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly float value;
        
        /// <summary>
        /// The non-unique entity name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entity;
        
        /// <summary>
        /// The entity type.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entityType;
        
        /// <summary>
        /// The project-wide entity ID.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entityId;

        public MetricValueTableRow(string metric, string path, uint? line, float value, string entity, string entityType, string entityId)
        {
            this.metric = metric;
            this.path = path;
            this.line = line;
            this.value = value;
            this.entity = entity;
            this.entityType = entityType;
            this.entityId = entityId;
        }
    }
}