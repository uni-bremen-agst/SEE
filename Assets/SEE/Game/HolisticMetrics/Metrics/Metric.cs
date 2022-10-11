using SEE.Game.City;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Metrics
{
    /// <summary>
    /// This could be any holistic metric (a metric that is calculated on the entire code city, not on individual
    /// nodes). If you want to implement a new metric, just implement this class. Then the new metric will automatically
    /// be available for adding to a board in the holistic metrics menu in the game.
    /// </summary>
    internal abstract class Metric : MonoBehaviour
    {
        /// <summary>
        /// If you want to implement a new metric, simply implement this method in the new class. This method will be
        /// called to retrieve the value you want to display, so just do whatever calculations you need to do and then
        /// return the value as a MetricValue. If you want to display a single value, use the class MetricValueRange,
        /// if you want to display multiple values (for example on a bar chart), use MetricValueCollection.
        /// </summary>
        /// <param name="city">The SEECity for which the metric should be calculated.</param>
        /// <returns>The calculated metric value</returns>
        internal abstract MetricValue Refresh(SEECity city);
    }
}
