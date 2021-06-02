using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Default)]
        public readonly AnalysisVersion version;

        /// <summary>
        /// List of queried entities.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly IList<Entity> entities;

        public EntityList(AnalysisVersion version, IList<Entity> entities)
        {
            this.version = version;
            this.entities = entities;
        }
    }
}