using System;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// Describes a Metric as configured for a project in a version.
    /// </summary>
    [Serializable]
    public class Metric
    {
        /// <summary>
        /// The ID of the metric.
        /// </summary>
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public readonly string Name;

        /// <summary>
        /// A more descriptive name of the metric.
        /// </summary>
        [JsonProperty(PropertyName = "displayName", Required = Required.Always)]
        public readonly string DisplayName;

        /// <summary>
        /// The configured minimum threshold for the metric.
        /// If not configured, this field will not be available.
        /// </summary>
        [JsonProperty(PropertyName = "minValue", Required = Required.Default)]
        public readonly float? MinValue;

        /// <summary>
        /// The configured maximum threshold for the metric.
        /// If not configured, this field will not be available.
        /// </summary>
        [JsonProperty(PropertyName = "maxValue", Required = Required.Default)]
        public readonly float? MaxValue;

        public Metric(string name, string displayName, float? minValue, float? maxValue)
        {
            this.Name = name;
            this.DisplayName = displayName;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
        }
    }
}