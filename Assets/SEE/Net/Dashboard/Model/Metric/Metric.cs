using System;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly string name;

        /// <summary>
        /// A more descriptive name of the metric.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly string displayName;
        
        /// <summary>
        /// The configured minimum threshold for the metric.
        /// If not configured, this field will not be available.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly float? minValue; 
        
        /// <summary>
        /// The configured maximum threshold for the metric.
        /// If not configured, this field will not be available.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public readonly float? maxValue;

        public Metric(string name, string displayName, float? minValue, float? maxValue)
        {
            this.name = name;
            this.displayName = displayName;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }
}