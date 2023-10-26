using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// Contains a list of entities and the version of their versioned aspects.
    /// </summary>
    [Serializable]
    public class EntityList
    {
        /// <summary>
        /// The version this entity list was queried with.
        /// </summary>
        [JsonProperty(PropertyName = "version", Required = Required.Default)]
        public readonly AnalysisVersion Version;

        /// <summary>
        /// List of queried entities.
        /// </summary>
        [JsonProperty(PropertyName = "entities", Required = Required.Always)]
        public readonly IList<Entity> Entities;

        public EntityList(AnalysisVersion version, IList<Entity> entities)
        {
            this.Version = version;
            this.Entities = entities;
        }
    }
}