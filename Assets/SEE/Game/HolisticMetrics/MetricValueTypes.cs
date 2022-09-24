// This file contains all the definition for special metric value types. If you need to, you can add any new value type
// here, but bear in mind that you will also need to add an implementation for this type in each widget that is
// supposed to be compatible with the new type.

using System.Collections.Generic;

namespace SEE.Game.HolisticMetrics
{
    public class MetricValue
    {
        /// <summary>
        /// The name of the metric.
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Represents one concrete value of a metric.
    /// </summary>
    public class MetricValueRange : MetricValue
    {
        /// <summary>
        /// The worst possible value for this metric.
        /// </summary>
        public float Lower;
        
        /// <summary>
        /// The best possible value for this metric.
        /// </summary>
        public float Higher;
        
        /// <summary>
        /// The concrete value.
        /// </summary>
        public float Value;
    }
    
    public class MetricValueCollection : MetricValue
    {
        public List<MetricValue> MetricValues;
    }
}