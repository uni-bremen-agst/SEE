using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// Describes a Metric as configured for a project in a version.
    /// </summary>
    [Serializable]
    public class MetricList
    {
        /// <summary>
        /// The version this metric list was queried with.
        /// </summary>
        [JsonProperty(PropertyName = "version", Required = Required.Always)]
        public readonly AnalysisVersion Version;

        /// <summary>
        /// List of queried metrics.
        /// </summary>
        [JsonProperty(PropertyName = "metrics", Required = Required.Always)]
        public readonly IList<Metric> Metrics;

        public MetricList(AnalysisVersion version, IList<Metric> metrics)
        {
            this.Version = version;
            this.Metrics = metrics;
        }
    }
}