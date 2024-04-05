using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SEE.Net.Dashboard.Model.Metric
{
    /// <summary>
    /// The result of a metric value table query.
    /// </summary>
    [Serializable]
    public class MetricValueTable
    {
        // Note: ColumnInfo isn't included, as it requires a new type which isn't yet necessary for SEE.

        /// <summary>
        /// The entity data.
        /// </summary>
        [JsonProperty(PropertyName = "rows", Required = Required.Always)]
        public readonly IList<MetricValueTableRow> Rows;

        public MetricValueTable(IList<MetricValueTableRow> rows)
        {
            this.Rows = rows;
        }
    }
}