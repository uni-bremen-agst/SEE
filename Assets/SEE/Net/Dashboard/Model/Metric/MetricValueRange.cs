using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        [JsonProperty(PropertyName = "startVersion", Required = Required.Always)]
        public readonly AnalysisVersion StartVersion;

        /// <summary>
        /// The end version of the metric value range.
        /// </summary>
        [JsonProperty(PropertyName = "endVersion", Required = Required.Always)]
        public readonly AnalysisVersion EndVersion;

        /// <summary>
        /// The ID of the entity.
        /// </summary>
        [JsonProperty(PropertyName = "entity", Required = Required.Always)]
        public readonly string Entity;

        /// <summary>
        /// The ID of the metric.
        /// </summary>
        [JsonProperty(PropertyName = "metric", Required = Required.Always)]
        public readonly string Metric;

        /// <summary>
        /// An array with the metric values.
        /// The array size is endVersion.index - startVersion.index + 1.
        /// Its values are numbers or null if no value is available.
        /// They correspond to the range defined by <see cref="StartVersion"/> and <see cref="EndVersion"/>.
        /// </summary>
        [JsonProperty(PropertyName = "values", Required = Required.Always)]
        public readonly IList<float?> Values;

        public MetricValueRange(AnalysisVersion startVersion, AnalysisVersion endVersion, string entity, string metric, IList<float?> values)
        {
            this.StartVersion = startVersion;
            this.EndVersion = endVersion;
            this.Entity = entity;
            this.Metric = metric;
            this.Values = values;
        }
    }
}