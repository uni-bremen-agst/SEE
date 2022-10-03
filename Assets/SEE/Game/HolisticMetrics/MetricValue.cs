using System.Collections.Generic;

namespace SEE.Game.HolisticMetrics
{
    internal class MetricValue
    {
        /// <summary>
        /// The name of the metric.
        /// </summary>
        internal string Name;
    }

    /// <summary>
    /// Represents one concrete value of a metric.
    /// </summary>
    internal class MetricValueRange : MetricValue
    {
        /// <summary>
        /// The worst possible value for this metric.
        /// </summary>
        internal float Lower;
        
        /// <summary>
        /// The best possible value for this metric.
        /// </summary>
        internal float Higher;
        
        /// <summary>
        /// The concrete value.
        /// </summary>
        internal float Value;
    }
    
    internal class MetricValueCollection : MetricValue
    {
        internal List<MetricValue> MetricValues;
    }
}
