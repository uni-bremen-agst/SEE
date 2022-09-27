// This file contains all the definition for special metric value types. If you need to, you can add any new value type
// here, but bear in mind that you will also need to add an implementation for this type in each widget that is
// supposed to be compatible with the new type.

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