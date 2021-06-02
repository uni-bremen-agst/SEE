using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// The result of a metric values query.
    /// </summary>
    [Serializable]
    public class MetricValueRange
    {
        /// <summary>
        /// The start version of the metric value range.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly AnalysisVersion startVersion;

        /// <summary>
        /// The end version of the metric value range.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly AnalysisVersion endVersion;

        /// <summary>
        /// The ID of the entity.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string entity;

        /// <summary>
        /// The ID of the metric.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string metric;

        /// <summary>
        /// An array with the metric values.
        /// The array size is <c>endVersion.index - startVersion.index + 1</c>.
        /// Its values are numbers or <c>null</c> if no value is available.
        /// They correspond to the range defined by <see cref="startVersion"/> and <see cref="endVersion"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly IList<float?> values;

        public MetricValueRange(AnalysisVersion startVersion, AnalysisVersion endVersion, string entity, string metric, IList<float?> values)
        {
            this.startVersion = startVersion;
            this.endVersion = endVersion;
            this.entity = entity;
            this.metric = metric;
            this.values = values;
        }
    }
}