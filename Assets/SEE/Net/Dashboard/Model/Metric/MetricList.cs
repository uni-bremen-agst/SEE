using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

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
        [JsonProperty(Required = Required.Always)]
        public readonly AnalysisVersion version;

        /// <summary>
        /// List of queried metrics.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public readonly IList<Metric> metrics;

        public MetricList(AnalysisVersion version, IList<Metric> metrics)
        {
            this.version = version;
            this.metrics = metrics;
        }
    }
}