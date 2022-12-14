using System.Collections.Generic;

namespace SEE.Game.HolisticMetrics
{
    /// <summary>
    /// Base class for both the <see cref="MetricValueRange"/> and the <see cref="MetricValueCollection"/>. Instances
    /// of this class will be passed to the widgets so they can display the values.
    /// </summary>
    internal abstract class MetricValue
    {
        /// <summary>
        /// The name of the metric.
        /// </summary>
        internal string Name;

        /// <summary>
        /// How many decimal places of the metric value(s) should be displayed.
        /// </summary>
        internal byte DecimalPlaces;
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
    
    /// <summary>
    /// A collection of metric values. This can, for example, be useful when displaying multiple metric values on a bar
    /// chart.
    /// </summary>
    internal class MetricValueCollection : MetricValue
    {
        /// <summary>
        /// All the metrics values that this collection contains.
        /// </summary>
        internal IList<MetricValueRange> MetricValues = new List<MetricValueRange>();
    }
}
